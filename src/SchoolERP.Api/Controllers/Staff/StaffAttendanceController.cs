using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Common;
using SchoolERP.Business.Staff.DTOs;
using SchoolERP.Business.Staff.Services.Interfaces;
using System.Security.Claims;

namespace SchoolERP.Api.Controllers.Staff;

[ApiController]
[Route("api/staff-attendance")]
[Authorize]
public class StaffAttendanceController : ControllerBase
{
    private readonly IPayrollService _payroll;
    private readonly IStaffService _staffService;

    public StaffAttendanceController(IPayrollService payroll, IStaffService staffService)
    {
        _payroll = payroll;
        _staffService = staffService;
    }

    // Admin marks anyone; a teacher may self-mark only their own record for today.
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher}")]
    public async Task<IActionResult> Mark([FromBody] MarkStaffAttendanceRequest request, CancellationToken ct)
    {
        var markedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        if (User.Role() == RoleNames.Teacher)
        {
            var ownStaffId = User.StaffId();
            if (ownStaffId is null || ownStaffId.Value != request.StaffId)
                return Forbid();
            // "Today" in the caller's local timezone can differ from UTC by up to a
            // day, so accept a ±1-day window around UTC today.
            var utcToday = DateOnly.FromDateTime(DateTime.UtcNow);
            if (Math.Abs(request.Date.DayNumber - utcToday.DayNumber) > 1)
                return BadRequest(ApiResponse<object>.Fail("Teachers may only mark their own attendance for today."));
        }

        try
        {
            var result = await _payroll.MarkStaffAttendanceAsync(request, markedBy, ct);
            return Ok(ApiResponse<StaffAttendanceSummary>.Ok(result, "Staff attendance marked."));
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

    // A staff member's attendance log. Admin may read anyone's; a teacher only their own.
    [HttpGet("staff/{staffId:guid}")]
    public async Task<IActionResult> GetForStaff(Guid staffId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (!User.CanAccessStaff(staffId))
            return Forbid();

        var result = await _payroll.GetStaffAttendanceAsync(staffId, from, to, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    // The caller's own attendance log (teacher portal).
    [HttpGet("me")]
    public async Task<IActionResult> GetMine([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var staffId = User.StaffId();
        if (staffId is null)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
            var me = await _staffService.GetByLinkedUserAsync(userId, ct);
            if (me is null) return NotFound(ApiResponse<object>.Fail("No staff profile is linked to this account."));
            staffId = me.Id;
        }

        var result = await _payroll.GetStaffAttendanceAsync(staffId.Value, from, to, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
