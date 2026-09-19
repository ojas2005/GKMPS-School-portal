namespace SchoolERP.Business.Notification.DTOs;

public record NotificationLogSummary(Guid Id, string EventType, string RecipientReference, string DispatchChannel, bool IsDelivered, DateTime CreatedAtUtc);
