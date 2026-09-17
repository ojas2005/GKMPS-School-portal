using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Academic;
using SchoolERP.DataAccess.Academic.Entities;
using SchoolERP.DataAccess.Academic.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Academic.Repositories;

public class SubjectRepository : ISubjectRepository
{
    private readonly AcademicDbContext _db;

    public SubjectRepository(AcademicDbContext db) => _db = db;

    public Task<Subject?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Subjects.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<IReadOnlyList<Subject>> FindByClassAsync(string? classId, CancellationToken ct = default) =>
        _db.Subjects.Where(s => classId == null || s.ClassId == classId).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Subject>)t.Result, ct);

    public Task<bool> ExistsAsync(string classId, string code, CancellationToken ct = default) =>
        _db.Subjects.AnyAsync(s => s.ClassId == classId && s.Code == code, ct);

    public async Task AddAsync(Subject subject, CancellationToken ct = default) =>
        await _db.Subjects.AddAsync(subject, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
