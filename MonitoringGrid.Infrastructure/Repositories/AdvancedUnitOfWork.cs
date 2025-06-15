using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Infrastructure.Data;
using System.Collections.Concurrent;
using System.Data;
using IDomainEventPublisher = MonitoringGrid.Core.Interfaces.IDomainEventPublisher;

namespace MonitoringGrid.Infrastructure.Repositories;

/// <summary>
/// Advanced Unit of Work implementation with transaction management and domain events
/// </summary>
public class AdvancedUnitOfWork : IAdvancedUnitOfWork
{
    private readonly MonitoringContext _context;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<AdvancedUnitOfWork> _logger;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private readonly List<IDomainEvent> _domainEvents = new();
    
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    public AdvancedUnitOfWork(
        MonitoringContext context,
        IDomainEventPublisher eventPublisher,
        ILogger<AdvancedUnitOfWork> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Repository Management

    public IRepository<T> Repository<T>() where T : class
    {
        return (IRepository<T>)_repositories.GetOrAdd(typeof(T), _ => CreateRepository<T>());
    }

    public IAdvancedRepository<T> AdvancedRepository<T>() where T : class
    {
        return (IAdvancedRepository<T>)_repositories.GetOrAdd(typeof(T), _ => CreateAdvancedRepository<T>());
    }

    private IRepository<T> CreateRepository<T>() where T : class
    {
        return new Repository<T>(_context);
    }

    private IAdvancedRepository<T> CreateAdvancedRepository<T>() where T : class
    {
        return new AdvancedRepository<T>(_context, _logger as ILogger<AdvancedRepository<T>> ??
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AdvancedRepository<T>>.Instance);
    }

    #endregion

    #region Transaction Management

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            _logger.LogWarning("Transaction already started");
            return;
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogDebug("Transaction started with ID: {TransactionId}", _transaction.TransactionId);
    }

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            _logger.LogWarning("Transaction already started");
            return;
        }

        _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
        _logger.LogDebug("Transaction started with isolation level {IsolationLevel} and ID: {TransactionId}", 
            isolationLevel, _transaction.TransactionId);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            _logger.LogWarning("No active transaction to commit");
            return;
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
            _logger.LogDebug("Transaction committed successfully: {TransactionId}", _transaction.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit transaction: {TransactionId}", _transaction.TransactionId);
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            _logger.LogWarning("No active transaction to rollback");
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            _logger.LogDebug("Transaction rolled back: {TransactionId}", _transaction.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback transaction: {TransactionId}", _transaction.TransactionId);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var wasTransactionStarted = _transaction == null;

        if (wasTransactionStarted)
        {
            await BeginTransactionAsync(isolationLevel, cancellationToken);
        }

        try
        {
            var result = await operation();

            if (wasTransactionStarted)
            {
                await CommitTransactionAsync(cancellationToken);
            }

            return result;
        }
        catch
        {
            if (wasTransactionStarted && _transaction != null)
            {
                await RollbackTransactionAsync(cancellationToken);
            }
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            await operation();
            return true;
        }, isolationLevel, cancellationToken);
    }

    #endregion

    #region Save Operations

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Collect domain events before saving
            CollectDomainEvents();

            // Apply audit information
            ApplyAuditInformation();

            // Save changes to database
            var result = await _context.SaveChangesAsync(cancellationToken);

            // Publish domain events after successful save
            await PublishDomainEventsAsync(cancellationToken);

            _logger.LogDebug("Saved {ChangeCount} changes to database", result);
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict occurred while saving changes");
            throw new InvalidOperationException("A concurrency conflict occurred. The record may have been modified by another user.", ex);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database update error occurred while saving changes");
            throw new InvalidOperationException("An error occurred while saving changes to the database.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while saving changes");
            throw;
        }
    }

    public async Task<int> SaveChangesWithoutEventsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ApplyAuditInformation();
            var result = await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saved {ChangeCount} changes to database without events", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while saving changes without events");
            throw;
        }
    }

    #endregion

    #region Bulk Operations

    public async Task<int> BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        var entityList = entities.ToList();
        if (!entityList.Any()) return 0;

        try
        {
            await _context.Set<T>().AddRangeAsync(entityList, cancellationToken);
            var result = await SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Bulk inserted {Count} entities of type {EntityType}", 
                entityList.Count, typeof(T).Name);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk insert {Count} entities of type {EntityType}", 
                entityList.Count, typeof(T).Name);
            throw;
        }
    }

    public async Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        var entityList = entities.ToList();
        if (!entityList.Any()) return 0;

        try
        {
            _context.Set<T>().UpdateRange(entityList);
            var result = await SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Bulk updated {Count} entities of type {EntityType}", 
                entityList.Count, typeof(T).Name);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk update {Count} entities of type {EntityType}", 
                entityList.Count, typeof(T).Name);
            throw;
        }
    }

    public async Task<int> BulkDeleteAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        var entityList = entities.ToList();
        if (!entityList.Any()) return 0;

        try
        {
            _context.Set<T>().RemoveRange(entityList);
            var result = await SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Bulk deleted {Count} entities of type {EntityType}", 
                entityList.Count, typeof(T).Name);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk delete {Count} entities of type {EntityType}", 
                entityList.Count, typeof(T).Name);
            throw;
        }
    }

    #endregion

    #region Domain Events

    private void CollectDomainEvents()
    {
        var domainEntities = _context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        foreach (var entity in domainEntities)
        {
            _domainEvents.AddRange(entity.Entity.DomainEvents);
            entity.Entity.ClearDomainEvents();
        }
    }

    private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        if (!_domainEvents.Any()) return;

        var events = _domainEvents.ToList();
        _domainEvents.Clear();

        foreach (var domainEvent in events)
        {
            try
            {
                await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
                _logger.LogDebug("Published domain event: {EventType}", domainEvent.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish domain event: {EventType}", domainEvent.GetType().Name);
                // Continue with other events
            }
        }
    }

    #endregion

    #region Audit Information

    private void ApplyAuditInformation()
    {
        var entries = _context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        var currentTime = DateTime.UtcNow;
        var currentUser = GetCurrentUser();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                SetPropertyIfExists(entry, "CreatedDate", currentTime);
                SetPropertyIfExists(entry, "CreatedBy", currentUser);
            }

            if (entry.State == EntityState.Modified)
            {
                SetPropertyIfExists(entry, "ModifiedDate", currentTime);
                SetPropertyIfExists(entry, "ModifiedBy", currentUser);
            }
        }
    }

    private void SetPropertyIfExists(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string propertyName, object value)
    {
        var property = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        if (property != null)
        {
            property.CurrentValue = value;
        }
    }

    private string GetCurrentUser()
    {
        // This would typically get the current user from the HTTP context or security context
        return "System"; // Placeholder
    }

    #endregion

    #region Domain Event Management (IUnitOfWork Implementation)

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

    #endregion

    #region Disposal

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context?.Dispose();
            _disposed = true;
        }
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }

        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        _disposed = true;
    }

    #endregion
}
