using SchoolERP.DataAccess.Fee.Entities;

namespace SchoolERP.DataAccess.Fee.Repositories.Interfaces;

public interface IFeeAccountRepository
{
    Task<StudentFeeAccount?> FindByStudentAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>Every account, open or closed (the billing job checks closed ones against the
    /// student's status in case a reopen was missed).</summary>
    Task<IReadOnlyList<StudentFeeAccount>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Which of these students already have an account.</summary>
    Task<IReadOnlySet<Guid>> StudentsWithAccountsAsync(IReadOnlyCollection<Guid> studentIds, CancellationToken ct = default);

    Task AddAsync(StudentFeeAccount account, CancellationToken ct = default);

    /// <summary>Hard delete, for undoing an admission that didn't complete.</summary>
    Task<int> DeleteAsync(Guid studentId, CancellationToken ct = default);

    Task AddAdjustmentAsync(FeeAdjustment adjustment, CancellationToken ct = default);
    Task<IReadOnlyList<FeeAdjustment>> GetAdjustmentsAsync(Guid studentId, CancellationToken ct = default);
    Task<int> DeleteAdjustmentsAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>Forgets inserts that failed to save, so the next SaveChanges doesn't retry them.</summary>
    void DiscardUnsavedInserts();

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
