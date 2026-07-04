using SchoolERP.Academic.DTOs;

namespace SchoolERP.Academic.Services.Interfaces;

public interface IHomeworkService
{
    Task<HomeworkSummary> AssignAsync(CreateHomeworkRequest request, Guid assignedByStaffId, CancellationToken ct = default);
    Task<IReadOnlyList<HomeworkSummary>> GetByClassAsync(string classId, string sectionId, CancellationToken ct = default);
}
