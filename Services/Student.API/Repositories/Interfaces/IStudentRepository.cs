using SchoolERP.Student.Entities;

namespace SchoolERP.Student.Repositories.Interfaces;

public interface IStudentRepository
{
    Task<StudentProfile?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<StudentProfile?> FindByAdmissionNumberAsync(string admissionNumber, CancellationToken ct = default);
    Task<IReadOnlyList<StudentProfile>> SearchStudentsAsync(string? classId, string? sectionId, string? keyword, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountStudentsAsync(string? classId, string? sectionId, CancellationToken ct = default);
    Task<bool> ExistsByAdmissionNumberAsync(string admissionNumber, CancellationToken ct = default);

    Task AddAsync(StudentProfile student, CancellationToken ct = default);

    /// <summary>Atomic class/section reassignment -- no load-then-save.</summary>
    Task<int> ReassignClassSectionAsync(Guid studentId, string classId, string sectionId, CancellationToken ct = default);

    /// <summary>Atomic status transition (Active -> TransferredOut/Graduated/Suspended).</summary>
    Task<int> UpdateStatusAsync(Guid studentId, string status, DateTime? transferredOutAtUtc, CancellationToken ct = default);

    /// <summary>Aggregate: total active admissions per class, computed in the database via GroupBy/Count, never pulled into memory.</summary>
    Task<IReadOnlyDictionary<string, int>> GetActiveCountByClassAsync(CancellationToken ct = default);

    /// <summary>Atomic parent-account link/unlink.</summary>
    Task<int> SetParentUserIdAsync(Guid studentId, Guid? parentUserId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
