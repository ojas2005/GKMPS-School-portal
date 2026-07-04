using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Fee.DTOs;
using SchoolERP.Fee.Services.Interfaces;
using SchoolERP.Shared.Common;

namespace SchoolERP.Fee.Controllers;

[ApiController]
[Route("api/fee-structures")]
[Authorize]
public class FeeStructuresController : ControllerBase
{
    private readonly IFeeStructureService _feeStructureService;

    public FeeStructuresController(IFeeStructureService feeStructureService) => _feeStructureService = feeStructureService;

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
    public async Task<IActionResult> Create([FromBody] CreateFeeStructureRequest request, CancellationToken ct)
    {
        var result = await _feeStructureService.CreateAsync(request, ct);
        return Ok(ApiResponse<FeeStructureSummary>.Ok(result, "Fee structure created."));
    }

    [HttpGet]
    public async Task<IActionResult> GetByClass([FromQuery] string classId, [FromQuery] string academicYear, CancellationToken ct)
    {
        var result = await _feeStructureService.GetByClassAsync(classId, academicYear, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
