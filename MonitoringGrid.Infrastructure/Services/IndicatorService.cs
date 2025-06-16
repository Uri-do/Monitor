using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Optimized service implementation for Indicator management operations
/// Includes caching and performance optimizations
/// </summary>
public class IndicatorService : IIndicatorService
{
    private readonly MonitoringContext _context;
    private readonly ICacheService _cacheService;
    private readonly ILogger<IndicatorService> _logger;

    public IndicatorService(
        MonitoringContext context,
        ICacheService cacheService,
        ILogger<IndicatorService> logger)
    {
        _context = context;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<List<Indicator>> GetAllIndicatorsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all indicators");

        return await _cacheService.GetOrSetAsync(
            CacheKeys.AllIndicators,
            async () =>
            {
                return await _context.Indicators
                    .Include(i => i.IndicatorContacts)
                        .ThenInclude(ic => ic.Contact)
                    .Include(i => i.OwnerContact)
                    .Include(i => i.Scheduler)
                    .AsSplitQuery() // Optimize for multiple includes
                    .OrderBy(i => i.IndicatorName)
                    .ToListAsync(cancellationToken);
            },
            CacheExpirations.Indicators,
            cancellationToken);
    }

    public async Task<Indicator?> GetIndicatorByIdAsync(long indicatorId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving indicator with ID {IndicatorId}", indicatorId);

        return await _context.Indicators
            .Include(i => i.IndicatorContacts)
                .ThenInclude(ic => ic.Contact)
            .Include(i => i.OwnerContact)
            .FirstOrDefaultAsync(i => i.IndicatorID == indicatorId, cancellationToken);
    }

    public async Task<List<Indicator>> GetActiveIndicatorsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving active indicators");

        return await _cacheService.GetOrSetAsync(
            CacheKeys.ActiveIndicators,
            async () =>
            {
                return await _context.Indicators
                    .Include(i => i.IndicatorContacts)
                        .ThenInclude(ic => ic.Contact)
                    .Include(i => i.OwnerContact)
                    .Include(i => i.Scheduler)
                    .Where(i => i.IsActive)
                    .AsSplitQuery()
                    .OrderBy(i => i.IndicatorName)
                    .ToListAsync(cancellationToken);
            },
            CacheExpirations.Indicators,
            cancellationToken);
    }

    public async Task<List<Indicator>> GetIndicatorsByOwnerAsync(int ownerContactId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving indicators for owner {OwnerContactId}", ownerContactId);

        return await _cacheService.GetOrSetAsync(
            CacheKeys.IndicatorsByOwner(ownerContactId),
            async () =>
            {
                return await _context.Indicators
                    .Include(i => i.IndicatorContacts)
                        .ThenInclude(ic => ic.Contact)
                    .Include(i => i.OwnerContact)
                    .Include(i => i.Scheduler)
                    .Where(i => i.OwnerContactId == ownerContactId && i.IsActive)
                    .AsSplitQuery()
                    .OrderBy(i => i.IndicatorName)
                    .ToListAsync(cancellationToken);
            },
            CacheExpirations.Indicators,
            cancellationToken);
    }

    public async Task<List<Indicator>> GetIndicatorsByPriorityAsync(string priority, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving indicators with priority {Priority}", priority);

        return await _cacheService.GetOrSetAsync(
            CacheKeys.IndicatorsByPriority(priority),
            async () =>
            {
                return await _context.Indicators
                    .Include(i => i.IndicatorContacts)
                        .ThenInclude(ic => ic.Contact)
                    .Include(i => i.OwnerContact)
                    .Include(i => i.Scheduler)
                    .Where(i => i.Priority == priority && i.IsActive)
                    .AsSplitQuery()
                    .OrderBy(i => i.IndicatorName)
                    .ToListAsync(cancellationToken);
            },
            CacheExpirations.Indicators,
            cancellationToken);
    }

    public async Task<List<Indicator>> GetDueIndicatorsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("GetDueIndicatorsAsync - Starting to retrieve indicators due for execution");

        return await _cacheService.GetOrSetAsync(
            CacheKeys.DueIndicators,
            async () =>
            {
                _logger.LogInformation("GetDueIndicatorsAsync - Cache miss, fetching active indicators");
                var indicators = await GetActiveIndicatorsAsync(cancellationToken);
                _logger.LogInformation("GetDueIndicatorsAsync - Retrieved {Count} active indicators", indicators?.Count ?? 0);

                if (indicators == null || !indicators.Any())
                {
                    _logger.LogInformation("GetDueIndicatorsAsync - No active indicators found");
                    return new List<Indicator>();
                }

                _logger.LogInformation("GetDueIndicatorsAsync - Active indicators: {IndicatorDetails}",
                    string.Join(", ", indicators.Select(i => $"{i.IndicatorName} (ID: {i.IndicatorID}, LastRun: {i.LastRun}, LastMinutes: {i.LastMinutes})")));

                var dueIndicators = new List<Indicator>();
                foreach (var indicator in indicators)
                {
                    // Detailed logging for IsDue() logic
                    _logger.LogInformation("GetDueIndicatorsAsync - Checking indicator {IndicatorName} (ID: {IndicatorID})",
                        indicator.IndicatorName, indicator.IndicatorID);

                    _logger.LogInformation("GetDueIndicatorsAsync - Indicator details: IsActive={IsActive}, Scheduler={HasScheduler}, LastRun={LastRun}",
                        indicator.IsActive, indicator.Scheduler != null, indicator.LastRun);

                    if (indicator.Scheduler != null)
                    {
                        _logger.LogInformation("GetDueIndicatorsAsync - Scheduler details: IsEnabled={IsEnabled}, ScheduleType={ScheduleType}, IntervalMinutes={IntervalMinutes}, CronExpression={CronExpression}",
                            indicator.Scheduler.IsEnabled, indicator.Scheduler.ScheduleType, indicator.Scheduler.IntervalMinutes, indicator.Scheduler.CronExpression);

                        var isCurrentlyActive = indicator.Scheduler.IsCurrentlyActive();
                        _logger.LogInformation("GetDueIndicatorsAsync - Scheduler IsCurrentlyActive: {IsCurrentlyActive}", isCurrentlyActive);

                        if (indicator.LastRun.HasValue)
                        {
                            var nextExecution = indicator.Scheduler.GetNextExecutionTime(indicator.LastRun);
                            _logger.LogInformation("GetDueIndicatorsAsync - NextExecutionTime: {NextExecution}, CurrentTime: {CurrentTime}",
                                nextExecution, DateTime.UtcNow);
                        }
                        else
                        {
                            _logger.LogInformation("GetDueIndicatorsAsync - No LastRun, should be due immediately");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("GetDueIndicatorsAsync - No scheduler attached to indicator");
                    }

                    var isDue = indicator.IsDue();
                    _logger.LogInformation("GetDueIndicatorsAsync - Final IsDue result: {IsDue} for {IndicatorName} (ID: {IndicatorID})",
                        isDue, indicator.IndicatorName, indicator.IndicatorID);

                    if (isDue)
                    {
                        dueIndicators.Add(indicator);
                    }
                }

                _logger.LogInformation("GetDueIndicatorsAsync - Found {Count} due indicators: {DueIndicatorNames}",
                    dueIndicators.Count,
                    string.Join(", ", dueIndicators.Select(i => $"{i.IndicatorName} (ID: {i.IndicatorID})")));

                return dueIndicators;
            },
            CacheExpirations.Short, // Short cache for due indicators as they change frequently
            cancellationToken);
    }

    public async Task<Indicator> CreateIndicatorAsync(Indicator indicator, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating new indicator {IndicatorName}", indicator.IndicatorName);

        indicator.CreatedDate = DateTime.UtcNow;
        indicator.UpdatedDate = DateTime.UtcNow;

        _context.Indicators.Add(indicator);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate relevant caches
        await InvalidateIndicatorCachesAsync(indicator, cancellationToken);

        _logger.LogInformation("Created indicator {IndicatorId}: {IndicatorName}",
            indicator.IndicatorID, indicator.IndicatorName);

        return indicator;
    }

    public async Task<Indicator> UpdateIndicatorAsync(Indicator indicator, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating indicator {IndicatorId}", indicator.IndicatorID);

        indicator.UpdatedDate = DateTime.UtcNow;

        _context.Indicators.Update(indicator);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate relevant caches
        await InvalidateIndicatorCachesAsync(indicator, cancellationToken);

        _logger.LogInformation("Updated indicator {IndicatorId}: {IndicatorName}",
            indicator.IndicatorID, indicator.IndicatorName);

        return indicator;
    }

    public async Task<bool> DeleteIndicatorAsync(long indicatorId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting indicator {IndicatorId}", indicatorId);
        
        var indicator = await GetIndicatorByIdAsync(indicatorId, cancellationToken);
        if (indicator == null)
        {
            _logger.LogWarning("Indicator {IndicatorId} not found for deletion", indicatorId);
            return false;
        }
        
        _context.Indicators.Remove(indicator);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Deleted indicator {IndicatorId}: {IndicatorName}", 
            indicatorId, indicator.IndicatorName);
        
        return true;
    }

    public async Task<bool> AddContactsToIndicatorAsync(long indicatorId, List<int> contactIds, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding {Count} contacts to indicator {IndicatorId}", contactIds.Count, indicatorId);
        
        var indicator = await GetIndicatorByIdAsync(indicatorId, cancellationToken);
        if (indicator == null)
        {
            _logger.LogWarning("Indicator {IndicatorId} not found", indicatorId);
            return false;
        }
        
        var existingContactIds = indicator.IndicatorContacts.Select(ic => ic.ContactId).ToHashSet();
        var newContactIds = contactIds.Where(id => !existingContactIds.Contains(id)).ToList();
        
        foreach (var contactId in newContactIds)
        {
            var indicatorContact = new IndicatorContact
            {
                IndicatorID = indicatorId,
                ContactId = contactId
            };
            _context.IndicatorContacts.Add(indicatorContact);
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Added {Count} new contacts to indicator {IndicatorId}", 
            newContactIds.Count, indicatorId);
        
        return true;
    }

    public async Task<bool> RemoveContactsFromIndicatorAsync(long indicatorId, List<int> contactIds, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Removing {Count} contacts from indicator {IndicatorId}", contactIds.Count, indicatorId);
        
        var indicatorContacts = await _context.IndicatorContacts
            .Where(ic => ic.IndicatorID == indicatorId && contactIds.Contains(ic.ContactId))
            .ToListAsync(cancellationToken);
        
        _context.IndicatorContacts.RemoveRange(indicatorContacts);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Removed {Count} contacts from indicator {IndicatorId}", 
            indicatorContacts.Count, indicatorId);
        
        return true;
    }

    public Task<List<IndicatorValueTrend>> GetIndicatorHistoryAsync(long indicatorId, int days = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving history for indicator {IndicatorId}, last {Days} days", indicatorId, days);

        // TODO: Implement with new IndicatorsExecutionHistory table
        // For now, return empty list since HistoricalData table is obsolete
        return Task.FromResult(new List<IndicatorValueTrend>());
    }

    public async Task<IndicatorDashboard> GetIndicatorDashboardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving indicator dashboard data");

        return await _cacheService.GetOrSetAsync(
            CacheKeys.IndicatorDashboard,
            async () =>
            {
                var today = DateTime.UtcNow.Date;

                // Use efficient database queries instead of loading all indicators into memory
                var dashboard = new IndicatorDashboard();

        // Get counts with single database queries
        dashboard.TotalIndicators = await _context.Indicators.CountAsync(cancellationToken);
        dashboard.ActiveIndicators = await _context.Indicators.CountAsync(i => i.IsActive, cancellationToken);
        dashboard.RunningIndicators = await _context.Indicators.CountAsync(i => i.IsCurrentlyRunning, cancellationToken);
        dashboard.IndicatorsExecutedToday = await _context.Indicators
            .CountAsync(i => i.LastRun != null && i.LastRun.Value.Date == today, cancellationToken);

        // Get priority counts with a single group by query
        dashboard.CountByPriority = await _context.Indicators
            .GroupBy(i => i.Priority)
            .Select(g => new IndicatorCountByPriority { Priority = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // Calculate due indicators efficiently using database query
        var now = DateTime.UtcNow;
        dashboard.DueIndicators = await _context.Indicators
            .Where(i => i.IsActive &&
                       (i.LastRun == null ||
                        EF.Functions.DateDiffMinute(i.LastRun.Value, now) >= i.LastMinutes))
            .CountAsync(cancellationToken);

        // TODO: Get recent executions from new IndicatorsExecutionHistory table
        // For now, return empty list since HistoricalData table is obsolete
        dashboard.RecentExecutions = new List<IndicatorExecutionSummary>();

        // Get alerts triggered today - use a safe default if AlertLogs doesn't exist
        try
        {
            dashboard.AlertsTriggeredToday = await _context.AlertLogs
                .Where(a => a.TriggerTime.Date == today)
                .CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve alert count, using default value");
            dashboard.AlertsTriggeredToday = 0;
        }

                _logger.LogDebug("Dashboard data retrieved efficiently with optimized queries");
                return dashboard;
            },
            CacheExpirations.Short, // Cache for 30 seconds since dashboard data changes frequently
            cancellationToken);
    }

    public async Task<IndicatorTestResult> TestIndicatorAsync(long indicatorId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Testing indicator {IndicatorId}", indicatorId);
        
        var indicator = await GetIndicatorByIdAsync(indicatorId, cancellationToken);
        if (indicator == null)
        {
            return new IndicatorTestResult
            {
                WasSuccessful = false,
                ErrorMessage = $"Indicator {indicatorId} not found"
            };
        }
        
        // This would be implemented with the actual execution logic
        // For now, return a placeholder result
        return new IndicatorTestResult
        {
            WasSuccessful = true,
            CurrentValue = 100,
            ThresholdValue = indicator.ThresholdValue,
            ThresholdBreached = false,
            ExecutionDuration = TimeSpan.FromSeconds(1)
        };
    }

    public async Task<IndicatorStatistics> GetIndicatorStatisticsAsync(long indicatorId, int days = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving statistics for indicator {IndicatorId}, last {Days} days", indicatorId, days);
        
        var indicator = await GetIndicatorByIdAsync(indicatorId, cancellationToken);
        if (indicator == null)
        {
            throw new ArgumentException($"Indicator {indicatorId} not found");
        }
        
        var startDate = DateTime.UtcNow.AddDays(-days);

        var alerts = await _context.AlertLogs
            .Where(a => a.IndicatorId == indicatorId && a.TriggerTime >= startDate)
            .ToListAsync(cancellationToken);

        // TODO: Implement with new IndicatorsExecutionHistory table
        return new IndicatorStatistics
        {
            IndicatorId = indicatorId,
            IndicatorName = indicator.IndicatorName,
            TotalExecutions = 0, // TODO: Get from new table
            SuccessfulExecutions = 0, // TODO: Get from new table
            FailedExecutions = 0, // TODO: Get from new table
            AlertsTriggered = alerts.Count(),
            AverageValue = null, // TODO: Calculate from new table
            MinValue = null, // TODO: Calculate from new table
            MaxValue = null, // TODO: Calculate from new table
            LastExecution = indicator.LastRun,
            NextExecution = indicator.GetNextRunTime(),
            ValueTrend = new List<IndicatorValueTrend>() // TODO: Get from new table
        };
    }

    /// <summary>
    /// Invalidates relevant caches when an indicator is modified
    /// </summary>
    private async Task InvalidateIndicatorCachesAsync(Indicator indicator, CancellationToken cancellationToken = default)
    {
        try
        {
            // Invalidate general caches
            await _cacheService.RemoveAsync(CacheKeys.AllIndicators, cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.ActiveIndicators, cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.DueIndicators, cancellationToken);
            await _cacheService.RemoveAsync(CacheKeys.IndicatorDashboard, cancellationToken);

            // Invalidate specific indicator cache
            await _cacheService.RemoveAsync(CacheKeys.IndicatorById(indicator.IndicatorID), cancellationToken);

            // Invalidate owner-specific cache
            await _cacheService.RemoveAsync(CacheKeys.IndicatorsByOwner(indicator.OwnerContactId), cancellationToken);

            // Invalidate priority-specific cache
            await _cacheService.RemoveAsync(CacheKeys.IndicatorsByPriority(indicator.Priority), cancellationToken);

            _logger.LogDebug("Invalidated caches for indicator {IndicatorId}", indicator.IndicatorID);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error invalidating caches for indicator {IndicatorId}", indicator.IndicatorID);
        }
    }
}
