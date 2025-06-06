namespace MonitoringGrid.Core.Events;

/// <summary>
/// Base interface for domain events
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Version of the event for schema evolution
    /// </summary>
    int Version { get; }
}

/// <summary>
/// Base implementation of domain events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        Version = 1;
    }

    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public virtual int Version { get; init; }
}
