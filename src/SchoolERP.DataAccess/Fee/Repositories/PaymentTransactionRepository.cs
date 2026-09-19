using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Fee;
using SchoolERP.DataAccess.Fee.Entities;
using SchoolERP.DataAccess.Fee.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Fee.Repositories;

public class PaymentTransactionRepository : IPaymentTransactionRepository
{
    private readonly FeeDbContext _db;

    public PaymentTransactionRepository(FeeDbContext db) => _db = db;

    public Task<PaymentTransaction?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.PaymentTransactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<PaymentTransaction?> FindByReceiptNumberAsync(string receiptNumber, CancellationToken ct = default) =>
        _db.PaymentTransactions.FirstOrDefaultAsync(t => t.ReceiptNumber == receiptNumber, ct);

    public Task<IReadOnlyList<PaymentTransaction>> FindByFeePaymentIdAsync(Guid feePaymentId, CancellationToken ct = default) =>
        _db.PaymentTransactions.Where(t => t.FeePaymentId == feePaymentId).OrderByDescending(t => t.PaidAtUtc).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<PaymentTransaction>)t.Result, ct);

    public Task<IReadOnlyList<PaymentTransaction>> FindByFeePaymentIdsAsync(IEnumerable<Guid> feePaymentIds, CancellationToken ct = default) =>
        _db.PaymentTransactions.Where(t => feePaymentIds.Contains(t.FeePaymentId)).OrderByDescending(t => t.PaidAtUtc).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<PaymentTransaction>)t.Result, ct);

    public async Task AddAsync(PaymentTransaction transaction, CancellationToken ct = default) =>
        await _db.PaymentTransactions.AddAsync(transaction, ct);

    public Task<int> SetReceiptBlobPathAsync(Guid id, string blobPath, CancellationToken ct = default) =>
        _db.PaymentTransactions.Where(t => t.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ReceiptBlobPath, blobPath), ct);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
