namespace SchoolERP.Academic.Constants;

/// <summary>
/// The fixed bell schedule and calendar the auto-generator stamps onto every slot.
/// 8 teaching periods of 40 minutes with a 40-minute lunch after period 4 =
/// exactly 08:00-14:00. The same schedule repeats Monday-Saturday (Sunday off).
/// Kept here as the single source of truth so the generator and any display agree.
/// </summary>
public static class TimetableConstants
{
    public record PeriodTime(int Period, string StartTime, string EndTime);

    public static readonly IReadOnlyList<PeriodTime> Periods = new List<PeriodTime>
    {
        new(1, "08:00", "08:40"),
        new(2, "08:40", "09:20"),
        new(3, "09:20", "10:00"),
        new(4, "10:00", "10:40"),
        // Lunch 10:40-11:20 (after period 4)
        new(5, "11:20", "12:00"),
        new(6, "12:00", "12:40"),
        new(7, "12:40", "13:20"),
        new(8, "13:20", "14:00"),
    };

    public const int LunchAfterPeriod = 4;
    public const string LunchStart = "10:40";
    public const string LunchEnd = "11:20";

    public static readonly IReadOnlyList<string> TeachingDays = new[]
    {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    };

    /// <summary>v1 schedules one section per class -- Section A (matches the frontend catalog).</summary>
    public const string DefaultSectionId = "5ec00000-0000-4000-8000-00000000000a";
}
