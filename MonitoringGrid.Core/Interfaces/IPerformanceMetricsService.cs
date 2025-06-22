

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for performance metrics collection and analysis
/// </summary>
public interface IPerformanceMetricsService
{
    /// <summary>
    /// Records a performance metric
    /// </summary>
    Task RecordMetricAsync(string metricName, TimeSpan duration, bool success = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for a specific operation
    /// </summary>
    Task<string?> GetMetricAsync(string metricName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for a time range
    /// </summary>
    Task<IEnumerable<string>> GetMetricsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets aggregated performance statistics
    /// </summary>
    Task<string> GetStatisticsAsync(string metricName, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);
}
