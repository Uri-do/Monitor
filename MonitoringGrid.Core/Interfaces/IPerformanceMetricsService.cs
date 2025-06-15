using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service for collecting and managing performance metrics
/// </summary>
public interface IPerformanceMetricsService
{
    /// <summary>
    /// Get current performance metrics
    /// </summary>
    Task<Result<PerformanceMetrics>> GetCurrentMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get performance metrics for a specific time range
    /// </summary>
    Task<Result<PerformanceMetrics>> GetMetricsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a performance metric
    /// </summary>
    Task<Result> RecordMetricAsync(string metricName, double value, Dictionary<string, string>? tags = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get database performance metrics
    /// </summary>
    Task<Result<DatabaseMetrics>> GetDatabaseMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache performance metrics
    /// </summary>
    Task<Result<CacheMetrics>> GetCacheMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get system performance metrics
    /// </summary>
    Task<Result<SystemMetrics>> GetSystemMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Increment a counter metric
    /// </summary>
    void IncrementCounter(string counterName, string? category = null);

    /// <summary>
    /// Record duration metric for performance tracking
    /// </summary>
    void RecordDuration(string operationName, TimeSpan duration);
}

// Note: PerformanceMetrics, DatabaseMetrics, CacheMetrics, and SystemMetrics
// are already defined in IPerformanceMetricsCollector.cs
