using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolERP.Common.Audit;

namespace SchoolERP.Business.Common.Audit;

/// <summary>Queues audit records for <see cref="AuditWriter"/>. Never blocks the caller.</summary>
public sealed class ChannelAuditTrail : IAuditTrail
{
    private readonly Channel<AuditRecord> _queue = Channel.CreateBounded<AuditRecord>(new BoundedChannelOptions(20_000)
    {
        // Under an extreme burst, keep the newest records rather than stall requests.
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true
    });

    public ChannelReader<AuditRecord> Reader => _queue.Reader;

    public void Record(AuditRecord record) => _queue.Writer.TryWrite(record);

    public void Complete() => _queue.Writer.TryComplete();
}

/// <summary>
/// Writes queued audit records to the database in batches, and deletes entries older than
/// the retention period once per start (the app scales to zero, so it starts at least daily
/// on a school day). On shutdown it drains what's left, so a scale-down doesn't lose records.
/// </summary>
public sealed class AuditWriter : BackgroundService
{
    private const int BatchSize = 200;
    private static readonly TimeSpan FlushEvery = TimeSpan.FromSeconds(3);

    private readonly ChannelAuditTrail _trail;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<AuditWriter> _logger;
    private readonly int _retentionDays;

    public AuditWriter(ChannelAuditTrail trail, IServiceScopeFactory scopes, IConfiguration configuration, ILogger<AuditWriter> logger)
    {
        _trail = trail;
        _scopes = scopes;
        _logger = logger;
        _retentionDays = Math.Max(365, configuration.GetValue("Audit:RetentionDays", 400));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await PurgeExpiredAsync(stoppingToken);

        var batch = new List<AuditRecord>(BatchSize);
        try
        {
            while (await _trail.Reader.WaitToReadAsync(stoppingToken))
            {
                // Gather what arrives within the flush window, then write it in one go.
                var deadline = DateTime.UtcNow + FlushEvery;
                while (batch.Count < BatchSize && DateTime.UtcNow < deadline)
                {
                    while (batch.Count < BatchSize && _trail.Reader.TryRead(out var record))
                        batch.Add(record);
                    if (batch.Count < BatchSize) await Task.Delay(200, stoppingToken);
                }
                await WriteAsync(batch, CancellationToken.None);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        // Shutting down: write whatever is still queued.
        _trail.Complete();
        while (_trail.Reader.TryRead(out var record)) batch.Add(record);
        await WriteAsync(batch, CancellationToken.None);
    }

    private async Task WriteAsync(List<AuditRecord> batch, CancellationToken ct)
    {
        if (batch.Count == 0) return;
        try
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IAuditSink>().WriteAsync(batch, ct);
        }
        catch (Exception ex)
        {
            // The trail must never take the app down; the console log is the fallback.
            _logger.LogError(ex, "Could not write {Count} audit records", batch.Count);
        }
        batch.Clear();
    }

    private async Task PurgeExpiredAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var removed = await scope.ServiceProvider.GetRequiredService<IAuditSink>()
                .DeleteOlderThanAsync(DateTime.UtcNow.AddDays(-_retentionDays), ct);
            if (removed > 0) _logger.LogInformation("Removed {Count} audit entries older than {Days} days", removed, _retentionDays);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Audit retention clean-up failed; will retry on next start");
        }
    }
}
