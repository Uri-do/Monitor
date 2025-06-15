namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for collecting performance metrics
/// </summary>
public interface IPerformanceMetricsCollector
{
    /// <summary>
    /// Records a request metric
    /// </summary>
    void RecordRequest(string endpoint, string method, int statusCode, long durationMs, long requestSize = 0, long responseSize = 0);

    /// <summary>
    /// Records a database operation metric
    /// </summary>
    void RecordDatabaseOperation(string operation, long durationMs, bool success = true);

    /// <summary>
    /// Records a cache operation metric
    /// </summary>
    void RecordCacheOperation(string operation, long durationMs, bool hit = true);

    /// <summary>
    /// Records a custom metric
    /// </summary>
    void RecordCustomMetric(string name, double value, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Gets current performance metrics
    /// </summary>
    Task<PerformanceMetrics> GetMetricsAsync();

    /// <summary>
    /// Resets all metrics
    /// </summary>
    void ResetMetrics();
}

/// <summary>
/// Performance metrics data
/// </summary>
public class PerformanceMetrics
{
    /// <summary>
    /// When the metrics were collected
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Request metrics
    /// </summary>
    public RequestMetrics Requests { get; set; } = new();

    /// <summary>
    /// Database metrics
    /// </summary>
    public DatabaseMetrics Database { get; set; } = new();

    /// <summary>
    /// Cache metrics
    /// </summary>
    public CacheMetrics Cache { get; set; } = new();

    /// <summary>
    /// System metrics
    /// </summary>
    public SystemMetrics System { get; set; } = new();

    /// <summary>
    /// Custom metrics
    /// </summary>
    public Dictionary<string, double> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Request performance metrics
/// </summary>
public class RequestMetrics
{
    /// <summary>
    /// Total number of requests
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Requests per second
    /// </summary>
    public double RequestsPerSecond { get; set; }

    /// <summary>
    /// Error rate percentage
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Metrics by endpoint
    /// </summary>
    public Dictionary<string, EndpointMetrics> ByEndpoint { get; set; } = new();

    /// <summary>
    /// Metrics by status code
    /// </summary>
    public Dictionary<int, long> ByStatusCode { get; set; } = new();
}

/// <summary>
/// Database performance metrics
/// </summary>
public class DatabaseMetrics
{
    /// <summary>
    /// Total number of operations
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Average operation time in milliseconds
    /// </summary>
    public double AverageOperationTime { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Operations per second
    /// </summary>
    public double OperationsPerSecond { get; set; }

    /// <summary>
    /// Metrics by operation type
    /// </summary>
    public Dictionary<string, OperationMetrics> ByOperation { get; set; } = new();
}

/// <summary>
/// Cache performance metrics
/// </summary>
public class CacheMetrics
{
    /// <summary>
    /// Total number of operations
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Cache hit rate percentage
    /// </summary>
    public double HitRate { get; set; }

    /// <summary>
    /// Average operation time in milliseconds
    /// </summary>
    public double AverageOperationTime { get; set; }

    /// <summary>
    /// Operations per second
    /// </summary>
    public double OperationsPerSecond { get; set; }

    /// <summary>
    /// Metrics by cache type
    /// </summary>
    public Dictionary<string, CacheOperationMetrics> ByType { get; set; } = new();
}

/// <summary>
/// System performance metrics
/// </summary>
public class SystemMetrics
{
    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsage { get; set; }

    /// <summary>
    /// Memory usage in bytes
    /// </summary>
    public long MemoryUsage { get; set; }

    /// <summary>
    /// Working set in bytes
    /// </summary>
    public long WorkingSet { get; set; }

    /// <summary>
    /// Thread count
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    /// Handle count
    /// </summary>
    public int HandleCount { get; set; }

    /// <summary>
    /// Garbage collection metrics
    /// </summary>
    public Dictionary<int, long> GcCollections { get; set; } = new();
}

/// <summary>
/// Endpoint-specific metrics
/// </summary>
public class EndpointMetrics
{
    /// <summary>
    /// Total requests to this endpoint
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Error count
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Last request time
    /// </summary>
    public DateTime LastRequestTime { get; set; }
}

/// <summary>
/// Operation-specific metrics
/// </summary>
public class OperationMetrics
{
    /// <summary>
    /// Total operations
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Average operation time in milliseconds
    /// </summary>
    public double AverageOperationTime { get; set; }

    /// <summary>
    /// Success count
    /// </summary>
    public long SuccessCount { get; set; }

    /// <summary>
    /// Last operation time
    /// </summary>
    public DateTime LastOperationTime { get; set; }
}

/// <summary>
/// Cache operation-specific metrics
/// </summary>
public class CacheOperationMetrics
{
    /// <summary>
    /// Total operations
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Hit count
    /// </summary>
    public long HitCount { get; set; }

    /// <summary>
    /// Average operation time in milliseconds
    /// </summary>
    public double AverageOperationTime { get; set; }

    /// <summary>
    /// Last operation time
    /// </summary>
    public DateTime LastOperationTime { get; set; }
}
