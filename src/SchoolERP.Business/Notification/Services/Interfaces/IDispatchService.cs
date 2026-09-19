namespace SchoolERP.Business.Notification.Services.Interfaces;

/// <summary>
/// Abstraction over the actual send mechanism. A real deployment plugs in SendGrid/SES
/// for Email, Twilio/MSG91 for SMS, and FCM/APNs for Push behind this interface; nothing
/// else in the service needs to change.
/// </summary>
public interface IDispatchService
{
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct = default);
    Task<bool> SendSmsAsync(string toPhone, string message, CancellationToken ct = default);
    Task<bool> SendPushAsync(string userReference, string title, string body, CancellationToken ct = default);
}
