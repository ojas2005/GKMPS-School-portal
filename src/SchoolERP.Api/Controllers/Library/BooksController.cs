using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Library.DTOs;
using SchoolERP.Business.Library.Services.Interfaces;
using SchoolERP.Common;

namespace SchoolERP.Api.Controllers.Library;

[ApiController]
[Route("api/books")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService) => _bookService = bookService;

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Librarian}")]
    public async Task<IActionResult> Add([FromBody] AddBookRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bookService.AddAsync(request, ct);
            return Ok(ApiResponse<BookSummary>.Ok(result, "Book added."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? keyword, [FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await _bookService.SearchAsync(keyword, category, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
