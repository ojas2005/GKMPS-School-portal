using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Examination;
using SchoolERP.DataAccess.Examination.Entities;
using SchoolERP.DataAccess.Examination.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Examination.Repositories;

public class ExamRepository : IExamRepository
{
    private readonly ExaminationDbContext _db;

    public ExamRepository(ExaminationDbContext db) => _db = db;

    public Task<Exam?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Exams.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<IReadOnlyList<Exam>> FindByClassAsync(string? classId, CancellationToken ct = default) =>
        _db.Exams.Where(e => classId == null || e.ClassId == classId).OrderByDescending(e => e.ExamDateUtc).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Exam>)t.Result, ct);

    public async Task AddAsync(Exam exam, CancellationToken ct = default) =>
        await _db.Exams.AddAsync(exam, ct);

    public Task<int> PublishResultsAsync(Guid examId, CancellationToken ct = default) =>
        _db.Exams.Where(e => e.Id == examId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.IsResultPublished, true)
                .SetProperty(e => e.ResultPublishedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
