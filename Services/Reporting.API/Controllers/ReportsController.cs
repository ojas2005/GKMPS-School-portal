using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Reporting.Services.Interfaces;
using SchoolERP.Shared.Common;
using System.Security.Claims;

namespace SchoolERP.Reporting.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService) => _reportingService = reportingService;

    [HttpGet("enrollment")]
    public async Task<IActionResult> GetEnrollment(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var result = await _reportingService.GetEnrollmentReportAsync(userId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("enrollment/pdf")]
    public async Task<IActionResult> GetEnrollmentPdf(CancellationToken ct)
    {
        var bytes = await _reportingService.GenerateEnrollmentPdfAsync(ct);
        return File(bytes, "application/pdf", "enrollment-report.pdf");
    }

    [HttpGet("fee-collection")]
    public async Task<IActionResult> GetFeeCollection([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var result = await _reportingService.GetFeeCollectionReportAsync(fromUtc, toUtc, userId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
