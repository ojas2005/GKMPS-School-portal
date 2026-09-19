using SchoolERP.DataAccess.Student.Repositories.Interfaces;

namespace SchoolERP.Business.Identity.Auth;

public record StudentProfile(Guid StudentId, string ClassId, string SectionId);

/// <summary>
/// Resolves the student linked to a user account so the access token can carry
/// studentId/classId/sectionId claims: a Student's own record, or for a Parent the child
/// whose record points at that login. Best-effort: any failure returns null so login is
/// never blocked (staff accounts simply have no student profile).
/// </summary>
public class StudentProfileResolver
{
    private readonly IStudentRepository _students;
    private readonly ILogger<StudentProfileResolver> _logger;

    public StudentProfileResolver(IStudentRepository students, ILogger<StudentProfileResolver> logger)
    {
        _students = students;
        _logger = logger;
    }

    /// <param name="asParent">True for Parent-role accounts: match the student whose
    /// ParentUserId is this user (their child) instead of the student's own login.</param>
    public async Task<StudentProfile?> ResolveByUserAsync(Guid userId, bool asParent = false, CancellationToken ct = default)
    {
        try
        {
            var student = asParent
                ? await _students.FindByParentUserAsync(userId, ct)
                : await _students.FindByLinkedUserAsync(userId, ct);

            return student is null ? null : new StudentProfile(student.Id, student.ClassId, student.SectionId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not resolve student profile for user {UserId}", userId);
            return null;
        }
    }
}
