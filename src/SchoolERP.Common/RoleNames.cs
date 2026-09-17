namespace SchoolERP.Common;

/// <summary>
/// Canonical role names shared by every controller's [Authorize(Roles = ...)] attributes
/// and by the Identity module's JWT role claims, so the string literals live in exactly one place.
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

    /// <summary>
    /// Administrative rank: SuperAdmin (the owner) &gt; Principal &gt; Admin &gt; everyone else.
    /// Account-management actions (create, reset password, activate/deactivate) are only
    /// allowed downward, so an Admin can never mint a SuperAdmin or take over the owner.
    /// </summary>
    public static int Rank(string? role) => role switch
    {
        SuperAdmin => 3,
        Principal => 2,
        Admin => 1,
        _ => 0
    };

    /// <summary>
    /// True if <paramref name="actorRole"/> may create or administer an account holding
    /// <paramref name="targetRole"/>. The owner may manage every role (including other
    /// SuperAdmins); everyone else only roles strictly below their own.
    /// </summary>
    public static bool CanManageRole(string? actorRole, string? targetRole) =>
        actorRole == SuperAdmin || (Rank(actorRole) > 0 && Rank(actorRole) > Rank(targetRole));
}
