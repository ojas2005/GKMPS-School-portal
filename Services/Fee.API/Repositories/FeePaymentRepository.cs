using Microsoft.EntityFrameworkCore;
using SchoolERP.Fee.Data;
using SchoolERP.Fee.Entities;
using SchoolERP.Fee.Repositories.Interfaces;

namespace SchoolERP.Fee.Repositories;

public class FeePaymentRepository : IFeePaymentRepository
{
    private readonly FeeDbContext _db;

    public FeePaymentRepository(FeeDbContext db) => _db = db;

    public Task<FeePayment?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.FeePayments.Include(p => p.FeeStructure).FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<FeePayment?> FindByStudentAndStructureAsync(Guid studentId, Guid feeStructureId, CancellationToken ct = default) =>
        _db.FeePayments.FirstOrDefaultAsync(p => p.StudentId == studentId && p.FeeStructureId == feeStructureId, ct);

    public Task<IReadOnlyList<FeePayment>> FindByStudentAsync(Guid studentId, CancellationToken ct = default) =>
        _db.FeePayments.Include(p => p.FeeStructure).Where(p => p.StudentId == studentId).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<FeePayment>)t.Result, ct);

    public Task<IReadOnlyList<FeePayment>> FindByClassAsync(string classId, CancellationToken ct = default) =>
        _db.FeePayments.Where(p => p.ClassId == classId).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<FeePayment>)t.Result, ct);

    public async Task AddAsync(FeePayment payment, CancellationToken ct = default) =>
        await _db.FeePayments.AddAsync(payment, ct);

    public async Task AddRangeAsync(IEnumerable<FeePayment> payments, CancellationToken ct = default) =>
        await _db.FeePayments.AddRangeAsync(payments, ct);

    public Task<int> IncrementPaidAmountAsync(Guid feePaymentId, decimal amount, CancellationToken ct = default) =>
        _db.FeePayments.Where(p => p.Id == feePaymentId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.PaidAmount, p => p.PaidAmount + amount)
                .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow), ct);

    public Task<int> MarkWaiverRequestedAsync(Guid feePaymentId, CancellationToken ct = default) =>
        _db.FeePayments.Where(p => p.Id == feePaymentId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.IsWaiverRequested, true), ct);

    public Task<int> ApproveWaiverAsync(Guid feePaymentId, decimal waiverAmount, Guid approvedByUserId, CancellationToken ct = default) =>
        _db.FeePayments.Where(p => p.Id == feePaymentId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.IsWaiverApproved, true)
                .SetProperty(p => p.WaiverAmount, waiverAmount)
                .SetProperty(p => p.WaiverApprovedByUserId, approvedByUserId), ct);

    public async Task<decimal> GetTotalCollectedAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        // Aggregate query via SumAsync executed in PostgreSQL -- never pulled into app memory.
        var query = _db.PaymentTransactions.AsQueryable();
        if (fromUtc.HasValue) query = query.Where(t => t.PaidAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(t => t.PaidAtUtc <= toUtc.Value);

        return await query.AnyAsync(ct) ? await query.SumAsync(t => t.Amount, ct) : 0m;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
