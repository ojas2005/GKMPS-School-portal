using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Business.Identity.DTOs;
using SchoolERP.Business.Identity.Services.Interfaces;
using SchoolERP.Common;
using System.Security.Claims;

namespace SchoolERP.Api.Controllers.Identity;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    // Admins can look up anyone (the student/teacher detail pages need the login id);
    // everyone else only themselves -- UserSummary carries email/username/last-login,
    // which one student has no business reading about another.
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var callerRole = User.FindFirstValue(ClaimTypes.Role);
        var isAdmin = callerRole is RoleNames.SuperAdmin or RoleNames.Principal or RoleNames.Admin;
        var callerUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!isAdmin && callerUserId != id.ToString())
        {
            return Forbid();
        }

        var user = await _userService.GetByIdAsync(id, ct);
        return user is null
            ? NotFound(ApiResponse<object>.Fail("User not found."))
            : Ok(ApiResponse<object>.Ok(user));
    }

    [HttpGet]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> Search([FromQuery] string? role, [FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await _userService.SearchAsync(role, keyword, page, pageSize, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> SetActiveStatus(Guid id, [FromQuery] bool isActive, CancellationToken ct)
    {
        var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "unknown";

        try
        {
            await _userService.SetActiveStatusAsync(id, isActive, actorUserId, actorRole, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "Status updated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // Owner/admin sets a new password directly (no knowledge of the old one needed) --
    // mirrors how accounts are created in the first place: the owner chooses credentials
    // and hands them to the person. Passwords are one-way hashed, so there is no "view"
    // equivalent -- only reset.
    [HttpPatch("{id:guid}/password")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> SetPassword(Guid id, [FromBody] SetPasswordRequest request, CancellationToken ct)
    {
        var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "unknown";

        try
        {
            await _userService.SetPasswordAsync(id, request.NewPassword, actorUserId, actorRole, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "Password updated."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    // Rollback for a failed onboarding: removes a login that has never been used.
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
    public async Task<IActionResult> DeleteUnused(Guid id, CancellationToken ct)
    {
        var actorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        var actorRole = User.FindFirstValue(ClaimTypes.Role) ?? "unknown";

        try
        {
            await _userService.DeleteUnusedAsync(id, actorUserId, actorRole, ct);
            return Ok(ApiResponse<object>.Ok(new { }, "Account removed."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
