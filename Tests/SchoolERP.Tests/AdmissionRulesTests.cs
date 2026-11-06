using SchoolERP.Common;

namespace SchoolERP.Tests;

public class AdmissionRulesTests
{
    [Theory]
    [InlineData("2026-27", "2026-27")]
    [InlineData("2026-2027", "2026-27")]
    [InlineData("2026/27", "2026-27")]
    [InlineData(" 2026 – 27 ", "2026-27")]
    [InlineData("2026", "2026-27")]
    [InlineData("1999-00", "1999-00")]
    [InlineData("2026-28", "2026-28")]      // not one session: left as typed
    [InlineData("Session A", "Session A")]
    public void Academic_years_are_matched_however_they_were_typed(string typed, string expected) =>
        Assert.Equal(expected, AcademicYears.Normalize(typed));

    [Theory]
    [InlineData(2026, 4, 1, "2026-27")]
    [InlineData(2027, 3, 31, "2026-27")]
    [InlineData(2026, 11, 7, "2026-27")]
    [InlineData(2027, 1, 15, "2026-27")]
    [InlineData(2026, 3, 31, "2025-26")]
    public void The_school_year_starts_in_April(int y, int m, int d, string expected) =>
        Assert.Equal(expected, AcademicYears.Current(new DateTime(y, m, d)));

    [Fact]
    public void A_fee_structure_typed_as_2026_2027_counts_for_2026_27() =>
        Assert.True(AcademicYears.Same("2026-2027", "2026-27"));

    [Theory]
    [InlineData("9876543210", "9876543210")]
    [InlineData("98765 43210", "9876543210")]
    [InlineData("+91-98765-43210", "9876543210")]
    [InlineData("098765 43210", "9876543210")]
    [InlineData("1234567890", null)]   // mobile numbers start 6-9
    [InlineData("98765", null)]
    public void Mobile_numbers_are_stored_as_ten_digits(string typed, string? expected) =>
        Assert.Equal(expected, Phones.NormalizeMobile(typed));

    [Theory]
    [InlineData("current", RosterFilter.Current)]
    [InlineData(null, RosterFilter.Current)]
    [InlineData("REMOVED", RosterFilter.Removed)]
    [InlineData("all", RosterFilter.All)]
    [InlineData("nonsense", RosterFilter.Current)]
    public void Lists_show_current_people_unless_asked_otherwise(string? value, RosterFilter expected) =>
        Assert.Equal(expected, RosterFilters.Parse(value));
}
