using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
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

    public async Task<Result<Core.Models.PagedResult<Indicator>>> GetAllIndicatorsAsync(
        IndicatorFilterOptions? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving all indicators with filter");

            var indicators = await _cacheService.GetOrSetAsync(
                CacheKeys.AllIndicators,
                async () =>
                {
                    var query = _context.Indicators
                        .Include(i => i.IndicatorContacts)
                            .ThenInclude(ic => ic.Contact)
                        .Include(i => i.OwnerContact)
                        // .Include(i => i.Scheduler) // Temporarily disabled until Schedulers table is created
                        .AsSplitQuery();

                    // Apply filters if provided
                    if (filter != null)
                    {
                        if (filter.IsActive.HasValue)
                            query = query.Where(i => i.IsActive == filter.IsActive.Value);

                        if (filter.Priorities != null && filter.Priorities.Any())
                            query = query.Where(i => filter.Priorities.Contains(i.Priority));

                        if (filter.OwnerContactIds != null && filter.OwnerContactIds.Any())
                            query = query.Where(i => filter.OwnerContactIds.Contains(i.OwnerContactId));
                    }

                    return await query
                        .OrderBy(i => i.IndicatorName)
                        .ToListAsync(cancellationToken);
                },
                CacheExpirations.Indicators,
                cancellationToken);

            // Apply pagination
            var pagination = filter?.Pagination ?? new PaginationOptions();
            var totalCount = indicators.Count;
            var pagedIndicators = indicators
                .Skip((pagination.Page - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToList();

            var result = new Core.Models.PagedResult<Indicator>
            {
                Items = pagedIndicators,
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize)
            };

            return Result<Core.Models.PagedResult<Indicator>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all indicators");
            return Result.Failure<Core.Models.PagedResult<Indicator>>(Error.Failure("Indicator.RetrievalFailed", $"Failed to retrieve indicators: {ex.Message}"));
        }
    }

    public async Task<Result<Indicator>> GetIndicatorByIdAsync(long indicatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("DEBUG: Retrieving indicator with ID {IndicatorId}", indicatorId);

            var indicator = await _context.Indicators
                .Include(i => i.IndicatorContacts)
                    .ThenInclude(ic => ic.Contact)
                .Include(i => i.OwnerContact)
                .Include(i => i.Scheduler)
                .FirstOrDefaultAsync(i => i.IndicatorID == indicatorId, cancellationToken);

            _logger.LogInformation("DEBUG: Query executed for indicator {IndicatorId}, result: {Found}",
                indicatorId, indicator != null ? "FOUND" : "NOT FOUND");

            if (indicator == null)
            {
                _logger.LogWarning("DEBUG: Indicator {IndicatorId} not found, returning failure", indicatorId);
                return Result.Failure<Indicator>(Error.NotFound("Indicator", indicatorId));
            }

            _logger.LogInformation("DEBUG: Successfully found indicator {IndicatorId}: {IndicatorName}",
                indicator.IndicatorID, indicator.IndicatorName);
            return Result<Indicator>.Success(indicator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DEBUG: Error retrieving indicator {IndicatorId}", indicatorId);
            return Result.Failure<Indicator>(Error.Failure("Indicator.RetrievalFailed", $"Failed to retrieve indicator: {ex.Message}"));
        }
    }

    public async Task<Result<List<Indicator>>> GetActiveIndicatorsAsync(
        IndicatorFilterOptions? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving active indicators");

            var indicators = await _cacheService.GetOrSetAsync(
                CacheKeys.ActiveIndicators,
                async () =>
                {
                    var query = _context.Indicators
                        .Include(i => i.IndicatorContacts)
                            .ThenInclude(ic => ic.Contact)
                        .Include(i => i.OwnerContact)
                        .Include(i => i.Scheduler) // Re-enabled now that Schedulers table exists
                        .Where(i => i.IsActive)
                        .AsSplitQuery();

                    // Apply additional filters if provided
                    if (filter != null)
                    {
                        if (filter.Priorities != null && filter.Priorities.Any())
                            query = query.Where(i => filter.Priorities.Contains(i.Priority));

                        if (filter.OwnerContactIds != null && filter.OwnerContactIds.Any())
                            query = query.Where(i => filter.OwnerContactIds.Contains(i.OwnerContactId));
                    }

                    return await query
                        .OrderBy(i => i.IndicatorName)
                        .ToListAsync(cancellationToken);
                },
                CacheExpirations.Indicators,
                cancellationToken);

            return Result<List<Indicator>>.Success(indicators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active indicators");
            return Result.Failure<List<Indicator>>(Error.Failure("Indicator.ActiveRetrievalFailed", $"Failed to retrieve active indicators: {ex.Message}"));
        }
    }

    public async Task<Result<Core.Models.PagedResult<Indicator>>> GetIndicatorsByOwnerAsync(
        int ownerContactId,
        PaginationOptions? pagination = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving indicators for owner {OwnerContactId}", ownerContactId);

            var indicators = await _cacheService.GetOrSetAsync(
                CacheKeys.IndicatorsByOwner(ownerContactId),
                async () =>
                {
                    return await _context.Indicators
                        .Include(i => i.IndicatorContacts)
                            .ThenInclude(ic => ic.Contact)
                        .Include(i => i.OwnerContact)
                        .Include(i => i.Scheduler) // Re-enabled now that Schedulers table exists
                        .Where(i => i.OwnerContactId == ownerContactId && i.IsActive)
                        .AsSplitQuery()
                        .OrderBy(i => i.IndicatorName)
                        .ToListAsync(cancellationToken);
                },
                CacheExpirations.Indicators,
                cancellationToken);

            // Apply pagination
            var paginationOptions = pagination ?? new PaginationOptions();
            var totalCount = indicators.Count;
            var pagedIndicators = indicators
                .Skip((paginationOptions.Page - 1) * paginationOptions.PageSize)
                .Take(paginationOptions.PageSize)
                .ToList();

            var result = new Core.Models.PagedResult<Indicator>
            {
                Items = pagedIndicators,
                TotalCount = totalCount,
                Page = paginationOptions.Page,
                PageSize = paginationOptions.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / paginationOptions.PageSize)
            };

            return Result<Core.Models.PagedResult<Indicator>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving indicators for owner {OwnerContactId}", ownerContactId);
            return Result.Failure<Core.Models.PagedResult<Indicator>>(Error.Failure("Indicator.OwnerRetrievalFailed", $"Failed to retrieve indicators for owner: {ex.Message}"));
        }
    }

    public async Task<Result<List<Indicator>>> GetIndicatorsByPriorityAsync(
        string priority,
        IndicatorFilterOptions? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving indicators with priority {Priority}", priority);

            var indicators = await _cacheService.GetOrSetAsync(
                CacheKeys.IndicatorsByPriority(priority),
                async () =>
                {
                    var query = _context.Indicators
                        .Include(i => i.IndicatorContacts)
                            .ThenInclude(ic => ic.Contact)
                        .Include(i => i.OwnerContact)
                        .Include(i => i.Scheduler) // Re-enabled now that Schedulers table exists
                        .Where(i => i.Priority == priority && i.IsActive)
                        .AsSplitQuery();

                    // Apply additional filters if provided
                    if (filter != null)
                    {
                        if (filter.OwnerContactIds != null && filter.OwnerContactIds.Any())
                            query = query.Where(i => filter.OwnerContactIds.Contains(i.OwnerContactId));
                    }

                    return await query
                        .OrderBy(i => i.IndicatorName)
                        .ToListAsync(cancellationToken);
                },
                CacheExpirations.Indicators,
                cancellationToken);

            return Result<List<Indicator>>.Success(indicators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving indicators with priority {Priority}", priority);
            return Result.Failure<List<Indicator>>(Error.Failure("Indicator.PriorityRetrievalFailed", $"Failed to retrieve indicators by priority: {ex.Message}"));
        }
    }

    public async Task<Result<List<Indicator>>> GetDueIndicatorsAsync(
        PriorityFilterOptions? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GetDueIndicatorsAsync - Starting to retrieve indicators due for execution");

            var dueIndicators = await _cacheService.GetOrSetAsync(
                CacheKeys.DueIndicators,
                async () =>
                {
                    _logger.LogInformation("GetDueIndicatorsAsync - Cache miss, fetching active indicators");
                    var activeResult = await GetActiveIndicatorsAsync(null, cancellationToken);
                    if (!activeResult.IsSuccess)
                    {
                        _logger.LogWarning("GetDueIndicatorsAsync - Failed to get active indicators: {Error}", activeResult.Error.Message);
                        return new List<Indicator>();
                    }

                    var indicators = activeResult.Value;
                    _logger.LogInformation("GetDueIndicatorsAsync - Retrieved {Count} active indicators", indicators?.Count ?? 0);

                    if (indicators == null || !indicators.Any())
                    {
                        _logger.LogInformation("GetDueIndicatorsAsync - No active indicators found");
                        return new List<Indicator>();
                    }

                    _logger.LogInformation("GetDueIndicatorsAsync - Active indicators: {IndicatorDetails}",
                        string.Join(", ", indicators.Select(i => $"{i.IndicatorName} (ID: {i.IndicatorID}, LastRun: {i.LastRun}, LastMinutes: {i.LastMinutes})")));

                    var dueList = new List<Indicator>();
                    foreach (var indicator in indicators)
                    {
                        // Apply priority filter if provided
                        if (filter?.Priorities != null && filter.Priorities.Any() && !filter.Priorities.Contains(indicator.Priority))
                            continue;

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
                            dueList.Add(indicator);
                        }
                    }

                    _logger.LogInformation("GetDueIndicatorsAsync - Found {Count} due indicators: {DueIndicatorNames}",
                        dueList.Count,
                        string.Join(", ", dueList.Select(i => $"{i.IndicatorName} (ID: {i.IndicatorID})")));

                    return dueList;
                },
                TimeSpan.FromSeconds(30), // Very short cache for due indicators as they change every minute
                cancellationToken);

            return Result<List<Indicator>>.Success(dueIndicators);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving due indicators");
            return Result.Failure<List<Indicator>>(Error.Failure("Indicator.DueRetrievalFailed", $"Failed to retrieve due indicators: {ex.Message}"));
        }
    }

    public async Task<Result<Indicator>> CreateIndicatorAsync(CreateIndicatorRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating new indicator {IndicatorName}", request.IndicatorName);

            var indicator = new Indicator
            {
                IndicatorName = request.IndicatorName,
                IndicatorCode = request.IndicatorCode,
                IndicatorDesc = request.IndicatorDesc,
                CollectorID = request.CollectorID,
                CollectorItemName = request.CollectorItemName,
                SchedulerID = request.SchedulerID,
                LastMinutes = request.LastMinutes,
                ThresholdType = request.ThresholdType,
                ThresholdField = request.ThresholdField,
                ThresholdComparison = request.ThresholdComparison,
                ThresholdValue = request.ThresholdValue,
                Priority = request.Priority,
                OwnerContactId = request.OwnerContactId,
                AverageLastDays = request.AverageLastDays,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.Indicators.Add(indicator);
            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate relevant caches
            await InvalidateIndicatorCachesAsync(indicator, cancellationToken);

            _logger.LogInformation("Created indicator {IndicatorId}: {IndicatorName}",
                indicator.IndicatorID, indicator.IndicatorName);

            return Result<Indicator>.Success(indicator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating indicator {IndicatorName}", request.IndicatorName);
            return Result.Failure<Indicator>(Error.Failure("Indicator.CreateFailed", $"Failed to create indicator: {ex.Message}"));
        }
    }

    public async Task<Result<Indicator>> UpdateIndicatorAsync(UpdateIndicatorRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating indicator {IndicatorId}", request.IndicatorID);

            var indicator = await _context.Indicators
                .FirstOrDefaultAsync(i => i.IndicatorID == request.IndicatorID, cancellationToken);

            if (indicator == null)
            {
                return Result.Failure<Indicator>(Error.NotFound("Indicator", request.IndicatorID));
            }

            // Update properties
            if (!string.IsNullOrEmpty(request.IndicatorName))
                indicator.IndicatorName = request.IndicatorName;
            if (!string.IsNullOrEmpty(request.IndicatorCode))
                indicator.IndicatorCode = request.IndicatorCode;
            if (request.IndicatorDesc != null)
                indicator.IndicatorDesc = request.IndicatorDesc;
            if (request.CollectorID.HasValue)
                indicator.CollectorID = request.CollectorID.Value;
            if (!string.IsNullOrEmpty(request.CollectorItemName))
                indicator.CollectorItemName = request.CollectorItemName;
            if (request.SchedulerID.HasValue)
                indicator.SchedulerID = request.SchedulerID;
            if (request.LastMinutes.HasValue)
                indicator.LastMinutes = request.LastMinutes.Value;
            if (!string.IsNullOrEmpty(request.ThresholdType))
                indicator.ThresholdType = request.ThresholdType;
            if (!string.IsNullOrEmpty(request.ThresholdField))
                indicator.ThresholdField = request.ThresholdField;
            if (!string.IsNullOrEmpty(request.ThresholdComparison))
                indicator.ThresholdComparison = request.ThresholdComparison;
            if (request.ThresholdValue.HasValue)
                indicator.ThresholdValue = request.ThresholdValue.Value;
            if (!string.IsNullOrEmpty(request.Priority))
                indicator.Priority = request.Priority;
            if (request.OwnerContactId.HasValue)
                indicator.OwnerContactId = request.OwnerContactId.Value;
            if (request.AverageLastDays.HasValue)
                indicator.AverageLastDays = request.AverageLastDays;
            if (request.IsActive.HasValue)
                indicator.IsActive = request.IsActive.Value;

            indicator.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate relevant caches
            await InvalidateIndicatorCachesAsync(indicator, cancellationToken);

            _logger.LogInformation("Updated indicator {IndicatorId}: {IndicatorName}",
                indicator.IndicatorID, indicator.IndicatorName);

            return Result<Indicator>.Success(indicator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating indicator {IndicatorId}", request.IndicatorID);
            return Result.Failure<Indicator>(Error.Failure("Indicator.UpdateFailed", $"Failed to update indicator: {ex.Message}"));
        }
    }

    public async Task<Result> DeleteIndicatorAsync(
        long indicatorId,
        DeleteIndicatorOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting indicator {IndicatorId}", indicatorId);

            var indicatorResult = await GetIndicatorByIdAsync(indicatorId, cancellationToken);
            if (!indicatorResult.IsSuccess)
            {
                _logger.LogWarning("Indicator {IndicatorId} not found for deletion", indicatorId);
                return Result.Failure(Error.NotFound("Indicator", indicatorId));
            }

            var indicator = indicatorResult.Value;

            // Check for dependencies if options specify
            if (options?.CheckDependencies == true)
            {
                // Check for related executions, alerts, etc.
                var hasAlerts = await _context.AlertLogs
                    .AnyAsync(a => a.IndicatorId == indicatorId, cancellationToken);

                if (hasAlerts && options.ForceDelete != true)
                {
                    return Result.Failure(Error.Validation("Indicator.HasDependencies", "Cannot delete indicator with existing alerts. Use ForceDelete option to override."));
                }
            }

            _context.Indicators.Remove(indicator);
            await _context.SaveChangesAsync(cancellationToken);

            // Invalidate relevant caches
            await InvalidateIndicatorCachesAsync(indicator, cancellationToken);

            _logger.LogInformation("Deleted indicator {IndicatorId}: {IndicatorName}",
                indicatorId, indicator.IndicatorName);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting indicator {IndicatorId}", indicatorId);
            return Result.Failure(Error.Failure("Indicator.DeleteFailed", $"Failed to delete indicator: {ex.Message}"));
        }
    }

    public async Task<Result> AddContactsToIndicatorAsync(
        long indicatorId,
        List<int> contactIds,
        ContactAssignmentOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Adding {Count} contacts to indicator {IndicatorId}", contactIds.Count, indicatorId);

            var indicatorResult = await GetIndicatorByIdAsync(indicatorId, cancellationToken);
            if (!indicatorResult.IsSuccess)
            {
                _logger.LogWarning("Indicator {IndicatorId} not found", indicatorId);
                return Result.Failure(Error.NotFound("Indicator", indicatorId));
            }

            var indicator = indicatorResult.Value;

            var existingContactIds = indicator.IndicatorContacts.Select(ic => ic.ContactId).ToHashSet();
            var newContactIds = contactIds.Where(id => !existingContactIds.Contains(id)).ToList();

            if (!newContactIds.Any())
            {
                _logger.LogInformation("All contacts already assigned to indicator {IndicatorId}", indicatorId);
                return Result.Success();
            }

            // Validate contacts exist if options specify
            if (options?.ValidateContacts == true)
            {
                var existingContacts = await _context.Contacts
                    .Where(c => newContactIds.Contains(c.ContactId))
                    .Select(c => c.ContactId)
                    .ToListAsync(cancellationToken);

                var invalidContacts = newContactIds.Except(existingContacts).ToList();
                if (invalidContacts.Any())
                {
                    return Result.Failure(Error.Validation("Contact.InvalidIds", $"Invalid contact IDs: {string.Join(", ", invalidContacts)}"));
                }
            }

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

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding contacts to indicator {IndicatorId}", indicatorId);
            return Result.Failure(Error.Failure("Indicator.AddContactsFailed", $"Failed to add contacts to indicator: {ex.Message}"));
        }
    }

    public async Task<Result> RemoveContactsFromIndicatorAsync(
        long indicatorId,
        List<int> contactIds,
        ContactRemovalOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Removing {Count} contacts from indicator {IndicatorId}", contactIds.Count, indicatorId);

            var indicatorContacts = await _context.IndicatorContacts
                .Where(ic => ic.IndicatorID == indicatorId && contactIds.Contains(ic.ContactId))
                .ToListAsync(cancellationToken);

            if (!indicatorContacts.Any())
            {
                _logger.LogInformation("No contacts found to remove from indicator {IndicatorId}", indicatorId);
                return Result.Success();
            }

            _context.IndicatorContacts.RemoveRange(indicatorContacts);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed {Count} contacts from indicator {IndicatorId}",
                indicatorContacts.Count, indicatorId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing contacts from indicator {IndicatorId}", indicatorId);
            return Result.Failure(Error.Failure("Indicator.RemoveContactsFailed", $"Failed to remove contacts from indicator: {ex.Message}"));
        }
    }

    public async Task<Result<Core.Models.PagedResult<Core.Models.IndicatorValueTrend>>> GetIndicatorHistoryAsync(
        long indicatorId,
        HistoryFilterOptions? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var days = filter?.Days ?? 30;
            _logger.LogDebug("Retrieving history for indicator {IndicatorId}, last {Days} days", indicatorId, days);

            // TODO: Implement with new IndicatorsExecutionHistory table
            // For now, return empty list since HistoricalData table is obsolete
            var emptyResult = new Core.Models.PagedResult<Core.Models.IndicatorValueTrend>
            {
                Items = new List<Core.Models.IndicatorValueTrend>(),
                TotalCount = 0,
                Page = filter?.Pagination?.Page ?? 1,
                PageSize = filter?.Pagination?.PageSize ?? 50,
                TotalPages = 0
            };

            return Result<Core.Models.PagedResult<Core.Models.IndicatorValueTrend>>.Success(emptyResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving indicator history for {IndicatorId}", indicatorId);
            return Result.Failure<Core.Models.PagedResult<Core.Models.IndicatorValueTrend>>(Error.Failure("Indicator.HistoryRetrievalFailed", $"Failed to retrieve indicator history: {ex.Message}"));
        }
    }

    public async Task<Result<Core.Models.IndicatorDashboard>> GetIndicatorDashboardAsync(
        DashboardOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving indicator dashboard data");

            var dashboard = await _cacheService.GetOrSetAsync(
                CacheKeys.IndicatorDashboard,
                async () =>
                {
                    var today = DateTime.UtcNow.Date;

                    // Use efficient database queries instead of loading all indicators into memory
                    var dashboardData = new Core.Models.IndicatorDashboard();

                    // Get counts with single database queries
                    dashboardData.TotalIndicators = await _context.Indicators.CountAsync(cancellationToken);
                    dashboardData.ActiveIndicators = await _context.Indicators.CountAsync(i => i.IsActive, cancellationToken);
                    dashboardData.RunningIndicators = await _context.Indicators.CountAsync(i => i.IsCurrentlyRunning, cancellationToken);
                    dashboardData.IndicatorsExecutedToday = await _context.Indicators
                        .CountAsync(i => i.LastRun != null && i.LastRun.Value.Date == today, cancellationToken);

                    // Get priority counts with a single group by query
                    dashboardData.CountByPriority = await _context.Indicators
                        .GroupBy(i => i.Priority)
                        .Select(g => new IndicatorCountByPriority { Priority = g.Key, Count = g.Count() })
                        .ToListAsync(cancellationToken);

                    // Calculate due indicators efficiently using database query
                    var now = DateTime.UtcNow;
                    dashboardData.DueIndicators = await _context.Indicators
                        .Where(i => i.IsActive &&
                                   (i.LastRun == null ||
                                    EF.Functions.DateDiffMinute(i.LastRun.Value, now) >= i.LastMinutes))
                        .CountAsync(cancellationToken);

                    // TODO: Get recent executions from new IndicatorsExecutionHistory table
                    // For now, return empty list since HistoricalData table is obsolete
                    dashboardData.RecentExecutions = new List<IndicatorExecutionSummary>();

                    // Get alerts triggered today - use a safe default if AlertLogs doesn't exist
                    try
                    {
                        dashboardData.AlertsTriggeredToday = await _context.AlertLogs
                            .Where(a => a.TriggerTime.Date == today)
                            .CountAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not retrieve alert count, using default value");
                        dashboardData.AlertsTriggeredToday = 0;
                    }

                    _logger.LogDebug("Dashboard data retrieved efficiently with optimized queries");
                    return dashboardData;
                },
                CacheExpirations.Short, // Cache for 30 seconds since dashboard data changes frequently
                cancellationToken);

            return Result<Core.Models.IndicatorDashboard>.Success(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving indicator dashboard data");
            return Result.Failure<Core.Models.IndicatorDashboard>(Error.Failure("Indicator.DashboardRetrievalFailed", $"Failed to retrieve dashboard data: {ex.Message}"));
        }
    }

    public async Task<Result<Core.Models.IndicatorTestResult>> TestIndicatorAsync(
        long indicatorId,
        TestExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Testing indicator {IndicatorId}", indicatorId);

            var indicatorResult = await GetIndicatorByIdAsync(indicatorId, cancellationToken);
            if (!indicatorResult.IsSuccess)
            {
                return Result.Failure<Core.Models.IndicatorTestResult>(Error.NotFound("Indicator", indicatorId));
            }

            var indicator = indicatorResult.Value;

            // This would be implemented with the actual execution logic
            // For now, return a placeholder result
            var testResult = new Core.Models.IndicatorTestResult
            {
                WasSuccessful = true,
                CurrentValue = 100,
                ThresholdValue = indicator.ThresholdValue,
                ThresholdBreached = false,
                ExecutionDuration = TimeSpan.FromSeconds(1)
            };

            return Result<Core.Models.IndicatorTestResult>.Success(testResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing indicator {IndicatorId}", indicatorId);
            return Result.Failure<Core.Models.IndicatorTestResult>(Error.Failure("Indicator.TestFailed", $"Failed to test indicator: {ex.Message}"));
        }
    }

    public async Task<Result<Core.Models.IndicatorStatistics>> GetIndicatorStatisticsAsync(
        long indicatorId,
        StatisticsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var days = options?.Days ?? 30;
            _logger.LogDebug("Retrieving statistics for indicator {IndicatorId}, last {Days} days", indicatorId, days);

            var indicatorResult = await GetIndicatorByIdAsync(indicatorId, cancellationToken);
            if (!indicatorResult.IsSuccess)
            {
                return Result.Failure<Core.Models.IndicatorStatistics>(Error.NotFound("Indicator", indicatorId));
            }

            var indicator = indicatorResult.Value;

            var startDate = DateTime.UtcNow.AddDays(-days);

            var alerts = await _context.AlertLogs
                .Where(a => a.IndicatorId == indicatorId && a.TriggerTime >= startDate)
                .ToListAsync(cancellationToken);

            // TODO: Implement with new IndicatorsExecutionHistory table
            var statistics = new Core.Models.IndicatorStatistics
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
                ValueTrend = new List<Core.Models.IndicatorValueTrend>() // TODO: Get from new table
            };

            return Result<Core.Models.IndicatorStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for indicator {IndicatorId}", indicatorId);
            return Result.Failure<Core.Models.IndicatorStatistics>(Error.Failure("Indicator.StatisticsRetrievalFailed", $"Failed to retrieve indicator statistics: {ex.Message}"));
        }
    }

    public async Task<Result<Core.Models.IndicatorExecutionResult>> ExecuteIndicatorAsync(
        long indicatorId,
        ExecutionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Executing indicator {IndicatorId}", indicatorId);

            var indicatorResult = await GetIndicatorByIdAsync(indicatorId, cancellationToken);
            if (!indicatorResult.IsSuccess)
            {
                return Result.Failure<Core.Models.IndicatorExecutionResult>(Error.NotFound("Indicator", indicatorId));
            }

            var indicator = indicatorResult.Value;

            // TODO: Implement actual execution logic
            var executionResult = new Core.Models.IndicatorExecutionResult
            {
                ExecutionId = 0, // Will be set when saved to database
                IndicatorId = indicatorId,
                IndicatorName = indicator.IndicatorName,
                WasSuccessful = true,
                Value = 100,
                ThresholdBreached = false,
                ExecutionDuration = TimeSpan.FromSeconds(1),
                StartTime = DateTime.UtcNow,
                ExecutionContext = "Manual execution"
            };

            return Result<Core.Models.IndicatorExecutionResult>.Success(executionResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing indicator {IndicatorId}", indicatorId);
            return Result.Failure<Core.Models.IndicatorExecutionResult>(Error.Failure("Indicator.ExecutionFailed", $"Failed to execute indicator: {ex.Message}"));
        }
    }

    public async Task<Result<Core.Models.BulkOperationResult>> BulkUpdateIndicatorsAsync(
        BulkUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Bulk updating {Count} indicators", request.IndicatorIds.Count);

            var results = new List<Core.Models.BulkOperationItem>();

            foreach (var indicatorId in request.IndicatorIds)
            {
                try
                {
                    var updateRequest = new UpdateIndicatorRequest
                    {
                        IndicatorId = indicatorId,
                        IsActive = request.IsActive,
                        Priority = request.Priority
                    };

                    var result = await UpdateIndicatorAsync(updateRequest, cancellationToken);

                    results.Add(new Core.Models.BulkOperationItem
                    {
                        Id = indicatorId,
                        IsSuccess = result.IsSuccess,
                        ErrorMessage = result.IsSuccess ? null : result.Error.Message
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new Core.Models.BulkOperationItem
                    {
                        Id = indicatorId,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            var bulkResult = new Core.Models.BulkOperationResult
            {
                TotalItems = request.IndicatorIds.Count,
                SuccessfulItems = results.Count(r => r.IsSuccess),
                FailedItems = results.Count(r => !r.IsSuccess),
                Results = results
            };

            return Result<Core.Models.BulkOperationResult>.Success(bulkResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk update operation");
            return Result.Failure<Core.Models.BulkOperationResult>(Error.Failure("Indicator.BulkUpdateFailed", $"Bulk update failed: {ex.Message}"));
        }
    }

    public async Task<Result<Core.Models.ValidationResult>> ValidateIndicatorAsync(
        ValidateIndicatorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating indicator configuration");

            var validationResult = new Core.Models.ValidationResult
            {
                IsValid = true,
                Errors = new List<Core.Models.ValidationError>()
            };

            // Determine which request to validate
            string? indicatorName = null;
            string? indicatorCode = null;
            decimal? thresholdValue = null;
            long? indicatorId = null;

            if (request.CreateRequest != null)
            {
                indicatorName = request.CreateRequest.IndicatorName;
                indicatorCode = request.CreateRequest.IndicatorCode;
                thresholdValue = request.CreateRequest.ThresholdValue;
                indicatorId = null; // New indicator
            }
            else if (request.UpdateRequest != null)
            {
                indicatorName = request.UpdateRequest.IndicatorName;
                indicatorCode = request.UpdateRequest.IndicatorCode;
                thresholdValue = request.UpdateRequest.ThresholdValue;
                indicatorId = request.UpdateRequest.IndicatorID;
            }

            // Basic validation
            if (string.IsNullOrEmpty(indicatorName))
                validationResult.Errors.Add(new Core.Models.ValidationError { Field = "IndicatorName", Message = "Indicator name is required" });

            if (string.IsNullOrEmpty(indicatorCode))
                validationResult.Errors.Add(new Core.Models.ValidationError { Field = "IndicatorCode", Message = "Indicator code is required" });

            if (thresholdValue.HasValue && thresholdValue <= 0)
                validationResult.Errors.Add(new Core.Models.ValidationError { Field = "ThresholdValue", Message = "Threshold value must be greater than 0" });

            // Check for duplicate codes
            if (!string.IsNullOrEmpty(indicatorCode))
            {
                var existingIndicator = await _context.Indicators
                    .Where(i => i.IndicatorCode == indicatorCode && i.IndicatorID != indicatorId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingIndicator != null)
                    validationResult.Errors.Add(new Core.Models.ValidationError { Field = "IndicatorCode", Message = $"Indicator code '{indicatorCode}' already exists" });
            }

            validationResult.IsValid = !validationResult.Errors.Any();

            return Result<Core.Models.ValidationResult>.Success(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating indicator");
            return Result.Failure<Core.Models.ValidationResult>(Error.Failure("Indicator.ValidationFailed", $"Validation failed: {ex.Message}"));
        }
    }

    public async Task<Result<Core.Models.IndicatorPerformanceMetrics>> GetIndicatorPerformanceAsync(
        long indicatorId,
        PerformanceMetricsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving performance metrics for indicator {IndicatorId}", indicatorId);

            var indicatorResult = await GetIndicatorByIdAsync(indicatorId, cancellationToken);
            if (!indicatorResult.IsSuccess)
            {
                return Result.Failure<Core.Models.IndicatorPerformanceMetrics>(Error.NotFound("Indicator", indicatorId));
            }

            // TODO: Implement actual performance metrics calculation
            var metrics = new Core.Models.IndicatorPerformanceMetrics
            {
                IndicatorId = indicatorId,
                IndicatorName = indicatorResult.Value.IndicatorName,
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow,
                Overall = new Core.Models.PerformanceMetrics
                {
                    AverageExecutionTime = TimeSpan.FromSeconds(1),
                    MinExecutionTime = TimeSpan.FromMilliseconds(500),
                    MaxExecutionTime = TimeSpan.FromSeconds(2),
                    AverageMemoryUsage = 1024 * 1024, // 1MB
                    AverageCpuUsage = 5.0,
                    TotalQueries = 100,
                    TotalQueryTime = TimeSpan.FromMinutes(5)
                }
            };

            return Result<Core.Models.IndicatorPerformanceMetrics>.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving performance metrics for indicator {IndicatorId}", indicatorId);
            return Result.Failure<Core.Models.IndicatorPerformanceMetrics>(Error.Failure("Indicator.PerformanceRetrievalFailed", $"Failed to retrieve performance metrics: {ex.Message}"));
        }
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
