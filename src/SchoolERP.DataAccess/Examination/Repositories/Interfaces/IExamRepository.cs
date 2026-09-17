using SchoolERP.DataAccess.Examination.Entities;

namespace SchoolERP.DataAccess.Examination.Repositories.Interfaces;

public interface IExamRepository
{
    Task<Exam?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Exam>> FindByClassAsync(string? classId, CancellationToken ct = default);
    Task AddAsync(Exam exam, CancellationToken ct = default);

    /// <summary>Two-step workflow: exam results start unpublished; PublishResultsAsync flips the flag atomically once marks are finalized.</summary>
    Task<int> PublishResultsAsync(Guid examId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
