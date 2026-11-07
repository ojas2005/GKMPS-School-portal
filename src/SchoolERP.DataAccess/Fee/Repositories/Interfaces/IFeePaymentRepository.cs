using Microsoft.EntityFrameworkCore.Storage;
using SchoolERP.DataAccess.Fee.Entities;

namespace SchoolERP.DataAccess.Fee.Repositories.Interfaces;

/// <summary>Which fee entries a list or total covers. Every field is optional.</summary>
public sealed record FeeEntryFilter
{
    public IReadOnlyCollection<Guid>? StudentIds { get; init; }
    public string? ClassId { get; init; }
    public string? AcademicYear { get; init; }
    public string? Category { get; init; }
    /// <summary>Fees falling due in this calendar month.</summary>
    public int? DueYear { get; init; }
    public int? DueMonth { get; init; }
    /// <summary>Pending, PartiallyPaid, Paid, Outstanding (anything still owed) or Overdue (owed and past due).</summary>
    public string? Status { get; init; }
    /// <summary>Hide fees that haven't fallen due yet (on or before this date counts as due).</summary>
    public DateTime? DueOnOrBefore { get; init; }
    /// <summary>The school's today, for Overdue.</summary>
    public DateTime Today { get; init; }
}

public sealed record FeeCategoryTotals(string Category, decimal Billed, decimal Paid, decimal Discount, decimal PendingDue, decimal Upcoming);

public sealed record StudentFeeTotals(Guid StudentId, string? ClassId, decimal Billed, decimal Paid, decimal Discount, decimal PendingDue, decimal Upcoming, DateTime? OldestUnpaidDueDate);

public sealed record MonthAmount(int Year, int Month, decimal Amount);

public interface IFeePaymentRepository
{
    Task<FeePayment?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<FeePayment?> FindByStudentAndStructureAsync(Guid studentId, Guid feeStructureId, CancellationToken ct = default);
    Task<IReadOnlyList<FeePayment>> FindByStudentAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>The (Category, AcademicYear, PeriodKey) of every generated fee these students have.</summary>
    Task<IReadOnlyList<(Guid StudentId, string Category, string? AcademicYear, string? PeriodKey)>> FindGeneratedKeysAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default);

    /// <summary>Every due (structure-linked or ad-hoc) tagged with this ClassId -- backs the owner's pending-by-class summary.</summary>
    Task<IReadOnlyList<FeePayment>> FindByClassAsync(string classId, CancellationToken ct = default);

    /// <summary>A page of fee entries, oldest due first, and how many match in all.</summary>
    Task<(IReadOnlyList<FeePayment> Items, int Total)> SearchAsync(FeeEntryFilter filter, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Per-student totals over the matching fees, most owed first.</summary>
    Task<(IReadOnlyList<StudentFeeTotals> Items, int Total)> StudentTotalsAsync(FeeEntryFilter filter, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Totals per category over the matching fees.</summary>
    Task<IReadOnlyList<FeeCategoryTotals>> CategoryTotalsAsync(FeeEntryFilter filter, CancellationToken ct = default);

    /// <summary>What fell due each month in [fromDate, toDate] (billed amounts).</summary>
    Task<IReadOnlyList<MonthAmount>> BilledByMonthAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);

    Task AddAsync(FeePayment payment, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<FeePayment> payments, CancellationToken ct = default);

    /// <summary>Atomic, guarded increment of PaidAmount: applies only while the fee still has at
    /// least <paramref name="amount"/> outstanding. Returns 0 if it would overpay.</summary>
    Task<int> IncrementPaidAmountAsync(Guid feePaymentId, decimal amount, DateTime paidAtUtc, CancellationToken ct = default);

    /// <summary>Atomic, guarded concession: adds to WaiverAmount while the fee still has at least
    /// <paramref name="amount"/> outstanding. Returns 0 if it would take the fee below zero.</summary>
    Task<int> AddDiscountAsync(Guid feePaymentId, decimal amount, CancellationToken ct = default);

    Task<int> MarkWaiverRequestedAsync(Guid feePaymentId, CancellationToken ct = default);
    Task<int> ApproveWaiverAsync(Guid feePaymentId, decimal waiverAmount, Guid approvedByUserId, CancellationToken ct = default);

    /// <summary>Re-prices a fee that hasn't been touched yet (nothing paid, nothing waived).</summary>
    Task<int> RepriceUntouchedAsync(Guid feePaymentId, decimal newAmount, string classId, CancellationToken ct = default);

    /// <summary>Deletes the student's fees that aren't due yet and haven't been touched (nothing paid or
    /// taken off) -- when a student leaves, so they aren't billed for exams they won't sit.</summary>
    Task<(int Count, decimal Total)> DeleteUntouchedUpcomingAsync(Guid studentId, DateTime today, CancellationToken ct = default);

    /// <summary>Deletes every fee of a student, only if none has a payment. For undoing an admission.</summary>
    Task<int> DeleteUnpaidForStudentAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>Aggregate: total fees collected across all payments, computed via SumAsync in the database.</summary>
    Task<decimal> GetTotalCollectedAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);

    /// <summary>Collections per school-calendar month for payments in [fromUtc, toUtc).</summary>
    Task<IReadOnlyList<MonthAmount>> CollectedByMonthAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
