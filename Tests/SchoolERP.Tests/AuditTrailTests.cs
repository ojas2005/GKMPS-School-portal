using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolERP.Business.Common.Audit;
using SchoolERP.Common.Audit;

namespace SchoolERP.Tests;

public class AuditTrailTests
{
    private sealed class FakeSink : IAuditSink
    {
        public readonly List<AuditRecord> Written = [];
        public int Batches;
        public DateTime? PurgedBefore;
        public Task WriteAsync(IReadOnlyList<AuditRecord> records, CancellationToken ct)
        {
            lock (Written) { Written.AddRange(records); Batches++; }
            return Task.CompletedTask;
        }
        public Task<int> DeleteOlderThanAsync(DateTime cutoffUtc, CancellationToken ct) { PurgedBefore = cutoffUtc; return Task.FromResult(0); }
    }

    private static (ChannelAuditTrail Trail, AuditWriter Writer, FakeSink Sink) Create(int? retentionDays = null)
    {
        var sink = new FakeSink();
        var services = new ServiceCollection().AddScoped<IAuditSink>(_ => sink).BuildServiceProvider();
        var config = new ConfigurationBuilder().AddInMemoryCollection(
            retentionDays is null ? [] : new Dictionary<string, string?> { ["Audit:RetentionDays"] = retentionDays.ToString() }).Build();
        var trail = new ChannelAuditTrail();
        return (trail, new AuditWriter(trail, services.GetRequiredService<IServiceScopeFactory>(), config, NullLogger<AuditWriter>.Instance), sink);
    }

    private static AuditRecord Entry(int i) => new(DateTime.UtcNow, "GET /api/students/{id}", Guid.NewGuid(), "Teacher", $"record-{i}", 200);

    [Fact]
    public async Task Records_are_written_in_batches()
    {
        var (trail, writer, sink) = Create();
        await writer.StartAsync(CancellationToken.None);
        for (var i = 0; i < 450; i++) trail.Record(Entry(i));

        for (var wait = 0; wait < 100 && sink.Written.Count < 450; wait++) await Task.Delay(100);
        await writer.StopAsync(CancellationToken.None);

        Assert.Equal(450, sink.Written.Count);
        Assert.True(sink.Batches < 10, $"expected a few large batches, got {sink.Batches}");
    }

    [Fact]
    public async Task Nothing_queued_is_lost_when_the_app_shuts_down()
    {
        var (trail, writer, sink) = Create();
        await writer.StartAsync(CancellationToken.None);
        for (var i = 0; i < 25; i++) trail.Record(Entry(i));

        await writer.StopAsync(CancellationToken.None);   // e.g. scaling to zero straight away

        Assert.Equal(25, sink.Written.Count);
    }

    [Fact]
    public async Task Old_entries_are_removed_but_a_full_year_is_always_kept()
    {
        var (_, writer, sink) = Create(retentionDays: 30);   // a too-short setting is overridden
        await writer.StartAsync(CancellationToken.None);
        for (var wait = 0; wait < 50 && sink.PurgedBefore is null; wait++) await Task.Delay(50);
        await writer.StopAsync(CancellationToken.None);

        Assert.NotNull(sink.PurgedBefore);
        Assert.True(sink.PurgedBefore <= DateTime.UtcNow.AddDays(-365).AddMinutes(1));
    }

    [Fact]
    public void Recording_never_blocks_even_when_the_queue_is_full()
    {
        var trail = new ChannelAuditTrail();
        for (var i = 0; i < 25_000; i++) trail.Record(Entry(i));   // beyond the queue's capacity
    }
}
