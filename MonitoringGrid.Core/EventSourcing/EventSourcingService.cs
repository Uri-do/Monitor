using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Core.EventSourcing;

/// <summary>
/// Service for handling event sourcing operations and audit trail management
/// </summary>
public interface IEventSourcingService
{
    /// <summary>
    /// Publish and store domain events for audit trail
    /// </summary>
    Task PublishAndStoreEventsAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit trail for a specific entity
    /// </summary>
    Task<IEnumerable<AuditTrailEntry>> GetAuditTrailAsync(string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit trail with filtering options
    /// </summary>
    Task<IEnumerable<AuditTrailEntry>> GetAuditTrailAsync(AuditTrailQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get system-wide audit trail
    /// </summary>
    Task<IEnumerable<AuditTrailEntry>> GetSystemAuditTrailAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replay events to rebuild entity state (for event sourcing)
    /// </summary>
    Task<T?> ReplayEventsAsync<T>(string streamId, CancellationToken cancellationToken = default) where T : class, new();
}

public class EventSourcingService : IEventSourcingService
{
    private readonly IEventStore _eventStore;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<EventSourcingService> _logger;

    public EventSourcingService(
        IEventStore eventStore,
        IDomainEventPublisher eventPublisher,
        ILogger<EventSourcingService> logger)
    {
        _eventStore = eventStore;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task PublishAndStoreEventsAsync(IEnumerable<DomainEvent> events, CancellationToken cancellationToken = default)
    {
        var eventList = events.ToList();
        if (!eventList.Any()) return;

        try
        {
            // Group events by stream ID for efficient storage
            var eventGroups = eventList.GroupBy(e => GetStreamIdForEvent(e));

            foreach (var group in eventGroups)
            {
                await _eventStore.AppendEventsAsync(group.Key, group, cancellationToken: cancellationToken);
            }

            // Publish events for immediate processing
            await _eventPublisher.PublishAsync(eventList, cancellationToken);

            _logger.LogInformation("Published and stored {EventCount} domain events across {StreamCount} streams",
                eventList.Count, eventGroups.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish and store {EventCount} domain events", eventList.Count);
            throw;
        }
    }

    public async Task<IEnumerable<AuditTrailEntry>> GetAuditTrailAsync(string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        var streamId = $"{entityType}-{entityId}";
        var events = await _eventStore.GetEventsAsync(streamId, cancellationToken: cancellationToken);

        return events.Select(MapToAuditTrailEntry).OrderByDescending(e => e.Timestamp);
    }

    public async Task<IEnumerable<AuditTrailEntry>> GetAuditTrailAsync(AuditTrailQuery query, CancellationToken cancellationToken = default)
    {
        var events = await _eventStore.GetAuditTrailAsync(
            query.EntityType,
            query.EntityId,
            query.UserId,
            query.From,
            query.To,
            cancellationToken);

        var auditEntries = events.Select(MapToAuditTrailEntry);

        // Apply additional filtering
        if (!string.IsNullOrEmpty(query.EventType))
        {
            auditEntries = auditEntries.Where(e => e.EventType.Contains(query.EventType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            auditEntries = auditEntries.Where(e => 
                e.Description.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                e.Details.Contains(query.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        return auditEntries
            .OrderByDescending(e => e.Timestamp)
            .Skip(query.Skip)
            .Take(query.Take);
    }

    public async Task<IEnumerable<AuditTrailEntry>> GetSystemAuditTrailAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30); // Default to last 30 days
        var toDate = to ?? DateTime.UtcNow;

        var events = await _eventStore.GetEventsByTimeRangeAsync(fromDate, toDate, cancellationToken);

        return events.Select(MapToAuditTrailEntry).OrderByDescending(e => e.Timestamp);
    }

    public async Task<T?> ReplayEventsAsync<T>(string streamId, CancellationToken cancellationToken = default) where T : class, new()
    {
        var events = await _eventStore.GetEventsAsync(streamId, cancellationToken: cancellationToken);
        
        if (!events.Any())
            return null;

        var entity = new T();
        
        // Apply events to rebuild state
        foreach (var storedEvent in events.OrderBy(e => e.Version))
        {
            // In a full implementation, this would use reflection or a more sophisticated
            // event application mechanism to rebuild entity state
            _logger.LogDebug("Replaying event {EventType} for stream {StreamId}", storedEvent.EventType, streamId);
        }

        return entity;
    }

    private static string GetStreamIdForEvent(DomainEvent domainEvent)
    {
        return domainEvent switch
        {
            KpiExecutedEvent kpiEvent => EventStoreExtensions.GetKpiStreamId(kpiEvent.KpiId),
            KpiCreatedEvent kpiEvent => EventStoreExtensions.GetKpiStreamId(kpiEvent.KpiId),
            KpiUpdatedEvent kpiEvent => EventStoreExtensions.GetKpiStreamId(kpiEvent.KpiId),
            KpiDeactivatedEvent kpiEvent => EventStoreExtensions.GetKpiStreamId(kpiEvent.KpiId),
            AlertTriggeredEvent alertEvent => EventStoreExtensions.GetAlertStreamId(alertEvent.AlertId),
            AlertResolvedEvent alertEvent => EventStoreExtensions.GetAlertStreamId(alertEvent.AlertId),
            _ => $"Unknown-{domainEvent.EventId}"
        };
    }

    private static AuditTrailEntry MapToAuditTrailEntry(StoredEvent storedEvent)
    {
        var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(storedEvent.Metadata) ?? new();
        
        return new AuditTrailEntry
        {
            Id = storedEvent.EventId,
            EntityType = ExtractEntityTypeFromStreamId(storedEvent.StreamId),
            EntityId = ExtractEntityIdFromStreamId(storedEvent.StreamId),
            EventType = storedEvent.EventType,
            Description = GenerateDescription(storedEvent.EventType, storedEvent.EventData),
            Details = storedEvent.EventData,
            UserId = metadata.GetValueOrDefault("UserId")?.ToString(),
            Timestamp = storedEvent.Timestamp,
            CorrelationId = metadata.GetValueOrDefault("CorrelationId")?.ToString(),
            CausationId = metadata.GetValueOrDefault("CausationId")?.ToString()
        };
    }

    private static string ExtractEntityTypeFromStreamId(string streamId)
    {
        var parts = streamId.Split('-');
        return parts.Length > 0 ? parts[0] : "Unknown";
    }

    private static string ExtractEntityIdFromStreamId(string streamId)
    {
        var parts = streamId.Split('-');
        return parts.Length > 1 ? parts[1] : "Unknown";
    }

    private static string GenerateDescription(string eventType, string eventData)
    {
        return eventType switch
        {
            "KpiExecutedEvent" => "KPI was executed",
            "KpiCreatedEvent" => "KPI was created",
            "KpiUpdatedEvent" => "KPI was updated",
            "KpiDeactivatedEvent" => "KPI was deactivated",
            "AlertTriggeredEvent" => "Alert was triggered",
            "AlertResolvedEvent" => "Alert was resolved",
            _ => $"Event of type {eventType} occurred"
        };
    }
}

/// <summary>
/// Query parameters for audit trail retrieval
/// </summary>
public class AuditTrailQuery
{
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? UserId { get; set; }
    public string? EventType { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 100;
}

/// <summary>
/// Audit trail entry for display and analysis
/// </summary>
public class AuditTrailEntry
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
}
