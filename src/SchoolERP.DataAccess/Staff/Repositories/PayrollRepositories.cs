using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Staff;
using SchoolERP.DataAccess.Staff.Entities;
using SchoolERP.DataAccess.Staff.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Staff.Repositories;

public class PayoutRepository : IPayoutRepository
{
    private readonly StaffDbContext _db;

    public PayoutRepository(StaffDbContext db) => _db = db;

    public async Task<IReadOnlyList<Payout>> FindByStaffAsync(Guid staffId, CancellationToken ct = default) =>
        await _db.Payouts
            .Where(p => p.StaffId == staffId)
            .OrderByDescending(p => p.PaidOnUtc)
            .ToListAsync(ct);

    public async Task<decimal> GetTotalForMonthAsync(Guid staffId, int year, int month, CancellationToken ct = default)
    {
        var query = _db.Payouts.Where(p => p.StaffId == staffId && p.PaidOnUtc.Year == year && p.PaidOnUtc.Month == month);
        return await query.AnyAsync(ct) ? await query.SumAsync(p => p.Amount, ct) : 0m;
    }

    public async Task AddAsync(Payout payout, CancellationToken ct = default) =>
        await _db.Payouts.AddAsync(payout, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

public class StaffAttendanceRepository : IStaffAttendanceRepository
{
    private readonly StaffDbContext _db;

    public StaffAttendanceRepository(StaffDbContext db) => _db = db;

    public async Task<IReadOnlyList<StaffAttendanceRecord>> FindByStaffAndRangeAsync(Guid staffId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await _db.StaffAttendance
            .Where(a => a.StaffId == staffId && a.Date >= from && a.Date <= to)
            .OrderByDescending(a => a.Date)
            .ToListAsync(ct);

    public Task<bool> HasMarkedForDateAsync(Guid staffId, DateOnly date, CancellationToken ct = default) =>
        _db.StaffAttendance.AnyAsync(a => a.StaffId == staffId && a.Date == date, ct);

    public async Task AddAsync(StaffAttendanceRecord record, CancellationToken ct = default) =>
        await _db.StaffAttendance.AddAsync(record, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
