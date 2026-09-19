using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Identity.Entities;

/// <summary>
/// One signed-in device or browser. Created at sign-in; every access token carries its id
/// ("sid") and every refresh token belongs to it. It stays alive while the user is active
/// (LastActivityUtc keeps moving) and ends after a stretch of inactivity, on sign-out, or
/// when an admin or a password change signs the user out everywhere.
/// </summary>
public class UserSession : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Hard cap: even a constantly active session must sign in again after this.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? EndedAtUtc { get; set; }

    /// <summary>Why it ended: see <see cref="SessionEndReasons"/>.</summary>
    public string? EndReason { get; set; }

    public string? CreatedByIp { get; set; }
}

public static class SessionEndReasons
{
    public const string SignedOut = "signed-out";
    public const string Idle = "idle";
    public const string Expired = "expired";
    public const string PasswordChanged = "password-changed";
    public const string PasswordReset = "password-reset";
    public const string Deactivated = "deactivated";
    public const string TokenReuse = "token-reuse";
}
