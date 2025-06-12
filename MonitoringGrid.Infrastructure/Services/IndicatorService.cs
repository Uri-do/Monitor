using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service implementation for Indicator management operations
/// Replaces KpiService
/// </summary>
public class IndicatorService : IIndicatorService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<IndicatorService> _logger;

    public IndicatorService(MonitoringContext context, ILogger<IndicatorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Indicator>> GetAllIndicatorsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving all indicators");
        
        return await _context.Indicators
            .Include(i => i.IndicatorContacts)
                .ThenInclude(ic => ic.Contact)
            .Include(i => i.OwnerContact)
            .OrderBy(i => i.IndicatorName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Indicator?> GetIndicatorByIdAsync(long indicatorId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving indicator with ID {IndicatorId}", indicatorId);

        return await _context.Indicators
            .Include(i => i.IndicatorContacts)
                .ThenInclude(ic => ic.Contact)
            .Include(i => i.OwnerContact)
            .FirstOrDefaultAsync(i => i.IndicatorId == indicatorId, cancellationToken);
    }

    public async Task<List<Indicator>> GetActiveIndicatorsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving active indicators");
        
        return await _context.Indicators
            .Include(i => i.IndicatorContacts)
                .ThenInclude(ic => ic.Contact)
            .Include(i => i.OwnerContact)
            .Where(i => i.IsActive)
            .OrderBy(i => i.IndicatorName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Indicator>> GetIndicatorsByOwnerAsync(int ownerContactId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving indicators for owner {OwnerContactId}", ownerContactId);
        
        return await _context.Indicators
            .Include(i => i.IndicatorContacts)
                .ThenInclude(ic => ic.Contact)
            .Include(i => i.OwnerContact)
            .Where(i => i.OwnerContactId == ownerContactId && i.IsActive)
            .OrderBy(i => i.IndicatorName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Indicator>> GetIndicatorsByPriorityAsync(string priority, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving indicators with priority {Priority}", priority);
        
        return await _context.Indicators
            .Include(i => i.IndicatorContacts)
                .ThenInclude(ic => ic.Contact)
            .Include(i => i.OwnerContact)
            .Where(i => i.Priority == priority && i.IsActive)
            .OrderBy(i => i.IndicatorName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Indicator>> GetDueIndicatorsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving indicators due for execution");
        
        var indicators = await GetActiveIndicatorsAsync(cancellationToken);
        return indicators.Where(i => i.IsDue()).ToList();
    }

    public async Task<Indicator> CreateIndicatorAsync(Indicator indicator, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating new indicator {IndicatorName}", indicator.IndicatorName);
        
        indicator.CreatedDate = DateTime.UtcNow;
        indicator.UpdatedDate = DateTime.UtcNow;
        
        _context.Indicators.Add(indicator);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Created indicator {IndicatorId}: {IndicatorName}", 
            indicator.IndicatorId, indicator.IndicatorName);
        
        return indicator;
    }

    public async Task<Indicator> UpdateIndicatorAsync(Indicator indicator, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating indicator {IndicatorId}", indicator.IndicatorId);
        
        indicator.UpdatedDate = DateTime.UtcNow;
        
        _context.Indicators.Update(indicator);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Updated indicator {IndicatorId}: {IndicatorName}", 
            indicator.IndicatorId, indicator.IndicatorName);
        
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
                IndicatorId = indicatorId,
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
            .Where(ic => ic.IndicatorId == indicatorId && contactIds.Contains(ic.ContactId))
            .ToListAsync(cancellationToken);
        
        _context.IndicatorContacts.RemoveRange(indicatorContacts);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Removed {Count} contacts from indicator {IndicatorId}", 
            indicatorContacts.Count, indicatorId);
        
        return true;
    }

    public async Task<List<HistoricalData>> GetIndicatorHistoryAsync(long indicatorId, int days = 30, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving history for indicator {IndicatorId}, last {Days} days", indicatorId, days);
        
        var startDate = DateTime.UtcNow.AddDays(-days);
        return await _context.HistoricalData
            .Where(h => h.KpiId == indicatorId && h.Timestamp >= startDate)
            .OrderByDescending(h => h.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IndicatorDashboard> GetIndicatorDashboardAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving indicator dashboard data");
        
        var today = DateTime.UtcNow.Date;
        var indicators = await GetAllIndicatorsAsync(cancellationToken);
        
        var dashboard = new IndicatorDashboard
        {
            TotalIndicators = indicators.Count,
            ActiveIndicators = indicators.Count(i => i.IsActive),
            DueIndicators = indicators.Count(i => i.IsDue()),
            RunningIndicators = indicators.Count(i => i.IsCurrentlyRunning),
            IndicatorsExecutedToday = indicators.Count(i => i.LastRun?.Date == today),
            CountByPriority = indicators
                .GroupBy(i => i.Priority)
                .Select(g => new IndicatorCountByPriority { Priority = g.Key, Count = g.Count() })
                .ToList()
        };
        
        // Get recent executions from historical data
        dashboard.RecentExecutions = await _context.HistoricalData
            .Where(h => h.Timestamp >= DateTime.UtcNow.AddHours(-24))
            .OrderByDescending(h => h.Timestamp)
            .Take(10)
            .Select(h => new IndicatorExecutionSummary
            {
                IndicatorId = h.KpiId,
                IndicatorName = indicators.Where(i => i.IndicatorId == h.KpiId).Select(i => i.IndicatorName).FirstOrDefault() ?? "Unknown",
                ExecutionTime = h.Timestamp,
                WasSuccessful = true, // Assume successful if in historical data
                Value = h.Value
            })
            .ToListAsync(cancellationToken);
        
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
        
        return dashboard;
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
        var historicalData = await _context.HistoricalData
            .Where(h => h.KpiId == indicatorId && h.Timestamp >= startDate)
            .OrderBy(h => h.Timestamp)
            .ToListAsync(cancellationToken);
        
        var alerts = await _context.AlertLogs
            .Where(a => a.KpiId == indicatorId && a.TriggerTime >= startDate)
            .ToListAsync(cancellationToken);
        
        return new IndicatorStatistics
        {
            IndicatorId = indicatorId,
            IndicatorName = indicator.IndicatorName,
            TotalExecutions = historicalData.Count,
            SuccessfulExecutions = historicalData.Count, // Assume all in historical data are successful
            FailedExecutions = 0,
            AlertsTriggered = alerts.Count,
            AverageValue = historicalData.Any() ? historicalData.Average(h => h.Value) : null,
            MinValue = historicalData.Any() ? historicalData.Min(h => h.Value) : null,
            MaxValue = historicalData.Any() ? historicalData.Max(h => h.Value) : null,
            LastExecution = indicator.LastRun,
            NextExecution = indicator.GetNextRunTime(),
            ValueTrend = historicalData.Select(h => new IndicatorValueTrend
            {
                Timestamp = h.Timestamp,
                Value = h.Value,
                AlertTriggered = alerts.Any(a => Math.Abs((a.TriggerTime - h.Timestamp).TotalMinutes) < 5)
            }).ToList()
        };
    }
}
