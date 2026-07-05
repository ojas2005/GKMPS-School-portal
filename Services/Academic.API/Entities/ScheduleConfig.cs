using SchoolERP.Shared.Entities;

namespace SchoolERP.Academic.Entities;

/// <summary>
/// The owner's timetable-generation setup: the two teacher groups (junior/senior) with
/// each teacher's subject, daily period cap and covered classes, plus the pre-primary
/// class-teacher assignments. Stored as one JSON blob (single row for the school) and
/// re-loaded/edited/regenerated from it. Mirrors the JSONB pattern used by Timetable.
/// </summary>
public class ScheduleConfig : BaseEntity
{
    public required string ConfigJson { get; set; }
}
