using SchoolERP.Business.Notification.Services.Interfaces;
using SchoolERP.Common.Events;

namespace SchoolERP.Business.Notification.Handlers;

public class UserRegisteredHandler : IEventHandler<UserRegisteredEvent>
{
    private readonly INotificationService _notificationService;

    public UserRegisteredHandler(INotificationService notificationService) => _notificationService = notificationService;

    public Task HandleAsync(UserRegisteredEvent e, CancellationToken ct)
    {
        return _notificationService.DispatchAsync(
            eventType: "UserRegistered",
            recipientReference: e.Email,
            channel: "Email",
            subjectOrTitle: "Welcome to the School Portal",
            body: $"Hi, your {e.Role} account on the school portal has been created. The school office will give you your login ID and password.",
            payload: e,
            ct: ct);
    }
}
