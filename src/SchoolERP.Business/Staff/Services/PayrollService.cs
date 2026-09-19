using SchoolERP.Business.Staff.DTOs;
using SchoolERP.DataAccess.Staff.Entities;
using SchoolERP.DataAccess.Staff.Repositories.Interfaces;
using SchoolERP.Business.Staff.Services.Interfaces;

namespace SchoolERP.Business.Staff.Services;

/// <summary>
/// Payout ledger + staff attendance. Payouts are recorded by the owner/admin and are
/// immutable entries; staff attendance mirrors the student pattern (one row per staff
/// member per day, duplicate-prevention guard before insert).
/// </summary>
public class PayrollService : IPayrollService
{
    private readonly IPayoutRepository _payouts;
    private readonly IStaffAttendanceRepository _attendance;
    private readonly IStaffRepository _staff;
    private readonly ILogger<PayrollService> _logger;

    public PayrollService(IPayoutRepository payouts, IStaffAttendanceRepository attendance, IStaffRepository staff, ILogger<PayrollService> logger)
    {
        _payouts = payouts;
        _attendance = attendance;
        _staff = staff;
        _logger = logger;
    }

    public async Task<PayoutSummary> RecordPayoutAsync(RecordPayoutRequest request, Guid recordedByUserId, CancellationToken ct = default)
    {
        _ = await _staff.FindByIdAsync(request.StaffId, ct)
            ?? throw new KeyNotFoundException("Staff member not found.");

        var payout = new Payout
        {
            StaffId = request.StaffId,
            Amount = request.Amount,
            PeriodLabel = request.PeriodLabel,
            PaidOnUtc = request.PaidOnUtc ?? DateTime.UtcNow,
            Method = string.IsNullOrWhiteSpace(request.Method) ? "BankTransfer" : request.Method,
            Reference = request.Reference,
            Notes = request.Notes,
            RecordedByUserId = recordedByUserId
        };

        await _payouts.AddAsync(payout, ct);
        await _payouts.SaveChangesAsync(ct);

        _logger.LogInformation("Payout recorded: staff={StaffId} amount={Amount} period={Period}",
            payout.StaffId, payout.Amount, payout.PeriodLabel);

        return ToSummary(payout);
    }

    public async Task<IReadOnlyList<PayoutSummary>> GetPayoutsForStaffAsync(Guid staffId, CancellationToken ct = default)
    {
        var payouts = await _payouts.FindByStaffAsync(staffId, ct);
        return payouts.Select(ToSummary).ToList();
    }

    public async Task<StaffAttendanceSummary> MarkStaffAttendanceAsync(MarkStaffAttendanceRequest request, Guid markedByUserId, CancellationToken ct = default)
    {
        _ = await _staff.FindByIdAsync(request.StaffId, ct)
            ?? throw new KeyNotFoundException("Staff member not found.");

        if (await _attendance.HasMarkedForDateAsync(request.StaffId, request.Date, ct))
            throw new InvalidOperationException("Attendance has already been marked for this staff member today.");

        var record = new StaffAttendanceRecord
        {
            StaffId = request.StaffId,
            Date = request.Date,
            Status = request.Status,
            MarkedByUserId = markedByUserId
        };

        await _attendance.AddAsync(record, ct);
        await _attendance.SaveChangesAsync(ct);

        return ToSummary(record);
    }

    public async Task<IReadOnlyList<StaffAttendanceSummary>> GetStaffAttendanceAsync(Guid staffId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var records = await _attendance.FindByStaffAndRangeAsync(staffId, from, to, ct);
        return records.Select(ToSummary).ToList();
    }

    public async Task<PendingSalaryResponse> GetPendingSalaryAsync(Guid staffId, CancellationToken ct = default)
    {
        var staff = await _staff.FindByIdAsync(staffId, ct)
            ?? throw new KeyNotFoundException("Staff member not found.");

        var now = DateTime.UtcNow;
        var paidThisMonth = await _payouts.GetTotalForMonthAsync(staffId, now.Year, now.Month, ct);
        var pending = Math.Max(0, (staff.MonthlySalary ?? 0) - paidThisMonth);
        var periodLabel = now.ToString("MMMM yyyy");

        return new PendingSalaryResponse(staffId, staff.MonthlySalary, paidThisMonth, pending, periodLabel);
    }

    private static PayoutSummary ToSummary(Payout p) =>
        new(p.Id, p.StaffId, p.Amount, p.PeriodLabel, p.PaidOnUtc, p.Method, p.Reference, p.Notes);

    private static StaffAttendanceSummary ToSummary(StaffAttendanceRecord a) =>
        new(a.Id, a.StaffId, a.Date, a.Status, a.MarkedAtUtc);
}
