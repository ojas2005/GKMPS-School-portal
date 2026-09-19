using System.Net;
using System.Net.Mail;
using SchoolERP.Business.Notification.Services.Interfaces;

namespace SchoolERP.Business.Notification.Services;

/// <summary>
/// Sends Email through any SMTP relay (Gmail/Workspace, SES, SendGrid, Mailgun, ...),
/// configured via the Smtp section (Smtp__Host, Smtp__Port, Smtp__Username, Smtp__Password,
/// Smtp__From, Smtp__EnableSsl). Registered in Program.cs only when Smtp:Host is set.
/// SMS and Push still have no provider, so they are reported as not delivered.
/// </summary>
public class SmtpDispatchService : IDispatchService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpDispatchService> _logger;

    public SmtpDispatchService(IConfiguration configuration, ILogger<SmtpDispatchService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        // Accounts created without a real address get a placeholder like "<loginid>@gkmps.local".
        if (!MailAddress.TryCreate(toEmail, out var to) || to.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"'{toEmail}' is not a deliverable email address.");

        var section = _configuration.GetSection("Smtp");
        var from = section["From"] ?? section["Username"]
            ?? throw new InvalidOperationException("Smtp:From (or Smtp:Username) must be configured.");

        using var client = new SmtpClient(section["Host"], section.GetValue("Port", 587))
        {
            EnableSsl = section.GetValue("EnableSsl", true),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        if (!string.IsNullOrEmpty(section["Username"]))
            client.Credentials = new NetworkCredential(section["Username"], section["Password"]);

        using var message = new MailMessage(new MailAddress(from, section["FromName"] ?? "GKMPS School Portal"), to)
        {
            Subject = subject,
            Body = body
        };

        await client.SendMailAsync(message, ct);
        _logger.LogInformation("[EMAIL] sent to={ToEmail} subject={Subject}", toEmail, subject);
        return true;
    }

    public Task<bool> SendSmsAsync(string toPhone, string message, CancellationToken ct = default) =>
        throw new NotSupportedException("No SMS provider is configured.");

    public Task<bool> SendPushAsync(string userReference, string title, string body, CancellationToken ct = default) =>
        throw new NotSupportedException("No push provider is configured.");
}
