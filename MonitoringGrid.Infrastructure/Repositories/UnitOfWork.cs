using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;
using System.Collections.Concurrent;

namespace MonitoringGrid.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for managing transactions and domain events
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly MonitoringContext _context;
    private readonly ConcurrentDictionary<Type, object> _repositories;
    private readonly List<IDomainEvent> _domainEvents;
    private bool _disposed;

    public UnitOfWork(MonitoringContext context)
    {
        _context = context;
        _repositories = new ConcurrentDictionary<Type, object>();
        _domainEvents = new List<IDomainEvent>();
    }

    public IRepository<T> Repository<T>() where T : class
    {
        return (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ => new Repository<T>(_context));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect domain events from aggregate roots before saving
        CollectDomainEvents();

        // Save changes to database
        var result = await _context.SaveChangesAsync(cancellationToken);

        // Publish domain events after successful save
        await PublishDomainEventsAsync(cancellationToken);

        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction == null)
        {
            await _context.Database.BeginTransactionAsync(cancellationToken);
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = _context.Database.CurrentTransaction;
        if (transaction != null)
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents()
    {
        return _domainEvents.AsReadOnly();
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void CollectDomainEvents()
    {
        // Get all tracked entities that are aggregate roots
        var aggregateRoots = _context.ChangeTracker.Entries()
            .Where(e => e.Entity is Core.Common.AggregateRoot)
            .Select(e => e.Entity as Core.Common.AggregateRoot)
            .Where(ar => ar != null && ar.DomainEvents.Any())
            .ToList();

        // Collect all domain events
        foreach (var aggregateRoot in aggregateRoots)
        {
            foreach (var domainEvent in aggregateRoot!.DomainEvents)
            {
                _domainEvents.Add(domainEvent);
            }
        }

        // Clear domain events from aggregate roots
        foreach (var aggregateRoot in aggregateRoots)
        {
            aggregateRoot!.ClearDomainEvents();
        }
    }

    private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        // In a real implementation, you would use a domain event publisher
        // For now, we'll just clear the events after "publishing"
        
        // TODO: Implement actual domain event publishing using MediatR or similar
        // Example:
        // foreach (var domainEvent in _domainEvents)
        // {
        //     await _mediator.Publish(domainEvent, cancellationToken);
        // }

        _domainEvents.Clear();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Database.CurrentTransaction?.Dispose();
            _context.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
