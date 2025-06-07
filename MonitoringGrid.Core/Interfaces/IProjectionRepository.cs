using System.Linq.Expressions;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Repository interface for query projections and optimized data transfer
/// </summary>
public interface IProjectionRepository<T> where T : class
{
    /// <summary>
    /// Gets projected results to reduce data transfer
    /// </summary>
    Task<IEnumerable<TResult>> GetProjectedAsync<TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projected results with ordering
    /// </summary>
    Task<IEnumerable<TResult>> GetProjectedAsync<TResult, TKey>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, TKey>> orderBy,
        bool descending = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paged projected results for large datasets
    /// </summary>
    Task<PagedResult<TResult>> GetPagedProjectedAsync<TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paged projected results with ordering
    /// </summary>
    Task<PagedResult<TResult>> GetPagedProjectedAsync<TResult, TKey>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, TKey>> orderBy,
        bool descending = false,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated data for dashboard scenarios
    /// </summary>
    Task<TResult> GetAggregatedAsync<TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<IQueryable<T>, TResult>> aggregation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets grouped data with projections
    /// </summary>
    Task<IEnumerable<TResult>> GetGroupedProjectedAsync<TKey, TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TKey>> groupBy,
        Expression<Func<IGrouping<TKey, T>, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets distinct projected values
    /// </summary>
    Task<IEnumerable<TResult>> GetDistinctProjectedAsync<TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets top N projected results
    /// </summary>
    Task<IEnumerable<TResult>> GetTopProjectedAsync<TResult, TKey>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, TKey>> orderBy,
        int count,
        bool descending = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the predicate (optimized for existence checks)
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count with predicate (optimized for counting)
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets projected results with complex includes (for when navigation properties are needed)
    /// </summary>
    Task<IEnumerable<TResult>> GetProjectedWithIncludesAsync<TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        params Expression<Func<T, object>>[] includes);
}

/// <summary>
/// Paged result container for efficient pagination
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public int StartIndex => (PageNumber - 1) * PageSize + 1;
    public int EndIndex => Math.Min(PageNumber * PageSize, TotalCount);
}

/// <summary>
/// Performance metrics for query optimization
/// </summary>
public class QueryPerformanceMetrics
{
    public TimeSpan ExecutionTime { get; set; }
    public int RecordsReturned { get; set; }
    public int RecordsScanned { get; set; }
    public bool UsedIndex { get; set; }
    public string? QueryPlan { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Query optimization hints
/// </summary>
public class QueryOptimizationHints
{
    public bool UseNoLock { get; set; } = false;
    public bool ForceIndex { get; set; } = false;
    public string? IndexName { get; set; }
    public int? QueryTimeout { get; set; }
    public bool EnableQueryPlanCapture { get; set; } = false;
    public bool UseReadUncommitted { get; set; } = false;
}

/// <summary>
/// Bulk operation result
/// </summary>
public class BulkOperationResult
{
    public int AffectedRows { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Cache configuration for query results
/// </summary>
public class QueryCacheConfig
{
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);
    public string? CacheKey { get; set; }
    public bool SlidingExpiration { get; set; } = false;
    public string[]? Tags { get; set; }
    public bool BypassCache { get; set; } = false;
}
