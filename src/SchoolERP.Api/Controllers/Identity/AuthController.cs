using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SchoolERP.Business.Identity.DTOs;
using SchoolERP.Business.Identity.Services.Interfaces;
using SchoolERP.Common;
using System.Security.Claims;

namespace SchoolERP.Api.Controllers.Identity;

/// <summary>
/// Thin controller: every decision (validation, token issuance, rotation) is delegated
/// to IAuthService. Login/refresh/register are gated by the stricter "auth" rate-limit
/// policy configured in Program.cs.
/// </summary>
[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    // Self-registration is disabled: only the school owner/admin creates accounts and
    // hands the login ID + password to the student/teacher.
    [HttpPost("register")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        // Accounts can only be created strictly below the caller's own rank (the owner can
        // create anything) -- otherwise an Admin could mint themselves a SuperAdmin login.
        if (!RoleNames.CanManageRole(User.Role(), request.Role))
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<object>.Fail($"You are not allowed to create '{request.Role}' accounts."));

        try
        {
            var result = await _authService.RegisterAsync(request, ct);
            return Ok(ApiResponse<RegisteredUser>.Ok(result, "Account created."));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.LoginAsync(request, ip, ct);
            return Ok(ApiResponse<AuthResult>.Ok(result, "Login successful."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("login/two-factor")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginTwoFactor([FromBody] TwoFactorLoginRequest request, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.CompleteTwoFactorLoginAsync(request, ip, ct);
            return Ok(ApiResponse<AuthResult>.Ok(result, "Login successful."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.RefreshAsync(request, ip, ct);
            return Ok(ApiResponse<AuthResult>.Ok(result, "Token refreshed."));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request.RefreshToken, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Logged out."));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _authService.ChangePasswordAsync(userId, request, ip, ct);
            return Ok(ApiResponse<AuthResult>.Ok(result, "Password changed."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
