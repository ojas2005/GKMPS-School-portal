using MassTransit;
using SchoolERP.Notification.Services.Interfaces;
using SchoolERP.Shared.Events;

namespace SchoolERP.Notification.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly INotificationService _notificationService;

    public UserRegisteredConsumer(INotificationService notificationService) => _notificationService = notificationService;

    public Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var e = context.Message;
        return _notificationService.DispatchAsync(
            eventType: "UserRegistered",
            recipientReference: e.Email,
            channel: "Email",
            subjectOrTitle: "Welcome to the School Portal",
            body: $"Hi, your {e.Role} account has been created. Please set your password to get started.",
            payload: e,
            ct: context.CancellationToken);
    }
}
