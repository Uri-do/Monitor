using MonitoringGrid.Core.Events;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for publishing domain events
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a domain event
    /// </summary>
    /// <param name="domainEvent">The domain event to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
