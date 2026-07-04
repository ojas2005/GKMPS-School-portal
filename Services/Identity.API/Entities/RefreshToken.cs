using SchoolERP.Shared.Entities;

namespace SchoolERP.Identity.Entities;

/// <summary>
/// Rotated refresh token. Short-lived JWT access tokens are re-issued from a valid,
/// unexpired, unrevoked refresh token; the previous token is revoked and replaced
/// (rotation) on every refresh call to limit replay-attack windows. Also mirrored
/// into Redis (IDistributedCache) as the fast revocation-check path used by the
/// YARP gateway.
/// </summary>
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public required string TokenHash { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
