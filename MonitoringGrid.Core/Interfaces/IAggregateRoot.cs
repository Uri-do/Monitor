using MonitoringGrid.Core.Events;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for aggregate root entities that can raise domain events
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets the domain events raised by this aggregate
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Adds a domain event to this aggregate
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    void AddDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    /// Clears all domain events from this aggregate
    /// </summary>
    void ClearDomainEvents();
}
