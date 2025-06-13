using Microsoft.EntityFrameworkCore;
using EnterpriseApp.Core.Entities;
using EnterpriseApp.Core.Enums;
using EnterpriseApp.Core.Interfaces;
using EnterpriseApp.Infrastructure.Data;

namespace EnterpriseApp.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for DomainEntity
/// </summary>
public class DomainEntityRepository : Repository<DomainEntity>, IDomainEntityRepository
{
    /// <summary>
    /// Initializes a new instance of the DomainEntityRepository class
    /// </summary>
    public DomainEntityRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DomainEntity>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DomainEntity>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.Category == category)
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DomainEntity>> GetByStatusAsync(DomainEntityStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.Status == status)
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DomainEntity>> GetWithItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Items.Where(i => i.IsActive))
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DomainEntity>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var normalizedSearchTerm = searchTerm.ToLower();

        return await _dbSet
            .Where(e => e.Name.ToLower().Contains(normalizedSearchTerm) ||
                       (e.Description != null && e.Description.ToLower().Contains(normalizedSearchTerm)) ||
                       (e.Category != null && e.Category.ToLower().Contains(normalizedSearchTerm)) ||
                       (e.Tags != null && e.Tags.ToLower().Contains(normalizedSearchTerm)))
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DomainEntity>> GetByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var normalizedTag = tag.ToLower();

        return await _dbSet
            .Where(e => e.Tags != null && e.Tags.ToLower().Contains(normalizedTag))
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DomainEntity>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.CreatedDate >= startDate && e.CreatedDate <= endDate)
            .OrderByDescending(e => e.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets DomainEntities with pagination and filtering
    /// </summary>
    public async Task<(IEnumerable<DomainEntity> Items, int TotalCount)> GetPagedWithFilterAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        string? category = null,
        DomainEntityStatus? status = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(normalizedSearchTerm) ||
                                   (e.Description != null && e.Description.ToLower().Contains(normalizedSearchTerm)));
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(e => e.Category == category);
        }

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(e => e.IsActive == isActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var items = await query
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Gets DomainEntity statistics
    /// </summary>
    public async Task<DomainEntityStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var totalCount = await _dbSet.CountAsync(cancellationToken);
        var activeCount = await _dbSet.CountAsync(e => e.IsActive, cancellationToken);
        var inactiveCount = totalCount - activeCount;

        // Status counts
        var statusCounts = await _dbSet
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        // Category counts
        var categoryCounts = await _dbSet
            .Where(e => e.Category != null)
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key!, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count, cancellationToken);

        // Priority counts
        var priorityCounts = await _dbSet
            .GroupBy(e => e.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Priority, x => x.Count, cancellationToken);

        // Recent activity (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recentActivityCount = await _dbSet
            .CountAsync(e => e.CreatedDate >= thirtyDaysAgo || e.ModifiedDate >= thirtyDaysAgo, cancellationToken);

        // Average items per entity
        var entitiesWithItems = await _dbSet
            .Include(e => e.Items)
            .ToListAsync(cancellationToken);

        var averageItemsPerEntity = entitiesWithItems.Any() 
            ? entitiesWithItems.Average(e => e.Items.Count) 
            : 0;

        return new Core.Models.DomainEntityStatistics
        {
            TotalCount = totalCount,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount,
            StatusCounts = statusCounts,
            CategoryCounts = categoryCounts,
            PriorityCounts = priorityCounts,
            RecentActivityCount = recentActivityCount,
            AverageItemsPerEntity = averageItemsPerEntity
        };
    }

    /// <summary>
    /// Gets DomainEntities by priority range
    /// </summary>
    public async Task<IEnumerable<DomainEntity>> GetByPriorityRangeAsync(int minPriority, int maxPriority, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.Priority >= minPriority && e.Priority <= maxPriority)
            .OrderBy(e => e.Priority)
            .ThenBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets DomainEntities that were modified recently
    /// </summary>
    public async Task<IEnumerable<DomainEntity>> GetRecentlyModifiedAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        return await _dbSet
            .Where(e => e.ModifiedDate >= cutoffDate)
            .OrderByDescending(e => e.ModifiedDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets DomainEntities by external ID
    /// </summary>
    public async Task<DomainEntity?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => e.ExternalId == externalId, cancellationToken);
    }

    /// <summary>
    /// Gets all unique categories
    /// </summary>
    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(e => e.Category != null)
            .Select(e => e.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all unique tags
    /// </summary>
    public async Task<IEnumerable<string>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithTags = await _dbSet
            .Where(e => e.Tags != null && e.Tags != "")
            .Select(e => e.Tags!)
            .ToListAsync(cancellationToken);

        var allTags = new HashSet<string>();

        foreach (var tagString in entitiesWithTags)
        {
            var tags = tagString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(t => t.Trim())
                               .Where(t => !string.IsNullOrEmpty(t));

            foreach (var tag in tags)
            {
                allTags.Add(tag);
            }
        }

        return allTags.OrderBy(t => t);
    }

    /// <summary>
    /// Bulk updates the status of multiple entities
    /// </summary>
    public async Task<int> BulkUpdateStatusAsync(IEnumerable<int> entityIds, DomainEntityStatus newStatus, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var entities = await _dbSet
            .Where(e => entityIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            entity.Status = newStatus;
            entity.ModifiedDate = DateTime.UtcNow;
            entity.ModifiedBy = modifiedBy;
        }

        return entities.Count;
    }

    /// <summary>
    /// Bulk updates the active status of multiple entities
    /// </summary>
    public async Task<int> BulkUpdateActiveStatusAsync(IEnumerable<int> entityIds, bool isActive, string modifiedBy, CancellationToken cancellationToken = default)
    {
        var entities = await _dbSet
            .Where(e => entityIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            entity.IsActive = isActive;
            entity.ModifiedDate = DateTime.UtcNow;
            entity.ModifiedBy = modifiedBy;
        }

        return entities.Count;
    }
}
