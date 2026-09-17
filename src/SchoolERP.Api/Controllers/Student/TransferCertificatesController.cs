using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Common;
using SchoolERP.Business.Student.DTOs;
using SchoolERP.Business.Student.Services.Interfaces;
using System.Security.Claims;

namespace SchoolERP.Api.Controllers.Student;

[ApiController]
[Route("api/transfer-certificates")]
public class TransferCertificatesController : ControllerBase
{
    private readonly ITransferCertificateService _certificateService;

    public TransferCertificatesController(ITransferCertificateService certificateService) => _certificateService = certificateService;

    [HttpPost("students/{studentId:guid}/request")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Teacher}")]
    public async Task<IActionResult> RequestCertificate(Guid studentId, [FromBody] RequestTransferCertificateRequest request, CancellationToken ct)
    {
        var submittedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        try
        {
            var result = await _certificateService.RequestAsync(studentId, request, submittedBy, ct);
            return Ok(ApiResponse<TransferCertificateSummary>.Ok(result, "Transfer certificate requested; awaiting approval."));
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

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal}")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var approvedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        try
        {
            var result = await _certificateService.ApproveAsync(id, approvedBy, ct);
            return Ok(ApiResponse<TransferCertificateSummary>.Ok(result, "Transfer certificate approved."));
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

    [HttpGet("{id:guid}/download")]
    [Authorize]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        try
        {
            var requiredStudentId = User.IsSelfServiceRole() ? User.StudentId() ?? Guid.Empty : (Guid?)null;
            var sasUrl = await _certificateService.GenerateAndGetDownloadUrlAsync(id, requiredStudentId, ct);
            return Ok(ApiResponse<object>.Ok(new { downloadUrl = sasUrl, expiresInMinutes = 15 }));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
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

    /// <summary>
    /// Public, unauthenticated verification endpoint -- mirrors the reference
    /// architecture's VerifyCertificate(code) pattern so a third party (e.g. a
    /// receiving school) can confirm authenticity without an account.
    /// </summary>
    [HttpGet("verify/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string code, CancellationToken ct)
    {
        var result = await _certificateService.VerifyAsync(code, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
