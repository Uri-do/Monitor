using System.Text.Json;
using MonitoringGrid.Core.Events;

namespace MonitoringGrid.Core.EventSourcing;

/// <summary>
/// Event store for persisting domain events for audit trail and event sourcing
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Append events to the event store
    /// </summary>
    Task AppendEventsAsync(string streamId, IEnumerable<DomainEvent> events, long expectedVersion = -1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get events from a specific stream
    /// </summary>
    Task<IEnumerable<StoredEvent>> GetEventsAsync(string streamId, long fromVersion = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all events of a specific type
    /// </summary>
    Task<IEnumerable<StoredEvent>> GetEventsByTypeAsync<T>(CancellationToken cancellationToken = default) where T : DomainEvent;

    /// <summary>
    /// Get events within a time range
    /// </summary>
    Task<IEnumerable<StoredEvent>> GetEventsByTimeRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get events for audit trail with filtering
    /// </summary>
    Task<IEnumerable<StoredEvent>> GetAuditTrailAsync(string? entityType = null, string? entityId = null, string? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of event store (for development/testing)
/// In production, this would be replaced with a persistent store like EventStore, SQL Server, or CosmosDB
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly List<StoredEvent> _events = new();
    private readonly object _lock = new();

    public Task AppendEventsAsync(string streamId, IEnumerable<DomainEvent> events, long expectedVersion = -1, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var eventList = events.ToList();
            if (!eventList.Any()) return Task.CompletedTask;

            // Check expected version if specified
            if (expectedVersion >= 0)
            {
                var currentVersion = _events.Where(e => e.StreamId == streamId).Count();
                if (currentVersion != expectedVersion)
                {
                    throw new InvalidOperationException($"Expected version {expectedVersion} but current version is {currentVersion}");
                }
            }

            var nextVersion = _events.Where(e => e.StreamId == streamId).Count();

            foreach (var domainEvent in eventList)
            {
                var storedEvent = new StoredEvent
                {
                    EventId = Guid.NewGuid(),
                    StreamId = streamId,
                    EventType = domainEvent.GetType().Name,
                    EventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    Metadata = JsonSerializer.Serialize(new
                    {
                        domainEvent.OccurredOn,
                        domainEvent.EventId,
                        UserId = GetCurrentUserId(),
                        CorrelationId = GetCorrelationId(),
                        CausationId = GetCausationId()
                    }),
                    Version = nextVersion++,
                    Timestamp = domainEvent.OccurredOn
                };

                _events.Add(storedEvent);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<StoredEvent>> GetEventsAsync(string streamId, long fromVersion = 0, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var events = _events
                .Where(e => e.StreamId == streamId && e.Version >= fromVersion)
                .OrderBy(e => e.Version)
                .ToList();

            return Task.FromResult<IEnumerable<StoredEvent>>(events);
        }
    }

    public Task<IEnumerable<StoredEvent>> GetEventsByTypeAsync<T>(CancellationToken cancellationToken = default) where T : DomainEvent
    {
        lock (_lock)
        {
            var eventTypeName = typeof(T).Name;
            var events = _events
                .Where(e => e.EventType == eventTypeName)
                .OrderBy(e => e.Timestamp)
                .ToList();

            return Task.FromResult<IEnumerable<StoredEvent>>(events);
        }
    }

    public Task<IEnumerable<StoredEvent>> GetEventsByTimeRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var events = _events
                .Where(e => e.Timestamp >= from && e.Timestamp <= to)
                .OrderBy(e => e.Timestamp)
                .ToList();

            return Task.FromResult<IEnumerable<StoredEvent>>(events);
        }
    }

    public Task<IEnumerable<StoredEvent>> GetAuditTrailAsync(string? entityType = null, string? entityId = null, string? userId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var query = _events.AsQueryable();

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(e => e.StreamId.StartsWith($"{entityType}-"));
            }

            if (!string.IsNullOrEmpty(entityId))
            {
                query = query.Where(e => e.StreamId.EndsWith($"-{entityId}"));
            }

            if (from.HasValue)
            {
                query = query.Where(e => e.Timestamp >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(e => e.Timestamp <= to.Value);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(e => e.Metadata.Contains($"\"UserId\":\"{userId}\""));
            }

            var events = query
                .OrderByDescending(e => e.Timestamp)
                .ToList();

            return Task.FromResult<IEnumerable<StoredEvent>>(events);
        }
    }

    private static string? GetCurrentUserId()
    {
        // In a real implementation, this would get the current user from the HTTP context
        return "system"; // Placeholder
    }

    private static string? GetCorrelationId()
    {
        // In a real implementation, this would get the correlation ID from the current context
        return System.Diagnostics.Activity.Current?.TraceId.ToString();
    }

    private static string? GetCausationId()
    {
        // In a real implementation, this would get the causation ID from the current context
        return System.Diagnostics.Activity.Current?.SpanId.ToString();
    }
}

/// <summary>
/// Represents a stored event in the event store
/// </summary>
public class StoredEvent
{
    public Guid EventId { get; set; }
    public string StreamId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventData { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event store extensions for easier usage
/// </summary>
public static class EventStoreExtensions
{
    /// <summary>
    /// Append a single event to the event store
    /// </summary>
    public static Task AppendEventAsync(this IEventStore eventStore, string streamId, DomainEvent domainEvent, long expectedVersion = -1, CancellationToken cancellationToken = default)
    {
        return eventStore.AppendEventsAsync(streamId, new[] { domainEvent }, expectedVersion, cancellationToken);
    }

    /// <summary>
    /// Generate stream ID for an entity
    /// </summary>
    public static string GetStreamId<T>(object entityId) where T : class
    {
        return $"{typeof(T).Name}-{entityId}";
    }

    /// <summary>
    /// Generate stream ID for Indicator events
    /// </summary>
    public static string GetIndicatorStreamId(long indicatorId)
    {
        return $"Indicator-{indicatorId}";
    }

    /// <summary>
    /// Generate stream ID for alert events
    /// </summary>
    public static string GetAlertStreamId(long alertId)
    {
        return $"Alert-{alertId}";
    }
}
