namespace SchoolERP.Business.Notification.Services.Interfaces;

public interface INotificationService
{
    /// <summary>Forgets everything sent to these addresses (a person's data being erased).</summary>
    Task<int> ForgetRecipientsAsync(IReadOnlyCollection<string> recipientReferences, CancellationToken ct = default);

    Task DispatchAsync(string eventType, string recipientReference, string channel, string subjectOrTitle, string body, object payload, CancellationToken ct = default);
}
