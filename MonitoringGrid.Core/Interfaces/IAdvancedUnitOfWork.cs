using System.Data;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Advanced Unit of Work interface with transaction management and bulk operations
/// </summary>
public interface IAdvancedUnitOfWork : IUnitOfWork, IAsyncDisposable
{
    #region Repository Management

    /// <summary>
    /// Gets an advanced repository for the specified entity type
    /// </summary>
    IAdvancedRepository<T> AdvancedRepository<T>() where T : class;

    #endregion

    #region Transaction Management

    /// <summary>
    /// Begins a transaction with specified isolation level
    /// </summary>
    Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation within a transaction
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an operation within a transaction
    /// </summary>
    Task ExecuteInTransactionAsync(
        Func<Task> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    #endregion

    #region Save Operations

    /// <summary>
    /// Saves changes without publishing domain events
    /// </summary>
    Task<int> SaveChangesWithoutEventsAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Bulk insert entities
    /// </summary>
    Task<int> BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Bulk update entities
    /// </summary>
    Task<int> BulkUpdateAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Bulk delete entities
    /// </summary>
    Task<int> BulkDeleteAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    #endregion
}
