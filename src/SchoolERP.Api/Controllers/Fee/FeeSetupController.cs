using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Common;

namespace SchoolERP.Api.Controllers.Fee;

/// <summary>The fee structure: class-wise admission, tuition and exam fees; when fees fall due;
/// and the fee preview the admission form shows.</summary>
[ApiController]
[Route("api/fee-setup")]
[Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
public class FeeSetupController : ControllerBase
{
    private const string Managers = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}";

    private readonly IFeeSetupService _setup;
    private readonly IFeeAccountService _accounts;

    public FeeSetupController(IFeeSetupService setup, IFeeAccountService accounts)
    {
        _setup = setup;
        _accounts = accounts;
    }

    [HttpGet("classes")]
    public async Task<IActionResult> GetRates(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _setup.GetRatesAsync(ct)));

    [HttpPut("classes")]
    [Authorize(Roles = Managers)]
    public async Task<IActionResult> UpdateRates([FromBody] UpdateClassFeeRatesRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _setup.UpdateRatesAsync(request, User.UserId(), User.Role(), ct);
            return Ok(ApiResponse<object>.Ok(result, "Fee structure saved. New amounts apply to fees created from now on."));
        }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule(CancellationToken ct) =>
        Ok(ApiResponse<FeeScheduleDto>.Ok(await _setup.GetScheduleAsync(ct)));

    [HttpPut("schedule")]
    [Authorize(Roles = Managers)]
    public async Task<IActionResult> UpdateSchedule([FromBody] FeeScheduleDto request, CancellationToken ct)
    {
        try
        {
            var result = await _setup.UpdateScheduleAsync(request, User.UserId(), User.Role(), ct);
            return Ok(ApiResponse<FeeScheduleDto>.Ok(result, "Due dates saved."));
        }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    // The admission form's live fee summary for the chosen class and route. Changes nothing.
    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] string classId, [FromQuery] Guid? routeId, [FromQuery] decimal? previousDues, CancellationToken ct)
    {
        try
        {
            return Ok(ApiResponse<FeeBreakdown>.Ok(await _accounts.PreviewAsync(classId, routeId, null, previousDues, ct)));
        }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message)); }
    }
}
