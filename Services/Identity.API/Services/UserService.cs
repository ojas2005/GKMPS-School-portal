using System.Text.Json;
using Microsoft.Extensions.Logging;
using SchoolERP.Identity.DTOs;
using SchoolERP.Identity.Entities;
using SchoolERP.Identity.Repositories.Interfaces;
using SchoolERP.Identity.Services.Interfaces;
using SchoolERP.Shared.Common;

namespace SchoolERP.Identity.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository users, ILogger<UserService> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task<UserSummary?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(id, ct);
        return user is null ? null : ToSummary(user);
    }

    public async Task<PagedResult<UserSummary>> SearchAsync(string? role, string? keyword, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        var users = await _users.SearchUsersAsync(role, keyword, page, pageSize, ct);
        var total = await _users.CountUsersAsync(role, ct);

        return new PagedResult<UserSummary>
        {
            Items = users.Select(ToSummary).ToList(),
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task SetActiveStatusAsync(Guid userId, bool isActive, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var before = await _users.FindByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        await _users.SetActiveStatusAsync(userId, isActive, ct);

        // Audit trail: who changed what, before/after state -- required for every admin action.
        _logger.LogInformation(
            "AUDIT actor={ActorUserId} role={ActorRole} action=User.SetActiveStatus entity=User entityId={UserId} before={Before} after={After}",
            actorUserId, actorRole, userId,
            JsonSerializer.Serialize(new { before.IsActive }),
            JsonSerializer.Serialize(new { IsActive = isActive }));
    }

    private static UserSummary ToSummary(User u) =>
        new(u.Id, u.Email, u.FullName, u.Role, u.IsActive, u.LastLoginAtUtc, u.Username);
}
