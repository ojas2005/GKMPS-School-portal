using MassTransit;
using SchoolERP.Notification.Services.Interfaces;
using SchoolERP.Shared.Events;

namespace SchoolERP.Notification.Consumers;

public class FeePaidConsumer : IConsumer<FeePaidEvent>
{
    private readonly INotificationService _notificationService;

    public FeePaidConsumer(INotificationService notificationService) => _notificationService = notificationService;

    public Task Consume(ConsumeContext<FeePaidEvent> context)
    {
        var e = context.Message;
        return _notificationService.DispatchAsync(
            eventType: "FeePaid",
            recipientReference: e.StudentId.ToString(), // resolved to a contact address by the real dispatch provider
            channel: "Push",
            subjectOrTitle: "Payment Received",
            body: $"Receipt {e.ReceiptNumber}: payment of {e.AmountPaid} {e.Currency} received.",
            payload: e,
            ct: context.CancellationToken);
    }
}
