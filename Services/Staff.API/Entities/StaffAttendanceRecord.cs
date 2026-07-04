using SchoolERP.Shared.Entities;

namespace SchoolERP.Staff.Entities;

/// <summary>
/// One row per staff member per calendar day, mirroring Attendance.API's student
/// records. The (StaffId, Date) unique index guarantees one record per day.
/// </summary>
public class StaffAttendanceRecord : BaseEntity
{
    public Guid StaffId { get; set; }
    public StaffProfile? Staff { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>Present | Absent | Late | HalfDay | OnLeave.</summary>
    public string Status { get; set; } = "Present";

    public Guid MarkedByUserId { get; set; }
    public DateTime MarkedAtUtc { get; set; } = DateTime.UtcNow;
}
