using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchoolERP.Business.Identity.Auth;
using SchoolERP.Business.Identity.DTOs;
using SchoolERP.Business.Identity.Sessions;
using SchoolERP.DataAccess.Identity.Entities;
using SchoolERP.DataAccess.Identity.Repositories.Interfaces;
using SchoolERP.Business.Identity.Services.Interfaces;
using SchoolERP.Common;
using SchoolERP.Common.Events;

namespace SchoolERP.Business.Identity.Services;

/// <summary>
/// Owns every workflow rule around authentication: password verification, refresh-token
/// rotation, and account-lockout style checks. Controllers never touch a repository
/// directly -- that coordination lives here.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISessionService _sessions;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly IEventPublisher _events;
    private readonly StudentProfileResolver _studentProfiles;
    private readonly StaffProfileResolver _staffProfiles;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthService> _logger;
    private readonly int _maxFailedAttempts;
    private readonly TimeSpan _lockoutDuration;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        ISessionService sessions,
        ITokenGenerator tokenGenerator,
        IEventPublisher events,
        StudentProfileResolver studentProfiles,
        StaffProfileResolver staffProfiles,
        IOptions<JwtOptions> jwtOptions,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _sessions = sessions;
        _tokenGenerator = tokenGenerator;
        _events = events;
        _studentProfiles = studentProfiles;
        _staffProfiles = staffProfiles;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
        _maxFailedAttempts = configuration.GetValue("AccountLockout:MaxFailedAttempts", 5);
        _lockoutDuration = TimeSpan.FromMinutes(configuration.GetValue("AccountLockout:LockoutMinutes", 15));
    }

    // Resolves the linked student profile for self-service accounts: a Student's own record,
    // or for a Parent the child whose record points at this login (StudentProfile.ParentUserId).
    private async Task<StudentProfile?> ResolveStudentProfileAsync(User user, CancellationToken ct) =>
        user.Role switch
        {
            RoleNames.Student => await _studentProfiles.ResolveByUserAsync(user.Id, asParent: false, ct),
            RoleNames.Parent => await _studentProfiles.ResolveByUserAsync(user.Id, asParent: true, ct),
            _ => null
        };

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

        // Raised only after the account is saved; notification handlers run in the background.
        await _events.PublishAsync(new UserRegisteredEvent
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
            throw new UnauthorizedAccessException("This account has no password set. Contact the school administrator.");

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

    public async Task<AuthResult> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var tokenHash = _tokenGenerator.HashToken(request.RefreshToken);
        var storedToken = await _refreshTokens.FindByTokenHashAsync(tokenHash, ct)
            ?? throw new UnauthorizedAccessException("The refresh token is unrecognized.");

        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            var principal = _tokenGenerator.ValidateAccessTokenIgnoringExpiry(request.AccessToken)
                ?? throw new UnauthorizedAccessException("The access token is malformed.");

            // JwtSecurityTokenHandler maps the JWT `sub` claim to ClaimTypes.NameIdentifier on
            // the way in, so look under both names.
            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                ?? throw new UnauthorizedAccessException("The access token is missing its subject claim.");

            if (storedToken.UserId.ToString() != userIdClaim)
                throw new UnauthorizedAccessException("The refresh token does not match this access token.");
        }

        if (!storedToken.IsActive)
        {
            // A token that was already swapped for a newer one being used again means two
            // parties hold it -- possible theft, so sign the user out everywhere. Two
            // exceptions: a token swapped only moments ago is almost always a second browser
            // tab that refreshed at the same time (it retries with the new token), and a token
            // that was simply revoked (sign-out, password change, admin action) or expired is
            // just stale -- neither may sign the user out of the session they're using.
            var justSwapped = storedToken.RevokedAtUtc is { } swappedAt && DateTime.UtcNow - swappedAt < ConcurrentRefreshWindow;
            if (storedToken.ReplacedByTokenHash is not null && !justSwapped)
            {
                await _refreshTokens.RevokeAllForUserAsync(storedToken.UserId, ct);
                await _sessions.EndAllForUserAsync(storedToken.UserId, SessionEndReasons.TokenReuse, ct);
            }
            throw new UnauthorizedAccessException("The refresh token has expired or was already used.");
        }

        var user = storedToken.User ?? await _users.FindByIdAsync(storedToken.UserId, ct)
            ?? throw new UnauthorizedAccessException("The account no longer exists.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account has been deactivated.");

        // The session must still be alive: not signed out, not idle too long, not past its cap.
        // Tokens from before sessions existed get a fresh session instead of a forced sign-out.
        UserSession session;
        if (storedToken.SessionId is { } sessionId)
        {
            var (continued, state) = await _sessions.ContinueAsync(sessionId, user.Id, ct);
            if (continued is null)
            {
                await _refreshTokens.RevokeAsync(storedToken.Id, replacedByTokenHash: null, ct);
                throw new UnauthorizedAccessException(_sessions.Explain(state));
            }
            session = continued;
        }
        else
        {
            session = await _sessions.StartAsync(user.Id, DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays), ipAddress, ct);
        }

        // Rotate: revoke the presented token, issue a brand new pair.
        var newRawRefreshToken = _tokenGenerator.GenerateRefreshTokenRaw();
        var newHash = _tokenGenerator.HashToken(newRawRefreshToken);

        // Exactly one request may use up a refresh token. Checking "is it active?" and then
        // revoking it as two steps let two simultaneous requests both pass the check and both
        // get a fresh login -- a stolen token used at the same moment as the real one went
        // unnoticed. The conditional swap closes that: the loser is refused.
        if (!await _refreshTokens.TryRotateAsync(storedToken.Id, newHash, ct))
            throw new UnauthorizedAccessException("The refresh token has expired or was already used.");

        var profile = await ResolveStudentProfileAsync(user, ct);
        var staffProfile = await ResolveStaffProfileAsync(user, ct);
        var newAccessToken = _tokenGenerator.GenerateAccessToken(user, profile, staffProfile, session.Id);
        var newTokenEntity = new SchoolERP.DataAccess.Identity.Entities.RefreshToken
        {
            UserId = user.Id,
            SessionId = session.Id,
            TokenHash = newHash,
            // Never outlives the session's hard cap.
            ExpiresAtUtc = Min(DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays), session.ExpiresAtUtc),
            CreatedByIp = ipAddress
        };
        await _refreshTokens.AddAsync(newTokenEntity, ct);
        await _refreshTokens.SaveChangesAsync(ct);

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
            staffProfile?.ClassTeacherOfSectionId,
            _sessions.IdleTimeoutMinutes);
    }

    // Possession of the raw refresh token is the proof of ownership here -- no access token
    // is required, so logging out still revokes the session after a page reload has wiped
    // the in-memory access token.
    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = _tokenGenerator.HashToken(refreshToken);
        var stored = await _refreshTokens.FindByTokenHashAsync(tokenHash, ct);
        if (stored is not null && stored.RevokedAtUtc is null)
        {
            await _refreshTokens.RevokeAsync(stored.Id, replacedByTokenHash: null, ct);
        }
        if (stored?.SessionId is { } sessionId)
        {
            await _sessions.EndAsync(sessionId, SessionEndReasons.SignedOut, ct);
        }
    }

    public async Task<AuthResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId, ct)
            ?? throw new UnauthorizedAccessException("The account no longer exists.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("This account has been deactivated.");

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("The current password is incorrect.");

        if (request.CurrentPassword == request.NewPassword)
            throw new InvalidOperationException("The new password must be different from the current one.");

        await _users.SetPasswordHashAsync(user.Id, _passwordHasher.HashPassword(user, request.NewPassword), ct);

        // Sign out every other session, then hand this one a fresh token pair.
        await _refreshTokens.RevokeAllForUserAsync(user.Id, ct);
        await _sessions.EndAllForUserAsync(user.Id, SessionEndReasons.PasswordChanged, ct);
        _logger.LogInformation("AUDIT actor={UserId} action=User.ChangePassword entity=User entityId={UserId}", user.Id, user.Id);

        return await IssueTokensAsync(user, ipAddress, ct);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, string? ipAddress, CancellationToken ct)
    {
        // Every sign-in is its own session; its refresh tokens never outlive it.
        var session = await _sessions.StartAsync(user.Id, DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays), ipAddress, ct);

        var profile = await ResolveStudentProfileAsync(user, ct);
        var staffProfile = await ResolveStaffProfileAsync(user, ct);
        var accessToken = _tokenGenerator.GenerateAccessToken(user, profile, staffProfile, session.Id);
        var rawRefreshToken = _tokenGenerator.GenerateRefreshTokenRaw();
        var tokenHash = _tokenGenerator.HashToken(rawRefreshToken);

        var refreshTokenEntity = new SchoolERP.DataAccess.Identity.Entities.RefreshToken
        {
            UserId = user.Id,
            SessionId = session.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedByIp = ipAddress
        };

        await _refreshTokens.AddAsync(refreshTokenEntity, ct);
        await _refreshTokens.SaveChangesAsync(ct);

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
            staffProfile?.ClassTeacherOfSectionId,
            _sessions.IdleTimeoutMinutes);
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

    // How long after a token is swapped a second use still counts as a concurrent refresh
    // from another tab rather than as theft.
    private static readonly TimeSpan ConcurrentRefreshWindow = TimeSpan.FromSeconds(30);
}
