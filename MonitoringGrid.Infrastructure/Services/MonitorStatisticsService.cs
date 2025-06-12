using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service implementation for managing monitor statistics and collectors
/// </summary>
public class MonitorStatisticsService : IMonitorStatisticsService
{
    private readonly ProgressPlayContext _context;
    private readonly ILogger<MonitorStatisticsService> _logger;

    public MonitorStatisticsService(ProgressPlayContext context, ILogger<MonitorStatisticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<MonitorStatisticsCollector>> GetActiveCollectorsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving active statistics collectors");
        
        return await _context.MonitorStatisticsCollectors
            .Where(c => c.IsActive == true)
            .OrderBy(c => c.CollectorDesc)
            .ToListAsync(cancellationToken);
    }

    public async Task<MonitorStatisticsCollector?> GetCollectorByIdAsync(long collectorId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving collector by ID {CollectorId}", collectorId);
        
        return await _context.MonitorStatisticsCollectors
            .FirstOrDefaultAsync(c => c.ID == collectorId, cancellationToken);
    }

    public async Task<MonitorStatisticsCollector?> GetCollectorByCollectorIdAsync(long collectorId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving collector by CollectorID {CollectorId}", collectorId);
        
        return await _context.MonitorStatisticsCollectors
            .FirstOrDefaultAsync(c => c.CollectorID == collectorId, cancellationToken);
    }

    public async Task<List<MonitorStatisticsCollector>> GetCollectorsWithStatisticsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving collectors with statistics");
        
        return await _context.MonitorStatisticsCollectors
            .Include(c => c.Statistics.Take(100)) // Limit to recent statistics
            .OrderBy(c => c.CollectorDesc)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MonitorStatistics>> GetStatisticsAsync(long collectorId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving statistics for collector {CollectorId} from {FromDate} to {ToDate}", collectorId, fromDate, toDate);
        
        return await _context.MonitorStatistics
            .Where(s => s.CollectorID == collectorId && 
                       s.Day >= fromDate.Date && 
                       s.Day <= toDate.Date)
            .OrderBy(s => s.Day)
            .ThenBy(s => s.Hour)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MonitorStatistics>> GetLatestStatisticsAsync(long collectorId, int hours = 24, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving latest {Hours} hours of statistics for collector {CollectorId}", hours, collectorId);
        
        var fromDate = DateTime.Now.AddHours(-hours).Date;
        
        return await _context.MonitorStatistics
            .Where(s => s.CollectorID == collectorId && s.Day >= fromDate)
            .OrderByDescending(s => s.Day)
            .ThenByDescending(s => s.Hour)
            .Take(hours)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetCollectorItemNamesAsync(long collectorId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving item names for collector {CollectorId}", collectorId);
        
        return await _context.MonitorStatistics
            .Where(s => s.CollectorID == collectorId && !string.IsNullOrEmpty(s.ItemName))
            .Select(s => s.ItemName!)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateCollectorLastRunAsync(long collectorId, DateTime lastRun, string? result = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating last run for collector {CollectorId}", collectorId);
        
        var collector = await _context.MonitorStatisticsCollectors
            .FirstOrDefaultAsync(c => c.CollectorID == collectorId, cancellationToken);
            
        if (collector != null)
        {
            collector.LastRun = lastRun;
            collector.LastRunResult = result;
            collector.UpdatedDate = DateTime.UtcNow;
            
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Updated last run for collector {CollectorId}", collectorId);
        }
        else
        {
            _logger.LogWarning("Collector {CollectorId} not found for last run update", collectorId);
        }
    }
}
