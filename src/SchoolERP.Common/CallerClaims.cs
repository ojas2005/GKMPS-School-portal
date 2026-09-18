using System.Security.Claims;

namespace SchoolERP.Common;

/// <summary>
/// Helpers for reading the caller's identity/role claims and enforcing per-student
/// data scoping consistently across every module. Student/Parent accounts carry
/// studentId/classId/sectionId claims (issued at login by the Identity module); staff/teacher accounts
/// do not, and are gated by role attributes instead.
/// </summary>
public static class CallerClaims
{
    public const string StudentIdClaim = "studentId";
    public const string ClassIdClaim = "classId";
    public const string SectionIdClaim = "sectionId";
    public const string StaffIdClaim = "staffId";
    public const string ClassTeacherOfClassIdClaim = "classTeacherOfClassId";
    public const string ClassTeacherOfSectionIdClaim = "classTeacherOfSectionId";
    /// <summary>The sign-in session an access token belongs to (see UserSession).</summary>
    public const string SessionIdClaim = "sid";
    /// <summary>A step the user must finish before using the app ("change-password", "setup-two-factor").</summary>
    public const string PendingActionClaim = "pending";

    public static string? PendingAction(this ClaimsPrincipal user) => user.FindFirst(PendingActionClaim)?.Value;

    public static string? Role(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value;

    public static Guid? StudentId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(StudentIdClaim)?.Value, out var id) ? id : null;

    public static string? ClassId(this ClaimsPrincipal user) =>
        user.FindFirst(ClassIdClaim)?.Value;

    public static string? SectionId(this ClaimsPrincipal user) =>
        user.FindFirst(SectionIdClaim)?.Value;

    public static Guid? StaffId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst(StaffIdClaim)?.Value, out var id) ? id : null;

    public static string? ClassTeacherOfClassId(this ClaimsPrincipal user) =>
        user.FindFirst(ClassTeacherOfClassIdClaim)?.Value;

    public static string? ClassTeacherOfSectionId(this ClaimsPrincipal user) =>
        user.FindFirst(ClassTeacherOfSectionIdClaim)?.Value;

    // The JWT handler maps "sid" to ClaimTypes.Sid on the way in (as it maps "sub" to
    // NameIdentifier), so look under both names.
    public static Guid? SessionId(this ClaimsPrincipal user) =>
        Guid.TryParse((user.FindFirst(SessionIdClaim) ?? user.FindFirst(ClaimTypes.Sid))?.Value, out var id) ? id : null;

    public static Guid? UserId(this ClaimsPrincipal user) =>
        Guid.TryParse((user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub"))?.Value, out var id) ? id : null;

    /// <summary>
    /// True if the caller may WRITE class-scoped records (e.g. mark attendance) for the
    /// given class: admin/principal always may; a Teacher only for the class they are
    /// class teacher (head teacher) of; everyone else never.
    /// </summary>
    public static bool CanManageClass(this ClaimsPrincipal user, string? classId)
    {
        var role = user.Role();
        if (role == RoleNames.SuperAdmin || role == RoleNames.Principal || role == RoleNames.Admin)
            return true;
        if (role != RoleNames.Teacher) return false;
        var own = user.ClassTeacherOfClassId();
        return !string.IsNullOrEmpty(own) && own == classId;
    }

    /// <summary>True if the caller may read the given staff member's own records (payouts, staff attendance).</summary>
    public static bool CanAccessStaff(this ClaimsPrincipal user, Guid staffId)
    {
        var role = user.Role();
        if (role == RoleNames.SuperAdmin || role == RoleNames.Principal || role == RoleNames.Admin)
            return true;
        var own = user.StaffId();
        return own.HasValue && own.Value == staffId;
    }

    /// <summary>Roles that may only ever see their OWN data.</summary>
    public static bool IsSelfServiceRole(this ClaimsPrincipal user)
    {
        var role = user.Role();
        return role == RoleNames.Student || role == RoleNames.Parent;
    }

    /// <summary>
    /// Self-service check only: a student/parent passes only for their own id, and every staff
    /// role passes. For what staff may read about a student, use <see cref="StudentAccess"/>.
    /// </summary>
    public static bool CanAccessStudent(this ClaimsPrincipal user, Guid studentId)
    {
        if (!user.IsSelfServiceRole()) return true;
        var own = user.StudentId();
        return own.HasValue && own.Value == studentId;
    }

    /// <summary>True if the caller may read data scoped to the given class.</summary>
    public static bool CanAccessClass(this ClaimsPrincipal user, string? classId)
    {
        if (!user.IsSelfServiceRole()) return true;
        var own = user.ClassId();
        return !string.IsNullOrEmpty(own) && own == classId;
    }
}
