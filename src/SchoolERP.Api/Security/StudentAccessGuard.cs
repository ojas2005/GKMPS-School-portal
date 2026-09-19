using System.Security.Claims;
using SchoolERP.Business.Student.Services.Interfaces;
using SchoolERP.Common;

namespace SchoolERP.Api.Security;

/// <summary>
/// Applies <see cref="StudentAccess"/> to a request. A teacher's access depends on the
/// student's class, so only then is the student looked up; every other role is decided by
/// role alone.
/// </summary>
public class StudentAccessGuard
{
    private readonly IStudentService _students;

    public StudentAccessGuard(IStudentService students) => _students = students;

    public async Task<bool> CanReadAsync(ClaimsPrincipal user, StudentRecord record, Guid studentId, CancellationToken ct)
    {
        if (!user.NeedsStudentClass())
            return user.CanRead(record, studentId, null, null);

        var student = await _students.GetByIdAsync(studentId, ct);
        return student is not null && user.CanRead(record, studentId, student.ClassId, student.SectionId);
    }
}
