using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Specifications;
using System.Linq.Expressions;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Advanced repository interface with specification pattern and performance optimizations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IAdvancedRepository<T> : IRepository<T> where T : class
{
    #region Specification Pattern

    /// <summary>
    /// Gets a single entity by specification
    /// </summary>
    Task<T?> GetSingleBySpecificationAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities by specification
    /// </summary>
    Task<IEnumerable<T>> GetBySpecificationAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities by specification
    /// </summary>
    Task<int> CountBySpecificationAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the specification
    /// </summary>
    Task<bool> AnyBySpecificationAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);

    #endregion

    #region Advanced Queries

    /// <summary>
    /// Gets paged results with filtering and sorting
    /// </summary>
    Task<PagedResult<T>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Gets projected results
    /// </summary>
    Task<IEnumerable<TResult>> GetProjectedAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paged projected results
    /// </summary>
    Task<PagedResult<TResult>> GetProjectedPagedAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        int page,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Advanced Query Optimization

    /// <summary>
    /// Gets queryable for advanced scenarios
    /// </summary>
    IQueryable<T> GetQueryable();

    /// <summary>
    /// Checks if entity exists
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets first or default entity with filtering and ordering
    /// </summary>
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes);

    #endregion
}
