namespace SchoolERP.Attendance.Repositories.Interfaces;

public interface IMonthlyAttendanceSummaryRepository
{
    Task<bool> ExistsAsync(Guid studentId, int year, int month, CancellationToken ct = default);
    Task CreateEmptyAsync(Guid studentId, int year, int month, CancellationToken ct = default);

    /// <summary>Atomic counter bump -- ExecuteUpdateAsync, never load-then-save. status is Present|Absent|Late.</summary>
    Task<int> IncrementCounterAsync(Guid studentId, int year, int month, string status, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
