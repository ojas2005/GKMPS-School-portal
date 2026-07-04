using SchoolERP.Staff.Entities;

namespace SchoolERP.Staff.Repositories.Interfaces;

public interface IPayoutRepository
{
    Task<IReadOnlyList<Payout>> FindByStaffAsync(Guid staffId, CancellationToken ct = default);

    /// <summary>Aggregate: total payouts recorded for this staff member in the given
    /// calendar month/year, computed via SumAsync in PostgreSQL.</summary>
    Task<decimal> GetTotalForMonthAsync(Guid staffId, int year, int month, CancellationToken ct = default);

    Task AddAsync(Payout payout, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IStaffAttendanceRepository
{
    Task<IReadOnlyList<StaffAttendanceRecord>> FindByStaffAndRangeAsync(Guid staffId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Duplicate-prevention guard: true if this staff member already has a record for the given date.</summary>
    Task<bool> HasMarkedForDateAsync(Guid staffId, DateOnly date, CancellationToken ct = default);

    Task AddAsync(StaffAttendanceRecord record, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
