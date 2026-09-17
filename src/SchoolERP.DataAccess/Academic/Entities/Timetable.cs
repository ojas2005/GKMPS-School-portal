using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Academic.Entities;

/// <summary>
/// One Timetable row per class/section. SlotsJson holds a variable-shape array of
/// { day, period, subjectId, teacherStaffId, startTime, endTime } serialized with
/// System.Text.Json -- deserialized/validated in the service layer, never queried
/// column-by-column, since the slot count/shape varies per class.
/// </summary>
public class Timetable : BaseEntity
{
    public required string ClassId { get; set; }
    public required string SectionId { get; set; }
    public required string SlotsJson { get; set; }
    public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;
}
