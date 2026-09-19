using SchoolERP.DataAccess.Examination.Entities;

namespace SchoolERP.DataAccess.Examination.Repositories.Interfaces;

public interface IMarksRepository
{
    Task<MarksEntry?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<MarksEntry?> FindByExamAndStudentAsync(Guid examId, Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<MarksEntry>> FindByExamAsync(Guid examId, CancellationToken ct = default);

    /// <summary>All of a student's marks entries (with Exam included), optionally published results only.</summary>
    Task<IReadOnlyList<MarksEntry>> FindByStudentAsync(Guid studentId, bool publishedOnly, CancellationToken ct = default);

    /// <summary>Duplicate-prevention guard: marks already entered for this student/exam.</summary>
    Task<bool> HasMarksEnteredAsync(Guid examId, Guid studentId, CancellationToken ct = default);

    Task AddAsync(MarksEntry entry, CancellationToken ct = default);

    /// <summary>Atomic correction of an already-entered score, without a load-then-save round trip.</summary>
    Task<int> UpdateMarksAsync(Guid id, decimal marksObtained, string? grade, CancellationToken ct = default);

    /// <summary>Aggregate: average marks for a subject/exam, computed via AverageAsync in PostgreSQL.</summary>
    Task<double> GetAverageMarksAsync(Guid examId, CancellationToken ct = default);

    /// <summary>Aggregate: rank generation -- students ordered by marks, computed via GroupBy/OrderBy in the database.</summary>
    Task<IReadOnlyList<(Guid StudentId, decimal MarksObtained, int Rank)>> GetRankingsAsync(Guid examId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
