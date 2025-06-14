namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for performance monitoring operations
/// </summary>
public interface IPerformanceMonitoringService
{
    /// <summary>
    /// Measures execution time of an async operation
    /// </summary>
    Task<T> MeasureAsync<T>(string operationName, Func<Task<T>> operation);

    /// <summary>
    /// Measures execution time of a synchronous operation
    /// </summary>
    T Measure<T>(string operationName, Func<T> operation);

    /// <summary>
    /// Records a custom metric
    /// </summary>
    Task RecordMetricAsync(string metricName, TimeSpan duration, bool success = true);

    /// <summary>
    /// Gets performance metrics for a specific operation
    /// </summary>
    PerformanceMetric? GetMetric(string metricName);

    /// <summary>
    /// Gets all performance metrics
    /// </summary>
    List<PerformanceMetric> GetAllMetrics();

    /// <summary>
    /// Gets system performance summary
    /// </summary>
    Task<SystemPerformanceSummary> GetSystemPerformanceSummaryAsync();

    /// <summary>
    /// Resets all performance counters
    /// </summary>
    void ResetCounters();

    /// <summary>
    /// Gets memory usage information
    /// </summary>
    MemoryUsageInfo GetMemoryUsage();

    /// <summary>
    /// Forces garbage collection and returns memory usage before and after
    /// </summary>
    MemoryCleanupResult ForceGarbageCollection();
}

/// <summary>
/// Performance metric data
/// </summary>
public class PerformanceMetric
{
    public string MetricName { get; set; } = string.Empty;
    public long TotalExecutions { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public double SuccessRate { get; set; }
    public DateTime LastExecution { get; set; }
}

/// <summary>
/// System performance summary
/// </summary>
public class SystemPerformanceSummary
{
    public DateTime Timestamp { get; set; }
    public int ProcessId { get; set; }
    public long WorkingSetMemory { get; set; }
    public long PrivateMemory { get; set; }
    public long VirtualMemory { get; set; }
    public TimeSpan ProcessorTime { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public long TotalOperations { get; set; }
    public long SuccessfulOperations { get; set; }
    public long FailedOperations { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public List<PerformanceMetric> TopSlowOperations { get; set; } = new();
}

/// <summary>
/// Memory usage information
/// </summary>
public class MemoryUsageInfo
{
    public long WorkingSet { get; set; }
    public long PrivateMemory { get; set; }
    public long VirtualMemory { get; set; }
    public long ManagedMemory { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
}

/// <summary>
/// Memory cleanup result
/// </summary>
public class MemoryCleanupResult
{
    public long MemoryBefore { get; set; }
    public long MemoryAfter { get; set; }
    public long MemoryFreed { get; set; }
    public int Gen0CollectionsTriggered { get; set; }
    public int Gen1CollectionsTriggered { get; set; }
    public int Gen2CollectionsTriggered { get; set; }
    public DateTime Timestamp { get; set; }
}
