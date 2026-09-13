using SchoolERP.Notification.Services.Interfaces;

namespace SchoolERP.Notification.Services;

/// <summary>
/// Fallback used when no provider is configured (no Smtp:Host): logs the outbound message
/// and reports it as NOT delivered, so NotificationLogs never claim a message reached
/// someone when it did not. Set the Smtp section to send real email (see SmtpDispatchService).
/// </summary>
public class LoggingDispatchService : IDispatchService
{
    private readonly ILogger<LoggingDispatchService> _logger;

    public LoggingDispatchService(ILogger<LoggingDispatchService> logger) => _logger = logger;

    public Task<bool> SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] (not sent) to={ToEmail} subject={Subject}", toEmail, subject);
        throw new NotSupportedException("No email provider is configured (set Smtp:Host).");
    }

    public Task<bool> SendSmsAsync(string toPhone, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("[SMS] (not sent) to={ToPhone}", toPhone);
        throw new NotSupportedException("No SMS provider is configured.");
    }

    public Task<bool> SendPushAsync(string userReference, string title, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("[PUSH] (not sent) to={UserReference} title={Title}", userReference, title);
        throw new NotSupportedException("No push provider is configured.");
    }
}
