using SchoolERP.Student.Entities;

namespace SchoolERP.Student.Repositories.Interfaces;

public interface IStudentDocumentRepository
{
    Task<StudentDocument?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StudentDocument>> FindByStudentIdAsync(Guid studentId, CancellationToken ct = default);
    Task<bool> HasDocumentTypeAsync(Guid studentId, string documentType, CancellationToken ct = default);
    Task AddAsync(StudentDocument document, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
