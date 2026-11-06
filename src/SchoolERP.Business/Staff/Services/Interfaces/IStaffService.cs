using SchoolERP.Common;
using SchoolERP.Business.Staff.DTOs;

namespace SchoolERP.Business.Staff.Services.Interfaces;

public interface IStaffService
{
    Task<StaffSummary> OnboardAsync(CreateStaffRequest request, CancellationToken ct = default);
    Task<StaffSummary?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StaffSummary?> GetByLinkedUserAsync(Guid linkedUserId, CancellationToken ct = default);
    Task<StaffSummary> AssignClassTeacherAsync(Guid staffId, AssignClassTeacherRequest request, string actorUserId, string actorRole, CancellationToken ct = default);
    Task<StaffSummary> SetSalaryAsync(Guid staffId, SetSalaryRequest request, CancellationToken ct = default);
    Task<PagedResult<StaffSummary>> SearchAsync(string? designation, string? keyword, RosterFilter roster, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Removes a staff member: their login is switched off and they leave class-teacher duty and every list.</summary>
    Task<StaffSummary> RemoveAsync(Guid staffId, string actorUserId, string actorRole, CancellationToken ct = default);
    Task<StaffSummary> RestoreAsync(Guid staffId, string actorUserId, string actorRole, CancellationToken ct = default);
}
