using SchoolERP.Fee.Entities;

namespace SchoolERP.Fee.Repositories.Interfaces;

public interface IPaymentTransactionRepository
{
    Task<PaymentTransaction?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaymentTransaction?> FindByReceiptNumberAsync(string receiptNumber, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentTransaction>> FindByFeePaymentIdAsync(Guid feePaymentId, CancellationToken ct = default);

    /// <summary>Every transaction across any of these due rows -- backs the per-student receipt list.</summary>
    Task<IReadOnlyList<PaymentTransaction>> FindByFeePaymentIdsAsync(IEnumerable<Guid> feePaymentIds, CancellationToken ct = default);

    Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default);
    Task<int> SetReceiptBlobPathAsync(Guid id, string blobPath, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
