using SchoolERP.Business.Academic.DTOs;

namespace SchoolERP.Business.Academic.Services.Interfaces;

public interface IHomeworkService
{
    Task<HomeworkSummary> AssignAsync(CreateHomeworkRequest request, Guid assignedByStaffId, CancellationToken ct = default);
    Task<IReadOnlyList<HomeworkSummary>> GetByClassAsync(string classId, string sectionId, CancellationToken ct = default);
}
