using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Shared.Common;
using SchoolERP.Student.DTOs;
using SchoolERP.Student.Services.Interfaces;
using System.Security.Claims;

namespace SchoolERP.Student.Controllers;

[ApiController]
[Route("api/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService) => _studentService = studentService;

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> Admit([FromBody] CreateStudentRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _studentService.AdmitStudentAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<StudentSummary>.Ok(result, "Student admitted."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        // Students/parents may only read their OWN record.
        if (!User.CanAccessStudent(id))
            return Forbid();

        var student = await _studentService.GetByIdAsync(id, ct);
        if (student is null)
            return NotFound(ApiResponse<object>.Fail("Student not found."));

        // A teacher may read a student's details for enquiry only if they are the
        // class teacher (head teacher) of that student's class.
        if (User.Role() == RoleNames.Teacher && User.ClassTeacherOfClassId() != student.ClassId)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail("Only the class teacher of this student's class may view their details."));

        return Ok(ApiResponse<StudentSummary>.Ok(student));
    }

    // The full directory is admin/class-teacher only; students/parents read their own
    // record via GET /api/students/{id} using the studentId from their token. A teacher
    // is silently scoped to the class/section they are the class teacher of.
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher},{RoleNames.Accountant},{RoleNames.Librarian}")]
    public async Task<IActionResult> Search([FromQuery] string? classId, [FromQuery] string? sectionId, [FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        if (User.Role() == RoleNames.Teacher)
        {
            var ownClassId = User.ClassTeacherOfClassId();
            if (string.IsNullOrEmpty(ownClassId))
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<object>.Fail("Only class teachers may browse student details."));
            classId = ownClassId;
            sectionId = User.ClassTeacherOfSectionId();
        }

        var result = await _studentService.SearchAsync(classId, sectionId, keyword, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPatch("{id:guid}/class")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> ReassignClass(Guid id, [FromBody] ReassignClassRequest request, CancellationToken ct)
    {
        var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "unknown";

        try
        {
            await _studentService.ReassignClassAsync(id, request, actorUserId, actorRole, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "Class reassigned."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // Owner/admin edits everything collected at admission (name, DOB, gender, class/section,
    // admission number, guardian details) in one shot. Login credentials are a separate,
    // Identity.API-owned concern handled by the admin password-reset flow.
    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> UpdateDetails(Guid id, [FromBody] UpdateStudentRequest request, CancellationToken ct)
    {
        var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "unknown";

        try
        {
            var result = await _studentService.UpdateDetailsAsync(id, request, actorUserId, actorRole, ct);
            return Ok(ApiResponse<StudentSummary>.Ok(result, "Student details updated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // Links the guardian's Parent-role login to this student (null unlinks). The parent must
    // sign in again afterwards to pick up the student claims.
    [HttpPatch("{id:guid}/parent-account")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> LinkParentAccount(Guid id, [FromBody] LinkParentAccountRequest request, CancellationToken ct)
    {
        var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "unknown";

        try
        {
            var result = await _studentService.LinkParentAccountAsync(id, request.ParentUserId, actorUserId, actorRole, ct);
            return Ok(ApiResponse<StudentSummary>.Ok(result, request.ParentUserId is null ? "Parent login unlinked." : "Parent login linked."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("stats/active-by-class")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher}")]
    public async Task<IActionResult> ActiveCountByClass(CancellationToken ct)
    {
        var result = await _studentService.GetActiveCountByClassAsync(ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
