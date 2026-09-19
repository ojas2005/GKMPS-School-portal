using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Identity.TwoFactor;
using SchoolERP.Common;

namespace SchoolERP.Api.Controllers.Account;

public record TwoFactorCodeRequest([Required] string Code);
public record TwoFactorDisableRequest([Required] string Password, [Required] string Code);

/// <summary>The signed-in user's own two-step sign-in (authenticator app).</summary>
[ApiController]
[Authorize]
[Route("api/account/two-factor")]
public class TwoFactorController : ControllerBase
{
    private readonly ITwoFactorService _twoFactor;

    public TwoFactorController(ITwoFactorService twoFactor) => _twoFactor = twoFactor;

    private Guid Me => User.UserId() ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> Status(CancellationToken ct) =>
        Ok(ApiResponse<TwoFactorStatus>.Ok(await _twoFactor.GetStatusAsync(Me, ct)));

    /// <summary>Step 1: a new secret for the authenticator app (as a QR link and as a typed key).</summary>
    [HttpPost("setup")]
    public Task<IActionResult> Setup(CancellationToken ct) => Run(async () =>
        Ok(ApiResponse<TwoFactorSetup>.Ok(await _twoFactor.BeginSetupAsync(Me, ct))));

    /// <summary>Step 2: a code from the app proves it's set up; returns the one-time recovery codes.</summary>
    [HttpPost("enable")]
    public Task<IActionResult> Enable([FromBody] TwoFactorCodeRequest request, CancellationToken ct) => Run(async () =>
        Ok(ApiResponse<object>.Ok(new { recoveryCodes = await _twoFactor.EnableAsync(Me, request.Code, ct) },
            "Two-step sign-in is on. Keep the recovery codes somewhere safe.")));

    [HttpPost("disable")]
    public Task<IActionResult> Disable([FromBody] TwoFactorDisableRequest request, CancellationToken ct) => Run(async () =>
    {
        await _twoFactor.DisableAsync(Me, request.Password, request.Code, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Two-step sign-in is off."));
    });

    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse<object>.Fail(ex.Message)); }
    }
}
