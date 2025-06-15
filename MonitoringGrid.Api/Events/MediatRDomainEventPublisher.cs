using MediatR;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.Events;

/// <summary>
/// MediatR-based implementation of domain event publisher
/// </summary>
public class MediatRDomainEventPublisher : MonitoringGrid.Core.Interfaces.IDomainEventPublisher
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediatRDomainEventPublisher> _logger;

    public MediatRDomainEventPublisher(IMediator mediator, ILogger<MediatRDomainEventPublisher> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing domain event via MediatR: {EventType} with ID {EventId}",
            domainEvent.GetType().Name, domainEvent.EventId);

        try
        {
            // Use reflection to create the correct notification type and publish
            var eventType = domainEvent.GetType();
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
            var notification = Activator.CreateInstance(notificationType, domainEvent);

            if (notification is INotification mediatrNotification)
            {
                await _mediator.Publish(mediatrNotification, cancellationToken);

                _logger.LogDebug("Successfully published domain event {EventType} with ID {EventId} via MediatR",
                    eventType.Name, domainEvent.EventId);
            }
            else
            {
                throw new InvalidOperationException($"Failed to create MediatR notification for domain event: {eventType.Name}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish domain event {EventType} with ID {EventId} via MediatR",
                domainEvent.GetType().Name, domainEvent.EventId);
            throw;
        }
    }


}
