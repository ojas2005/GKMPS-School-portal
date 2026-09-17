using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Attendance.DTOs;
using SchoolERP.Business.Attendance.Services.Interfaces;
using SchoolERP.Common;
using System.Security.Claims;

namespace SchoolERP.Api.Controllers.Attendance;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService) => _attendanceService = attendanceService;

    // Admin/principal may mark any class; a Teacher only the class they are the class
    // teacher (head teacher) of — enforced via the classTeacherOfClassId JWT claim.
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher}")]
    public async Task<IActionResult> Mark([FromBody] MarkAttendanceRequest request, CancellationToken ct)
    {
        if (!User.CanManageClass(request.ClassId))
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail("Only the class teacher of this class may upload its attendance."));

        var markedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        try
        {
            var result = await _attendanceService.MarkAsync(request, markedBy, ct);
            return Ok(ApiResponse<AttendanceRecordSummary>.Ok(result, "Attendance marked."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // The full class register: admin sees any class; a Teacher only their own class
    // (class teachers may see their students' records for enquiry purposes).
    [HttpGet("class")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher}")]
    public async Task<IActionResult> GetForClass([FromQuery] string classId, [FromQuery] string sectionId, [FromQuery] DateOnly date, CancellationToken ct)
    {
        if (!User.CanManageClass(classId))
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail("Only the class teacher of this class may view its register."));

        var result = await _attendanceService.GetForClassAsync(classId, sectionId, date, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("students/{studentId:guid}/percentage")]
    public async Task<IActionResult> GetPercentage(Guid studentId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        // Students/parents may only read their OWN attendance.
        if (!User.CanAccessStudent(studentId))
            return Forbid();

        var result = await _attendanceService.GetAttendancePercentageAsync(studentId, from, to, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    // A student's day-by-day attendance log — the student portal's "My Attendance" list.
    [HttpGet("students/{studentId:guid}/records")]
    public async Task<IActionResult> GetRecords(Guid studentId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (!User.CanAccessStudent(studentId))
            return Forbid();

        var result = await _attendanceService.GetRecordsForStudentAsync(studentId, from, to, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
