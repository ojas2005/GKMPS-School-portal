using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolERP.Communication.DTOs;
using SchoolERP.Communication.Services.Interfaces;
using SchoolERP.Shared.Common;
using System.Security.Claims;

namespace SchoolERP.Communication.Controllers;

[ApiController]
[Route("api/parent-messages")]
[Authorize]
public class ParentMessagesController : ControllerBase
{
    private readonly IParentMessageService _messageService;

    public ParentMessagesController(IParentMessageService messageService) => _messageService = messageService;

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        if (!CanUseThread(request.StudentId))
            return Forbid();

        var senderId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var result = await _messageService.SendAsync(request, senderId, ct);
        return Ok(ApiResponse<ParentMessageSummary>.Ok(result, "Message sent."));
    }

    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetThread(Guid studentId, CancellationToken ct)
    {
        if (!CanUseThread(studentId))
            return Forbid();

        var result = await _messageService.GetThreadAsync(studentId, ct);

        // Admins oversee every conversation; anyone else only sees messages they are part of.
        if (RoleNames.Rank(User.Role()) == 0)
        {
            var me = CurrentUserId();
            result = result.Where(m => m.SenderUserId == me || m.RecipientUserId == me).ToList();
        }
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("{messageId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid messageId, CancellationToken ct)
    {
        // Only the recipient can mark a message read.
        var updated = await _messageService.MarkReadAsync(messageId, CurrentUserId(), ct);
        return updated
            ? Ok(ApiResponse<object>.Ok(new { }, "Marked as read."))
            : NotFound(ApiResponse<object>.Fail("Message not found."));
    }

    // Teacher <-> parent threads: staff (teachers and admins) and the student's own parent.
    // Students themselves, the accountant and the librarian have no part in them.
    private bool CanUseThread(Guid studentId) =>
        User.Role() is RoleNames.SuperAdmin or RoleNames.Principal or RoleNames.Admin or RoleNames.Teacher
        || (User.Role() == RoleNames.Parent && User.CanAccessStudent(studentId));

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
