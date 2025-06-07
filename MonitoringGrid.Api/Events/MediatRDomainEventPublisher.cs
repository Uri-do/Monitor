using MediatR;
using MonitoringGrid.Core.Events;

namespace MonitoringGrid.Api.Events;

/// <summary>
/// MediatR-based implementation of domain event publisher
/// </summary>
public class MediatRDomainEventPublisher : IDomainEventPublisher
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediatRDomainEventPublisher> _logger;

    public MediatRDomainEventPublisher(IMediator mediator, ILogger<MediatRDomainEventPublisher> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
        where TEvent : IDomainEvent
    {
        _logger.LogInformation("Publishing domain event via MediatR: {EventType} with ID {EventId}", 
            typeof(TEvent).Name, domainEvent.EventId);

        try
        {
            // Wrap the domain event in a MediatR notification
            var notification = new DomainEventNotification<TEvent>(domainEvent);
            
            // Publish through MediatR - this will find and execute all registered handlers
            await _mediator.Publish(notification, cancellationToken);
            
            _logger.LogDebug("Successfully published domain event {EventType} with ID {EventId} via MediatR", 
                typeof(TEvent).Name, domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish domain event {EventType} with ID {EventId} via MediatR", 
                typeof(TEvent).Name, domainEvent.EventId);
            throw;
        }
    }

    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var eventList = domainEvents.ToList();
        if (!eventList.Any()) return;

        _logger.LogInformation("Publishing {EventCount} domain events via MediatR", eventList.Count);

        var publishTasks = new List<Task>();

        foreach (var domainEvent in eventList)
        {
            // Use reflection to create the correct notification type and publish
            var eventType = domainEvent.GetType();
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
            var notification = Activator.CreateInstance(notificationType, domainEvent);

            if (notification is INotification mediatrNotification)
            {
                var publishTask = _mediator.Publish(mediatrNotification, cancellationToken);
                publishTasks.Add(publishTask);
                
                _logger.LogDebug("Queued domain event for publishing: {EventType} with ID {EventId}", 
                    eventType.Name, domainEvent.EventId);
            }
            else
            {
                _logger.LogWarning("Failed to create MediatR notification for domain event: {EventType} with ID {EventId}", 
                    eventType.Name, domainEvent.EventId);
            }
        }

        try
        {
            // Execute all publish operations in parallel
            await Task.WhenAll(publishTasks);
            
            _logger.LogInformation("Successfully published {EventCount} domain events via MediatR", eventList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish some domain events via MediatR");
            throw;
        }
    }
}
