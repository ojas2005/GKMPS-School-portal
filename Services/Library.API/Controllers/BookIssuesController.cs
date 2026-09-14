using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Library.DTOs;
using SchoolERP.Library.Services.Interfaces;
using SchoolERP.Shared.Common;

namespace SchoolERP.Library.Controllers;

[ApiController]
[Route("api/book-issues")]
[Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Librarian}")]
public class BookIssuesController : ControllerBase
{
    private readonly IBookIssueService _issueService;

    public BookIssuesController(IBookIssueService issueService) => _issueService = issueService;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool activeOnly = true, [FromQuery] Guid? studentId = null, CancellationToken ct = default)
    {
        var result = await _issueService.ListAsync(activeOnly, studentId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Issue([FromBody] IssueBookRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _issueService.IssueAsync(request, ct);
            return Ok(ApiResponse<BookIssueSummary>.Ok(result, "Book issued."));
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

    [HttpPost("return")]
    public async Task<IActionResult> Return([FromBody] ReturnBookRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _issueService.ReturnAsync(request, ct);
            return Ok(ApiResponse<BookIssueSummary>.Ok(result, "Book returned."));
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
