namespace SchoolERP.Common;

/// <summary>A class the school teaches, in the order the school counts them (PG first).</summary>
public sealed record SchoolClass(string Id, string Name, int Order);

/// <summary>
/// The school's classes. The ids are the same stable GUIDs the frontend uses
/// (core/constants/classes.ts): students, fees, exams and attendance store them as plain
/// strings, so both sides must agree on them.
/// </summary>
public static class SchoolClasses
{
    public static readonly IReadOnlyList<SchoolClass> All =
    [
        new("c1a55001-0000-4000-8000-000000000001", "PG", 1),
        new("c1a55002-0000-4000-8000-000000000002", "Nursery", 2),
        new("c1a55003-0000-4000-8000-000000000003", "LKG", 3),
        new("c1a55004-0000-4000-8000-000000000004", "UKG", 4),
        new("c1a55005-0000-4000-8000-000000000005", "Class 1", 5),
        new("c1a55006-0000-4000-8000-000000000006", "Class 2", 6),
        new("c1a55007-0000-4000-8000-000000000007", "Class 3", 7),
        new("c1a55008-0000-4000-8000-000000000008", "Class 4", 8),
        new("c1a55009-0000-4000-8000-000000000009", "Class 5", 9),
        new("c1a55010-0000-4000-8000-000000000010", "Class 6", 10),
        new("c1a55011-0000-4000-8000-000000000011", "Class 7", 11),
        new("c1a55012-0000-4000-8000-000000000012", "Class 8", 12),
        new("c1a55013-0000-4000-8000-000000000013", "Class 9", 13),
        new("c1a55014-0000-4000-8000-000000000014", "Class 10", 14),
    ];

    public static SchoolClass? Find(string? classId) =>
        All.FirstOrDefault(c => string.Equals(c.Id, classId, StringComparison.OrdinalIgnoreCase));

    public static string NameOf(string? classId) => Find(classId)?.Name ?? classId ?? "-";

    public static bool IsKnown(string? classId) => Find(classId) is not null;
}
