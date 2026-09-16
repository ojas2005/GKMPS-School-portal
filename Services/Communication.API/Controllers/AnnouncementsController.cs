using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Communication.DTOs;
using SchoolERP.Communication.Services.Interfaces;
using SchoolERP.Shared.Common;
using System.Security.Claims;

namespace SchoolERP.Communication.Controllers;

[ApiController]
[Route("api/announcements")]
[Authorize]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementsController(IAnnouncementService announcementService) => _announcementService = announcementService;

    // Owner (SuperAdmin/Principal/Admin) may target any role/class; a Teacher may only
    // reach the Students of the one class they are class teacher of -- enforced (not
    // just checked) in the service layer, which overrides the request's targeting.
    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher}")]
    public async Task<IActionResult> Post([FromBody] CreateAnnouncementRequest request, CancellationToken ct)
    {
        var postedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        try
        {
            var result = await _announcementService.PostAsync(request, postedBy, User.Role() ?? "unknown", User.ClassTeacherOfClassId(), ct);
            return Ok(ApiResponse<AnnouncementSummary>.Ok(result, "Announcement posted."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.Fail(ex.Message));
        }
    }

    // Matches no class, so only school-wide (untargeted) announcements come back.
    private const string NoClass = "__none__";

    [HttpGet]
    public async Task<IActionResult> GetRelevant([FromQuery] string? role, [FromQuery] string? classId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        // Only the owner/principal/admin may browse every audience (the composer's history).
        // Everyone else sees what is addressed to their OWN role and class, taken from the
        // token -- never from the query string, which the caller controls.
        if (User.Role() == RoleNames.Teacher)
        {
            // Teachers also see what they (or the office) sent to their own class's students,
            // so the role filter is left open -- but the class is always their own.
            role = null;
            classId = User.ClassTeacherOfClassId() ?? NoClass;
        }
        else if (RoleNames.Rank(User.Role()) == 0)
        {
            role = User.Role();
            classId = User.IsSelfServiceRole() ? User.ClassId() ?? NoClass : NoClass;
        }

        var result = await _announcementService.GetRelevantAsync(role, classId, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
