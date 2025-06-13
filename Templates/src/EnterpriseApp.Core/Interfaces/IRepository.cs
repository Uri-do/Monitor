using System.Linq.Expressions;

namespace EnterpriseApp.Core.Interfaces;

/// <summary>
/// Generic repository interface for data access
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its ID
    /// </summary>
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with pagination
    /// </summary>
    Task<IEnumerable<T>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching the specified criteria
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the criteria or null
    /// </summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the criteria
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of entities matching the criteria
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity
    /// </summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities
    /// </summary>
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity
    /// </summary>
    Task RemoveAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple entities
    /// </summary>
    Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity by its ID
    /// </summary>
    Task RemoveByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with includes
    /// </summary>
    Task<IEnumerable<T>> GetWithIncludesAsync(params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Finds entities with includes
    /// </summary>
    Task<IEnumerable<T>> FindWithIncludesAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Gets entities with ordering
    /// </summary>
    Task<IEnumerable<T>> GetOrderedAsync<TKey>(Expression<Func<T, TKey>> orderBy, bool ascending = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with complex query options
    /// </summary>
    Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        string includeProperties = "",
        int? skip = null,
        int? take = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Unit of Work interface for managing transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets a repository for the specified entity type
    /// </summary>
    IRepository<T> Repository<T>() where T : class;

    /// <summary>
    /// Saves all changes to the database
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a function within a transaction
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an action within a transaction
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}

/// <summary>
/// Specific repository interface for DomainEntity
/// </summary>
public interface IDomainEntityRepository : IRepository<Entities.DomainEntity>
{
    /// <summary>
    /// Gets active DomainEntities
    /// </summary>
    Task<IEnumerable<Entities.DomainEntity>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets DomainEntities by category
    /// </summary>
    Task<IEnumerable<Entities.DomainEntity>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets DomainEntities by status
    /// </summary>
    Task<IEnumerable<Entities.DomainEntity>> GetByStatusAsync(Enums.DomainEntityStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets DomainEntities with their items
    /// </summary>
    Task<IEnumerable<Entities.DomainEntity>> GetWithItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches DomainEntities by name or description
    /// </summary>
    Task<IEnumerable<Entities.DomainEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets DomainEntities by tag
    /// </summary>
    Task<IEnumerable<Entities.DomainEntity>> GetByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets DomainEntities created within a date range
    /// </summary>
    Task<IEnumerable<Entities.DomainEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Audit repository interface
/// </summary>
public interface IAuditRepository : IRepository<Entities.AuditLog>
{
    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    Task<IEnumerable<Entities.AuditLog>> GetByEntityAsync(string entityName, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific user
    /// </summary>
    Task<IEnumerable<Entities.AuditLog>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs within a date range
    /// </summary>
    Task<IEnumerable<Entities.AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs by action type
    /// </summary>
    Task<IEnumerable<Entities.AuditLog>> GetByActionAsync(Enums.AuditAction action, CancellationToken cancellationToken = default);
}
