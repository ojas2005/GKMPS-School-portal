using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Academic.DTOs;
using SchoolERP.Business.Academic.Services.Interfaces;
using SchoolERP.Common;

namespace SchoolERP.Api.Controllers.Academic;

[ApiController]
[Route("api/timetables")]
[Authorize]
public class TimetablesController : ControllerBase
{
    private readonly ITimetableService _timetableService;
    private readonly IScheduleConfigService _scheduleConfigService;
    private readonly ITimetableGeneratorService _generatorService;

    private const string AdminRoles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}";

    public TimetablesController(
        ITimetableService timetableService,
        IScheduleConfigService scheduleConfigService,
        ITimetableGeneratorService generatorService)
    {
        _timetableService = timetableService;
        _scheduleConfigService = scheduleConfigService;
        _generatorService = generatorService;
    }

    [HttpPut]
    [Authorize(Roles = AdminRoles)]
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

    // ---- Auto-generation config (owner sets up the teacher groups) ----

    [HttpGet("config")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var result = await _scheduleConfigService.GetAsync(ct);
        return Ok(ApiResponse<ScheduleConfigResponse>.Ok(result));
    }

    [HttpPut("config")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> SaveConfig([FromBody] SaveScheduleConfigRequest request, CancellationToken ct)
    {
        await _scheduleConfigService.SaveAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Timetable configuration saved."));
    }

    [HttpPost("generate")]
    [Authorize(Roles = AdminRoles)]
    public async Task<IActionResult> Generate(CancellationToken ct)
    {
        var result = await _generatorService.GenerateAsync(ct);
        var message = result.Warnings.Count > 0
            ? "Generated with warnings — some classes could not be fully covered."
            : "Timetable generated for all classes.";
        return Ok(ApiResponse<GenerateTimetableResult>.Ok(result, message));
    }

    // ---- Teacher's own schedule ----

    [HttpGet("teacher/me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var staffId = User.StaffId();
        if (staffId is null)
            return BadRequest(ApiResponse<object>.Fail("Your account has no staff profile linked."));

        var result = await _generatorService.GetForTeacherAsync(staffId.Value, ct);
        return Ok(ApiResponse<TeacherTimetableResponse>.Ok(result));
    }

    [HttpGet("teacher/{staffId:guid}")]
    public async Task<IActionResult> GetForTeacher(Guid staffId, CancellationToken ct)
    {
        // A teacher may read only their own; admin/principal may read anyone's.
        if (!User.CanAccessStaff(staffId))
            return Forbid();

        var result = await _generatorService.GetForTeacherAsync(staffId, ct);
        return Ok(ApiResponse<TeacherTimetableResponse>.Ok(result));
    }
}
