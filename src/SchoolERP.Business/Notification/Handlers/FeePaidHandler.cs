using SchoolERP.Business.Notification.Services.Interfaces;
using SchoolERP.Common.Events;

namespace SchoolERP.Business.Notification.Handlers;

public class FeePaidHandler : IEventHandler<FeePaidEvent>
{
    private readonly INotificationService _notificationService;

    public FeePaidHandler(INotificationService notificationService) => _notificationService = notificationService;

    public Task HandleAsync(FeePaidEvent e, CancellationToken ct)
    {
        return _notificationService.DispatchAsync(
            eventType: "FeePaid",
            recipientReference: e.StudentId.ToString(), // resolved to a contact address by the real dispatch provider
            channel: "Push",
            subjectOrTitle: "Payment Received",
            body: $"Receipt {e.ReceiptNumber}: payment of {e.AmountPaid} {e.Currency} received.",
            payload: e,
            ct: ct);
    }
}
