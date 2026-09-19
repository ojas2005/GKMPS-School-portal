using SchoolERP.Business.Staff.DTOs;

namespace SchoolERP.Business.Staff.Services.Interfaces;

public interface IPayrollService
{
    Task<PayoutSummary> RecordPayoutAsync(RecordPayoutRequest request, Guid recordedByUserId, CancellationToken ct = default);
    Task<IReadOnlyList<PayoutSummary>> GetPayoutsForStaffAsync(Guid staffId, CancellationToken ct = default);

    Task<StaffAttendanceSummary> MarkStaffAttendanceAsync(MarkStaffAttendanceRequest request, Guid markedByUserId, CancellationToken ct = default);
    Task<IReadOnlyList<StaffAttendanceSummary>> GetStaffAttendanceAsync(Guid staffId, DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Pending = MonthlySalary minus payouts recorded in the current calendar month, clamped at 0.</summary>
    Task<PendingSalaryResponse> GetPendingSalaryAsync(Guid staffId, CancellationToken ct = default);
}
