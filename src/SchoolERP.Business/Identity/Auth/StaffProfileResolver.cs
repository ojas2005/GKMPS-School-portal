using SchoolERP.DataAccess.Staff.Repositories.Interfaces;

namespace SchoolERP.Business.Identity.Auth;

public record StaffClaimsProfile(Guid StaffId, string? ClassTeacherOfClassId, string? ClassTeacherOfSectionId);

/// <summary>
/// Resolves the staff record linked to a user account so the access token can carry
/// staffId/classTeacherOfClassId/classTeacherOfSectionId claims (used to let only the class
/// teacher of a class mark its attendance, and to scope payout/attendance reads to the
/// caller's own record). Best-effort: any failure returns null so login is never blocked.
/// </summary>
public class StaffProfileResolver
{
    private readonly IStaffRepository _staff;
    private readonly ILogger<StaffProfileResolver> _logger;

    public StaffProfileResolver(IStaffRepository staff, ILogger<StaffProfileResolver> logger)
    {
        _staff = staff;
        _logger = logger;
    }

    public async Task<StaffClaimsProfile?> ResolveByUserAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var staff = await _staff.FindByLinkedUserAsync(userId, ct);
            return staff is null ? null : new StaffClaimsProfile(staff.Id, staff.ClassTeacherOfClassId, staff.ClassTeacherOfSectionId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not resolve staff profile for user {UserId}", userId);
            return null;
        }
    }
}
