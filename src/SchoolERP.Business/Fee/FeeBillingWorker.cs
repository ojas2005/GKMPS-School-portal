using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolERP.Business.Fee.Services.Interfaces;

namespace SchoolERP.Business.Fee;

/// <summary>
/// Keeps fee accounts up to date: creates each month's tuition and transport fees and each
/// session's exam fees as their time comes. The app sleeps when idle, so this runs shortly
/// after every start and then every FeeBilling:IntervalHours (6) while awake; months missed
/// while asleep are caught up on the next run. Running twice can't double-bill (unique index).
/// Turn off with FeeBilling:Enabled=false.
/// </summary>
public sealed class FeeBillingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<FeeBillingWorker> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _interval;

    public FeeBillingWorker(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<FeeBillingWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
        _enabled = configuration.GetValue("FeeBilling:Enabled", true);
        _interval = TimeSpan.FromHours(Math.Clamp(configuration.GetValue("FeeBilling:IntervalHours", 6), 1, 24));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled) return;
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);   // let the first requests through
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunOnceAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IFeeAccountService>().GenerateDueFeesAsync(null, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Fee billing run failed; it will be retried on the next run");
        }
    }
}
