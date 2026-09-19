using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Identity;
using SchoolERP.DataAccess.Identity.Entities;
using SchoolERP.DataAccess.Identity.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Identity.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _db;

    public UserRepository(IdentityDbContext db) => _db = db;

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Users.Include(u => u.RefreshTokens).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower(), ct);

    public Task<User?> FindByLoginAsync(string loginId, CancellationToken ct = default)
    {
        var normalized = loginId.Trim().ToLower();
        return _db.Users.FirstOrDefaultAsync(
            u => u.Email == normalized || (u.Username != null && u.Username.ToLower() == normalized), ct);
    }

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default)
    {
        var normalized = username.Trim().ToLower();
        return _db.Users.AnyAsync(u => u.Username != null && u.Username.ToLower() == normalized, ct);
    }

    public async Task<IReadOnlyList<User>> SearchUsersAsync(string? role, string? keyword, int page, int pageSize, CancellationToken ct = default)
    {
        return await Filter(role, keyword)
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountUsersAsync(string? role, string? keyword, CancellationToken ct = default) =>
        Filter(role, keyword).CountAsync(ct);

    // Shared by search and count so the page total always matches the filtered results.
    private IQueryable<User> Filter(string? role, string? keyword)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(u => EF.Functions.Like(u.FullName, $"%{keyword}%")
                || EF.Functions.Like(u.Email, $"%{keyword}%")
                || (u.Username != null && EF.Functions.Like(u.Username, $"%{keyword}%")));

        return query;
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.Email == email.ToLower(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _db.Users.AddAsync(user, ct);

    public Task<int> UpdateLastLoginAsync(Guid userId, DateTime loginAtUtc, CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.LastLoginAtUtc, loginAtUtc), ct);

    public Task<int> SetActiveStatusAsync(Guid userId, bool isActive, CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.IsActive, isActive)
                .SetProperty(u => u.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SetPasswordHashAsync(Guid userId, string passwordHash, CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.PasswordHash, passwordHash)
                .SetProperty(u => u.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> IncrementFailedLoginAttemptsAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.FailedLoginAttempts, u => u.FailedLoginAttempts + 1)
                .SetProperty(u => u.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SetLockoutAsync(Guid userId, DateTime lockoutEndUtc, CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.LockoutEndUtc, lockoutEndUtc)
                .SetProperty(u => u.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> ResetFailedLoginAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.FailedLoginAttempts, 0)
                .SetProperty(u => u.LockoutEndUtc, (DateTime?)null)
                .SetProperty(u => u.UpdatedAtUtc, DateTime.UtcNow), ct);

    public async Task HardDeleteAsync(Guid userId, CancellationToken ct = default)
    {
        await _db.RefreshTokens.Where(rt => rt.UserId == userId).ExecuteDeleteAsync(ct);
        await _db.Users.IgnoreQueryFilters().Where(u => u.Id == userId).ExecuteDeleteAsync(ct);
    }

    public Task<int> SetMustChangePasswordAsync(Guid userId, bool mustChange, CancellationToken ct = default) =>
        _db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.MustChangePassword, mustChange), ct);

    public Task<int> SetTwoFactorAsync(Guid userId, string? secret, bool enabled, string? recoveryCodes, CancellationToken ct = default) =>
        _db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.TwoFactorSecret, secret)
                .SetProperty(u => u.TwoFactorEnabled, enabled)
                .SetProperty(u => u.TwoFactorRecoveryCodes, recoveryCodes)
                .SetProperty(u => u.TwoFactorLastStep, (long?)null), ct);

    public async Task<bool> TryAdvanceTwoFactorStepAsync(Guid userId, long step, CancellationToken ct = default) =>
        await _db.Users.Where(u => u.Id == userId && (u.TwoFactorLastStep == null || u.TwoFactorLastStep < step))
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.TwoFactorLastStep, step), ct) == 1;

    public async Task<bool> TryReplaceRecoveryCodesAsync(Guid userId, string expected, string? remaining, CancellationToken ct = default) =>
        await _db.Users.Where(u => u.Id == userId && u.TwoFactorRecoveryCodes == expected)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.TwoFactorRecoveryCodes, remaining), ct) == 1;

    public Task<int> AnonymizeAsync(Guid userId, CancellationToken ct = default) =>
        _db.Users.IgnoreQueryFilters().Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.FullName, "Former user")
                .SetProperty(u => u.Email, u => "erased-" + u.Id + "@invalid.local")
                .SetProperty(u => u.Username, (string?)null)
                .SetProperty(u => u.PasswordHash, (string?)null)
                .SetProperty(u => u.IsActive, false)
                .SetProperty(u => u.TwoFactorSecret, (string?)null)
                .SetProperty(u => u.TwoFactorEnabled, false)
                .SetProperty(u => u.TwoFactorRecoveryCodes, (string?)null)
                .SetProperty(u => u.MustChangePassword, false), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
