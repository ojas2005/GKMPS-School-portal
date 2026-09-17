using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Common;
using SchoolERP.Business.Staff.DTOs;
using SchoolERP.Business.Staff.Services.Interfaces;
using System.Security.Claims;

namespace SchoolERP.Api.Controllers.Staff;

[ApiController]
[Route("api/staff/{staffId:guid}/leave-requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;

    public LeaveRequestsController(ILeaveRequestService leaveRequestService) => _leaveRequestService = leaveRequestService;

    [HttpPost]
    public async Task<IActionResult> RequestLeave(Guid staffId, [FromBody] RequestLeaveRequest request, CancellationToken ct)
    {
        // Staff apply for their own leave; admins may file on someone's behalf.
        if (!User.CanAccessStaff(staffId))
            return Forbid();

        try
        {
            var result = await _leaveRequestService.RequestAsync(staffId, request, ct);
            return Ok(ApiResponse<LeaveRequestSummary>.Ok(result, "Leave requested; awaiting approval."));
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

    [HttpPost("{leaveRequestId:guid}/decision")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> Decide(Guid staffId, Guid leaveRequestId, [FromBody] DecideLeaveRequest decision, CancellationToken ct)
    {
        var actorUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "unknown";

        try
        {
            var result = await _leaveRequestService.DecideAsync(leaveRequestId, decision, actorUserId, actorRole, ct);
            return Ok(ApiResponse<LeaveRequestSummary>.Ok(result, decision.Approve ? "Leave approved." : "Leave rejected."));
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
}
