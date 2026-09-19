using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Common;

namespace SchoolERP.Api.Controllers.Fee;

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
    public async Task<IActionResult> GetByClass([FromQuery] string? classId, [FromQuery] string? academicYear, CancellationToken ct)
    {
        // Both filters are optional for staff (omit to list everything); students/parents
        // only ever see their own class's structures.
        if (User.IsSelfServiceRole())
        {
            classId ??= User.ClassId();
            if (!User.CanAccessClass(classId))
                return Forbid();
        }

        var result = await _feeStructureService.GetByClassAsync(classId, academicYear, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
