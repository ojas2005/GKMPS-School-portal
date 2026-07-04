using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Academic.DTOs;
using SchoolERP.Academic.Services.Interfaces;
using SchoolERP.Shared.Common;

namespace SchoolERP.Academic.Controllers;

[ApiController]
[Route("api/timetables")]
[Authorize]
public class TimetablesController : ControllerBase
{
    private readonly ITimetableService _timetableService;

    public TimetablesController(ITimetableService timetableService) => _timetableService = timetableService;

    [HttpPut]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> Set([FromBody] SetTimetableRequest request, CancellationToken ct)
    {
        var result = await _timetableService.SetAsync(request, ct);
        return Ok(ApiResponse<TimetableSummary>.Ok(result, "Timetable updated."));
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string classId, [FromQuery] string sectionId, CancellationToken ct)
    {
        // Students/parents may only read their OWN class timetable.
        if (!User.CanAccessClass(classId))
            return Forbid();

        var result = await _timetableService.GetAsync(classId, sectionId, ct);
        return result is null
            ? NotFound(ApiResponse<object>.Fail("No timetable set for this class/section yet."))
            : Ok(ApiResponse<TimetableSummary>.Ok(result));
    }
}
