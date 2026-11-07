namespace SchoolERP.Common;

/// <summary>
/// The school's calendar date. The server runs in UTC, but a fee falls due on a date in
/// India: between midnight and 05:30 IST the UTC date is still yesterday. India has no
/// daylight saving, so a fixed +05:30 is exact (and needs no time-zone database in the image).
/// </summary>
public static class SchoolClock
{
    public static readonly TimeSpan IndiaOffset = TimeSpan.FromMinutes(330);

    public static DateTime Today(TimeProvider clock) => clock.GetUtcNow().ToOffset(IndiaOffset).Date;

    /// <summary>The school date of a UTC instant (for "paid in which month").</summary>
    public static DateTime DateOf(DateTime utc) => (DateTime.SpecifyKind(utc, DateTimeKind.Utc) + IndiaOffset).Date;
}
