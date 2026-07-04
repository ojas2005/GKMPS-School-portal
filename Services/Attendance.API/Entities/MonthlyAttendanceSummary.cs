using SchoolERP.Shared.Entities;

namespace SchoolERP.Attendance.Entities;

/// <summary>
/// Rolled-up per-student, per-month counters. PresentCount/AbsentCount/LateCount are
/// bumped atomically via ExecuteUpdateAsync every time a daily record is marked --
/// never recomputed by loading and re-summing every AttendanceRecord row.
/// </summary>
public class MonthlyAttendanceSummary : BaseEntity
{
    public Guid StudentId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public int TotalMarkedDays { get; set; }
}
