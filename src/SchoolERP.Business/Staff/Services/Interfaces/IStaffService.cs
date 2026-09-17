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
    Task<PagedResult<StaffSummary>> SearchAsync(string? designation, string? keyword, int page, int pageSize, CancellationToken ct = default);
}
