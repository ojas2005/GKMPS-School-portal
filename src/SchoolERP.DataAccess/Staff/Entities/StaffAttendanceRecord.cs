using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Staff.Entities;

/// <summary>
/// One row per staff member per calendar day, mirroring the Attendance module's student
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
