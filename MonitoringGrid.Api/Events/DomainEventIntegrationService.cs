using MediatR;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Enums;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.Events;

/// <summary>
/// Service that demonstrates domain event integration and provides event statistics
/// </summary>
public class DomainEventIntegrationService
{
    private readonly IMediator _mediator;
    private readonly ILogger<DomainEventIntegrationService> _logger;
    private readonly IDomainEventPublisher _domainEventPublisher;

    // Event statistics
    private static readonly Dictionary<string, int> EventCounts = new();
    private static readonly Dictionary<string, DateTime> LastEventTimes = new();

    public DomainEventIntegrationService(
        IMediator mediator,
        ILogger<DomainEventIntegrationService> logger,
        IDomainEventPublisher domainEventPublisher)
    {
        _mediator = mediator;
        _logger = logger;
        _domainEventPublisher = domainEventPublisher;
    }

    /// <summary>
    /// Publishes a domain event and tracks statistics
    /// </summary>
    public async Task PublishEventAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent).Name;
        
        _logger.LogInformation("Publishing domain event: {EventType} with ID {EventId}", 
            eventType, domainEvent.EventId);

        try
        {
            // Update statistics
            UpdateEventStatistics(eventType);

            // Publish the event
            await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);

            _logger.LogDebug("Successfully published domain event: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish domain event: {EventType}", eventType);
            throw;
        }
    }

    /// <summary>
    /// Gets event statistics for monitoring
    /// </summary>
    public DomainEventStatistics GetEventStatistics()
    {
        return new DomainEventStatistics
        {
            TotalEvents = EventCounts.Values.Sum(),
            EventCounts = new Dictionary<string, int>(EventCounts),
            LastEventTimes = new Dictionary<string, DateTime>(LastEventTimes),
            MostActiveEventType = EventCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "None",
            EventTypes = EventCounts.Keys.ToList()
        };
    }

    /// <summary>
    /// Demonstrates event-driven workflow by simulating a complete KPI lifecycle
    /// </summary>
    public async Task DemonstrateEventDrivenWorkflowAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting domain event workflow demonstration");

        try
        {
            // Simulate KPI creation
            var demoKpi = new KPI
            {
                KpiId = 999,
                Indicator = "Demo KPI",
                Owner = "System",
                Priority = 1,
                Frequency = 60
            };
            var kpiCreatedEvent = new KpiCreatedEvent(demoKpi);

            await PublishEventAsync(kpiCreatedEvent, cancellationToken);

            // Wait a bit to simulate processing time
            await Task.Delay(100, cancellationToken);

            // Simulate KPI execution
            var kpiExecutedEvent = new KpiExecutedEvent(
                kpiId: 999,
                indicator: "Demo KPI",
                owner: "System",
                wasSuccessful: true,
                currentValue: 95.5m,
                historicalValue: 98.2m);

            await PublishEventAsync(kpiExecutedEvent, cancellationToken);

            // Wait a bit more
            await Task.Delay(100, cancellationToken);

            // Simulate threshold breach
            var thresholdBreachedEvent = new KpiThresholdBreachedEvent(
                kpiId: 999,
                indicator: "Demo KPI",
                currentValue: 85.0m,
                historicalValue: 98.2m,
                deviation: 13.4m,
                severity: "Medium");

            await PublishEventAsync(thresholdBreachedEvent, cancellationToken);

            // Wait a bit more
            await Task.Delay(100, cancellationToken);

            // Simulate KPI deactivation
            var kpiDeactivatedEvent = new KpiDeactivatedEvent(
                kpiId: 999,
                indicator: "Demo KPI",
                reason: "Demo completed",
                deactivatedBy: "DemoService");

            await PublishEventAsync(kpiDeactivatedEvent, cancellationToken);

            _logger.LogInformation("Domain event workflow demonstration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during domain event workflow demonstration");
            throw;
        }
    }

    /// <summary>
    /// Resets event statistics (useful for testing)
    /// </summary>
    public void ResetStatistics()
    {
        EventCounts.Clear();
        LastEventTimes.Clear();
        _logger.LogInformation("Domain event statistics reset");
    }

    private static void UpdateEventStatistics(string eventType)
    {
        lock (EventCounts)
        {
            EventCounts[eventType] = EventCounts.GetValueOrDefault(eventType, 0) + 1;
            LastEventTimes[eventType] = DateTime.UtcNow;
        }
    }
}

/// <summary>
/// Domain event statistics model
/// </summary>
public class DomainEventStatistics
{
    public int TotalEvents { get; set; }
    public Dictionary<string, int> EventCounts { get; set; } = new();
    public Dictionary<string, DateTime> LastEventTimes { get; set; } = new();
    public string MostActiveEventType { get; set; } = string.Empty;
    public List<string> EventTypes { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
