using System.Linq.Expressions;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Generic repository interface for data access
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets all entities
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities with navigation properties
    /// </summary>
    Task<IEnumerable<T>> GetAllWithIncludesAsync(params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Gets entities with filtering
    /// </summary>
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with filtering and navigation properties
    /// </summary>
    Task<IEnumerable<T>> GetWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Gets entities with filtering, ordering, and navigation properties
    /// </summary>
    Task<IEnumerable<T>> GetWithIncludesAsync<TKey>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TKey>> orderBy,
        bool ascending = true,
        params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Gets a single entity by ID
    /// </summary>
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single entity by ID with navigation properties
    /// </summary>
    Task<T?> GetByIdWithIncludesAsync(object id, params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Gets the first entity matching the predicate
    /// </summary>
    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the predicate with navigation properties
    /// </summary>
    Task<T?> GetFirstOrDefaultWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Gets entities with pagination support
    /// </summary>
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with pagination and navigation properties
    /// </summary>
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedWithIncludesAsync<TKey>(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Adds a new entity
    /// </summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity
    /// </summary>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity
    /// </summary>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes entities by ID
    /// </summary>
    Task DeleteAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the predicate
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the predicate
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a raw SQL query
    /// </summary>
    Task<IEnumerable<T>> ExecuteRawSqlAsync(string sql, params object[] parameters);

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
