using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolERP.DataAccess.Identity.Repositories.Interfaces;
using SchoolERP.DataAccess.Notification.Repositories.Interfaces;

namespace SchoolERP.Business.Common.Retention;

/// <summary>
/// Deletes records that have served their purpose (docs/DATA-RETENTION.md): finished sign-in
/// sessions and their refresh tokens after Retention:SessionDays (30), and notification delivery
/// records after Retention:NotificationDays (400). Sign-in history itself stays in the audit
/// trail. The app sleeps when idle, so this runs a minute after each start and then daily.
/// </summary>
public sealed class RetentionCleanup : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<RetentionCleanup> _logger;
    private readonly int _sessionDays;
    private readonly int _notificationDays;

    public RetentionCleanup(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<RetentionCleanup> logger)
    {
        _scopes = scopes;
        _logger = logger;
        // Sessions last at most 7 days, so anything shorter than that would cut live ones.
        _sessionDays = Math.Max(8, configuration.GetValue("Retention:SessionDays", 30));
        _notificationDays = Math.Max(30, configuration.GetValue("Retention:NotificationDays", 400));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);   // keep waking up fast
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(DateTime.UtcNow, stoppingToken);
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    public async Task RunOnceAsync(DateTime nowUtc, CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var services = scope.ServiceProvider;
            var sessions = await services.GetRequiredService<IUserSessionRepository>().DeleteFinishedBeforeAsync(nowUtc.AddDays(-_sessionDays), ct);
            var tokens = await services.GetRequiredService<IRefreshTokenRepository>().DeleteFinishedBeforeAsync(nowUtc.AddDays(-_sessionDays), ct);
            var notifications = await services.GetRequiredService<INotificationLogRepository>().DeleteOlderThanAsync(nowUtc.AddDays(-_notificationDays), ct);
            if (sessions + tokens + notifications > 0)
                _logger.LogInformation("Retention clean-up removed {Sessions} sessions, {Tokens} refresh tokens, {Notifications} notification records",
                    sessions, tokens, notifications);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Retention clean-up failed; will retry later");
        }
    }
}
