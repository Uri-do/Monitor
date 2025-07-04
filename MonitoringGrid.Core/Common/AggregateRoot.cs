using MonitoringGrid.Core.Events;

namespace MonitoringGrid.Core.Common;

/// <summary>
/// Base class for aggregate roots in Domain-Driven Design
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Gets the domain events that have been raised by this aggregate
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to be published
    /// </summary>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a domain event
    /// </summary>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all domain events
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Gets events of a specific type
    /// </summary>
    public IEnumerable<T> GetEventsOfType<T>() where T : IDomainEvent
    {
        return _domainEvents.OfType<T>();
    }

    /// <summary>
    /// Checks if any events of a specific type have been raised
    /// </summary>
    public bool HasEventsOfType<T>() where T : IDomainEvent
    {
        return _domainEvents.Any(e => e is T);
    }

    /// <summary>
    /// Gets the total number of domain events
    /// </summary>
    public int TotalEventCount => _domainEvents.Count;

    /// <summary>
    /// Clears events of a specific type
    /// </summary>
    public void ClearEventsOfType<T>() where T : IDomainEvent
    {
        var eventsToRemove = _domainEvents.OfType<T>().ToList();
        foreach (var eventToRemove in eventsToRemove)
        {
            RemoveDomainEvent(eventToRemove);
        }
    }

    /// <summary>
    /// Marks the aggregate as having unsaved changes
    /// </summary>
    protected void MarkAsModified()
    {
        // This can be used by the infrastructure layer to track changes
        // For example, updating a ModifiedDate property
    }

    /// <summary>
    /// Gets a summary of the aggregate's current state for debugging
    /// </summary>
    public virtual string GetStateSummary()
    {
        return $"{GetType().Name} with {TotalEventCount} pending events";
    }
}

/// <summary>
/// Base class for entities within an aggregate
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Checks if this entity is the same as another entity
    /// </summary>
    public abstract bool IsSameAs(Entity other);

    /// <summary>
    /// Gets the entity's identifier
    /// </summary>
    public abstract object GetId();
}

/// <summary>
/// Base class for entities with a specific ID type
/// </summary>
/// <typeparam name="TId">Type of the entity ID</typeparam>
public abstract class Entity<TId> : Entity where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    public override bool IsSameAs(Entity other)
    {
        if (other is not Entity<TId> entity)
            return false;

        if (ReferenceEquals(this, entity))
            return true;

        if (GetType() != entity.GetType())
            return false;

        return Id.Equals(entity.Id);
    }

    public override object GetId() => Id;

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && IsSameAs(entity);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
