using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Common;
using SchoolERP.DataAccess.Audit;

namespace SchoolERP.Api.Controllers.Audit;

/// <summary>
/// Searches the audit trail -- e.g. everything one user did, or everyone who touched one
/// student's record. For investigating incidents and answering data-protection requests.
/// </summary>
[ApiController]
[Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal}")]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditReader _audit;

    public AuditController(IAuditReader audit) => _audit = audit;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] Guid? actorUserId, [FromQuery] string? entityId, [FromQuery] string? action,
        [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await _audit.SearchAsync(new AuditQuery(actorUserId, entityId, action, fromUtc, toUtc,
            Math.Max(1, page), Math.Clamp(pageSize, 1, 200)), ct);
        return Ok(ApiResponse<object>.Ok(result));
    }
}
