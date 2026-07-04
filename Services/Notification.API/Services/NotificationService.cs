using System.Text.Json;
using SchoolERP.Notification.Entities;
using SchoolERP.Notification.Repositories.Interfaces;
using SchoolERP.Notification.Services.Interfaces;

namespace SchoolERP.Notification.Services;

/// <summary>
/// Central fan-out point used by every MassTransit consumer: records the attempt,
/// calls the channel-appropriate dispatch method, and flips the delivered/failed flag
/// atomically. Consumers stay thin -- they just describe *what* happened and *who*
/// to notify; this class owns *how*.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationLogRepository _logs;
    private readonly IDispatchService _dispatch;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(INotificationLogRepository logs, IDispatchService dispatch, ILogger<NotificationService> logger)
    {
        _logs = logs;
        _dispatch = dispatch;
        _logger = logger;
    }

    public async Task DispatchAsync(string eventType, string recipientReference, string channel, string subjectOrTitle, string body, object payload, CancellationToken ct = default)
    {
        var log = new NotificationLog
        {
            EventType = eventType,
            RecipientReference = recipientReference,
            DispatchChannel = channel,
            PayloadJson = JsonSerializer.Serialize(payload)
        };

        await _logs.AddAsync(log, ct);
        await _logs.SaveChangesAsync(ct);

        bool sent;
        try
        {
            sent = channel switch
            {
                "Email" => await _dispatch.SendEmailAsync(recipientReference, subjectOrTitle, body, ct),
                "SMS" => await _dispatch.SendSmsAsync(recipientReference, body, ct),
                "Push" => await _dispatch.SendPushAsync(recipientReference, subjectOrTitle, body, ct),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dispatch failed for {EventType} to {Recipient}", eventType, recipientReference);
            await _logs.MarkFailedAsync(log.Id, ex.Message, ct);
            return;
        }

        if (sent)
            await _logs.MarkDeliveredAsync(log.Id, ct);
        else
            await _logs.MarkFailedAsync(log.Id, "Dispatch returned false.", ct);
    }
}
