using SchoolERP.Identity.Entities;

namespace SchoolERP.Identity.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Matches by username OR email (both case-insensitive).</summary>
    Task<User?> FindByLoginAsync(string loginId, CancellationToken ct = default);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default);
    Task<IReadOnlyList<User>> SearchUsersAsync(string? role, string? keyword, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountUsersAsync(string? role, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);

    /// <summary>Atomic bump -- no load-then-save round trip.</summary>
    Task<int> UpdateLastLoginAsync(Guid userId, DateTime loginAtUtc, CancellationToken ct = default);

    /// <summary>Atomic activate/deactivate toggle.</summary>
    Task<int> SetActiveStatusAsync(Guid userId, bool isActive, CancellationToken ct = default);

    /// <summary>Atomic password-hash overwrite -- used for admin-initiated resets.</summary>
    Task<int> SetPasswordHashAsync(Guid userId, string passwordHash, CancellationToken ct = default);

    /// <summary>Atomic increment (SQL-level, not load-then-save) after a failed password check.</summary>
    Task<int> IncrementFailedLoginAttemptsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Locks the account until <paramref name="lockoutEndUtc"/>.</summary>
    Task<int> SetLockoutAsync(Guid userId, DateTime lockoutEndUtc, CancellationToken ct = default);

    /// <summary>Clears the failure counter and any lockout -- called on a successful login.</summary>
    Task<int> ResetFailedLoginAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Removes the user row and its refresh tokens outright (not a soft delete, so
    /// the email/login ID can be reused).</summary>
    Task HardDeleteAsync(Guid userId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
