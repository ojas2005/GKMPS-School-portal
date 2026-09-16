using SchoolERP.Shared.Common;
using SchoolERP.Student.DTOs;

namespace SchoolERP.Student.Services.Interfaces;

public interface IStudentService
{
    Task<StudentSummary> AdmitStudentAsync(CreateStudentRequest request, CancellationToken ct = default);
    Task<StudentSummary?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<StudentSummary>> SearchAsync(string? classId, string? sectionId, string? keyword, int page, int pageSize, CancellationToken ct = default);
    Task ReassignClassAsync(Guid studentId, ReassignClassRequest request, string actorUserId, string actorRole, CancellationToken ct = default);
    Task<StudentSummary> UpdateDetailsAsync(Guid studentId, UpdateStudentRequest request, string actorUserId, string actorRole, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, int>> GetActiveCountByClassAsync(CancellationToken ct = default);
    Task<StudentSummary> LinkParentAccountAsync(Guid studentId, Guid? parentUserId, string actorUserId, string actorRole, CancellationToken ct = default);
}
