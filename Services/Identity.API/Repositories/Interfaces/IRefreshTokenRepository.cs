using SchoolERP.Identity.Entities;

namespace SchoolERP.Identity.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<IReadOnlyList<RefreshToken>> FindActiveByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>Atomic revoke via ExecuteUpdateAsync -- avoids loading the full entity to flip one flag.</summary>
    Task<int> RevokeAsync(Guid tokenId, string? replacedByTokenHash, CancellationToken ct = default);

    /// <summary>Revokes every active token for a user (used on password change / suspicious activity).</summary>
    Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
