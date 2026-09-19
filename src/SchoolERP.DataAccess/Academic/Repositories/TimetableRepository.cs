using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Academic;
using SchoolERP.DataAccess.Academic.Entities;
using SchoolERP.DataAccess.Academic.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Academic.Repositories;

public class TimetableRepository : ITimetableRepository
{
    private readonly AcademicDbContext _db;

    public TimetableRepository(AcademicDbContext db) => _db = db;

    public Task<Timetable?> FindByClassSectionAsync(string classId, string sectionId, CancellationToken ct = default) =>
        _db.Timetables.FirstOrDefaultAsync(t => t.ClassId == classId && t.SectionId == sectionId, ct);

    public async Task<IReadOnlyList<Timetable>> FindAllAsync(CancellationToken ct = default) =>
        await _db.Timetables.ToListAsync(ct);

    public async Task AddAsync(Timetable timetable, CancellationToken ct = default) =>
        await _db.Timetables.AddAsync(timetable, ct);

    public Task<int> UpdateSlotsAsync(Guid id, string slotsJson, CancellationToken ct = default) =>
        _db.Timetables.Where(t => t.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.SlotsJson, slotsJson)
                .SetProperty(t => t.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
