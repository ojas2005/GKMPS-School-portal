using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Communication.DTOs;

public record CreateAnnouncementRequest([Required] string Title, [Required] string Body, string? TargetRolesCsv, string? TargetClassId, DateTime? ExpiresAtUtc);
public record AnnouncementSummary(Guid Id, string Title, string Body, string? TargetRolesCsv, string? TargetClassId, DateTime PublishedAtUtc);

public record SendMessageRequest([Required] Guid StudentId, [Required] Guid RecipientUserId, [Required] string Body);
public record ParentMessageSummary(Guid Id, Guid StudentId, Guid SenderUserId, Guid RecipientUserId, string Body, bool IsRead, DateTime CreatedAtUtc);
