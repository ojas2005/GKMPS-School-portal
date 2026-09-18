using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Common;
using System.Security.Claims;

namespace SchoolERP.Api.Controllers.Fee;

[ApiController]
[Route("api/fee-payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IFeePaymentService _feePaymentService;

    public PaymentsController(IFeePaymentService feePaymentService) => _feePaymentService = feePaymentService;

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _feePaymentService.RecordPaymentAsync(request, ct);
            return Ok(ApiResponse<FeePaymentSummary>.Ok(result, "Payment recorded."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // A student's own fee ledger. Staff/accountant may read any student; a
    // student/parent may read only their own (enforced via the studentId claim).
    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetForStudent(Guid studentId, CancellationToken ct)
    {
        if (!User.CanAccessStudent(studentId))
            return Forbid();

        var result = await _feePaymentService.GetPaymentsForStudentAsync(studentId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    // Called right after admitting a student (and any time later to backfill new fee
    // structures): creates one due per unassessed FeeStructure in their class/year, plus
    // an optional one-time opening balance for pre-existing/pending fee at admission.
    [HttpPost("students/{studentId:guid}/assess")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
    public async Task<IActionResult> AssessDues(Guid studentId, [FromBody] AssessDuesRequest request, CancellationToken ct)
    {
        var result = await _feePaymentService.AssessDuesAsync(studentId, request, ct);
        return Ok(ApiResponse<object>.Ok(result, "Dues assessed."));
    }

    // Owner view: every student with a due in this class, grouped with their pending total.
    [HttpGet("pending-summary")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
    public async Task<IActionResult> GetPendingSummary([FromQuery] string classId, CancellationToken ct)
    {
        var result = await _feePaymentService.GetPendingSummaryByClassAsync(classId, ct);
        return Ok(ApiResponse<ClassPendingSummaryResponse>.Ok(result));
    }

    // Owner records fee submission directly from a student's detail page: amount, a
    // custom period (e.g. "May 2026 - Jun 2026") and a description, in one step -- no
    // FeeStructure needs to exist first. Immediately fully paid; a receipt is produced.
    [HttpPost("students/{studentId:guid}/submit")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
    public async Task<IActionResult> SubmitPayment(Guid studentId, [FromBody] SubmitFeePaymentRequest request, CancellationToken ct)
    {
        var result = await _feePaymentService.SubmitPaymentAsync(studentId, request, ct);
        return Ok(ApiResponse<FeeSubmissionResult>.Ok(result, "Fee submitted."));
    }

    // Creates an unpaid ad-hoc due (no payment, no receipt) -- e.g. a transport route's
    // monthly fee added the moment a student is mapped to that route.
    [HttpPost("students/{studentId:guid}/dues")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
    public async Task<IActionResult> AddAdHocDue(Guid studentId, [FromBody] AddAdHocDueRequest request, CancellationToken ct)
    {
        var result = await _feePaymentService.AddAdHocDueAsync(studentId, request, ct);
        return Ok(ApiResponse<FeePaymentSummary>.Ok(result, "Due added."));
    }

    // Every receipt for a student -- staff/teacher for any student, a student/parent
    // only their own (same scoping as the fee ledger above).
    [HttpGet("students/{studentId:guid}/transactions")]
    public async Task<IActionResult> GetTransactionsForStudent(Guid studentId, CancellationToken ct)
    {
        if (!User.CanAccessStudent(studentId))
            return Forbid();

        var result = await _feePaymentService.GetTransactionsForStudentAsync(studentId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("transactions/{transactionId:guid}/receipt")]
    [EnableRateLimiting("documents")]
    public async Task<IActionResult> GetReceipt(Guid transactionId, CancellationToken ct)
    {
        try
        {
            var requiredStudentId = User.IsSelfServiceRole() ? User.StudentId() ?? Guid.Empty : (Guid?)null;
            var url = await _feePaymentService.GetReceiptDownloadUrlAsync(transactionId, requiredStudentId, ct);
            return Ok(ApiResponse<object>.Ok(new { downloadUrl = url, expiresInMinutes = 15 }));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("waivers/request")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
    public async Task<IActionResult> RequestWaiver([FromBody] RequestWaiverRequest request, CancellationToken ct)
    {
        try
        {
            await _feePaymentService.RequestWaiverAsync(request, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "Waiver requested; awaiting approval."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("{feePaymentId:guid}/waivers/approve")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal}")]
    public async Task<IActionResult> ApproveWaiver(Guid feePaymentId, [FromBody] ApproveWaiverRequest request, CancellationToken ct)
    {
        var approvedBy = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        try
        {
            await _feePaymentService.ApproveWaiverAsync(feePaymentId, request, approvedBy, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "Waiver approved."));
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

    [HttpGet("collection-totals")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}")]
    public async Task<IActionResult> GetCollectionTotals([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        var result = await _feePaymentService.GetCollectionTotalsAsync(fromUtc, toUtc, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
