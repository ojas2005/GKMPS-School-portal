using SchoolERP.Shared.Entities;

namespace SchoolERP.Attendance.Entities;

/// <summary>
/// One row per student per calendar day. The (StudentId, Date) unique index is the
/// data-integrity guarantee behind "one attendance record per student per day" and
/// backs the HasMarkedAttendanceToday() duplicate-prevention check.
/// </summary>
public class AttendanceRecord : BaseEntity
{
    public Guid StudentId { get; set; }
    public required string ClassId { get; set; }
    public required string SectionId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Present | Absent | Late | HalfDay | Excused.</summary>
    public string Status { get; set; } = "Present";

    public TimeOnly? ArrivalTime { get; set; }
    public bool IsLate { get; set; } = false;

    public Guid MarkedByUserId { get; set; }
    public DateTime MarkedAtUtc { get; set; } = DateTime.UtcNow;
}
