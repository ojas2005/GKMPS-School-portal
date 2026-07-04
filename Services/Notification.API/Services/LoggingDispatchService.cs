using SchoolERP.Notification.Services.Interfaces;

namespace SchoolERP.Notification.Services;

/// <summary>
/// Default implementation: logs the outbound message instead of calling a real
/// provider. Swap this for a SendGrid/Twilio/FCM-backed implementation in production
/// by registering a different IDispatchService in Program.cs -- no other code changes.
/// </summary>
public class LoggingDispatchService : IDispatchService
{
    private readonly ILogger<LoggingDispatchService> _logger;

    public LoggingDispatchService(ILogger<LoggingDispatchService> logger) => _logger = logger;

    public Task<bool> SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] to={ToEmail} subject={Subject}", toEmail, subject);
        return Task.FromResult(true);
    }

    public Task<bool> SendSmsAsync(string toPhone, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("[SMS] to={ToPhone} message={Message}", toPhone, message);
        return Task.FromResult(true);
    }

    public Task<bool> SendPushAsync(string userReference, string title, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("[PUSH] to={UserReference} title={Title}", userReference, title);
        return Task.FromResult(true);
    }
}
