using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Examination;
using SchoolERP.DataAccess.Examination.Entities;
using SchoolERP.DataAccess.Examination.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Examination.Repositories;

public class MarksRepository : IMarksRepository
{
    private readonly ExaminationDbContext _db;

    public MarksRepository(ExaminationDbContext db) => _db = db;

    public Task<MarksEntry?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.MarksEntries.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<MarksEntry?> FindByExamAndStudentAsync(Guid examId, Guid studentId, CancellationToken ct = default) =>
        _db.MarksEntries.FirstOrDefaultAsync(m => m.ExamId == examId && m.StudentId == studentId, ct);

    public Task<IReadOnlyList<MarksEntry>> FindByExamAsync(Guid examId, CancellationToken ct = default) =>
        _db.MarksEntries.Where(m => m.ExamId == examId).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<MarksEntry>)t.Result, ct);

    public Task<IReadOnlyList<MarksEntry>> FindByStudentAsync(Guid studentId, bool publishedOnly, CancellationToken ct = default)
    {
        var query = _db.MarksEntries.Include(m => m.Exam).Where(m => m.StudentId == studentId);
        if (publishedOnly)
            query = query.Where(m => m.Exam!.IsResultPublished);
        return query.OrderByDescending(m => m.Exam!.ExamDateUtc).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<MarksEntry>)t.Result, ct);
    }

    public Task<bool> HasMarksEnteredAsync(Guid examId, Guid studentId, CancellationToken ct = default) =>
        _db.MarksEntries.AnyAsync(m => m.ExamId == examId && m.StudentId == studentId, ct);

    public async Task AddAsync(MarksEntry entry, CancellationToken ct = default) =>
        await _db.MarksEntries.AddAsync(entry, ct);

    public Task<int> UpdateMarksAsync(Guid id, decimal marksObtained, string? grade, CancellationToken ct = default) =>
        _db.MarksEntries.Where(m => m.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(m => m.MarksObtained, marksObtained)
                .SetProperty(m => m.Grade, grade)
                .SetProperty(m => m.UpdatedAtUtc, DateTime.UtcNow), ct);

    public async Task<double> GetAverageMarksAsync(Guid examId, CancellationToken ct = default)
    {
        var query = _db.MarksEntries.Where(m => m.ExamId == examId);
        // Aggregate query via AverageAsync executed in PostgreSQL -- rows never pulled into app memory.
        if (!await query.AnyAsync(ct))
            return 0d;

        return await query.AverageAsync(m => (double)m.MarksObtained, ct);
    }

    public async Task<IReadOnlyList<(Guid StudentId, decimal MarksObtained, int Rank)>> GetRankingsAsync(Guid examId, CancellationToken ct = default)
    {
        // Rank generation via an aggregate ORDER BY in the database, not an in-memory sort.
        var ordered = await _db.MarksEntries
            .Where(m => m.ExamId == examId)
            .OrderByDescending(m => m.MarksObtained)
            .Select(m => new { m.StudentId, m.MarksObtained })
            .ToListAsync(ct);

        return ordered
            .Select((m, index) => (m.StudentId, m.MarksObtained, Rank: index + 1))
            .ToList();
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
