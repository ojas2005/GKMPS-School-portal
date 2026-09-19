using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Identity.Entities;

/// <summary>
/// Rotated refresh token. Short-lived JWT access tokens are re-issued from a valid,
/// unexpired, unrevoked refresh token; the previous token is revoked and replaced
/// (rotation) on every refresh call to limit replay-attack windows. Stored hashed.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>The sign-in this token belongs to. Null only for tokens issued before sessions existed.</summary>
    public Guid? SessionId { get; set; }

    public required string TokenHash { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
