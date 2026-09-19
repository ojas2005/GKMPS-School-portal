using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolERP.Business.Common.Events;
using SchoolERP.Common.Events;

namespace SchoolERP.Tests;

public class EventBusTests
{
    private sealed class Recorder
    {
        public TaskCompletionSource<FeePaidEvent> Received { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class RecordingHandler(Recorder recorder) : IEventHandler<FeePaidEvent>
    {
        public Task HandleAsync(FeePaidEvent e, CancellationToken ct)
        {
            recorder.Received.TrySetResult(e);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHandler : IEventHandler<FeePaidEvent>
    {
        public Task HandleAsync(FeePaidEvent e, CancellationToken ct) => throw new InvalidOperationException("boom");
    }

    [Fact]
    public async Task Published_events_reach_every_handler_even_if_one_fails()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Recorder>();
        services.AddScoped<IEventHandler<FeePaidEvent>, FailingHandler>();
        services.AddScoped<IEventHandler<FeePaidEvent>, RecordingHandler>();
        var provider = services.BuildServiceProvider();

        var bus = new InProcessEventBus();
        var dispatcher = new EventDispatcher(bus, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<EventDispatcher>.Instance);
        await dispatcher.StartAsync(CancellationToken.None);

        var paid = new FeePaidEvent { PaymentId = Guid.NewGuid(), StudentId = Guid.NewGuid(), AmountPaid = 500, Currency = "INR", ReceiptNumber = "R-1" };
        await bus.PublishAsync(paid);

        var received = await provider.GetRequiredService<Recorder>().Received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(paid.PaymentId, received.PaymentId);

        await dispatcher.StopAsync(CancellationToken.None);
    }
}
