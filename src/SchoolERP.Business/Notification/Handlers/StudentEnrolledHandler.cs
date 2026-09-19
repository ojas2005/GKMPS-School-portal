using SchoolERP.Business.Notification.Services.Interfaces;
using SchoolERP.Common.Events;

namespace SchoolERP.Business.Notification.Handlers;

public class StudentEnrolledHandler : IEventHandler<StudentEnrolledEvent>
{
    private readonly INotificationService _notificationService;

    public StudentEnrolledHandler(INotificationService notificationService) => _notificationService = notificationService;

    public Task HandleAsync(StudentEnrolledEvent e, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(e.ParentEmail))
            return Task.CompletedTask;

        return _notificationService.DispatchAsync(
            eventType: "StudentEnrolled",
            recipientReference: e.ParentEmail,
            channel: "Email",
            subjectOrTitle: "Admission Confirmed",
            body: $"{e.FullName} (Admission No. {e.AdmissionNumber}) has been admitted. Welcome to the school!",
            payload: e,
            ct: ct);
    }
}
