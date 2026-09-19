using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Identity;
using SchoolERP.DataAccess.Identity.Entities;
using SchoolERP.DataAccess.Identity.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Identity.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IdentityDbContext _db;

    public RefreshTokenRepository(IdentityDbContext db) => _db = db;

    public Task<RefreshToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.RefreshTokens.Include(rt => rt.User).FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

    public Task<IReadOnlyList<RefreshToken>> FindActiveByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null && rt.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<RefreshToken>)t.Result, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default) =>
        await _db.RefreshTokens.AddAsync(token, ct);

    public Task<int> RevokeAsync(Guid tokenId, string? replacedByTokenHash, CancellationToken ct = default) =>
        _db.RefreshTokens
            .Where(rt => rt.Id == tokenId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAtUtc, DateTime.UtcNow)
                .SetProperty(rt => rt.ReplacedByTokenHash, replacedByTokenHash), ct);

    public async Task<bool> TryRotateAsync(Guid tokenId, string replacedByTokenHash, CancellationToken ct = default) =>
        await _db.RefreshTokens
            .Where(rt => rt.Id == tokenId && rt.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAtUtc, DateTime.UtcNow)
                .SetProperty(rt => rt.ReplacedByTokenHash, replacedByTokenHash), ct) == 1;

    public Task<int> RevokeAllForUserAsync(Guid userId, CancellationToken ct = default) =>
        _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(rt => rt.RevokedAtUtc, DateTime.UtcNow), ct);

    public Task<int> DeleteFinishedBeforeAsync(DateTime cutoffUtc, CancellationToken ct = default) =>
        _db.RefreshTokens
            .Where(rt => rt.ExpiresAtUtc < cutoffUtc || (rt.RevokedAtUtc != null && rt.RevokedAtUtc < cutoffUtc))
            .ExecuteDeleteAsync(ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
