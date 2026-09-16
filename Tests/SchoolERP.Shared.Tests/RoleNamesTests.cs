using SchoolERP.Shared.Common;

namespace SchoolERP.Shared.Tests;

public class RoleNamesTests
{
    [Theory]
    [InlineData(RoleNames.SuperAdmin, RoleNames.SuperAdmin)]
    [InlineData(RoleNames.SuperAdmin, RoleNames.Principal)]
    [InlineData(RoleNames.SuperAdmin, RoleNames.Admin)]
    [InlineData(RoleNames.Principal, RoleNames.Admin)]
    [InlineData(RoleNames.Principal, RoleNames.Teacher)]
    [InlineData(RoleNames.Admin, RoleNames.Teacher)]
    [InlineData(RoleNames.Admin, RoleNames.Student)]
    [InlineData(RoleNames.Admin, RoleNames.Parent)]
    [InlineData(RoleNames.Admin, RoleNames.Accountant)]
    [InlineData(RoleNames.Admin, RoleNames.Librarian)]
    public void Can_manage_roles_below_own_rank(string actor, string target) =>
        Assert.True(RoleNames.CanManageRole(actor, target));

    [Theory]
    // The privilege-escalation cases found in review: nobody but the owner creates or
    // administers the owner (or anyone at/above their own rank).
    [InlineData(RoleNames.Admin, RoleNames.SuperAdmin)]
    [InlineData(RoleNames.Principal, RoleNames.SuperAdmin)]
    [InlineData(RoleNames.Admin, RoleNames.Principal)]
    [InlineData(RoleNames.Admin, RoleNames.Admin)]
    [InlineData(RoleNames.Principal, RoleNames.Principal)]
    [InlineData(RoleNames.Teacher, RoleNames.Student)]
    [InlineData(RoleNames.Accountant, RoleNames.Teacher)]
    [InlineData(RoleNames.Student, RoleNames.Student)]
    [InlineData(null, RoleNames.Student)]
    public void Cannot_manage_roles_at_or_above_own_rank(string? actor, string target) =>
        Assert.False(RoleNames.CanManageRole(actor, target));
}
