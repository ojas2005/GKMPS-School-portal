using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Identity.Entities;
using SchoolERP.DataAccess.Identity.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Identity.Repositories;

public class UserSessionRepository : IUserSessionRepository
{
    private readonly IdentityDbContext _db;

    public UserSessionRepository(IdentityDbContext db) => _db = db;

    public Task<UserSession?> FindByIdAsync(Guid sessionId, CancellationToken ct = default) =>
        _db.UserSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, ct);

    public async Task AddAsync(UserSession session, CancellationToken ct = default) =>
        await _db.UserSessions.AddAsync(session, ct);

    public Task<int> TouchAsync(Guid sessionId, DateTime activityUtc, CancellationToken ct = default) =>
        _db.UserSessions
            .Where(s => s.Id == sessionId && s.EndedAtUtc == null && s.LastActivityUtc < activityUtc)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.LastActivityUtc, activityUtc), ct);

    public Task<int> EndAsync(Guid sessionId, string reason, CancellationToken ct = default) =>
        _db.UserSessions
            .Where(s => s.Id == sessionId && s.EndedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.EndedAtUtc, DateTime.UtcNow)
                .SetProperty(s => s.EndReason, reason), ct);

    public Task<int> EndAllForUserAsync(Guid userId, string reason, CancellationToken ct = default) =>
        _db.UserSessions
            .Where(s => s.UserId == userId && s.EndedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.EndedAtUtc, DateTime.UtcNow)
                .SetProperty(s => s.EndReason, reason), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
