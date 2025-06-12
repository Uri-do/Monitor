using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service interface for managing monitor statistics and collectors
/// </summary>
public interface IMonitorStatisticsService
{
    /// <summary>
    /// Get all active statistics collectors
    /// </summary>
    Task<List<MonitorStatisticsCollector>> GetActiveCollectorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get collector by ID
    /// </summary>
    Task<MonitorStatisticsCollector?> GetCollectorByIdAsync(long collectorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get collector by CollectorID
    /// </summary>
    Task<MonitorStatisticsCollector?> GetCollectorByCollectorIdAsync(long collectorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all collectors with their statistics
    /// </summary>
    Task<List<MonitorStatisticsCollector>> GetCollectorsWithStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get statistics for a specific collector and time range
    /// </summary>
    Task<List<MonitorStatistics>> GetStatisticsAsync(long collectorId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get latest statistics for a collector
    /// </summary>
    Task<List<MonitorStatistics>> GetLatestStatisticsAsync(long collectorId, int hours = 24, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get distinct item names for a collector
    /// </summary>
    Task<List<string>> GetCollectorItemNamesAsync(long collectorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update collector last run information
    /// </summary>
    Task UpdateCollectorLastRunAsync(long collectorId, DateTime lastRun, string? result = null, CancellationToken cancellationToken = default);
}
