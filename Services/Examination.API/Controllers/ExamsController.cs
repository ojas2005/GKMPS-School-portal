using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Examination.DTOs;
using SchoolERP.Examination.Services.Interfaces;
using SchoolERP.Shared.Common;

namespace SchoolERP.Examination.Controllers;

[ApiController]
[Route("api/exams")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly IMarksService _marksService;

    public ExamsController(IExamService examService, IMarksService marksService)
    {
        _examService = examService;
        _marksService = marksService;
    }

    // A student's results across all their exams. Staff see everything; a student sees
    // only their own PUBLISHED results.
    [HttpGet("students/{studentId:guid}/results")]
    public async Task<IActionResult> GetStudentResults(Guid studentId, CancellationToken ct)
    {
        if (!User.CanAccessStudent(studentId))
            return Forbid();

        var publishedOnly = User.IsSelfServiceRole();
        var result = await _marksService.GetForStudentAsync(studentId, publishedOnly, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher}")]
    public async Task<IActionResult> Create([FromBody] CreateExamRequest request, CancellationToken ct)
    {
        var result = await _examService.CreateAsync(request, ct);
        return Ok(ApiResponse<ExamSummary>.Ok(result, "Exam created."));
    }

    [HttpGet]
    public async Task<IActionResult> GetByClass([FromQuery] string classId, CancellationToken ct)
    {
        var result = await _examService.GetByClassAsync(classId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("{examId:guid}/publish")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal}")]
    public async Task<IActionResult> Publish(Guid examId, CancellationToken ct)
    {
        try
        {
            await _examService.PublishResultsAsync(examId, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "Results published."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet("{examId:guid}/stats")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher}")]
    public async Task<IActionResult> GetStats(Guid examId, CancellationToken ct)
    {
        var result = await _examService.GetStatsAsync(examId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{examId:guid}/students/{studentId:guid}/report-card")]
    public async Task<IActionResult> GetReportCard(Guid examId, Guid studentId, CancellationToken ct)
    {
        // Students/parents may only read their OWN report card.
        if (!User.CanAccessStudent(studentId))
            return Forbid();

        try
        {
            var url = await _examService.GenerateReportCardAsync(examId, studentId, ct);
            return Ok(ApiResponse<object>.Ok(new { downloadUrl = url, expiresInMinutes = 15 }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
