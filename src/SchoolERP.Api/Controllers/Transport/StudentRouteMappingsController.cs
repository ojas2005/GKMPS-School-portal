using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Transport.DTOs;
using SchoolERP.Business.Transport.Services.Interfaces;
using SchoolERP.Common;

namespace SchoolERP.Api.Controllers.Transport;

[ApiController]
[Route("api/student-route-mappings")]
[Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
public class StudentRouteMappingsController : ControllerBase
{
    private readonly IStudentRouteMappingService _mappingService;

    public StudentRouteMappingsController(IStudentRouteMappingService mappingService) => _mappingService = mappingService;

    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] AssignStudentRouteRequest request, CancellationToken ct)
    {
        var result = await _mappingService.AssignAsync(request, ct);
        return Ok(ApiResponse<StudentRouteMappingSummary>.Ok(result, "Route assigned."));
    }

    [HttpGet]
    public async Task<IActionResult> GetByRoute([FromQuery] Guid routeId, CancellationToken ct)
    {
        var result = await _mappingService.GetByRouteIdAsync(routeId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
