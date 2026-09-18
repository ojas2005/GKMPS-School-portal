using SchoolERP.Business.Identity.Sessions;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SchoolERP.Business.Identity.DTOs;
using SchoolERP.DataAccess.Identity.Entities;
using SchoolERP.DataAccess.Identity.Repositories.Interfaces;
using SchoolERP.Business.Identity.Services.Interfaces;
using SchoolERP.Common;

namespace SchoolERP.Business.Identity.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ISessionService _sessions;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository users, IRefreshTokenRepository refreshTokens, ISessionService sessions, ILogger<UserService> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _sessions = sessions;
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
        var total = await _users.CountUsersAsync(role, keyword, ct);

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

        EnsureCanManage(actorRole, before);
        if (!isActive && before.Id.ToString() == actorUserId)
            throw new InvalidOperationException("You cannot deactivate your own account.");

        await _users.SetActiveStatusAsync(userId, isActive, ct);
        if (!isActive)
        {
            // A deactivated account must not keep refreshing its way back in, and its open
            // sessions stop working immediately.
            await _refreshTokens.RevokeAllForUserAsync(userId, ct);
            await _sessions.EndAllForUserAsync(userId, SessionEndReasons.Deactivated, ct);
        }

        // Audit trail: who changed what, before/after state -- required for every admin action.
        _logger.LogInformation(
            "AUDIT actor={ActorUserId} role={ActorRole} action=User.SetActiveStatus entity=User entityId={UserId} before={Before} after={After}",
            actorUserId, actorRole, userId,
            JsonSerializer.Serialize(new { before.IsActive }),
            JsonSerializer.Serialize(new { IsActive = isActive }));
    }

    public async Task SetPasswordAsync(Guid userId, string newPassword, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        EnsureCanManage(actorRole, user);

        var newHash = _passwordHasher.HashPassword(user, newPassword);
        await _users.SetPasswordHashAsync(userId, newHash, ct);

        // Force re-login everywhere -- a session issued under the old password shouldn't
        // survive an admin-initiated reset.
        await _refreshTokens.RevokeAllForUserAsync(userId, ct);
        await _sessions.EndAllForUserAsync(userId, SessionEndReasons.PasswordReset, ct);

        // Audit trail -- never log the password itself, only that it changed.
        _logger.LogInformation(
            "AUDIT actor={ActorUserId} role={ActorRole} action=User.PasswordReset entity=User entityId={UserId}",
            actorUserId, actorRole, userId);
    }

    public async Task DeleteUnusedAsync(Guid userId, string actorUserId, string actorRole, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        EnsureCanManage(actorRole, user);

        // Only for rolling back a half-finished onboarding (login created, profile creation
        // failed). An account that has ever signed in holds real history -- deactivate it instead.
        if (user.LastLoginAtUtc is not null)
            throw new InvalidOperationException("This account has already been used; deactivate it instead of deleting it.");

        await _users.HardDeleteAsync(userId, ct);

        _logger.LogInformation(
            "AUDIT actor={ActorUserId} role={ActorRole} action=User.DeleteUnused entity=User entityId={UserId}",
            actorUserId, actorRole, userId);
    }

    private static void EnsureCanManage(string actorRole, User target)
    {
        if (!RoleNames.CanManageRole(actorRole, target.Role))
            throw new UnauthorizedAccessException($"You are not allowed to manage '{target.Role}' accounts.");
    }

    private static UserSummary ToSummary(User u) =>
        new(u.Id, u.Email, u.FullName, u.Role, u.IsActive, u.LastLoginAtUtc, u.Username);
}
