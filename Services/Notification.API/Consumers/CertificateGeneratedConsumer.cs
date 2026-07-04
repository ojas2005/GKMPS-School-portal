using MassTransit;
using SchoolERP.Notification.Services.Interfaces;
using SchoolERP.Shared.Events;

namespace SchoolERP.Notification.Consumers;

public class CertificateGeneratedConsumer : IConsumer<CertificateGeneratedEvent>
{
    private readonly INotificationService _notificationService;

    public CertificateGeneratedConsumer(INotificationService notificationService) => _notificationService = notificationService;

    public Task Consume(ConsumeContext<CertificateGeneratedEvent> context)
    {
        var e = context.Message;
        return _notificationService.DispatchAsync(
            eventType: "CertificateGenerated",
            recipientReference: e.SubjectId.ToString(),
            channel: "Push",
            subjectOrTitle: $"{e.DocumentType} Ready",
            // Only the verification code is carried, never a raw SAS URL -- matches the
            // "never leak time-limited access via a notification payload" rule.
            body: $"Your {e.DocumentType} is ready. Verification code: {e.VerificationCode}",
            payload: e,
            ct: context.CancellationToken);
    }
}
