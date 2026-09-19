using SchoolERP.Business.Notification.Services.Interfaces;
using SchoolERP.Common.Events;

namespace SchoolERP.Business.Notification.Handlers;

public class CertificateGeneratedHandler : IEventHandler<CertificateGeneratedEvent>
{
    private readonly INotificationService _notificationService;

    public CertificateGeneratedHandler(INotificationService notificationService) => _notificationService = notificationService;

    public Task HandleAsync(CertificateGeneratedEvent e, CancellationToken ct)
    {
        return _notificationService.DispatchAsync(
            eventType: "CertificateGenerated",
            recipientReference: e.SubjectId.ToString(),
            channel: "Push",
            subjectOrTitle: $"{e.DocumentType} Ready",
            // Only the verification code is carried, never a raw SAS URL -- matches the
            // "never leak time-limited access via a notification payload" rule.
            body: $"Your {e.DocumentType} is ready. Verification code: {e.VerificationCode}",
            payload: e,
            ct: ct);
    }
}
