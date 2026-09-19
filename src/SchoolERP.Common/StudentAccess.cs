using System.Security.Claims;

namespace SchoolERP.Common;

/// <summary>The kinds of information held about a student, from least to most sensitive.</summary>
public enum StudentRecord
{
    /// <summary>Name, admission number, class/section and status -- enough to find the right child.</summary>
    Directory,
    /// <summary>Directory plus the parent's name, phone and email (fee follow-up).</summary>
    Contact,
    /// <summary>Everything on the profile: date of birth, gender, address, contacts.</summary>
    Profile,
    Attendance,
    Results,
    Fees,
    Certificates
}

/// <summary>
/// Who may see what about a student -- need-to-know, as data-protection law expects:
///
///   Owner, Principal, Admin      everything
///   Class teacher (own class)    profile, attendance, results (not fees)
///   Any other teacher            directory only
///   Accountant                   directory, parent contact, fees
///   Librarian                    directory only
///   Student / Parent             everything about their own record, nothing else
/// </summary>
public static class StudentAccess
{
    public static bool CanRead(this ClaimsPrincipal user, StudentRecord record, Guid studentId, string? studentClassId, string? studentSectionId)
    {
        var role = user.Role();
        if (role is RoleNames.SuperAdmin or RoleNames.Principal or RoleNames.Admin)
            return true;

        if (user.IsSelfServiceRole())
            return user.StudentId() == studentId;

        if (record == StudentRecord.Directory)
            return role is RoleNames.Teacher or RoleNames.Accountant or RoleNames.Librarian;

        return role switch
        {
            RoleNames.Accountant => record is StudentRecord.Contact or StudentRecord.Fees,
            RoleNames.Teacher => record is StudentRecord.Contact or StudentRecord.Profile or StudentRecord.Attendance or StudentRecord.Results
                                 && IsClassTeacherOf(user, studentClassId, studentSectionId),
            _ => false
        };
    }

    /// <summary>The most detailed view of a student's profile this caller may see.</summary>
    public static StudentRecord? ProfileView(this ClaimsPrincipal user, Guid studentId, string? classId, string? sectionId) =>
        user.CanRead(StudentRecord.Profile, studentId, classId, sectionId) ? StudentRecord.Profile
        : user.CanRead(StudentRecord.Contact, studentId, classId, sectionId) ? StudentRecord.Contact
        : user.CanRead(StudentRecord.Directory, studentId, classId, sectionId) ? StudentRecord.Directory
        : null;

    /// <summary>
    /// Whether answering needs the student's class (only a teacher's access depends on it) --
    /// lets callers skip loading the student for everyone else.
    /// </summary>
    public static bool NeedsStudentClass(this ClaimsPrincipal user) => user.Role() == RoleNames.Teacher;

    private static bool IsClassTeacherOf(ClaimsPrincipal user, string? classId, string? sectionId)
    {
        var ownClass = user.ClassTeacherOfClassId();
        var ownSection = user.ClassTeacherOfSectionId();
        return !string.IsNullOrEmpty(ownClass) && ownClass == classId
            && (string.IsNullOrEmpty(ownSection) || ownSection == sectionId);
    }
}
