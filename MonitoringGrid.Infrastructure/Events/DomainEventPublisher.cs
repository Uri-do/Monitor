using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Infrastructure.Events;

/// <summary>
/// Simple implementation of domain event publisher
/// In a production system, this would integrate with a message bus like MediatR, RabbitMQ, or Azure Service Bus
/// </summary>
public class DomainEventPublisher : MonitoringGrid.Core.Interfaces.IDomainEventPublisher
{
    private readonly ILogger<DomainEventPublisher> _logger;

    public DomainEventPublisher(ILogger<DomainEventPublisher> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Publishing domain event: {EventType} with ID {EventId}",
                domainEvent.GetType().Name, domainEvent.EventId);

            // Simulate async processing
            await Task.Delay(1, cancellationToken);

            _logger.LogDebug("Domain event {EventType} with ID {EventId} published successfully",
                domainEvent.GetType().Name, domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish domain event: {EventType}", domainEvent.GetType().Name);
            throw;
        }
    }
}
