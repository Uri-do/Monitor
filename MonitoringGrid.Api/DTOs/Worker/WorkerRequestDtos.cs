using System.ComponentModel.DataAnnotations;
using MonitoringGrid.Api.DTOs.Common;
using MonitoringGrid.Api.Validation;

namespace MonitoringGrid.Api.DTOs.Worker;

/// <summary>
/// Request DTO for starting worker service
/// </summary>
public class StartWorkerRequest
{
    /// <summary>
    /// Optional timeout for worker startup in milliseconds
    /// </summary>
    [Timeout(1000, 60000)] // 1 second to 1 minute
    public int? TimeoutMs { get; set; }

    /// <summary>
    /// Force start even if workers are already running
    /// </summary>
    [BooleanFlag]
    public bool Force { get; set; } = false;

    /// <summary>
    /// Additional startup arguments
    /// </summary>
    [SearchTerm(0, 500)]
    public string? Arguments { get; set; }
}

/// <summary>
/// Request DTO for stopping worker service
/// </summary>
public class StopWorkerRequest
{
    /// <summary>
    /// Optional timeout for worker shutdown in milliseconds
    /// </summary>
    [Timeout(1000, 30000)] // 1 second to 30 seconds
    public int? TimeoutMs { get; set; }

    /// <summary>
    /// Force stop (kill) workers if graceful shutdown fails
    /// </summary>
    [BooleanFlag]
    public bool Force { get; set; } = false;

    /// <summary>
    /// Stop all worker processes, not just tracked ones
    /// </summary>
    [BooleanFlag]
    public bool StopAll { get; set; } = true;
}

/// <summary>
/// Request DTO for restarting worker service
/// </summary>
public class RestartWorkerRequest
{
    /// <summary>
    /// Delay between stop and start in milliseconds
    /// </summary>
    [Timeout(500, 10000)] // 0.5 seconds to 10 seconds
    public int DelayMs { get; set; } = 2000;

    /// <summary>
    /// Force restart even if stop fails
    /// </summary>
    [BooleanFlag]
    public bool Force { get; set; } = false;

    /// <summary>
    /// Startup timeout in milliseconds
    /// </summary>
    [Timeout(1000, 60000)]
    public int? StartupTimeoutMs { get; set; }
}



/// <summary>
/// Request DTO for executing due indicators
/// </summary>
public class ExecuteDueIndicatorsRequest
{
    /// <summary>
    /// Execution context
    /// </summary>
    [ExecutionContext]
    public string Context { get; set; } = "Manual";

    /// <summary>
    /// Maximum number of indicators to execute
    /// </summary>
    [PositiveInteger]
    public int? MaxCount { get; set; }

    /// <summary>
    /// Execution timeout per indicator in milliseconds
    /// </summary>
    [Timeout(1000, 300000)]
    public int? TimeoutMs { get; set; }

    /// <summary>
    /// Whether to continue execution if one indicator fails
    /// </summary>
    [BooleanFlag]
    public bool ContinueOnError { get; set; } = true;
}

/// <summary>
/// Request DTO for activating indicators
/// </summary>
public class ActivateIndicatorsRequest
{
    /// <summary>
    /// Specific indicator IDs to activate (if null, activates all)
    /// </summary>
    public List<long>? IndicatorIds { get; set; }

    /// <summary>
    /// Reset last run time to make indicators due for execution
    /// </summary>
    [BooleanFlag]
    public bool ResetLastRun { get; set; } = true;

    /// <summary>
    /// Reset currently running status
    /// </summary>
    [BooleanFlag]
    public bool ResetRunningStatus { get; set; } = true;
}

/// <summary>
/// Request DTO for assigning schedulers
/// </summary>
public class AssignSchedulersRequest
{
    /// <summary>
    /// Specific indicator IDs to assign schedulers to (if null, assigns to all without schedulers)
    /// </summary>
    public List<long>? IndicatorIds { get; set; }

    /// <summary>
    /// Scheduler ID to assign (if null, creates/uses default scheduler)
    /// </summary>
    [PositiveInteger]
    public long? SchedulerId { get; set; }

    /// <summary>
    /// Force assignment even if indicators already have schedulers
    /// </summary>
    [BooleanFlag]
    public bool Force { get; set; } = false;
}

/// <summary>
/// Request DTO for getting worker status
/// </summary>
public class GetWorkerStatusRequest
{
    /// <summary>
    /// Include detailed service information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Include process performance metrics
    /// </summary>
    [BooleanFlag]
    public bool IncludeMetrics { get; set; } = false;

    /// <summary>
    /// Include recent execution history
    /// </summary>
    [BooleanFlag]
    public bool IncludeHistory { get; set; } = false;
}

/// <summary>
/// Request DTO for cleanup operations
/// </summary>
public class CleanupRequest
{
    /// <summary>
    /// Force cleanup even if processes are responding
    /// </summary>
    [BooleanFlag]
    public bool Force { get; set; } = false;

    /// <summary>
    /// Cleanup timeout in milliseconds
    /// </summary>
    [Timeout(1000, 30000)]
    public int TimeoutMs { get; set; } = 10000;

    /// <summary>
    /// Include cleanup of temporary files
    /// </summary>
    [BooleanFlag]
    public bool CleanupFiles { get; set; } = false;
}
