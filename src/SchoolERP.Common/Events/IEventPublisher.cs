namespace SchoolERP.Common.Events;

/// <summary>
/// Publishes an application event (e.g. <see cref="FeePaidEvent"/>) to every registered
/// <see cref="IEventHandler{TEvent}"/>. Handlers run in the background, after the call
/// returns, so a slow side effect (sending an email) never delays the request that raised
/// the event. Publish only after the change that the event describes has been saved.
/// </summary>
public interface IEventPublisher
{
    ValueTask PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class;
}

/// <summary>Reacts to one type of application event.</summary>
public interface IEventHandler<in TEvent> where TEvent : class
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}
