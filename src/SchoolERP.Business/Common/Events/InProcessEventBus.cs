using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolERP.Common.Events;

namespace SchoolERP.Business.Common.Events;

/// <summary>
/// In-process replacement for the RabbitMQ/MassTransit bus the microservices used: events
/// go onto a bounded in-memory queue and <see cref="EventDispatcher"/> hands each one to its
/// handlers on a background thread, in a fresh DI scope.
///
/// Trade-off: queued events live in memory, so an event raised in the moment before the
/// process stops is lost. Events only drive notifications (which are also recorded in
/// NotificationLogs), so that is acceptable here; no business data depends on them.
/// </summary>
public sealed class InProcessEventBus : IEventPublisher
{
    private readonly Channel<object> _queue = Channel.CreateBounded<object>(new BoundedChannelOptions(10_000)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true
    });

    public ChannelReader<object> Reader => _queue.Reader;

    public ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class =>
        _queue.Writer.WriteAsync(@event, ct);
}

/// <summary>Background worker that delivers queued events to their handlers.</summary>
public sealed class EventDispatcher : BackgroundService
{
    private readonly InProcessEventBus _bus;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventDispatcher> _logger;

    public EventDispatcher(InProcessEventBus bus, IServiceScopeFactory scopeFactory, ILogger<EventDispatcher> logger)
    {
        _bus = bus;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var @event in _bus.Reader.ReadAllAsync(stoppingToken))
        {
            var handlerType = typeof(IEventHandler<>).MakeGenericType(@event.GetType());
            using var scope = _scopeFactory.CreateScope();

            foreach (var handler in scope.ServiceProvider.GetServices(handlerType))
            {
                try
                {
                    await ((Task)handlerType.GetMethod(nameof(IEventHandler<object>.HandleAsync))!
                        .Invoke(handler, new[] { @event, stoppingToken })!);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // One failing handler must not stop the others or the dispatcher itself.
                    _logger.LogError(ex, "Event handler {Handler} failed for {EventType}", handler!.GetType().Name, @event.GetType().Name);
                }
            }
        }
    }
}
