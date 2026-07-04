using Microsoft.EntityFrameworkCore;
using SchoolERP.Fee.Data;
using SchoolERP.Fee.Entities;
using SchoolERP.Fee.Repositories.Interfaces;

namespace SchoolERP.Fee.Repositories;

public class FeeStructureRepository : IFeeStructureRepository
{
    private readonly FeeDbContext _db;

    public FeeStructureRepository(FeeDbContext db) => _db = db;

    public Task<FeeStructure?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.FeeStructures.FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<IReadOnlyList<FeeStructure>> FindByClassAsync(string classId, string academicYear, CancellationToken ct = default) =>
        _db.FeeStructures.Where(f => f.ClassId == classId && f.AcademicYear == academicYear).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<FeeStructure>)t.Result, ct);

    public async Task AddAsync(FeeStructure structure, CancellationToken ct = default) =>
        await _db.FeeStructures.AddAsync(structure, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
