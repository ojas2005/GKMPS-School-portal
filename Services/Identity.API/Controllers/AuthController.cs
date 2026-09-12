using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SchoolERP.Identity.DTOs;
using SchoolERP.Identity.Services.Interfaces;
using SchoolERP.Shared.Common;
using System.Security.Claims;

namespace SchoolERP.Identity.Controllers;

/// <summary>
/// Thin controller: every decision (validation, token issuance, rotation) is delegated
/// to IAuthService. Login/refresh/register are gated by the stricter "auth" rate-limit
/// policy configured in Program.cs (and re-enforced at the YARP gateway).
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
        try
        {
            var result = await _authService.RegisterAsync(request, ct);
            return Ok(ApiResponse<AuthResult>.Ok(result, "Account created."));
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
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        await _authService.LogoutAsync(userId, request.RefreshToken, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Logged out."));
    }
}
