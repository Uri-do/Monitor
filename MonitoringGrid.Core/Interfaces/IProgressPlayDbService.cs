using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service interface for ProgressPlayDB operations
/// Handles collector and statistics data retrieval
/// </summary>
public interface IProgressPlayDbService
{
    /// <summary>
    /// Get all available collectors from ProgressPlayDB.stats.tbl_Monitor_StatisticsCollectors
    /// </summary>
    Task<List<CollectorDto>> GetCollectorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get collector by ID
    /// </summary>
    Task<CollectorDto?> GetCollectorByIdAsync(long collectorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available item names for a specific collector from ProgressPlayDB.stats.tbl_Monitor_Statistics
    /// </summary>
    Task<List<string>> GetCollectorItemNamesAsync(long collectorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get statistics for a specific collector and item name
    /// </summary>
    Task<List<CollectorStatisticDto>> GetCollectorStatisticsAsync(long collectorId, string itemName,
        int lastDays = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a collector's stored procedure to get current data
    /// </summary>
    Task<List<CollectorStatisticDto>> ExecuteCollectorStoredProcedureAsync(long collectorId, int lastMinutes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get average value for a collector item over specified period
    /// </summary>
    Task<decimal?> GetCollectorItemAverageAsync(long collectorId, string itemName, string valueType,
        int? hour = null, DateTime? fromDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test connection to ProgressPlayDB
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}
