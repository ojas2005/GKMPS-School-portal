using SchoolERP.Common;
using SchoolERP.Business.Student.DTOs;

namespace SchoolERP.Business.Student.Services.Interfaces;

public interface IStudentService
{
    Task<AdmittedStudent> AdmitStudentAsync(CreateStudentRequest request, CancellationToken ct = default);
    Task<StudentSummary?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<StudentSummary>> SearchAsync(string? classId, string? sectionId, string? keyword, RosterFilter roster, int page, int pageSize, CancellationToken ct = default);
    Task ReassignClassAsync(Guid studentId, ReassignClassRequest request, string actorUserId, string actorRole, CancellationToken ct = default);
    Task<StudentSummary> UpdateDetailsAsync(Guid studentId, UpdateStudentRequest request, string actorUserId, string actorRole, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, int>> GetActiveCountByClassAsync(CancellationToken ct = default);
    /// <summary>Takes a current student off the rolls; their parent's login is switched off unless a sibling still uses it.</summary>
    Task<StudentSummary> RemoveAsync(Guid studentId, string? reason, string actorUserId, string actorRole, CancellationToken ct = default);
    /// <summary>Brings a removed student back, with their parent's login.</summary>
    Task<StudentSummary> RestoreAsync(Guid studentId, string actorUserId, string actorRole, CancellationToken ct = default);
    Task<StudentSummary> LinkParentAccountAsync(Guid studentId, Guid? parentUserId, string actorUserId, string actorRole, CancellationToken ct = default);
}
