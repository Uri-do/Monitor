using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.DTOs.ExecutionHistory;

/// <summary>
/// Execution history data transfer object
/// </summary>
public class ExecutionHistoryDto
{
    /// <summary>
    /// Historical record ID
    /// </summary>
    public int HistoricalId { get; set; }

    /// <summary>
    /// KPI/Indicator ID
    /// </summary>
    public long KpiId { get; set; }

    /// <summary>
    /// Indicator name
    /// </summary>
    public string Indicator { get; set; } = string.Empty;

    /// <summary>
    /// KPI owner name
    /// </summary>
    public string KpiOwner { get; set; } = string.Empty;

    /// <summary>
    /// Stored procedure name
    /// </summary>
    public string SpName { get; set; } = string.Empty;

    /// <summary>
    /// Execution timestamp
    /// </summary>
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// User who executed the indicator
    /// </summary>
    public string? ExecutedBy { get; set; }

    /// <summary>
    /// Execution method (Manual, Scheduled, etc.)
    /// </summary>
    public string? ExecutionMethod { get; set; }

    /// <summary>
    /// Current value from execution
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Historical value for comparison
    /// </summary>
    public decimal? HistoricalValue { get; set; }

    /// <summary>
    /// Deviation percentage
    /// </summary>
    public decimal? DeviationPercent { get; set; }

    /// <summary>
    /// Period for historical comparison
    /// </summary>
    public int Period { get; set; }

    /// <summary>
    /// Metric key identifier
    /// </summary>
    public string MetricKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution time in milliseconds
    /// </summary>
    public long? ExecutionTimeMs { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Server name
    /// </summary>
    public string? ServerName { get; set; }

    /// <summary>
    /// Whether alert should be triggered
    /// </summary>
    public bool ShouldAlert { get; set; }

    /// <summary>
    /// Whether alert was sent
    /// </summary>
    public bool AlertSent { get; set; }

    /// <summary>
    /// Session ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// IP address of executor
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// SQL command executed
    /// </summary>
    public string? SqlCommand { get; set; }

    /// <summary>
    /// Raw response from database
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// Execution context
    /// </summary>
    public string? ExecutionContext { get; set; }

    /// <summary>
    /// Performance category
    /// </summary>
    public string PerformanceCategory { get; set; } = string.Empty;

    /// <summary>
    /// Deviation category
    /// </summary>
    public string DeviationCategory { get; set; } = string.Empty;
}

/// <summary>
/// Detailed execution history with additional information
/// </summary>
public class ExecutionHistoryDetailDto : ExecutionHistoryDto
{
    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// SQL parameters used
    /// </summary>
    public string? SqlParameters { get; set; }

    /// <summary>
    /// Connection string used
    /// </summary>
    public string? ConnectionString { get; set; }
}

/// <summary>
/// Paginated execution history response
/// </summary>
public class PaginatedExecutionHistoryResponse
{
    /// <summary>
    /// List of execution history records
    /// </summary>
    public List<ExecutionHistoryDto> Executions { get; set; } = new();

    /// <summary>
    /// Total number of records
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Execution statistics DTO
/// </summary>
public class ExecutionStatsDto
{
    /// <summary>
    /// KPI ID
    /// </summary>
    public long KpiId { get; set; }

    /// <summary>
    /// Indicator name
    /// </summary>
    public string Indicator { get; set; } = string.Empty;

    /// <summary>
    /// Owner name
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Total number of executions
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Number of successful executions
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double? AvgExecutionTimeMs { get; set; }

    /// <summary>
    /// Minimum execution time in milliseconds
    /// </summary>
    public long? MinExecutionTimeMs { get; set; }

    /// <summary>
    /// Maximum execution time in milliseconds
    /// </summary>
    public long? MaxExecutionTimeMs { get; set; }

    /// <summary>
    /// Number of alerts triggered
    /// </summary>
    public int AlertsTriggered { get; set; }

    /// <summary>
    /// Number of alerts sent
    /// </summary>
    public int AlertsSent { get; set; }

    /// <summary>
    /// Last execution timestamp
    /// </summary>
    public string? LastExecution { get; set; }

    /// <summary>
    /// Number of unique executors
    /// </summary>
    public int UniqueExecutors { get; set; }

    /// <summary>
    /// Number of execution methods
    /// </summary>
    public int ExecutionMethods { get; set; }

    /// <summary>
    /// Whether the last execution was successful
    /// </summary>
    public bool? IsSuccessful { get; set; }
}
