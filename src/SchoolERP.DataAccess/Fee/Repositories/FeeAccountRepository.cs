using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Fee.Entities;
using SchoolERP.DataAccess.Fee.Repositories.Interfaces;

namespace SchoolERP.DataAccess.Fee.Repositories;

public class FeeAccountRepository : IFeeAccountRepository
{
    private readonly FeeDbContext _db;

    public FeeAccountRepository(FeeDbContext db) => _db = db;

    public Task<StudentFeeAccount?> FindByStudentAsync(Guid studentId, CancellationToken ct = default) =>
        _db.StudentFeeAccounts.FirstOrDefaultAsync(a => a.StudentId == studentId, ct);

    public async Task<IReadOnlyList<StudentFeeAccount>> GetAllAsync(CancellationToken ct = default) =>
        await _db.StudentFeeAccounts.ToListAsync(ct);

    public async Task<IReadOnlySet<Guid>> StudentsWithAccountsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default)
    {
        if (studentIds.Count == 0) return new HashSet<Guid>();
        var ids = await _db.StudentFeeAccounts.Where(a => studentIds.Contains(a.StudentId)).Select(a => a.StudentId).ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task AddAsync(StudentFeeAccount account, CancellationToken ct = default) =>
        await _db.StudentFeeAccounts.AddAsync(account, ct);

    public Task<int> DeleteAsync(Guid studentId, CancellationToken ct = default) =>
        _db.StudentFeeAccounts.Where(a => a.StudentId == studentId).ExecuteDeleteAsync(ct);

    public async Task AddAdjustmentAsync(FeeAdjustment adjustment, CancellationToken ct = default) =>
        await _db.FeeAdjustments.AddAsync(adjustment, ct);

    public async Task<IReadOnlyList<FeeAdjustment>> GetAdjustmentsAsync(Guid studentId, CancellationToken ct = default) =>
        await _db.FeeAdjustments.Where(a => a.StudentId == studentId).OrderByDescending(a => a.CreatedAtUtc).ToListAsync(ct);

    public Task<int> DeleteAdjustmentsAsync(Guid studentId, CancellationToken ct = default) =>
        _db.FeeAdjustments.Where(a => a.StudentId == studentId).ExecuteDeleteAsync(ct);

    public void DiscardUnsavedInserts()
    {
        foreach (var entry in _db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList())
            entry.State = EntityState.Detached;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
