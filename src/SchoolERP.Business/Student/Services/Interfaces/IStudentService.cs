using SchoolERP.Common;
using SchoolERP.Business.Student.DTOs;

namespace SchoolERP.Business.Student.Services.Interfaces;

public interface IStudentService
{
    /// <summary>
    /// Saves the student record of a new admission after its checks (unique admission number,
    /// a Parent login not serving another child). Fees, transport and the welcome email are
    /// AdmissionService's job, which calls this.
    /// </summary>
    Task<StudentSummary> CreateAdmissionAsync(CreateStudentRequest request, CancellationToken ct = default);
    /// <summary>Deletes a student record just created by an admission that couldn't be completed.</summary>
    Task UndoAdmissionAsync(Guid studentId, CancellationToken ct = default);
    Task<StudentSummary?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StudentSummary>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
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
