using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Shared.Common;
using SchoolERP.Staff.DTOs;
using SchoolERP.Staff.Services.Interfaces;
using System.Security.Claims;

namespace SchoolERP.Staff.Controllers;

[ApiController]
[Route("api/payouts")]
[Authorize]
public class PayoutsController : ControllerBase
{
    private readonly IPayrollService _payroll;
    private readonly IStaffService _staffService;

    public PayoutsController(IPayrollService payroll, IStaffService staffService)
    {
        _payroll = payroll;
        _staffService = staffService;
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
    public async Task<IActionResult> Record([FromBody] RecordPayoutRequest request, CancellationToken ct)
    {
        var recordedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        try
        {
            var result = await _payroll.RecordPayoutAsync(request, recordedBy, ct);
            return Ok(ApiResponse<PayoutSummary>.Ok(result, "Payout recorded."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // A staff member's payout ledger. Admin/accountant may read anyone's; a teacher
    // only their own (via the staffId claim).
    [HttpGet("staff/{staffId:guid}")]
    public async Task<IActionResult> GetForStaff(Guid staffId, CancellationToken ct)
    {
        if (!User.CanAccessStaff(staffId) && User.Role() != RoleNames.Accountant)
            return Forbid();

        var result = await _payroll.GetPayoutsForStaffAsync(staffId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    // The caller's own payout log (teacher portal).
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var staffId = await ResolveCallerStaffIdAsync(ct);
        if (staffId is null) return NotFound(ApiResponse<object>.Fail("No staff profile is linked to this account."));

        var result = await _payroll.GetPayoutsForStaffAsync(staffId.Value, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    // A staff member's pending salary for the current month. Admin/accountant may read
    // anyone's; a teacher only their own (via the staffId claim).
    [HttpGet("staff/{staffId:guid}/pending")]
    public async Task<IActionResult> GetPendingForStaff(Guid staffId, CancellationToken ct)
    {
        if (!User.CanAccessStaff(staffId) && User.Role() != RoleNames.Accountant)
            return Forbid();

        try
        {
            var result = await _payroll.GetPendingSalaryAsync(staffId, ct);
            return Ok(ApiResponse<PendingSalaryResponse>.Ok(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // The caller's own pending salary for the current month (teacher dashboard).
    [HttpGet("me/pending")]
    public async Task<IActionResult> GetMyPending(CancellationToken ct)
    {
        var staffId = await ResolveCallerStaffIdAsync(ct);
        if (staffId is null) return NotFound(ApiResponse<object>.Fail("No staff profile is linked to this account."));

        var result = await _payroll.GetPendingSalaryAsync(staffId.Value, ct);
        return Ok(ApiResponse<PendingSalaryResponse>.Ok(result));
    }

    private async Task<Guid?> ResolveCallerStaffIdAsync(CancellationToken ct)
    {
        var staffId = User.StaffId();
        if (staffId is not null) return staffId;

        // Fall back to resolving via the linked user id (older tokens without the claim).
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var me = await _staffService.GetByLinkedUserAsync(userId, ct);
        return me?.Id;
    }
}
