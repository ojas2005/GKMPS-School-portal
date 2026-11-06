namespace SchoolERP.Common;

/// <summary>Which people a student or staff list shows.</summary>
public enum RosterFilter
{
    /// <summary>Still at the school (the default everywhere: registers, fee lists, timetables).</summary>
    Current,
    /// <summary>Removed, or left with a transfer certificate.</summary>
    Removed,
    All
}

public static class RosterFilters
{
    /// <summary>"current" (or nothing), "removed" or "all".</summary>
    public static RosterFilter Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "removed" => RosterFilter.Removed,
        "all" => RosterFilter.All,
        _ => RosterFilter.Current
    };
}
