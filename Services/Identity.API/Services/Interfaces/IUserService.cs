using SchoolERP.Shared.Common;
using SchoolERP.Identity.DTOs;

namespace SchoolERP.Identity.Services.Interfaces;

public interface IUserService
{
    Task<UserSummary?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<UserSummary>> SearchAsync(string? role, string? keyword, int page, int pageSize, CancellationToken ct = default);
    Task SetActiveStatusAsync(Guid userId, bool isActive, string actorUserId, string actorRole, CancellationToken ct = default);

    /// <summary>Admin-initiated reset -- overwrites the password hash directly (no knowledge
    /// of the old password required) and revokes every existing session for the account.</summary>
    Task SetPasswordAsync(Guid userId, string newPassword, string actorUserId, string actorRole, CancellationToken ct = default);
}
