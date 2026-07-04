using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Examination.DTOs;
using SchoolERP.Examination.Services.Interfaces;
using SchoolERP.Shared.Common;
using System.Security.Claims;

namespace SchoolERP.Examination.Controllers;

[ApiController]
[Route("api/exams/{examId:guid}/marks")]
[Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher}")]
public class MarksController : ControllerBase
{
    private readonly IMarksService _marksService;

    public MarksController(IMarksService marksService) => _marksService = marksService;

    [HttpPost]
    public async Task<IActionResult> Enter(Guid examId, [FromBody] EnterMarksRequest request, CancellationToken ct)
    {
        var enteredBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        try
        {
            var result = await _marksService.EnterAsync(examId, request, enteredBy, ct);
            return Ok(ApiResponse<MarksSummary>.Ok(result, "Marks entered."));
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

    [HttpPatch("{marksEntryId:guid}")]
    public async Task<IActionResult> Correct(Guid examId, Guid marksEntryId, [FromBody] EnterMarksRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _marksService.CorrectAsync(marksEntryId, request.MarksObtained, request.Grade, ct);
            return Ok(ApiResponse<MarksSummary>.Ok(result, "Marks corrected."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
