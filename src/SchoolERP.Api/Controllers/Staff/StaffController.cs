using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Common;
using SchoolERP.Business.Staff.DTOs;
using SchoolERP.Business.Staff.Services.Interfaces;
using System.Security.Claims;

namespace SchoolERP.Api.Controllers.Staff;

[ApiController]
[Route("api/staff")]
[Authorize]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;

    public StaffController(IStaffService staffService) => _staffService = staffService;

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> Onboard([FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _staffService.OnboardAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse<StaffSummary>.Ok(result, "Staff onboarded."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // The caller's own staff profile (any staff role) — used by the teacher portal.
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var staff = await _staffService.GetByLinkedUserAsync(userId, ct);
        return staff is null
            ? NotFound(ApiResponse<object>.Fail("No staff profile is linked to this account."))
            : Ok(ApiResponse<StaffSummary>.Ok(staff));
    }

    // Anyone signed in may look a staff member up (a student's timetable shows teacher
    // names), but salary and contact details are only returned to admins, the accountant
    // (payroll), and the staff member themselves.
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var staff = await _staffService.GetByIdAsync(id, ct);
        if (staff is null) return NotFound(ApiResponse<object>.Fail("Staff member not found."));

        var canSeePrivate = User.CanAccessStaff(id) || User.Role() == RoleNames.Accountant;
        return Ok(ApiResponse<StaffSummary>.Ok(canSeePrivate ? staff : staff with { MonthlySalary = null, Phone = null, Email = null }));
    }

    [HttpGet]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
    public async Task<IActionResult> Search([FromQuery] string? designation, [FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await _staffService.SearchAsync(designation, keyword, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    // Make/unmake this teacher the class teacher (head teacher) of a class/section.
    // Grants them attendance-upload + student-enquiry rights for that class.
    [HttpPatch("{id:guid}/class-teacher")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> AssignClassTeacher(Guid id, [FromBody] AssignClassTeacherRequest request, CancellationToken ct)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        try
        {
            var result = await _staffService.AssignClassTeacherAsync(id, request, actorId, User.Role() ?? "unknown", ct);
            return Ok(ApiResponse<StaffSummary>.Ok(result, "Class-teacher assignment updated. The teacher must log in again to pick up the new rights."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // Owner defines/updates a teacher's monthly salary; drives their pending-salary readout.
    [HttpPatch("{id:guid}/salary")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> SetSalary(Guid id, [FromBody] SetSalaryRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _staffService.SetSalaryAsync(id, request, ct);
            return Ok(ApiResponse<StaffSummary>.Ok(result, "Salary updated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
