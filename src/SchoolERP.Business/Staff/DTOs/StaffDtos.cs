using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Business.Staff.DTOs;

public record CreateStaffRequest(
    [Required] Guid LinkedUserId, [Required] string EmployeeCode, [Required] string FullName,
    [Required] string Designation, string? SubjectsTaughtCsv, string? Phone, [EmailAddress] string? Email,
    // Set when this teacher is the class teacher (head teacher) of a class/section.
    string? ClassTeacherOfClassId = null, string? ClassTeacherOfSectionId = null,
    // The owner defines this at onboarding; drives the teacher's pending-salary readout.
    decimal? MonthlySalary = null);

public record StaffSummary(
    Guid Id, Guid LinkedUserId, string EmployeeCode, string FullName, string Designation, string Status,
    DateTime DateOfJoiningUtc, string? SubjectsTaughtCsv, string? Phone, string? Email,
    string? ClassTeacherOfClassId, string? ClassTeacherOfSectionId, decimal? MonthlySalary);

public record AssignClassTeacherRequest(string? ClassId, string? SectionId);

public record SetSalaryRequest([Required, Range(0, 100_000_000)] decimal MonthlySalary);

// Pending = MonthlySalary minus payouts recorded in the current calendar month (clamped
// at 0). Once the owner records a payout covering the month, this reads 0.
public record PendingSalaryResponse(Guid StaffId, decimal? MonthlySalary, decimal PaidThisMonth, decimal PendingSalary, string PeriodLabel);

public record RequestLeaveRequest([Required] string LeaveType, [Required] DateTime FromDateUtc, [Required] DateTime ToDateUtc, [Required] string Reason);

public record DecideLeaveRequest([Required] bool Approve, string? Note);

public record LeaveRequestSummary(
    Guid Id, Guid StaffId, string LeaveType, DateTime FromDateUtc, DateTime ToDateUtc,
    bool IsSubmitted, bool IsApproved, bool IsRejected);

public record RecordPayoutRequest(
    [Required] Guid StaffId, [Required, Range(0.01, 100_000_000)] decimal Amount,
    [Required] string PeriodLabel, string? Method, string? Reference, string? Notes,
    DateTime? PaidOnUtc = null);

public record PayoutSummary(
    Guid Id, Guid StaffId, decimal Amount, string PeriodLabel, DateTime PaidOnUtc,
    string Method, string? Reference, string? Notes);

public record MarkStaffAttendanceRequest([Required] Guid StaffId, [Required] DateOnly Date, [Required] string Status);

public record StaffAttendanceSummary(Guid Id, Guid StaffId, DateOnly Date, string Status, DateTime MarkedAtUtc);
