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
        var senderId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var result = await _messageService.SendAsync(request, senderId, ct);
        return Ok(ApiResponse<ParentMessageSummary>.Ok(result, "Message sent."));
    }

    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetThread(Guid studentId, CancellationToken ct)
    {
        var result = await _messageService.GetThreadAsync(studentId, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("{messageId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid messageId, CancellationToken ct)
    {
        await _messageService.MarkReadAsync(messageId, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Marked as read."));
    }
}
