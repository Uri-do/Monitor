using Microsoft.EntityFrameworkCore;
using EnterpriseApp.Core.Entities;
using EnterpriseApp.Core.Enums;
using EnterpriseApp.Core.Interfaces;
using EnterpriseApp.Infrastructure.Data;

namespace EnterpriseApp.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for AuditLog
/// </summary>
public class AuditRepository : Repository<AuditLog>, IAuditRepository
{
    /// <summary>
    /// Initializes a new instance of the AuditRepository class
    /// </summary>
    public AuditRepository(ApplicationDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, string entityId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetByActionAsync(AuditAction action, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Action == action)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs with pagination and filtering
    /// </summary>
    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedWithFilterAsync(
        int page,
        int pageSize,
        string? entityName = null,
        string? entityId = null,
        string? userId = null,
        AuditAction? action = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? severity = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(entityName))
        {
            query = query.Where(a => a.EntityName == entityName);
        }

        if (!string.IsNullOrEmpty(entityId))
        {
            query = query.Where(a => a.EntityId == entityId);
        }

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (action.HasValue)
        {
            query = query.Where(a => a.Action == action.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(severity))
        {
            query = query.Where(a => a.Severity == severity);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var items = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Gets audit logs by correlation ID
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.CorrelationId == correlationId)
            .OrderBy(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs by session ID
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetBySessionIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.SessionId == sessionId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs by IP address
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.IpAddress == ipAddress)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit logs by severity level
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetBySeverityAsync(string severity, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Severity == severity)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets audit statistics
    /// </summary>
    public async Task<AuditStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Action counts
        var actionCounts = await query
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Action, x => x.Count, cancellationToken);

        // Entity counts
        var entityCounts = await query
            .GroupBy(a => a.EntityName)
            .Select(g => new { EntityName = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EntityName, x => x.Count, cancellationToken);

        // User activity counts
        var userCounts = await query
            .GroupBy(a => a.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

        // Severity counts
        var severityCounts = await query
            .GroupBy(a => a.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Severity, x => x.Count, cancellationToken);

        // Daily activity (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var dailyActivity = await query
            .Where(a => a.Timestamp >= thirtyDaysAgo)
            .GroupBy(a => a.Timestamp.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);

        return new AuditStatistics
        {
            TotalCount = totalCount,
            ActionCounts = actionCounts,
            EntityCounts = entityCounts,
            TopUserActivity = userCounts,
            SeverityCounts = severityCounts,
            DailyActivity = dailyActivity,
            DateRange = new DateRange
            {
                StartDate = startDate,
                EndDate = endDate
            }
        };
    }

    /// <summary>
    /// Gets recent audit activity
    /// </summary>
    public async Task<IEnumerable<AuditLog>> GetRecentActivityAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Cleans up old audit logs
    /// </summary>
    public async Task<int> CleanupOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var oldLogs = await _dbSet
            .Where(a => a.Timestamp < cutoffDate)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(oldLogs);
        return oldLogs.Count;
    }

    /// <summary>
    /// Gets unique entity names from audit logs
    /// </summary>
    public async Task<IEnumerable<string>> GetEntityNamesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets unique user IDs from audit logs
    /// </summary>
    public async Task<IEnumerable<string>> GetUserIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Select(a => a.UserId)
            .Distinct()
            .OrderBy(userId => userId)
            .ToListAsync(cancellationToken);
    }
}

/// <summary>
/// Audit statistics model
/// </summary>
public class AuditStatistics
{
    /// <summary>
    /// Total number of audit logs
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Count by action type
    /// </summary>
    public Dictionary<AuditAction, int> ActionCounts { get; set; } = new();

    /// <summary>
    /// Count by entity type
    /// </summary>
    public Dictionary<string, int> EntityCounts { get; set; } = new();

    /// <summary>
    /// Top user activity
    /// </summary>
    public Dictionary<string, int> TopUserActivity { get; set; } = new();

    /// <summary>
    /// Count by severity level
    /// </summary>
    public Dictionary<string, int> SeverityCounts { get; set; } = new();

    /// <summary>
    /// Daily activity counts
    /// </summary>
    public Dictionary<DateTime, int> DailyActivity { get; set; } = new();

    /// <summary>
    /// Date range for the statistics
    /// </summary>
    public DateRange DateRange { get; set; } = new();
}

/// <summary>
/// Date range model
/// </summary>
public class DateRange
{
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }
}
