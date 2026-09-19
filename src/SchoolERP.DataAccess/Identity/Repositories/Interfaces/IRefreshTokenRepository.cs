using SchoolERP.DataAccess.Identity.Entities;

namespace SchoolERP.DataAccess.Identity.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<IReadOnlyList<RefreshToken>> FindActiveByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>Atomic revoke via ExecuteUpdateAsync -- avoids loading the full entity to flip one flag.</summary>
    Task<int> RevokeAsync(Guid tokenId, string? replacedByTokenHash, CancellationToken ct = default);

    /// <summary>
    /// Swaps an active token for its replacement in one conditional UPDATE. Returns false if
    /// the token was already revoked or swapped -- e.g. two requests presenting it at the same
    /// moment: exactly one of them wins.
    /// </summary>
    Task<bool> TryRotateAsync(Guid tokenId, string replacedByTokenHash, CancellationToken ct = default);

    /// <summary>Revokes every active token for a user (used on password change / suspicious activity).</summary>
    Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Deletes tokens that expired or were revoked before the cut-off (retention clean-up).</summary>
    Task<int> DeleteFinishedBeforeAsync(DateTime cutoffUtc, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
