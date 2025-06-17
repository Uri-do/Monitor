using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.DTOs.Common;

/// <summary>
/// Query performance metrics - shared across all API responses
/// </summary>
public class QueryMetrics
{
    /// <summary>
    /// Query execution time in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Number of database queries executed
    /// </summary>
    public int QueryCount { get; set; }

    /// <summary>
    /// Cache hit information
    /// </summary>
    public bool CacheHit { get; set; }

    /// <summary>
    /// Query timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Shared request DTO for executing indicators - used across Worker and Indicator controllers
/// </summary>
public class ExecuteIndicatorRequest
{
    /// <summary>
    /// Indicator ID to execute
    /// </summary>
    [Required]
    public long IndicatorId { get; set; }

    /// <summary>
    /// Execution context
    /// </summary>
    public string? ExecutionContext { get; set; } = "Manual";

    /// <summary>
    /// Whether to save execution results
    /// </summary>
    public bool SaveResults { get; set; } = true;

    /// <summary>
    /// Force execution even if indicator is currently running
    /// </summary>
    public bool ForceExecution { get; set; } = false;

    /// <summary>
    /// Override the default timeout for this execution
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Whether to send alerts if thresholds are met
    /// </summary>
    public bool SendAlerts { get; set; } = true;

    /// <summary>
    /// Whether to return detailed execution results
    /// </summary>
    public bool IncludeDetailedResults { get; set; } = false;

    // Legacy property for WorkerController compatibility
    /// <summary>
    /// Execution context (legacy property for WorkerController)
    /// </summary>
    public string Context
    {
        get => ExecutionContext ?? "Manual";
        set => ExecutionContext = value;
    }
}
