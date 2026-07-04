using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Transport.DTOs;
using SchoolERP.Transport.Services.Interfaces;
using SchoolERP.Shared.Common;

namespace SchoolERP.Transport.Controllers;

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
}
