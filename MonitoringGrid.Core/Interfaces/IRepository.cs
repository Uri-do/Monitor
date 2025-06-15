using System.Linq.Expressions;
using MonitoringGrid.Core.Specifications;

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
    /// Gets entities with filtering, ordering, and complex navigation properties using ThenInclude
    /// </summary>
    Task<IEnumerable<T>> GetWithThenIncludesAsync<TKey>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TKey>> orderBy,
        bool ascending = true,
        Func<IQueryable<T>, IQueryable<T>>? includeFunc = null);

    /// <summary>
    /// Gets a single entity by ID
    /// </summary>
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single entity by ID with navigation properties
    /// </summary>
    Task<T?> GetByIdWithIncludesAsync(object id, params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Gets a single entity by ID with complex navigation properties using ThenInclude
    /// </summary>
    Task<T?> GetByIdWithThenIncludesAsync(object id, Func<IQueryable<T>, IQueryable<T>>? includeFunc = null);

    /// <summary>
    /// Gets multiple entities by their IDs
    /// </summary>
    Task<IEnumerable<T>> GetByIdsAsync<TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Gets entities using a specification
    /// </summary>
    Task<IEnumerable<T>> GetAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets queryable for advanced scenarios
    /// </summary>
    IQueryable<T> GetQueryable();

    /// <summary>
    /// Gets entities using a specification with pagination
    /// </summary>
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching a specification
    /// </summary>
    Task<T?> GetFirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching a specification
    /// </summary>
    Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches a specification
    /// </summary>
    Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    // Bulk Operations
    /// <summary>
    /// Bulk insert multiple entities for better performance
    /// </summary>
    Task<int> BulkInsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk update multiple entities for better performance
    /// </summary>
    Task<int> BulkUpdateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk delete multiple entities for better performance
    /// </summary>
    Task<int> BulkDeleteAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk delete entities matching a predicate
    /// </summary>
    Task<int> BulkDeleteAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    // Query Optimization Methods
    /// <summary>
    /// Gets projected results to reduce data transfer
    /// </summary>
    Task<IEnumerable<TResult>> GetProjectedAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all projected results
    /// </summary>
    Task<IEnumerable<TResult>> GetProjectedAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paged projected results
    /// </summary>
    Task<(IEnumerable<TResult> Items, int TotalCount)> GetPagedProjectedAsync<TResult>(
        int pageNumber,
        int pageSize,
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets first projected result
    /// </summary>
    Task<TResult?> GetFirstProjectedAsync<TResult>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default);
}
