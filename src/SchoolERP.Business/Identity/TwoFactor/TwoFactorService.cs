using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SchoolERP.Common.Audit;
using SchoolERP.DataAccess.Identity.Entities;
using SchoolERP.DataAccess.Identity.Repositories.Interfaces;

namespace SchoolERP.Business.Identity.TwoFactor;

public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>Roles that must use two-step sign-in. They're walked through setting it up at their next sign-in.</summary>
    public string[] RequireTwoFactorForRoles { get; set; } = ["SuperAdmin"];
}

public record TwoFactorStatus(bool Enabled, bool Required, int RecoveryCodesLeft);
public record TwoFactorSetup(string SecretKey, string SetupUri);

public interface ITwoFactorService
{
    bool IsRequiredFor(string role);
    Task<TwoFactorStatus> GetStatusAsync(Guid userId, CancellationToken ct = default);
    Task<TwoFactorSetup> BeginSetupAsync(Guid userId, CancellationToken ct = default);
    /// <summary>Turns it on once the user proves their app works. Returns the one-time recovery codes.</summary>
    Task<IReadOnlyList<string>> EnableAsync(Guid userId, string code, CancellationToken ct = default);
    Task DisableAsync(Guid userId, string password, string code, CancellationToken ct = default);
    /// <summary>Checks a sign-in code: an authenticator code (each works once) or an unused recovery code.</summary>
    Task<bool> VerifyAsync(User user, string code, CancellationToken ct = default);
    /// <summary>For an admin when someone loses their phone: clears it so they set it up again.</summary>
    Task ResetAsync(Guid userId, CancellationToken ct = default);
}

public class TwoFactorService : ITwoFactorService
{
    private const int RecoveryCodeCount = 10;

    private readonly IUserRepository _users;
    private readonly TwoFactorProtector _protector;
    private readonly IAuditTrail _audit;
    private readonly TimeProvider _clock;
    private readonly SecurityOptions _options;
    private readonly string _issuer;
    private readonly PasswordHasher<User> _passwords = new();

    public TwoFactorService(IUserRepository users, TwoFactorProtector protector, IAuditTrail audit, TimeProvider clock,
        IOptions<SecurityOptions> options, IConfiguration configuration)
    {
        _users = users;
        _protector = protector;
        _audit = audit;
        _clock = clock;
        _options = options.Value;
        _issuer = configuration["School:Name"] ?? "School";
    }

    public bool IsRequiredFor(string role) => _options.RequireTwoFactorForRoles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public async Task<TwoFactorStatus> GetStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await Load(userId, ct);
        return new TwoFactorStatus(user.TwoFactorEnabled, IsRequiredFor(user.Role), CountCodes(user.TwoFactorRecoveryCodes));
    }

    public async Task<TwoFactorSetup> BeginSetupAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await Load(userId, ct);
        if (user.TwoFactorEnabled)
            throw new InvalidOperationException("Two-step sign-in is already on. Turn it off first to move it to a new phone.");

        var secret = Totp.NewSecret();
        await _users.SetTwoFactorAsync(user.Id, _protector.Protect(secret), enabled: false, recoveryCodes: null, ct);
        var key = Base32.Encode(secret);
        return new TwoFactorSetup(string.Join(' ', Enumerable.Range(0, (key.Length + 3) / 4).Select(i => key.Substring(i * 4, Math.Min(4, key.Length - i * 4)))),
            Totp.SetupUri(secret, user.Username ?? user.Email, _issuer));
    }

    public async Task<IReadOnlyList<string>> EnableAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var user = await Load(userId, ct);
        if (user.TwoFactorEnabled) throw new InvalidOperationException("Two-step sign-in is already on.");
        if (user.TwoFactorSecret is null) throw new InvalidOperationException("Start the setup first.");

        var step = Totp.Match(_protector.Unprotect(user.TwoFactorSecret), code, _clock.GetUtcNow());
        if (step is null) throw new InvalidOperationException("That code isn't right. Check your phone's time is correct and try the newest code.");

        var codes = Enumerable.Range(0, RecoveryCodeCount).Select(_ => NewRecoveryCode()).ToList();
        await _users.SetTwoFactorAsync(user.Id, user.TwoFactorSecret, enabled: true, string.Join(';', codes.Select(Hash)), ct);
        await _users.TryAdvanceTwoFactorStepAsync(user.Id, step.Value, ct);
        Audit("auth.two-factor.enabled", user);
        return codes;
    }

    public async Task DisableAsync(Guid userId, string password, string code, CancellationToken ct = default)
    {
        var user = await Load(userId, ct);
        if (!user.TwoFactorEnabled) return;
        if (IsRequiredFor(user.Role))
            throw new InvalidOperationException("Two-step sign-in is required for your role. Set it up on a new phone instead of turning it off.");
        if (string.IsNullOrEmpty(user.PasswordHash) || _passwords.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Your password isn't right.");
        if (!await VerifyAsync(user, code, ct))
            throw new InvalidOperationException("That code isn't right.");

        await _users.SetTwoFactorAsync(user.Id, null, enabled: false, null, ct);
        Audit("auth.two-factor.disabled", user);
    }

    public async Task<bool> VerifyAsync(User user, string code, CancellationToken ct = default)
    {
        if (!user.TwoFactorEnabled || user.TwoFactorSecret is null) return false;

        var step = Totp.Match(_protector.Unprotect(user.TwoFactorSecret), code, _clock.GetUtcNow());
        if (step is not null)
            return await _users.TryAdvanceTwoFactorStepAsync(user.Id, step.Value, ct); // false: code already used

        // Not an app code -- maybe one of the printed recovery codes (each works once).
        var stored = user.TwoFactorRecoveryCodes;
        if (string.IsNullOrEmpty(stored)) return false;
        var hashes = stored.Split(';').ToList();
        var given = Hash(NormaliseRecovery(code));
        var match = hashes.FirstOrDefault(h => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(h), Encoding.ASCII.GetBytes(given)));
        if (match is null) return false;
        hashes.Remove(match);
        if (!await _users.TryReplaceRecoveryCodesAsync(user.Id, stored, hashes.Count == 0 ? null : string.Join(';', hashes), ct))
            return false;
        Audit("auth.two-factor.recovery-code-used", user, $"left={hashes.Count}");
        return true;
    }

    public async Task ResetAsync(Guid userId, CancellationToken ct = default) =>
        await _users.SetTwoFactorAsync(userId, null, enabled: false, null, ct);

    private async Task<User> Load(Guid userId, CancellationToken ct) =>
        await _users.FindByIdAsync(userId, ct) ?? throw new KeyNotFoundException("User not found.");

    private void Audit(string action, User user, string? detail = null) =>
        _audit.Record(new AuditRecord(DateTime.UtcNow, action, user.Id, user.Role, user.Id.ToString(), Detail: detail));

    private static int CountCodes(string? stored) => string.IsNullOrEmpty(stored) ? 0 : stored.Split(';').Length;

    // Ten characters from an alphabet without look-alikes (no 0/O, 1/I/L), shown as xxxxx-xxxxx.
    private static string NewRecoveryCode()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        var chars = RandomNumberGenerator.GetItems<char>(alphabet, 10);
        return $"{new string(chars, 0, 5)}-{new string(chars, 5, 5)}";
    }

    private static string NormaliseRecovery(string code)
    {
        var raw = new string(code.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
        return raw.Length == 10 ? $"{raw[..5]}-{raw[5..]}" : raw;
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
