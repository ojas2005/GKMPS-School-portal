using SchoolERP.Shared.Common;
using SchoolERP.Identity.DTOs;

namespace SchoolERP.Identity.Services.Interfaces;

public interface IUserService
{
    Task<UserSummary?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<UserSummary>> SearchAsync(string? role, string? keyword, int page, int pageSize, CancellationToken ct = default);
    Task SetActiveStatusAsync(Guid userId, bool isActive, string actorUserId, string actorRole, CancellationToken ct = default);
}
