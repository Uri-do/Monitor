namespace MonitoringGrid.StandaloneWorker.Models;

/// <summary>
/// Configuration for standalone worker process
/// </summary>
public class StandaloneWorkerConfig
{
    /// <summary>
    /// Unique identifier for this worker process
    /// </summary>
    public string WorkerId { get; set; } = string.Empty;

    /// <summary>
    /// Whether to run in test mode with enhanced logging
    /// </summary>
    public bool TestMode { get; set; }

    /// <summary>
    /// Duration to run in seconds (0 = indefinite)
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Specific indicator IDs to process (empty = all)
    /// </summary>
    public List<int> IndicatorIds { get; set; } = new();

    /// <summary>
    /// Path to status file for IPC communication
    /// </summary>
    public string? StatusFilePath { get; set; }

    /// <summary>
    /// Base URL of the API for SignalR communication
    /// </summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:57653";

    /// <summary>
    /// Interval between indicator processing cycles (seconds)
    /// </summary>
    public int ProcessingIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of indicators to process in parallel
    /// </summary>
    public int MaxParallelIndicators { get; set; } = 3;

    /// <summary>
    /// Timeout for individual indicator execution (seconds)
    /// </summary>
    public int ExecutionTimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Worker process state
/// </summary>
public enum WorkerState
{
    Starting,
    Running,
    Processing,
    Idle,
    Stopping,
    Stopped,
    Failed
}

/// <summary>
/// Worker status for IPC communication
/// </summary>
public class WorkerStatus
{
    /// <summary>
    /// Worker identifier
    /// </summary>
    public string WorkerId { get; set; } = string.Empty;

    /// <summary>
    /// Current worker state
    /// </summary>
    public WorkerState State { get; set; }

    /// <summary>
    /// When the worker started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Current activity description
    /// </summary>
    public string? CurrentActivity { get; set; }

    /// <summary>
    /// Number of indicators processed
    /// </summary>
    public int IndicatorsProcessed { get; set; }

    /// <summary>
    /// Number of successful executions
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Process ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Error details if failed
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Whether the worker is healthy
    /// </summary>
    public bool IsHealthy { get; set; } = true;

    /// <summary>
    /// Time remaining if duration is set
    /// </summary>
    public TimeSpan? TimeRemaining { get; set; }
}
