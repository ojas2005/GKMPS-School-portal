using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Academic;
using SchoolERP.DataAccess.Academic.Entities;
using SchoolERP.DataAccess.Academic.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Academic.Repositories;

public class HomeworkRepository : IHomeworkRepository
{
    private readonly AcademicDbContext _db;

    public HomeworkRepository(AcademicDbContext db) => _db = db;

    public Task<Homework?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Homeworks.FirstOrDefaultAsync(h => h.Id == id, ct);

    public Task<IReadOnlyList<Homework>> FindByClassAsync(string classId, string sectionId, CancellationToken ct = default) =>
        _db.Homeworks.Where(h => h.ClassId == classId && h.SectionId == sectionId)
            .OrderByDescending(h => h.DueDateUtc)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Homework>)t.Result, ct);

    public async Task AddAsync(Homework homework, CancellationToken ct = default) =>
        await _db.Homeworks.AddAsync(homework, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
