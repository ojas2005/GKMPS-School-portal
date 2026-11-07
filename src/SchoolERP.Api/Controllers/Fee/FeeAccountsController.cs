using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Api.Security;
using SchoolERP.Business.Fee.DTOs;
using SchoolERP.Business.Fee.Services.Interfaces;
using SchoolERP.Common;

namespace SchoolERP.Api.Controllers.Fee;

/// <summary>Students' fee accounts: statements, opening accounts for students admitted before
/// automatic billing, and running the billing job by hand.</summary>
[ApiController]
[Route("api/fee-accounts")]
[Authorize]
public class FeeAccountsController : ControllerBase
{
    private const string FeeStaff = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin},{RoleNames.Accountant}";

    private readonly IFeeAccountService _accounts;
    private readonly IFeeLedgerService _ledger;

    public FeeAccountsController(IFeeAccountService accounts, IFeeLedgerService ledger)
    {
        _accounts = accounts;
        _ledger = ledger;
    }

    // A student's complete statement: every fee, payment and change. Fee staff for any student;
    // a parent only for their own child (same rule as the fee ledger).
    [HttpGet("{studentId:guid}")]
    public async Task<IActionResult> Statement(Guid studentId, [FromServices] StudentAccessGuard access, CancellationToken ct)
    {
        if (!await access.CanReadAsync(User, StudentRecord.Fees, studentId, ct))
            return Forbid();
        var statement = await _ledger.StatementAsync(studentId, ct);
        return statement is null
            ? NotFound(ApiResponse<object>.Fail("Student not found."))
            : Ok(ApiResponse<FeeStatementDto>.Ok(statement));
    }

    [HttpPost("{studentId:guid}/open")]
    [Authorize(Roles = FeeStaff)]
    public async Task<IActionResult> Open(Guid studentId, [FromBody] OpenFeeAccountRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _accounts.OpenForExistingStudentAsync(studentId, request, User.UserId(), User.Role(), ct);
            return Ok(ApiResponse<FeeBreakdown>.Ok(result, "Fee account opened."));
        }
        catch (InvalidOperationException ex) { return Conflict(ApiResponse<object>.Fail(ex.Message)); }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpPost("open-all")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> OpenAll([FromBody] OpenFeeAccountRequest request, CancellationToken ct)
    {
        var result = await _accounts.OpenForAllCurrentStudentsAsync(request, User.UserId(), User.Role(), ct);
        return Ok(ApiResponse<OpenAccountsResult>.Ok(result, $"Opened {result.Opened} fee account(s)."));
    }

    // Brings accounts up to date now instead of waiting for the next billing run.
    [HttpPost("generate")]
    [Authorize(Roles = FeeStaff)]
    public async Task<IActionResult> Generate([FromQuery] Guid? studentId, CancellationToken ct)
    {
        var result = await _accounts.GenerateDueFeesAsync(studentId, ct);
        return Ok(ApiResponse<FeeGenerationResult>.Ok(result,
            result.FeesCreated == 0 ? "Everything is already up to date." : $"Created {result.FeesCreated} fee(s)."));
    }
}
