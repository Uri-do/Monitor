using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Specifications;
using MonitoringGrid.Infrastructure.Data;
using System.Linq.Expressions;

namespace MonitoringGrid.Infrastructure.Repositories;

/// <summary>
/// Advanced repository implementation with specification pattern and performance optimizations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class AdvancedRepository<T> : Repository<T>, IAdvancedRepository<T> where T : class
{
    private readonly ILogger<AdvancedRepository<T>> _logger;

    public AdvancedRepository(MonitoringContext context, ILogger<AdvancedRepository<T>> logger)
        : base(context)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Advanced Repository Features

    #region Specification Pattern

    public virtual async Task<T?> GetSingleBySpecificationAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetBySpecificationAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<int> CountBySpecificationAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task<bool> AnyBySpecificationAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.AnyAsync(cancellationToken);
    }

    protected virtual IQueryable<T> ApplySpecification(ISpecification<T> specification)
    {
        var query = _context.Set<T>().AsQueryable();

        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Apply includes
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        // Apply string includes
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        // Apply ordering
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply paging
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip ?? 0).Take(specification.Take ?? 10);
        }

        return query;
    }

    #endregion

    #region Advanced Queries

    public virtual async Task<MonitoringGrid.Core.Models.PagedResult<T>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes)
    {
        var query = _context.Set<T>().AsQueryable();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        var totalCount = await query.CountAsync();

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return MonitoringGrid.Core.Models.PagedResult<T>.Create(items, totalCount, page, pageSize);
    }

    public virtual async Task<IEnumerable<TResult>> GetProjectedAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<T>().AsQueryable();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        return await query.Select(selector).ToListAsync(cancellationToken);
    }

    public virtual async Task<MonitoringGrid.Core.Models.PagedResult<TResult>> GetProjectedPagedAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        int page,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<T>().AsQueryable();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);

        return MonitoringGrid.Core.Models.PagedResult<TResult>.Create(items, totalCount, page, pageSize);
    }

    #endregion

    #region Bulk Operations

    public override async Task<int> BulkInsertAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        if (!entityList.Any()) return 0;

        try
        {
            await _context.Set<T>().AddRangeAsync(entityList, cancellationToken);
            var result = await _context.SaveChangesAsync(cancellationToken);

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

    public override async Task<int> BulkUpdateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        if (!entityList.Any()) return 0;

        try
        {
            _context.Set<T>().UpdateRange(entityList);
            var result = await _context.SaveChangesAsync(cancellationToken);

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

    public override async Task<int> BulkDeleteAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.Set<T>().Where(filter).ToListAsync(cancellationToken);
            if (!entities.Any()) return 0;

            _context.Set<T>().RemoveRange(entities);
            var result = await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Bulk deleted {Count} entities of type {EntityType}",
                entities.Count, typeof(T).Name);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk delete entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    #endregion

    #region Query Optimization

    public virtual IQueryable<T> GetQueryable()
    {
        return _context.Set<T>().AsQueryable();
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        return await _context.Set<T>().AnyAsync(filter, cancellationToken);
    }

    public override async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<T>().AsQueryable();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes)
    {
        var query = _context.Set<T>().AsQueryable();

        if (filter != null)
        {
            query = query.Where(filter);
        }

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        return await query.FirstOrDefaultAsync();
    }

    #endregion

    #endregion
}
