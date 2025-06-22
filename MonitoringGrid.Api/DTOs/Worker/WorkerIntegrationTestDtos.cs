using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.DTOs.Worker;

/// <summary>
/// Request to start a worker integration test
/// </summary>
public class StartWorkerTestRequest
{
    /// <summary>
    /// Type of test to run (indicator-execution, worker-lifecycle, real-time-monitoring, stress-test)
    /// </summary>
    [Required]
    public string TestType { get; set; } = string.Empty;

    /// <summary>
    /// Specific indicator IDs to test (optional, if empty will test all active indicators)
    /// </summary>
    public List<long>? IndicatorIds { get; set; }

    /// <summary>
    /// Test duration in seconds (for stress tests)
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Number of concurrent workers (for stress tests)
    /// </summary>
    public int? ConcurrentWorkers { get; set; }

    /// <summary>
    /// Additional test parameters
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }
}

/// <summary>
/// Response when starting a worker integration test
/// </summary>
public class WorkerTestExecutionResponse
{
    /// <summary>
    /// Unique test execution ID
    /// </summary>
    public string TestId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the test
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Test start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Test end time (if completed)
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Test duration in seconds
    /// </summary>
    public double? DurationSeconds => EndTime?.Subtract(StartTime).TotalSeconds;
}

/// <summary>
/// Status of the worker integration test system
/// </summary>
public class WorkerIntegrationTestStatus
{
    /// <summary>
    /// Whether any tests are currently running
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Number of active test executions
    /// </summary>
    public int ActiveTests { get; set; }

    /// <summary>
    /// Total number of indicators available for testing
    /// </summary>
    public int TotalIndicators { get; set; }

    /// <summary>
    /// Available indicators for testing
    /// </summary>
    public List<IndicatorSummary> AvailableIndicators { get; set; } = new();

    /// <summary>
    /// Recent test executions
    /// </summary>
    public List<WorkerTestExecution> RecentExecutions { get; set; } = new();

    /// <summary>
    /// Available test types
    /// </summary>
    public List<string> AvailableTestTypes { get; set; } = new()
    {
        "indicator-execution",
        "worker-lifecycle",
        "real-time-monitoring",
        "stress-test",
        "worker-process-management"
    };
}

/// <summary>
/// Request to start a worker process
/// </summary>
public class StartWorkerProcessRequest
{
    /// <summary>
    /// Optional worker ID (generated if not provided)
    /// </summary>
    public string? WorkerId { get; set; }

    /// <summary>
    /// Type of test to run
    /// </summary>
    public string TestType { get; set; } = "indicator-execution";

    /// <summary>
    /// Whether to run in test mode
    /// </summary>
    public bool TestMode { get; set; } = true;

    /// <summary>
    /// Duration to run in seconds (null = indefinite)
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Specific indicator IDs to process
    /// </summary>
    public List<int>? IndicatorIds { get; set; }
}

/// <summary>
/// Information about a worker process
/// </summary>
public class WorkerProcessInfo
{
    /// <summary>
    /// Worker identifier
    /// </summary>
    public string WorkerId { get; set; } = string.Empty;

    /// <summary>
    /// Process ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// When the process started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Path to status file
    /// </summary>
    public string StatusFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Type of test being run
    /// </summary>
    public string TestType { get; set; } = string.Empty;

    /// <summary>
    /// Indicator IDs being processed
    /// </summary>
    public List<int> IndicatorIds { get; set; } = new();

    /// <summary>
    /// Duration in seconds (null = indefinite)
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Whether running in test mode
    /// </summary>
    public bool IsTestMode { get; set; }

    /// <summary>
    /// The actual process (not serialized)
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public System.Diagnostics.Process Process { get; set; } = null!;
}

/// <summary>
/// Status of a worker process
/// </summary>
public class WorkerProcessStatus
{
    /// <summary>
    /// Worker identifier
    /// </summary>
    public string WorkerId { get; set; } = string.Empty;

    /// <summary>
    /// Process ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Whether the process is running
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Current state
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// When the process started
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Last status update
    /// </summary>
    public DateTime LastUpdate { get; set; }

    /// <summary>
    /// Current status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Current activity
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
    public double? TimeRemainingSeconds { get; set; }
}

/// <summary>
/// Summary information about an indicator
/// </summary>
public class IndicatorSummary
{
    /// <summary>
    /// Indicator ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Indicator name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Indicator description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Priority level
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Last run time
    /// </summary>
    public DateTime? LastRun { get; set; }
}

/// <summary>
/// Detailed information about a worker test execution
/// </summary>
public class WorkerTestExecution
{
    /// <summary>
    /// Unique test execution ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Type of test being executed
    /// </summary>
    public string TestType { get; set; } = string.Empty;

    /// <summary>
    /// Indicator IDs being tested
    /// </summary>
    public List<long> IndicatorIds { get; set; } = new();

    /// <summary>
    /// Test start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Test end time
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Whether the test is currently running
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Current status message
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Last update time
    /// </summary>
    public DateTime? LastUpdate { get; set; }

    /// <summary>
    /// Whether the test completed successfully
    /// </summary>
    public bool? Success { get; set; }

    /// <summary>
    /// Error message if test failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Test results and metrics
    /// </summary>
    public WorkerTestResults? Results { get; set; }

    /// <summary>
    /// Cancellation token source for stopping the test
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public CancellationTokenSource CancellationTokenSource { get; set; } = new();

    /// <summary>
    /// Test duration in seconds
    /// </summary>
    public double? DurationSeconds => EndTime?.Subtract(StartTime).TotalSeconds;
}

/// <summary>
/// Results and metrics from a worker test execution
/// </summary>
public class WorkerTestResults
{
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
    /// Average execution time in milliseconds
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Total execution time in milliseconds
    /// </summary>
    public double TotalExecutionTimeMs { get; set; }

    /// <summary>
    /// Memory usage during test
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// CPU usage percentage during test
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Number of alerts triggered during test
    /// </summary>
    public int AlertsTriggered { get; set; }

    /// <summary>
    /// Detailed execution results for each indicator
    /// </summary>
    public List<IndicatorExecutionResult> IndicatorResults { get; set; } = new();

    /// <summary>
    /// Performance metrics collected during test
    /// </summary>
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
}

/// <summary>
/// Result of executing a specific indicator during testing
/// </summary>
public class IndicatorExecutionResult
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
    /// Execution time in milliseconds
    /// </summary>
    public double ExecutionTimeMs { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of records processed
    /// </summary>
    public int RecordsProcessed { get; set; }

    /// <summary>
    /// Whether alerts were triggered
    /// </summary>
    public bool AlertsTriggered { get; set; }

    /// <summary>
    /// Execution start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Execution end time
    /// </summary>
    public DateTime EndTime { get; set; }
}
