using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Common;
using SchoolERP.Business.Student.DTOs;
using SchoolERP.Business.Student.Services.Interfaces;
using System.Security.Claims;

namespace SchoolERP.Api.Controllers.Student;

[ApiController]
[Route("api/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentsController(IStudentService studentService) => _studentService = studentService;

    // Admission: the student record, their route and their fee account with its first fees --
    // all of it, or (if any step fails) none of it. See AdmissionService.
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> Admit([FromBody] CreateStudentRequest request, [FromServices] SchoolERP.Business.Admissions.AdmissionService admissions, CancellationToken ct)
    {
        try
        {
            var result = await admissions.AdmitAsync(request, User.UserId(), User.Role(), ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Student.Id }, ApiResponse<AdmittedStudent>.Ok(result, "Student admitted."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        // Students/parents may only read their OWN record.
        if (User.IsSelfServiceRole() && User.StudentId() != id)
            return Forbid();

        var student = await _studentService.GetByIdAsync(id, ct);
        if (student is null)
            return NotFound(ApiResponse<object>.Fail("Student not found."));

        // Need-to-know: each role sees only as much of the profile as its work requires.
        var view = User.ProfileView(id, student.ClassId, student.SectionId);
        if (view is null)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail("You don't have access to this student's details."));

        return Ok(ApiResponse<StudentSummary>.Ok(student.ViewAs(view.Value)));
    }

    // The full directory is admin/class-teacher only; parents read their child's record via
    // GET /api/students/{id} using the studentId from their token. A teacher is silently
    // scoped to the class/section they are the class teacher of. status: current (default,
    // still at the school), removed (removed or left with a TC) or all.
    [HttpGet]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher},{RoleNames.Accountant},{RoleNames.Librarian}")]
    public async Task<IActionResult> Search([FromQuery] string? classId, [FromQuery] string? sectionId, [FromQuery] string? keyword, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
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

        var result = await _studentService.SearchAsync(classId, sectionId, keyword, RosterFilters.Parse(status), page, pageSize, ct);
        var visible = new PagedResult<StudentSummary>
        {
            Items = result.Items
                .Select(s => s.ViewAs(User.ProfileView(s.Id, s.ClassId, s.SectionId) ?? StudentRecord.Directory))
                .ToList(),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
        return Ok(ApiResponse<object>.Ok(visible));
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
    // Identity-module concern handled by the admin password-reset flow.
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

    // Takes a current student off the rolls (reversible). They leave class lists, registers
    // and fee lists; the parent's login is switched off unless a sibling still uses it.
    [HttpPost("{id:guid}/remove")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public Task<IActionResult> Remove(Guid id, [FromBody] RemoveStudentRequest request, CancellationToken ct) =>
        RosterChange(() => _studentService.RemoveAsync(id, request.Reason, ActorId(), ActorRole(), ct), "Student removed.");

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public Task<IActionResult> Restore(Guid id, CancellationToken ct) =>
        RosterChange(() => _studentService.RestoreAsync(id, ActorId(), ActorRole(), ct), "Student restored.");

    private async Task<IActionResult> RosterChange(Func<Task<StudentSummary>> change, string message)
    {
        try
        {
            return Ok(ApiResponse<StudentSummary>.Ok(await change(), message));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(ex.Message)); }
    }

    private string ActorId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
    private string ActorRole() => User.FindFirstValue(ClaimTypes.Role) ?? "unknown";

    public record EraseStudentRequest(string ConfirmAdmissionNumber);

    // Right to erasure (DPDP Act): removes a former student's personal data once they've left.
    [HttpPost("{id:guid}/erase-personal-data")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal}")]
    public async Task<IActionResult> ErasePersonalData(Guid id, [FromBody] EraseStudentRequest request,
        [FromServices] SchoolERP.Business.Student.Services.StudentErasureService erasure, CancellationToken ct)
    {
        var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "unknown";
        try
        {
            var result = await erasure.EraseAsync(id, request.ConfirmAdmissionNumber, actorUserId, actorRole, ct);
            return Ok(ApiResponse<object>.Ok(result, "Personal data erased."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(ex.Message)); }
    }

    // Every role that can open the enrollment report (including the accountant) may read it.
    [HttpGet("stats/active-by-class")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher},{RoleNames.Accountant}")]
    public async Task<IActionResult> ActiveCountByClass(CancellationToken ct)
    {
        var result = await _studentService.GetActiveCountByClassAsync(ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
