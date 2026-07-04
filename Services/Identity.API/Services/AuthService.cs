using Google.Apis.Auth;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchoolERP.Identity.Auth;
using SchoolERP.Identity.DTOs;
using SchoolERP.Identity.Entities;
using SchoolERP.Identity.Repositories.Interfaces;
using SchoolERP.Identity.Services.Interfaces;
using SchoolERP.Shared.Common;
using SchoolERP.Shared.Events;

namespace SchoolERP.Identity.Services;

/// <summary>
/// Owns every workflow rule around authentication: password verification, refresh-token
/// rotation, Google OAuth account linking, and account-lockout style checks. Controllers
/// never touch a repository directly -- that coordination lives here.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly IDistributedCache _cache;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly StudentProfileResolver _studentProfiles;
    private readonly StaffProfileResolver _staffProfiles;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthService> _logger;
    private readonly string? _googleClientId;
    private readonly int _maxFailedAttempts;
    private readonly TimeSpan _lockoutDuration;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        ITokenGenerator tokenGenerator,
        IDistributedCache cache,
        IPublishEndpoint publishEndpoint,
        StudentProfileResolver studentProfiles,
        StaffProfileResolver staffProfiles,
        IOptions<JwtOptions> jwtOptions,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _tokenGenerator = tokenGenerator;
        _cache = cache;
        _publishEndpoint = publishEndpoint;
        _studentProfiles = studentProfiles;
        _staffProfiles = staffProfiles;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
        _googleClientId = configuration["Authentication:Google:ClientId"];
        _maxFailedAttempts = configuration.GetValue("AccountLockout:MaxFailedAttempts", 5);
        _lockoutDuration = TimeSpan.FromMinutes(configuration.GetValue("AccountLockout:LockoutMinutes", 15));
    }

    // Resolves the linked student profile for self-service accounts (Student role).
    private async Task<StudentProfile?> ResolveStudentProfileAsync(User user, CancellationToken ct) =>
        user.Role == RoleNames.Student
            ? await _studentProfiles.ResolveByUserAsync(user.Id, ct)
            : null;

    // Resolves the linked staff profile for staff-side accounts so the token carries
    // staffId + class-teacher scoping claims.
    private async Task<StaffClaimsProfile?> ResolveStaffProfileAsync(User user, CancellationToken ct) =>
        user.Role is RoleNames.Teacher or RoleNames.Principal or RoleNames.Accountant or RoleNames.Librarian or RoleNames.Admin
            ? await _staffProfiles.ResolveByUserAsync(user.Id, ct)
            : null;

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        // Duplicate-prevention guard before insert, mirroring HasStudentReviewed()-style checks.
        if (await _users.ExistsByEmailAsync(request.Email, ct))
            throw new InvalidOperationException("An account with this email already exists.");

        if (!RoleNames.All.Contains(request.Role))
            throw new InvalidOperationException($"'{request.Role}' is not a recognized role.");

        var username = string.IsNullOrWhiteSpace(request.Username) ? null : request.Username.Trim();
        if (username is not null && await _users.ExistsByUsernameAsync(username, ct))
            throw new InvalidOperationException($"The login ID '{username}' is already taken.");

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            Username = username,
            FullName = request.FullName,
            Role = request.Role,
            IsEmailVerified = false,
            IsActive = true
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        }, ct);

        _logger.LogInformation("New account registered: {UserId} ({Role})", user.Id, user.Role);

        return await IssueTokensAsync(user, ipAddress: null, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _users.FindByLoginAsync(request.LoginId, ct)
            ?? throw new UnauthorizedAccessException("Invalid login ID or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account has been deactivated.");

        // Checked before the password itself so a locked-out account never leaks whether
        // the attempted password was actually correct.
        if (user.LockoutEndUtc is { } lockoutEnd && lockoutEnd > DateTime.UtcNow)
            throw new UnauthorizedAccessException(
                $"Too many failed attempts. Try again after {lockoutEnd:HH:mm} UTC.");

        if (string.IsNullOrEmpty(user.PasswordHash))
            throw new UnauthorizedAccessException("This account signs in via Google only.");

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            // IncrementFailedLoginAttemptsAsync is a raw SQL increment (ExecuteUpdateAsync) --
            // it bypasses the change tracker, so re-querying by Id afterwards on this same
            // DbContext would just hand back the already-tracked `user` instance untouched
            // (EF's identity map doesn't refresh tracked entities from a fresh row). Compute
            // the post-increment count from the value already in hand instead of reloading.
            var attempts = user.FailedLoginAttempts + 1;
            await _users.IncrementFailedLoginAttemptsAsync(user.Id, ct);

            if (attempts >= _maxFailedAttempts)
            {
                var lockoutEndUtc = DateTime.UtcNow.Add(_lockoutDuration);
                await _users.SetLockoutAsync(user.Id, lockoutEndUtc, ct);
                _logger.LogWarning(
                    "AUDIT action=User.LockedOut entity=User entityId={UserId} attempts={Attempts} lockoutEndUtc={LockoutEndUtc}",
                    user.Id, attempts, lockoutEndUtc);
                throw new UnauthorizedAccessException(
                    $"Too many failed attempts. Account locked until {lockoutEndUtc:HH:mm} UTC.");
            }

            throw new UnauthorizedAccessException("Invalid login ID or password.");
        }

        await _users.ResetFailedLoginAsync(user.Id, ct);
        await _users.UpdateLastLoginAsync(user.Id, DateTime.UtcNow, ct);

        return await IssueTokensAsync(user, ipAddress, ct);
    }

    public async Task<AuthResult> LoginWithGoogleAsync(GoogleLoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = string.IsNullOrEmpty(_googleClientId) ? null : new[] { _googleClientId }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google ID token validation failed");
            throw new UnauthorizedAccessException("Invalid Google credential.");
        }

        var user = await _users.FindByGoogleSubjectIdAsync(payload.Subject, ct)
                   ?? await _users.FindByEmailAsync(payload.Email, ct);

        if (user is null)
        {
            user = new User
            {
                Email = payload.Email.ToLowerInvariant(),
                FullName = payload.Name ?? payload.Email,
                Role = RoleNames.Student, // default; an Admin can elevate the role afterwards
                IsEmailVerified = payload.EmailVerified,
                GoogleSubjectId = payload.Subject
            };
            await _users.AddAsync(user, ct);
            await _users.SaveChangesAsync(ct);

            await _publishEndpoint.Publish(new UserRegisteredEvent
            {
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role
            }, ct);
        }
        else if (user.GoogleSubjectId is null)
        {
            // Link the existing password-based account to this Google identity.
            user.GoogleSubjectId = payload.Subject;
            await _users.SaveChangesAsync(ct);
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account has been deactivated.");

        await _users.UpdateLastLoginAsync(user.Id, DateTime.UtcNow, ct);

        return await IssueTokensAsync(user, ipAddress, ct);
    }

    public async Task<AuthResult> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var principal = _tokenGenerator.ValidateAccessTokenIgnoringExpiry(request.AccessToken)
            ?? throw new UnauthorizedAccessException("The access token is malformed.");

        var userIdClaim = principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
            ?? throw new UnauthorizedAccessException("The access token is missing its subject claim.");

        var tokenHash = _tokenGenerator.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokens.FindByTokenHashAsync(tokenHash, ct)
            ?? throw new UnauthorizedAccessException("The refresh token is unrecognized.");

        if (storedToken.UserId.ToString() != userIdClaim)
            throw new UnauthorizedAccessException("The refresh token does not match this access token.");

        if (!storedToken.IsActive)
        {
            // Reuse of a revoked/expired token is a signal of possible theft: revoke the whole family.
            await _refreshTokens.RevokeAllForUserAsync(storedToken.UserId, ct);
            throw new UnauthorizedAccessException("The refresh token has expired or was already used.");
        }

        var user = storedToken.User ?? await _users.FindByIdAsync(storedToken.UserId, ct)
            ?? throw new UnauthorizedAccessException("The account no longer exists.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account has been deactivated.");

        // Rotate: revoke the presented token, issue a brand new pair.
        var newRawRefreshToken = _tokenGenerator.GenerateRefreshTokenRaw();
        var newHash = _tokenGenerator.HashToken(newRawRefreshToken);

        await _refreshTokens.RevokeAsync(storedToken.Id, newHash, ct);

        var profile = await ResolveStudentProfileAsync(user, ct);
        var staffProfile = await ResolveStaffProfileAsync(user, ct);
        var newAccessToken = _tokenGenerator.GenerateAccessToken(user, profile, staffProfile);
        var newTokenEntity = new Entities.RefreshToken
        {
            UserId = user.Id,
            TokenHash = newHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        };
        await _refreshTokens.AddAsync(newTokenEntity, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        await CacheActiveRefreshTokenAsync(user.Id, newHash, ct);

        return new AuthResult(
            newAccessToken,
            newRawRefreshToken,
            DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            profile?.StudentId,
            profile?.ClassId,
            profile?.SectionId,
            user.Username,
            staffProfile?.StaffId,
            staffProfile?.ClassTeacherOfClassId,
            staffProfile?.ClassTeacherOfSectionId);
    }

    public async Task LogoutAsync(Guid userId, string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = _tokenGenerator.HashToken(refreshToken);
        var stored = await _refreshTokens.FindByTokenHashAsync(tokenHash, ct);
        if (stored is not null && stored.UserId == userId)
        {
            await _refreshTokens.RevokeAsync(stored.Id, replacedByTokenHash: null, ct);
            await _cache.RemoveAsync($"refresh-token:{userId}:{tokenHash}", ct);
        }
    }

    private async Task<AuthResult> IssueTokensAsync(User user, string? ipAddress, CancellationToken ct)
    {
        var profile = await ResolveStudentProfileAsync(user, ct);
        var staffProfile = await ResolveStaffProfileAsync(user, ct);
        var accessToken = _tokenGenerator.GenerateAccessToken(user, profile, staffProfile);
        var rawRefreshToken = _tokenGenerator.GenerateRefreshTokenRaw();
        var tokenHash = _tokenGenerator.HashToken(rawRefreshToken);

        var refreshTokenEntity = new Entities.RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        await _refreshTokens.AddAsync(refreshTokenEntity, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        await CacheActiveRefreshTokenAsync(user.Id, tokenHash, ct);

        return new AuthResult(
            accessToken,
            rawRefreshToken,
            DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            profile?.StudentId,
            profile?.ClassId,
            profile?.SectionId,
            user.Username,
            staffProfile?.StaffId,
            staffProfile?.ClassTeacherOfClassId,
            staffProfile?.ClassTeacherOfSectionId);
    }

    /// <summary>
    /// Mirrors the active refresh token into Redis so the YARP gateway can do a fast
    /// revocation check without round-tripping to Postgres on every request.
    /// </summary>
    private Task CacheActiveRefreshTokenAsync(Guid userId, string tokenHash, CancellationToken ct) =>
        _cache.SetStringAsync(
            $"refresh-token:{userId}:{tokenHash}",
            "active",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(_jwtOptions.RefreshTokenDays) },
            ct);
}
