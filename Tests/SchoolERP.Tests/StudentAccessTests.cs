using System.Security.Claims;
using SchoolERP.Common;

namespace SchoolERP.Tests;

public class StudentAccessTests
{
    private static readonly Guid Child = Guid.NewGuid();
    private const string ClassA = "class-10", SectionA = "A";

    private static ClaimsPrincipal As(string role, Guid? studentId = null, string? classTeacherOf = null, string? section = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (studentId is not null) claims.Add(new(CallerClaims.StudentIdClaim, studentId.ToString()!));
        if (classTeacherOf is not null) claims.Add(new(CallerClaims.ClassTeacherOfClassIdClaim, classTeacherOf));
        if (section is not null) claims.Add(new(CallerClaims.ClassTeacherOfSectionIdClaim, section));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static bool Can(ClaimsPrincipal user, StudentRecord record) => user.CanRead(record, Child, ClassA, SectionA);

    [Theory]
    [InlineData(RoleNames.SuperAdmin)]
    [InlineData(RoleNames.Principal)]
    [InlineData(RoleNames.Admin)]
    public void Office_roles_see_everything(string role)
    {
        Assert.All(Enum.GetValues<StudentRecord>(), r => Assert.True(Can(As(role), r)));
    }

    [Fact]
    public void The_librarian_sees_only_who_and_where_the_child_is()
    {
        var librarian = As(RoleNames.Librarian);
        Assert.True(Can(librarian, StudentRecord.Directory));
        Assert.All(new[] { StudentRecord.Contact, StudentRecord.Profile, StudentRecord.Attendance, StudentRecord.Results, StudentRecord.Fees, StudentRecord.Certificates },
            r => Assert.False(Can(librarian, r)));
    }

    [Fact]
    public void The_accountant_sees_parent_contact_and_fees_but_not_grades_or_attendance()
    {
        var accountant = As(RoleNames.Accountant);
        Assert.True(Can(accountant, StudentRecord.Contact));
        Assert.True(Can(accountant, StudentRecord.Fees));
        Assert.False(Can(accountant, StudentRecord.Profile));
        Assert.False(Can(accountant, StudentRecord.Results));
        Assert.False(Can(accountant, StudentRecord.Attendance));
    }

    [Fact]
    public void A_class_teacher_sees_their_own_class_but_not_fees()
    {
        var classTeacher = As(RoleNames.Teacher, classTeacherOf: ClassA, section: SectionA);
        Assert.True(Can(classTeacher, StudentRecord.Profile));
        Assert.True(Can(classTeacher, StudentRecord.Attendance));
        Assert.True(Can(classTeacher, StudentRecord.Results));
        Assert.False(Can(classTeacher, StudentRecord.Fees));
    }

    [Fact]
    public void A_teacher_of_another_class_or_section_sees_only_the_directory()
    {
        foreach (var teacher in new[] { As(RoleNames.Teacher), As(RoleNames.Teacher, classTeacherOf: "class-9", section: SectionA), As(RoleNames.Teacher, classTeacherOf: ClassA, section: "B") })
        {
            Assert.True(Can(teacher, StudentRecord.Directory));
            Assert.False(Can(teacher, StudentRecord.Profile));
            Assert.False(Can(teacher, StudentRecord.Results));
            Assert.False(Can(teacher, StudentRecord.Attendance));
        }
    }

    [Theory]
    [InlineData(RoleNames.Student)]
    [InlineData(RoleNames.Parent)]
    public void Students_and_parents_see_all_of_their_own_record_and_nothing_else(string role)
    {
        Assert.All(Enum.GetValues<StudentRecord>(), r => Assert.True(Can(As(role, studentId: Child), r)));
        Assert.All(Enum.GetValues<StudentRecord>(), r => Assert.False(As(role, studentId: Guid.NewGuid()).CanRead(r, Child, ClassA, SectionA)));
    }

    [Fact]
    public void Profiles_are_trimmed_to_the_callers_view()
    {
        Assert.Equal(StudentRecord.Directory, As(RoleNames.Librarian).ProfileView(Child, ClassA, SectionA));
        Assert.Equal(StudentRecord.Contact, As(RoleNames.Accountant).ProfileView(Child, ClassA, SectionA));
        Assert.Equal(StudentRecord.Profile, As(RoleNames.Teacher, classTeacherOf: ClassA, section: SectionA).ProfileView(Child, ClassA, SectionA));
        Assert.Null(As(RoleNames.Student, studentId: Guid.NewGuid()).ProfileView(Child, ClassA, SectionA));
    }
}
