using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Notification.Repositories.Interfaces;
using SchoolERP.Shared.Common;

namespace SchoolERP.Notification.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Roles = $"{RoleNames.SuperAdmin},{RoleNames.Principal},{RoleNames.Admin}")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationLogRepository _logs;

    public NotificationsController(INotificationLogRepository logs) => _logs = logs;

    [HttpGet]
    public async Task<IActionResult> GetByRecipient([FromQuery] string recipientReference, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var logs = await _logs.FindByRecipientAsync(recipientReference, page, pageSize, ct);
        var result = logs.Select(l => new { l.Id, l.EventType, l.RecipientReference, l.DispatchChannel, l.IsDelivered, l.CreatedAtUtc });
        return Ok(ApiResponse<object>.Ok(result));
    }
}
