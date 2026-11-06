using System.Text.RegularExpressions;

namespace SchoolERP.Common;

/// <summary>
/// Academic years in one spelling, "2026-27". The session starts in April, as in Indian schools.
/// Fee structures were once typed in by hand ("2026-2027", "2026/27", "2026 - 27"), so every
/// comparison goes through <see cref="Normalize"/>.
/// </summary>
public static partial class AcademicYears
{
    public const int FirstMonth = 4; // April

    /// <summary>The academic year a date falls in: 1 Apr 2026 to 31 Mar 2027 is "2026-27".</summary>
    public static string Current(DateTime date)
    {
        var start = date.Month >= FirstMonth ? date.Year : date.Year - 1;
        return Format(start);
    }

    public static string Format(int startYear) => $"{startYear}-{(startYear + 1) % 100:00}";

    /// <summary>
    /// "2026-27" for "2026-27", "2026-2027", "2026/27", "2026 – 27" or "2026"; anything else is
    /// returned trimmed, unchanged.
    /// </summary>
    public static string Normalize(string? value)
    {
        var text = (value ?? "").Trim();
        var m = YearPattern().Match(text);
        if (!m.Success) return text;
        var start = int.Parse(m.Groups[1].Value);
        if (m.Groups[2].Success)
        {
            var end = m.Groups[2].Value;
            var endYear = end.Length == 2 ? start / 100 * 100 + int.Parse(end) : int.Parse(end);
            if (endYear < start) endYear += 100; // "1999-00"
            if (endYear != start + 1) return text; // not a single session; leave as typed
        }
        return Format(start);
    }

    public static bool Same(string? a, string? b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^(\d{4})(?:\s*[-–/]\s*(\d{2}|\d{4}))?$")]
    private static partial Regex YearPattern();
}
