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
        try
        {
            var result = await _mappingService.AssignAsync(request, ct);
            return Ok(ApiResponse<StudentRouteMappingSummary>.Ok(result, "Route assigned. The new route's charge applies from the next transport fee."));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetForStudent(Guid studentId, CancellationToken ct) =>
        Ok(ApiResponse<StudentTransportSummary?>.Ok(await _mappingService.GetForStudentAsync(studentId, ct)));

    // Takes the student off transport: no transport fees are created for them from now on.
    [HttpDelete("students/{studentId:guid}")]
    public async Task<IActionResult> RemoveForStudent(Guid studentId, CancellationToken ct)
    {
        var removed = await _mappingService.RemoveForStudentAsync(studentId, ct);
        return removed
            ? Ok(ApiResponse<object>.Ok(new { }, "Transport stopped."))
            : NotFound(ApiResponse<object>.Fail("This student doesn't use transport."));
    }

    [HttpGet]
    public async Task<IActionResult> GetByRoute([FromQuery] Guid routeId, CancellationToken ct)
    {
        var result = await _mappingService.GetByRouteIdAsync(routeId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
