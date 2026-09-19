using System.Security.Claims;
using SchoolERP.Common;

namespace SchoolERP.Tests;

public class CallerClaimsTests
{
    private static ClaimsPrincipal User(string role, params (string Type, string Value)[] claims)
    {
        var all = claims.Select(c => new Claim(c.Type, c.Value)).Append(new Claim(ClaimTypes.Role, role));
        return new ClaimsPrincipal(new ClaimsIdentity(all, "test"));
    }

    [Fact]
    public void Student_can_only_access_own_record()
    {
        var own = Guid.NewGuid();
        var student = User(RoleNames.Student, (CallerClaims.StudentIdClaim, own.ToString()));

        Assert.True(student.CanAccessStudent(own));
        Assert.False(student.CanAccessStudent(Guid.NewGuid()));
    }

    [Fact]
    public void Parent_is_scoped_to_their_linked_child()
    {
        var child = Guid.NewGuid();
        var parent = User(RoleNames.Parent, (CallerClaims.StudentIdClaim, child.ToString()), (CallerClaims.ClassIdClaim, "class-10"));

        Assert.True(parent.CanAccessStudent(child));
        Assert.False(parent.CanAccessStudent(Guid.NewGuid()));
        Assert.True(parent.CanAccessClass("class-10"));
        Assert.False(parent.CanAccessClass("class-9"));
    }

    [Fact]
    public void Unlinked_parent_can_access_nothing()
    {
        var parent = User(RoleNames.Parent);

        Assert.False(parent.CanAccessStudent(Guid.NewGuid()));
        Assert.False(parent.CanAccessClass("class-10"));
    }

    [Fact]
    public void Teacher_manages_only_the_class_they_head()
    {
        var teacher = User(RoleNames.Teacher, (CallerClaims.ClassTeacherOfClassIdClaim, "class-10"));
        var subjectTeacher = User(RoleNames.Teacher);

        Assert.True(teacher.CanManageClass("class-10"));
        Assert.False(teacher.CanManageClass("class-9"));
        Assert.False(subjectTeacher.CanManageClass("class-10"));
        Assert.True(User(RoleNames.Admin).CanManageClass("class-9"));
    }

    [Fact]
    public void Staff_records_are_private_to_the_staff_member_and_admins()
    {
        var mine = Guid.NewGuid();
        var teacher = User(RoleNames.Teacher, (CallerClaims.StaffIdClaim, mine.ToString()));

        Assert.True(teacher.CanAccessStaff(mine));
        Assert.False(teacher.CanAccessStaff(Guid.NewGuid()));
        Assert.False(User(RoleNames.Student).CanAccessStaff(mine));
        Assert.True(User(RoleNames.Principal).CanAccessStaff(mine));
    }
}
