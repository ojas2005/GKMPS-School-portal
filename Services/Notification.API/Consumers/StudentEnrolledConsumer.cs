using MassTransit;
using SchoolERP.Notification.Services.Interfaces;
using SchoolERP.Shared.Events;

namespace SchoolERP.Notification.Consumers;

public class StudentEnrolledConsumer : IConsumer<StudentEnrolledEvent>
{
    private readonly INotificationService _notificationService;

    public StudentEnrolledConsumer(INotificationService notificationService) => _notificationService = notificationService;

    public Task Consume(ConsumeContext<StudentEnrolledEvent> context)
    {
        var e = context.Message;
        if (string.IsNullOrWhiteSpace(e.ParentEmail))
            return Task.CompletedTask;

        return _notificationService.DispatchAsync(
            eventType: "StudentEnrolled",
            recipientReference: e.ParentEmail,
            channel: "Email",
            subjectOrTitle: "Admission Confirmed",
            body: $"{e.FullName} (Admission No. {e.AdmissionNumber}) has been admitted. Welcome to the school!",
            payload: e,
            ct: context.CancellationToken);
    }
}
