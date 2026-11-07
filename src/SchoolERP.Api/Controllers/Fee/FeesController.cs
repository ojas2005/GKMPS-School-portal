using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Common;

namespace SchoolERP.Api.Controllers.Fee;

/// <summary>The fee dashboard: totals, every fee entry, per-student balances and the monthly report.</summary>
[ApiController]
[Route("api/fees")]
[Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
public class FeesController : ControllerBase
{
    private readonly IFeeLedgerService _ledger;

    public FeesController(IFeeLedgerService ledger) => _ledger = ledger;

    [HttpGet("overview")]
    public async Task<IActionResult> Overview([FromQuery] string? academicYear, CancellationToken ct) =>
        Ok(ApiResponse<FeeOverviewDto>.Ok(await _ledger.OverviewAsync(academicYear, ct)));

    [HttpGet("entries")]
    public async Task<IActionResult> Entries([FromQuery] FeeEntryQuery query, CancellationToken ct)
    {
        try { return Ok(ApiResponse<object>.Ok(await _ledger.SearchEntriesAsync(query, ct))); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpGet("students")]
    public async Task<IActionResult> Students([FromQuery] FeeEntryQuery query, CancellationToken ct)
    {
        try { return Ok(ApiResponse<object>.Ok(await _ledger.StudentBalancesAsync(query, ct))); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpGet("collections/monthly")]
    public async Task<IActionResult> Monthly([FromQuery] string? academicYear, CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _ledger.MonthlyCollectionsAsync(academicYear, ct)));
}
