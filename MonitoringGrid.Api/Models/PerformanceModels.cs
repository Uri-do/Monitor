namespace MonitoringGrid.Api.Models;

/// <summary>
/// Performance metrics for tracking endpoint performance
/// </summary>
public class PerformanceMetrics
{
    public long TotalRequests { get; set; }
    public long TotalDuration { get; set; }
    public long TotalRequestSize { get; set; }
    public long TotalResponseSize { get; set; }
    public long ErrorCount { get; set; }
    public DateTime FirstRequestTime { get; set; } = DateTime.UtcNow;
    public DateTime LastRequestTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database operation metrics
/// </summary>
public class DatabaseMetrics
{
    public long TotalOperations { get; set; }
    public long TotalDuration { get; set; }
    public long SuccessCount { get; set; }
    public DateTime FirstOperationTime { get; set; } = DateTime.UtcNow;
    public DateTime LastOperationTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache operation metrics
/// </summary>
public class ApiCacheMetrics
{
    public long TotalOperations { get; set; }
    public long TotalDuration { get; set; }
    public long HitCount { get; set; }
    public DateTime FirstOperationTime { get; set; } = DateTime.UtcNow;
    public DateTime LastOperationTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// System-level performance metrics
/// </summary>
public class SystemMetrics
{
    public long MemoryUsage { get; set; }
    public long WorkingSet { get; set; }
    public double CpuUsage { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public Dictionary<int, long> GcCollections { get; set; } = new();
}

/// <summary>
/// Performance snapshot at a point in time
/// </summary>
public class PerformanceSnapshot
{
    public DateTime Timestamp { get; set; }
    public SystemMetrics SystemMetrics { get; set; } = new();
    public Dictionary<string, EndpointMetricsSnapshot> EndpointMetrics { get; set; } = new();
    public Dictionary<string, DatabaseMetricsSnapshot> DatabaseMetrics { get; set; } = new();
    public Dictionary<string, CacheMetricsSnapshot> CacheMetrics { get; set; } = new();
}

/// <summary>
/// Endpoint metrics snapshot
/// </summary>
public class EndpointMetricsSnapshot
{
    public long TotalRequests { get; set; }
    public double AverageResponseTime { get; set; }
    public double ErrorRate { get; set; }
    public double RequestsPerSecond { get; set; }
    public DateTime LastRequestTime { get; set; }
}

/// <summary>
/// Database metrics snapshot
/// </summary>
public class DatabaseMetricsSnapshot
{
    public long TotalOperations { get; set; }
    public double AverageResponseTime { get; set; }
    public double SuccessRate { get; set; }
    public DateTime LastOperationTime { get; set; }
}

/// <summary>
/// Cache metrics snapshot
/// </summary>
public class ApiCacheMetricsSnapshot
{
    public long TotalOperations { get; set; }
    public double HitRate { get; set; }
    public double AverageResponseTime { get; set; }
    public DateTime LastOperationTime { get; set; }
}

/// <summary>
/// Performance trends over time
/// </summary>
public class PerformanceTrends
{
    public TimeSpan Period { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<RequestTrendPoint> RequestTrends { get; set; } = new();
    public List<ResponseTimeTrendPoint> ResponseTimeTrends { get; set; } = new();
    public List<ErrorRateTrendPoint> ErrorRateTrends { get; set; } = new();
}

/// <summary>
/// Request trend data point
/// </summary>
public class RequestTrendPoint
{
    public DateTime Timestamp { get; set; }
    public long RequestCount { get; set; }
    public double RequestsPerSecond { get; set; }
}

/// <summary>
/// Response time trend data point
/// </summary>
public class ResponseTimeTrendPoint
{
    public DateTime Timestamp { get; set; }
    public double AverageResponseTime { get; set; }
    public double P95ResponseTime { get; set; }
    public double P99ResponseTime { get; set; }
}

/// <summary>
/// Error rate trend data point
/// </summary>
public class ErrorRateTrendPoint
{
    public DateTime Timestamp { get; set; }
    public double ErrorRate { get; set; }
    public long ErrorCount { get; set; }
}

/// <summary>
/// Performance alert configuration
/// </summary>
public class PerformanceAlert
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PerformanceMetricType MetricType { get; set; }
    public double Threshold { get; set; }
    public TimeSpan EvaluationWindow { get; set; }
    public AlertSeverity Severity { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<string> NotificationChannels { get; set; } = new();
}

/// <summary>
/// Performance metric types for alerting
/// </summary>
public enum PerformanceMetricType
{
    ResponseTime,
    ErrorRate,
    RequestsPerSecond,
    CpuUsage,
    MemoryUsage,
    DatabaseResponseTime,
    CacheHitRate
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Performance benchmark results
/// </summary>
public class PerformanceBenchmark
{
    public string Name { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, BenchmarkResult> Results { get; set; } = new();
}

/// <summary>
/// Individual benchmark result
/// </summary>
public class BenchmarkResult
{
    public string Operation { get; set; } = string.Empty;
    public long IterationCount { get; set; }
    public double AverageTime { get; set; }
    public double MinTime { get; set; }
    public double MaxTime { get; set; }
    public double StandardDeviation { get; set; }
    public double OperationsPerSecond { get; set; }
}

/// <summary>
/// Performance optimization recommendation
/// </summary>
public class PerformanceRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecommendationType Type { get; set; }
    public RecommendationPriority Priority { get; set; }
    public string Component { get; set; } = string.Empty;
    public double EstimatedImpact { get; set; }
    public List<string> ActionItems { get; set; } = new();
}

/// <summary>
/// Performance recommendation types
/// </summary>
public enum RecommendationType
{
    Caching,
    DatabaseOptimization,
    CodeOptimization,
    Infrastructure,
    Configuration
}

/// <summary>
/// Recommendation priority levels
/// </summary>
public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Performance health status
/// </summary>
public class PerformanceHealth
{
    public PerformanceHealthStatus Status { get; set; }
    public DateTime CheckedAt { get; set; }
    public List<PerformanceHealthCheck> Checks { get; set; } = new();
    public double OverallScore { get; set; }
    public List<PerformanceRecommendation> Recommendations { get; set; } = new();
}

/// <summary>
/// Performance health status levels
/// </summary>
public enum PerformanceHealthStatus
{
    Excellent,
    Good,
    Fair,
    Poor,
    Critical
}

/// <summary>
/// Individual performance health check
/// </summary>
public class PerformanceHealthCheck
{
    public string Name { get; set; } = string.Empty;
    public PerformanceHealthStatus Status { get; set; }
    public double Score { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Cache metrics snapshot (alias for compatibility)
/// </summary>
public class CacheMetricsSnapshot : ApiCacheMetricsSnapshot
{
}
