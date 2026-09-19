using SchoolERP.DataAccess.Fee.Entities;

namespace SchoolERP.DataAccess.Fee.Repositories.Interfaces;

public interface IFeePaymentRepository
{
    Task<FeePayment?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<FeePayment?> FindByStudentAndStructureAsync(Guid studentId, Guid feeStructureId, CancellationToken ct = default);
    Task<IReadOnlyList<FeePayment>> FindByStudentAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>Every due (structure-linked or ad-hoc) tagged with this ClassId -- backs the owner's pending-by-class summary.</summary>
    Task<IReadOnlyList<FeePayment>> FindByClassAsync(string classId, CancellationToken ct = default);

    Task AddAsync(FeePayment payment, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<FeePayment> payments, CancellationToken ct = default);

    /// <summary>Atomic increment of PaidAmount -- ExecuteUpdateAsync, never load-then-save.</summary>
    Task<int> IncrementPaidAmountAsync(Guid feePaymentId, decimal amount, CancellationToken ct = default);

    Task<int> MarkWaiverRequestedAsync(Guid feePaymentId, CancellationToken ct = default);
    Task<int> ApproveWaiverAsync(Guid feePaymentId, decimal waiverAmount, Guid approvedByUserId, CancellationToken ct = default);

    /// <summary>Aggregate: total fees collected across all payments, computed via SumAsync in PostgreSQL.</summary>
    Task<decimal> GetTotalCollectedAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
