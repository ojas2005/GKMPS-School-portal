namespace SchoolERP.Shared.Common;

/// <summary>
/// Canonical role names shared by every service's [Authorize(Roles = ...)] attributes
/// and by Identity.API's JWT role claims, so the string literals live in exactly one place.
/// </summary>
public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Principal = "Principal";
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string Parent = "Parent";
    public const string Accountant = "Accountant";
    public const string Librarian = "Librarian";

    public static readonly string[] All =
    {
        SuperAdmin, Principal, Admin, Teacher, Student, Parent, Accountant, Librarian
    };
}
