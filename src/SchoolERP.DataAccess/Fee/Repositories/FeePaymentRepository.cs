using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SchoolERP.Common;
using SchoolERP.DataAccess.Fee.Entities;
using SchoolERP.DataAccess.Fee.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Fee.Repositories;

public class FeePaymentRepository : IFeePaymentRepository
{
    private readonly FeeDbContext _db;

    public FeePaymentRepository(FeeDbContext db) => _db = db;

    public Task<FeePayment?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.FeePayments.Include(p => p.FeeStructure).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<FeePayment?> FindByStudentAndStructureAsync(Guid studentId, Guid feeStructureId, CancellationToken ct = default) =>
        _db.FeePayments.FirstOrDefaultAsync(p => p.StudentId == studentId && p.FeeStructureId == feeStructureId, ct);

    public async Task<IReadOnlyList<FeePayment>> FindByStudentAsync(Guid studentId, CancellationToken ct = default) =>
        await _db.FeePayments.Include(p => p.FeeStructure).Where(p => p.StudentId == studentId)
            .OrderBy(p => p.DueDate ?? p.CreatedAtUtc).ThenBy(p => p.CreatedAtUtc).ToListAsync(ct);

    public async Task<IReadOnlyList<(Guid StudentId, string Category, string? AcademicYear, string? PeriodKey)>> FindGeneratedKeysAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default)
    {
        if (studentIds.Count == 0) return [];
        var rows = await _db.FeePayments
            .Where(p => studentIds.Contains(p.StudentId) && p.PeriodKey != null)
            .Select(p => new { p.StudentId, p.Category, p.AcademicYear, p.PeriodKey })
            .ToListAsync(ct);
        return rows.Select(r => (r.StudentId, r.Category, r.AcademicYear, r.PeriodKey)).ToList();
    }

    public async Task<IReadOnlyList<FeePayment>> FindByClassAsync(string classId, CancellationToken ct = default) =>
        await _db.FeePayments.Where(p => p.ClassId == classId).ToListAsync(ct);

    public async Task<(IReadOnlyList<FeePayment> Items, int Total)> SearchAsync(FeeEntryFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Filtered(filter);
        var total = await query.CountAsync(ct);
        var items = await query.Include(p => p.FeeStructure)
            .OrderBy(p => p.DueDate == null).ThenBy(p => p.DueDate).ThenBy(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<StudentFeeTotals> Items, int Total)> StudentTotalsAsync(FeeEntryFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var today = filter.Today;
        var grouped = Filtered(filter)
            .GroupBy(p => p.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                ClassId = g.Max(p => p.ClassId),
                Billed = g.Sum(p => p.TotalAmount),
                Paid = g.Sum(p => p.PaidAmount),
                Discount = g.Sum(p => p.WaiverAmount),
                PendingDue = g.Sum(p => p.DueDate == null || p.DueDate <= today ? p.TotalAmount - p.PaidAmount - p.WaiverAmount : 0),
                Upcoming = g.Sum(p => p.DueDate != null && p.DueDate > today ? p.TotalAmount - p.PaidAmount - p.WaiverAmount : 0),
                OldestUnpaid = g.Where(p => p.TotalAmount > p.PaidAmount + p.WaiverAmount).Min(p => p.DueDate)
            });

        var total = await grouped.CountAsync(ct);
        var rows = await grouped
            .OrderByDescending(r => r.PendingDue).ThenByDescending(r => r.Upcoming)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return (rows.Select(r => new StudentFeeTotals(r.StudentId, r.ClassId, r.Billed, r.Paid, r.Discount,
            Math.Max(0, r.PendingDue), Math.Max(0, r.Upcoming), r.OldestUnpaid)).ToList(), total);
    }

    public async Task<IReadOnlyList<FeeCategoryTotals>> CategoryTotalsAsync(FeeEntryFilter filter, CancellationToken ct = default)
    {
        var today = filter.Today;
        var rows = await Filtered(filter)
            .GroupBy(p => p.Category)
            .Select(g => new
            {
                Category = g.Key,
                Billed = g.Sum(p => p.DueDate == null || p.DueDate <= today ? p.TotalAmount : 0),
                Paid = g.Sum(p => p.PaidAmount),
                Discount = g.Sum(p => p.WaiverAmount),
                PendingDue = g.Sum(p => p.DueDate == null || p.DueDate <= today ? p.TotalAmount - p.PaidAmount - p.WaiverAmount : 0),
                Upcoming = g.Sum(p => p.DueDate != null && p.DueDate > today ? p.TotalAmount - p.PaidAmount - p.WaiverAmount : 0)
            })
            .ToListAsync(ct);
        return rows.Select(r => new FeeCategoryTotals(r.Category, r.Billed, r.Paid, r.Discount, Math.Max(0, r.PendingDue), Math.Max(0, r.Upcoming))).ToList();
    }

    public async Task<IReadOnlyList<MonthAmount>> BilledByMonthAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var rows = await _db.FeePayments
            .Where(p => p.DueDate != null && p.DueDate >= fromDate && p.DueDate <= toDate)
            .GroupBy(p => new { p.DueDate!.Value.Year, p.DueDate!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(p => p.TotalAmount) })
            .ToListAsync(ct);
        return rows.Select(r => new MonthAmount(r.Year, r.Month, r.Amount)).ToList();
    }

    public async Task<IReadOnlyList<MonthAmount>> CollectedByMonthAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        // Group by the school's calendar month: a payment at 01:00 IST on the 1st belongs to that month.
        var offsetMinutes = (int)SchoolClock.IndiaOffset.TotalMinutes;
        var rows = await _db.PaymentTransactions
            .Where(t => t.PaidAtUtc >= fromUtc && t.PaidAtUtc < toUtc)
            .Select(t => new { Local = t.PaidAtUtc.AddMinutes(offsetMinutes), t.Amount })
            .GroupBy(t => new { t.Local.Year, t.Local.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(t => t.Amount) })
            .ToListAsync(ct);
        return rows.Select(r => new MonthAmount(r.Year, r.Month, r.Amount)).ToList();
    }

    private IQueryable<FeePayment> Filtered(FeeEntryFilter f)
    {
        var query = _db.FeePayments.AsQueryable();
        if (f.StudentIds is not null) query = query.Where(p => f.StudentIds.Contains(p.StudentId));
        if (!string.IsNullOrWhiteSpace(f.ClassId)) query = query.Where(p => p.ClassId == f.ClassId);
        if (!string.IsNullOrWhiteSpace(f.AcademicYear)) query = query.Where(p => p.AcademicYear == f.AcademicYear);
        if (!string.IsNullOrWhiteSpace(f.Category)) query = query.Where(p => p.Category == f.Category);
        if (f.DueYear is { } y && f.DueMonth is { } m)
        {
            var from = new DateTime(y, m, 1);
            var to = from.AddMonths(1);
            query = query.Where(p => p.DueDate >= from && p.DueDate < to);
        }
        if (f.DueOnOrBefore is { } cutoff) query = query.Where(p => p.DueDate == null || p.DueDate <= cutoff);

        var today = f.Today;
        query = f.Status switch
        {
            FeeStatuses.Paid => query.Where(p => p.PaidAmount + p.WaiverAmount >= p.TotalAmount),
            FeeStatuses.PartiallyPaid => query.Where(p => p.PaidAmount > 0 && p.PaidAmount + p.WaiverAmount < p.TotalAmount),
            FeeStatuses.Pending => query.Where(p => p.PaidAmount == 0 && p.WaiverAmount < p.TotalAmount),
            "Outstanding" => query.Where(p => p.PaidAmount + p.WaiverAmount < p.TotalAmount),
            "Overdue" => query.Where(p => p.PaidAmount + p.WaiverAmount < p.TotalAmount && p.DueDate != null && p.DueDate < today),
            _ => query
        };
        return query;
    }

    public async Task AddAsync(FeePayment payment, CancellationToken ct = default) =>
        await _db.FeePayments.AddAsync(payment, ct);

    public async Task AddRangeAsync(IEnumerable<FeePayment> payments, CancellationToken ct = default) =>
        await _db.FeePayments.AddRangeAsync(payments, ct);

    public Task<int> IncrementPaidAmountAsync(Guid feePaymentId, decimal amount, DateTime paidAtUtc, CancellationToken ct = default) =>
        _db.FeePayments.Where(p => p.Id == feePaymentId && p.TotalAmount - p.PaidAmount - p.WaiverAmount >= amount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.PaidAmount, p => p.PaidAmount + amount)
                .SetProperty(p => p.LastPaidAtUtc, paidAtUtc)
                .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> AddDiscountAsync(Guid feePaymentId, decimal amount, CancellationToken ct = default) =>
        _db.FeePayments.Where(p => p.Id == feePaymentId && p.TotalAmount - p.PaidAmount - p.WaiverAmount >= amount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.WaiverAmount, p => p.WaiverAmount + amount)
                .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> MarkWaiverRequestedAsync(Guid feePaymentId, CancellationToken ct = default) =>
        _db.FeePayments.Where(p => p.Id == feePaymentId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.IsWaiverRequested, true), ct);

    public Task<int> ApproveWaiverAsync(Guid feePaymentId, decimal waiverAmount, Guid approvedByUserId, CancellationToken ct = default) =>
        _db.FeePayments.Where(p => p.Id == feePaymentId && p.TotalAmount - p.PaidAmount - p.WaiverAmount >= waiverAmount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsWaiverApproved, true)
                .SetProperty(p => p.WaiverAmount, p => p.WaiverAmount + waiverAmount)
                .SetProperty(p => p.WaiverApprovedByUserId, approvedByUserId)
                .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> RepriceUntouchedAsync(Guid feePaymentId, decimal newAmount, string classId, CancellationToken ct = default) =>
        _db.FeePayments.Where(p => p.Id == feePaymentId && p.PaidAmount == 0 && p.WaiverAmount == 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.TotalAmount, newAmount)
                .SetProperty(p => p.ClassId, classId)
                .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow), ct);

    public async Task<(int Count, decimal Total)> DeleteUntouchedUpcomingAsync(Guid studentId, DateTime today, CancellationToken ct = default)
    {
        var upcoming = _db.FeePayments.Where(p => p.StudentId == studentId && p.DueDate != null && p.DueDate > today
            && p.PaidAmount == 0 && p.WaiverAmount == 0 && p.PeriodKey != null);
        var total = await upcoming.SumAsync(p => (decimal?)p.TotalAmount, ct) ?? 0;
        var count = await upcoming.ExecuteDeleteAsync(ct);
        return (count, total);
    }

    public async Task<int> DeleteUnpaidForStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        if (await _db.FeePayments.AnyAsync(p => p.StudentId == studentId && p.PaidAmount > 0, ct))
            throw new InvalidOperationException("This student has fee payments recorded; their fees can't be deleted.");
        return await _db.FeePayments.Where(p => p.StudentId == studentId).ExecuteDeleteAsync(ct);
    }

    public async Task<decimal> GetTotalCollectedAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        // Aggregate query via SumAsync executed in the database -- never pulled into app memory.
        var query = _db.PaymentTransactions.AsQueryable();
        if (fromUtc.HasValue) query = query.Where(t => t.PaidAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(t => t.PaidAtUtc <= toUtc.Value);

        return await query.AnyAsync(ct) ? await query.SumAsync(t => t.Amount, ct) : 0m;
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
        _db.Database.BeginTransactionAsync(ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
