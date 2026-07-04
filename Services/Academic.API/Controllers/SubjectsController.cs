using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Academic.DTOs;
using SchoolERP.Academic.Services.Interfaces;
using SchoolERP.Shared.Common;

namespace SchoolERP.Academic.Controllers;

[ApiController]
[Route("api/subjects")]
[Authorize]
public class SubjectsController : ControllerBase
{
    private readonly ISubjectService _subjectService;

    public SubjectsController(ISubjectService subjectService) => _subjectService = subjectService;

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> Create([FromBody] CreateSubjectRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _subjectService.CreateAsync(request, ct);
            return Ok(ApiResponse<SubjectSummary>.Ok(result, "Subject created."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetByClass([FromQuery] string classId, CancellationToken ct)
    {
        // Students/parents may only read their OWN class's subjects & syllabus.
        if (!User.CanAccessClass(classId))
            return Forbid();

        var result = await _subjectService.GetByClassAsync(classId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    // Set/update the syllabus text a student sees for this subject.
    [HttpPatch("{id:guid}/syllabus")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher}")]
    public async Task<IActionResult> UpdateSyllabus(Guid id, [FromBody] UpdateSyllabusRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _subjectService.UpdateSyllabusAsync(id, request.SyllabusOutline, ct);
            return Ok(ApiResponse<SubjectSummary>.Ok(result, "Syllabus updated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
