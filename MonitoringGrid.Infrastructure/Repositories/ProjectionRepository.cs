using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;
using System.Diagnostics;
using System.Linq.Expressions;

namespace MonitoringGrid.Infrastructure.Repositories;

/// <summary>
/// High-performance repository implementation with query projections
/// </summary>
public class ProjectionRepository<T> : IProjectionRepository<T> where T : class
{
    protected readonly MonitoringContext _context;
    protected readonly DbSet<T> _dbSet;

    public ProjectionRepository(MonitoringContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<IEnumerable<TResult>> GetProjectedAsync<TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        return await query.Select(selector).ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TResult>> GetProjectedAsync<TResult, TKey>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, TKey>> orderBy,
        bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

        return await query.Select(selector).ToListAsync(cancellationToken);
    }

    public virtual async Task<PagedResult<TResult>> GetPagedProjectedAsync<TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return new PagedResult<TResult>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public virtual async Task<PagedResult<TResult>> GetPagedProjectedAsync<TResult, TKey>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, TKey>> orderBy,
        bool descending = false,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(cancellationToken);

        query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return new PagedResult<TResult>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public virtual async Task<TResult> GetAggregatedAsync<TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<IQueryable<T>, TResult>> aggregation,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        return await Task.FromResult(aggregation.Compile()(query));
    }

    public virtual async Task<IEnumerable<TResult>> GetGroupedProjectedAsync<TKey, TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TKey>> groupBy,
        Expression<Func<IGrouping<TKey, T>, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        return await query
            .GroupBy(groupBy)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TResult>> GetDistinctProjectedAsync<TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        return await query
            .Select(selector)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TResult>> GetTopProjectedAsync<TResult, TKey>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, TKey>> orderBy,
        int count,
        bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

        return await query
            .Take(count)
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TResult>> GetProjectedWithIncludesAsync<TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        if (predicate != null)
            query = query.Where(predicate);

        return await query.Select(selector).ToListAsync();
    }
}

/// <summary>
/// Performance-optimized repository with caching and metrics
/// </summary>
public class PerformanceOptimizedRepository<T> : ProjectionRepository<T> where T : class
{
    private readonly ILogger<PerformanceOptimizedRepository<T>> _logger;

    public PerformanceOptimizedRepository(MonitoringContext context, ILogger<PerformanceOptimizedRepository<T>> logger)
        : base(context)
    {
        _logger = logger;
    }

    public async Task<(IEnumerable<TResult> Result, QueryPerformanceMetrics Metrics)> GetProjectedWithMetricsAsync<TResult>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await GetProjectedAsync(predicate, selector, cancellationToken);
        stopwatch.Stop();

        var metrics = new QueryPerformanceMetrics
        {
            ExecutionTime = stopwatch.Elapsed,
            RecordsReturned = result.Count()
        };

        _logger.LogDebug("Query executed in {ElapsedMs}ms, returned {RecordCount} records",
            stopwatch.ElapsedMilliseconds, metrics.RecordsReturned);

        return (result, metrics);
    }

    public async Task<(PagedResult<TResult> Result, QueryPerformanceMetrics Metrics)> GetPagedProjectedWithMetricsAsync<TResult, TKey>(
        Expression<Func<T, bool>>? predicate,
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, TKey>> orderBy,
        bool descending = false,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await GetPagedProjectedAsync(predicate, selector, orderBy, descending, pageNumber, pageSize, cancellationToken);
        stopwatch.Stop();

        var metrics = new QueryPerformanceMetrics
        {
            ExecutionTime = stopwatch.Elapsed,
            RecordsReturned = result.Items.Count(),
            RecordsScanned = result.TotalCount
        };

        _logger.LogDebug("Paged query executed in {ElapsedMs}ms, returned {RecordCount}/{TotalCount} records",
            stopwatch.ElapsedMilliseconds, metrics.RecordsReturned, metrics.RecordsScanned);

        return (result, metrics);
    }
}
