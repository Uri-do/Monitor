using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Specialized repository interface for AlertLog with complex query support
/// </summary>
public interface IAlertRepository : IRepository<AlertLog>
{
    /// <summary>
    /// Get alerts with complex filtering and pagination
    /// </summary>
    Task<PaginatedAlerts<AlertLog>> GetAlertsWithFilteringAsync(AlertFilter filter);

    /// <summary>
    /// Get alert statistics for a given period
    /// </summary>
    Task<AlertStatistics> GetStatisticsAsync(int days = 30);

    /// <summary>
    /// Get alert dashboard data
    /// </summary>
    Task<AlertDashboard> GetDashboardAsync();

    /// <summary>
    /// Get alerts for a specific Indicator
    /// </summary>
    Task<IEnumerable<AlertLog>> GetAlertsByIndicatorAsync(long indicatorId, int days = 30);

    /// <summary>
    /// Get unresolved alerts count
    /// </summary>
    Task<int> GetUnresolvedCountAsync();

    /// <summary>
    /// Get alerts by severity level
    /// </summary>
    Task<IEnumerable<AlertLog>> GetAlertsBySeverityAsync(string severity, int days = 30);

    /// <summary>
    /// Bulk resolve alerts
    /// </summary>
    Task<int> BulkResolveAlertsAsync(IEnumerable<long> alertIds, string resolvedBy, string? resolutionNotes = null);
}
