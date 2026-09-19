using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Academic.DTOs;
using SchoolERP.Business.Academic.Services.Interfaces;
using SchoolERP.Common;
using System.Security.Claims;

namespace SchoolERP.Api.Controllers.Academic;

[ApiController]
[Route("api/homework")]
[Authorize]
public class HomeworkController : ControllerBase
{
    private readonly IHomeworkService _homeworkService;

    public HomeworkController(IHomeworkService homeworkService) => _homeworkService = homeworkService;

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher}")]
    public async Task<IActionResult> Assign([FromBody] CreateHomeworkRequest request, CancellationToken ct)
    {
        var assignedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var result = await _homeworkService.AssignAsync(request, assignedBy, ct);
        return Ok(ApiResponse<HomeworkSummary>.Ok(result, "Homework assigned."));
    }

    [HttpGet]
    public async Task<IActionResult> GetByClass([FromQuery] string classId, [FromQuery] string sectionId, CancellationToken ct)
    {
        // Students/parents may only read homework for their OWN class.
        if (!User.CanAccessClass(classId))
            return Forbid();

        var result = await _homeworkService.GetByClassAsync(classId, sectionId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
