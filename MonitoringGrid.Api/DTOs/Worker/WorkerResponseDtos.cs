namespace MonitoringGrid.Api.DTOs.Worker;

/// <summary>
/// Standardized response for worker operations
/// </summary>
public class WorkerOperationResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Operation result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Process ID involved in the operation (if applicable)
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// Operation timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Operation duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Additional operation details
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }

    /// <summary>
    /// Error code if operation failed
    /// </summary>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Enhanced worker status response
/// </summary>
public class WorkerStatusResponse
{
    /// <summary>
    /// Whether any worker is currently running
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Worker mode (Manual, Integrated)
    /// </summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>
    /// Primary process ID (if running)
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// Worker start time
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Worker uptime
    /// </summary>
    public TimeSpan? Uptime { get; set; }

    /// <summary>
    /// List of worker services
    /// </summary>
    public List<WorkerServiceInfo> Services { get; set; } = new();

    /// <summary>
    /// Performance metrics (if requested)
    /// </summary>
    public WorkerMetrics? Metrics { get; set; }

    /// <summary>
    /// Recent execution history (if requested)
    /// </summary>
    public List<ExecutionHistoryItem>? History { get; set; }

    /// <summary>
    /// Status check timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Worker service information
/// </summary>
public class WorkerServiceInfo
{
    /// <summary>
    /// Service name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Service status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime? LastActivity { get; set; }

    /// <summary>
    /// Service-specific details
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Worker performance metrics
/// </summary>
public class WorkerMetrics
{
    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double? CpuUsage { get; set; }

    /// <summary>
    /// Memory usage in MB
    /// </summary>
    public long? MemoryUsageMB { get; set; }

    /// <summary>
    /// Number of threads
    /// </summary>
    public int? ThreadCount { get; set; }

    /// <summary>
    /// Total indicators processed
    /// </summary>
    public long? TotalIndicatorsProcessed { get; set; }

    /// <summary>
    /// Successful executions
    /// </summary>
    public long? SuccessfulExecutions { get; set; }

    /// <summary>
    /// Failed executions
    /// </summary>
    public long? FailedExecutions { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double? AverageExecutionTimeMs { get; set; }
}

/// <summary>
/// Execution history item
/// </summary>
public class ExecutionHistoryItem
{
    /// <summary>
    /// Indicator ID
    /// </summary>
    public long IndicatorId { get; set; }

    /// <summary>
    /// Indicator name
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Execution timestamp
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Execution duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution context
    /// </summary>
    public string Context { get; set; } = string.Empty;
}

/// <summary>
/// Response for indicator execution operations
/// </summary>
public class IndicatorExecutionResponse
{
    /// <summary>
    /// Indicator ID
    /// </summary>
    public long IndicatorId { get; set; }

    /// <summary>
    /// Indicator name
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Execution duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Current value from execution
    /// </summary>
    public object? CurrentValue { get; set; }

    /// <summary>
    /// Whether threshold was breached
    /// </summary>
    public bool? ThresholdBreached { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution context
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Execution timestamp
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response for bulk indicator execution
/// </summary>
public class BulkIndicatorExecutionResponse
{
    /// <summary>
    /// Total number of indicators processed
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Number of successful executions
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Total execution duration in milliseconds
    /// </summary>
    public long TotalDurationMs { get; set; }

    /// <summary>
    /// Individual execution results
    /// </summary>
    public List<IndicatorExecutionResponse> Results { get; set; } = new();

    /// <summary>
    /// Execution context
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Execution timestamp
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response for cleanup operations
/// </summary>
public class CleanupResponse
{
    /// <summary>
    /// Whether cleanup was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of processes cleaned up
    /// </summary>
    public int ProcessesCleanedUp { get; set; }

    /// <summary>
    /// Number of files cleaned up (if applicable)
    /// </summary>
    public int? FilesCleanedUp { get; set; }

    /// <summary>
    /// Cleanup duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Cleanup details
    /// </summary>
    public List<string> Details { get; set; } = new();

    /// <summary>
    /// Cleanup timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
