namespace SchoolERP.Business.Notification.Services.Interfaces;

public interface INotificationService
{
    Task DispatchAsync(string eventType, string recipientReference, string channel, string subjectOrTitle, string body, object payload, CancellationToken ct = default);
}
