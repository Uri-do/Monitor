using MediatR;
using MonitoringGrid.Core.Events;

namespace MonitoringGrid.Api.Events;

/// <summary>
/// Wrapper interface to make domain events compatible with MediatR notifications
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type</typeparam>
public interface IDomainEventNotification<out TDomainEvent> : INotification
    where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// The wrapped domain event
    /// </summary>
    TDomainEvent DomainEvent { get; }
}

/// <summary>
/// Generic wrapper for domain events to make them MediatR notifications
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type</typeparam>
public class DomainEventNotification<TDomainEvent> : IDomainEventNotification<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    public DomainEventNotification(TDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }

    public TDomainEvent DomainEvent { get; }
}

/// <summary>
/// Base interface for MediatR domain event handlers
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type</typeparam>
public interface IDomainEventNotificationHandler<TDomainEvent> : INotificationHandler<DomainEventNotification<TDomainEvent>>
    where TDomainEvent : IDomainEvent
{
}

/// <summary>
/// Abstract base class for domain event handlers that provides a cleaner interface
/// </summary>
/// <typeparam name="TDomainEvent">The domain event type</typeparam>
public abstract class DomainEventNotificationHandler<TDomainEvent> : IDomainEventNotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    public async Task Handle(DomainEventNotification<TDomainEvent> notification, CancellationToken cancellationToken)
    {
        await HandleDomainEvent(notification.DomainEvent, cancellationToken);
    }

    /// <summary>
    /// Handle the domain event
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected abstract Task HandleDomainEvent(TDomainEvent domainEvent, CancellationToken cancellationToken);
}
