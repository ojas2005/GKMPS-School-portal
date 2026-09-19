using SchoolERP.Common;
using SchoolERP.Business.Identity.DTOs;

namespace SchoolERP.Business.Identity.Services.Interfaces;

public interface IUserService
{
    Task<UserSummary?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<UserSummary>> SearchAsync(string? role, string? keyword, int page, int pageSize, CancellationToken ct = default);
    Task SetActiveStatusAsync(Guid userId, bool isActive, string actorUserId, string actorRole, CancellationToken ct = default);

    /// <summary>Admin-initiated reset -- overwrites the password hash directly (no knowledge
    /// of the old password required) and revokes every existing session for the account.</summary>
    Task SetPasswordAsync(Guid userId, string newPassword, string actorUserId, string actorRole, CancellationToken ct = default);

    /// <summary>Hard-deletes an account that has never signed in -- used to roll back an
    /// onboarding whose profile-creation step failed after the login was created.</summary>
    /// <summary>Erases the person from a login (part of erasing a former student's data). Returns the email it held, so notifications to it can be forgotten too.</summary>
    Task<string?> ErasePersonalDataAsync(Guid userId, string actorUserId, string actorRole, CancellationToken ct = default);
    Task ResetTwoFactorAsync(Guid userId, string actorUserId, string actorRole, CancellationToken ct = default);
    Task DeleteUnusedAsync(Guid userId, string actorUserId, string actorRole, CancellationToken ct = default);
}
