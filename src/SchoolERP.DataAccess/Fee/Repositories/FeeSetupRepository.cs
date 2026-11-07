using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Fee.Entities;
using SchoolERP.DataAccess.Fee.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Fee.Repositories;

public class FeeSetupRepository : IFeeSetupRepository
{
    private readonly FeeDbContext _db;

    public FeeSetupRepository(FeeDbContext db) => _db = db;

    public async Task<IReadOnlyList<ClassFeeRate>> GetRatesAsync(CancellationToken ct = default) =>
        await _db.ClassFeeRates.ToListAsync(ct);

    public Task<ClassFeeRate?> FindRateAsync(string classId, CancellationToken ct = default) =>
        _db.ClassFeeRates.FirstOrDefaultAsync(r => r.ClassId == classId, ct);

    public async Task AddRateAsync(ClassFeeRate rate, CancellationToken ct = default) =>
        await _db.ClassFeeRates.AddAsync(rate, ct);

    public Task<FeeSchedule?> GetScheduleAsync(CancellationToken ct = default) =>
        _db.FeeSchedules.OrderBy(s => s.CreatedAtUtc).FirstOrDefaultAsync(ct);

    public async Task AddScheduleAsync(FeeSchedule schedule, CancellationToken ct = default) =>
        await _db.FeeSchedules.AddAsync(schedule, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
