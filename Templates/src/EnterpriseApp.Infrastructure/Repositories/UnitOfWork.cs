using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using EnterpriseApp.Core.Interfaces;
using EnterpriseApp.Infrastructure.Data;

namespace EnterpriseApp.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for managing transactions and repositories
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the UnitOfWork class
    /// </summary>
    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _repositories = new Dictionary<Type, object>();
    }

    /// <inheritdoc />
    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);

        if (_repositories.ContainsKey(type))
        {
            return (IRepository<T>)_repositories[type];
        }

        // Create specific repository implementations for known types
        var repository = CreateRepository<T>();
        _repositories[type] = repository;

        return repository;
    }

    /// <summary>
    /// Creates a repository instance for the specified type
    /// </summary>
    private IRepository<T> CreateRepository<T>() where T : class
    {
        var type = typeof(T);

        // Return specific repository implementations for known types
        if (type == typeof(Core.Entities.DomainEntity))
        {
            return (IRepository<T>)new DomainEntityRepository(_context);
        }

        if (type == typeof(Core.Entities.AuditLog))
        {
            return (IRepository<T>)new AuditRepository(_context);
        }

        // Return generic repository for other types
        return new Repository<T>(_context);
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Handle concurrency conflicts
            throw new InvalidOperationException("A concurrency conflict occurred while saving changes.", ex);
        }
        catch (DbUpdateException ex)
        {
            // Handle database update errors
            throw new InvalidOperationException("An error occurred while saving changes to the database.", ex);
        }
    }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await _transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress.");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var wasTransactionStarted = _transaction == null;

        if (wasTransactionStarted)
        {
            await BeginTransactionAsync(cancellationToken);
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

    /// <inheritdoc />
    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        await ExecuteInTransactionAsync(async () =>
        {
            await operation();
            return true; // Dummy return value
        }, cancellationToken);
    }

    /// <summary>
    /// Executes multiple operations in a single transaction
    /// </summary>
    public async Task ExecuteBatchAsync(IEnumerable<Func<Task>> operations, CancellationToken cancellationToken = default)
    {
        if (operations == null)
            throw new ArgumentNullException(nameof(operations));

        await ExecuteInTransactionAsync(async () =>
        {
            foreach (var operation in operations)
            {
                await operation();
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Gets the current transaction
    /// </summary>
    public IDbContextTransaction? CurrentTransaction => _transaction;

    /// <summary>
    /// Indicates if a transaction is currently active
    /// </summary>
    public bool HasActiveTransaction => _transaction != null;

    /// <summary>
    /// Resets the context state (useful for testing)
    /// </summary>
    public void ResetState()
    {
        _context.ChangeTracker.Clear();
        _repositories.Clear();
    }

    /// <summary>
    /// Gets the underlying DbContext
    /// </summary>
    public ApplicationDbContext Context => _context;

    /// <summary>
    /// Detaches an entity from the context
    /// </summary>
    public void Detach<T>(T entity) where T : class
    {
        _context.Entry(entity).State = EntityState.Detached;
    }

    /// <summary>
    /// Attaches an entity to the context
    /// </summary>
    public void Attach<T>(T entity) where T : class
    {
        _context.Set<T>().Attach(entity);
    }

    /// <summary>
    /// Gets the state of an entity
    /// </summary>
    public EntityState GetEntityState<T>(T entity) where T : class
    {
        return _context.Entry(entity).State;
    }

    /// <summary>
    /// Sets the state of an entity
    /// </summary>
    public void SetEntityState<T>(T entity, EntityState state) where T : class
    {
        _context.Entry(entity).State = state;
    }

    /// <summary>
    /// Reloads an entity from the database
    /// </summary>
    public async Task ReloadAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        await _context.Entry(entity).ReloadAsync(cancellationToken);
    }

    /// <summary>
    /// Disposes the Unit of Work
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
            _repositories.Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    ~UnitOfWork()
    {
        Dispose(false);
    }
}
