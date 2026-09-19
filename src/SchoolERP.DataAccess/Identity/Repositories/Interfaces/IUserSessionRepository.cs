using SchoolERP.DataAccess.Identity.Entities;

namespace SchoolERP.DataAccess.Identity.Repositories.Interfaces;

public interface IUserSessionRepository
{
    Task<UserSession?> FindByIdAsync(Guid sessionId, CancellationToken ct = default);

    Task AddAsync(UserSession session, CancellationToken ct = default);

    /// <summary>Moves LastActivityUtc forward (never backward) with a single UPDATE.</summary>
    Task<int> TouchAsync(Guid sessionId, DateTime activityUtc, CancellationToken ct = default);

    Task<int> EndAsync(Guid sessionId, string reason, CancellationToken ct = default);

    /// <summary>Ends every open session of a user (password change/reset, deactivation, token theft).</summary>
    Task<int> EndAllForUserAsync(Guid userId, string reason, CancellationToken ct = default);

    /// <summary>Deletes sessions that ended or expired before the cut-off, with their tokens (retention clean-up).</summary>
    Task<int> DeleteFinishedBeforeAsync(DateTime cutoffUtc, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
