using MonitoringGrid.Api.DTOs.Common;

namespace MonitoringGrid.Api.DTOs.Indicators;

/// <summary>
/// Enhanced indicator response DTO
/// </summary>
public class IndicatorResponse
{
    /// <summary>
    /// Indicator ID
    /// </summary>
    public long IndicatorID { get; set; }

    /// <summary>
    /// Indicator name
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Indicator description
    /// </summary>
    public string? IndicatorDescription { get; set; }

    /// <summary>
    /// Owner contact ID
    /// </summary>
    public long OwnerContactId { get; set; }

    /// <summary>
    /// Owner name
    /// </summary>
    public string? OwnerName { get; set; }

    /// <summary>
    /// Collector ID
    /// </summary>
    public long CollectorId { get; set; }

    /// <summary>
    /// Collector name
    /// </summary>
    public string? CollectorName { get; set; }

    /// <summary>
    /// SQL query
    /// </summary>
    public string SqlQuery { get; set; } = string.Empty;

    /// <summary>
    /// Whether the indicator is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Last minutes parameter
    /// </summary>
    public int LastMinutes { get; set; }

    /// <summary>
    /// Scheduler ID
    /// </summary>
    public int? SchedulerId { get; set; }

    /// <summary>
    /// Scheduler name
    /// </summary>
    public string? SchedulerName { get; set; }

    /// <summary>
    /// Alert threshold value
    /// </summary>
    public decimal? AlertThreshold { get; set; }

    /// <summary>
    /// Alert comparison operator
    /// </summary>
    public string? AlertOperator { get; set; }

    /// <summary>
    /// Last run time
    /// </summary>
    public DateTime? LastRun { get; set; }

    /// <summary>
    /// Last run status
    /// </summary>
    public string? LastRunStatus { get; set; }

    /// <summary>
    /// Last run duration in milliseconds
    /// </summary>
    public long? LastRunDurationMs { get; set; }

    /// <summary>
    /// Last run result count
    /// </summary>
    public int? LastRunResultCount { get; set; }

    /// <summary>
    /// Next scheduled run time
    /// </summary>
    public DateTime? NextRun { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Created by user
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime? ModifiedDate { get; set; }

    /// <summary>
    /// Last modified by user
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Additional configuration
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Execution statistics (if requested)
    /// </summary>
    public IndicatorExecutionStats? ExecutionStats { get; set; }

    /// <summary>
    /// Scheduler information (if requested)
    /// </summary>
    public IndicatorSchedulerInfo? Scheduler { get; set; }

    /// <summary>
    /// Collector information (if requested)
    /// </summary>
    public IndicatorCollectorInfo? Collector { get; set; }

    /// <summary>
    /// Recent executions (if requested)
    /// </summary>
    public List<IndicatorExecutionSummary>? RecentExecutions { get; set; }

    /// <summary>
    /// Additional details (if requested)
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Indicator execution statistics
/// </summary>
public class IndicatorExecutionStats
{
    /// <summary>
    /// Total executions count
    /// </summary>
    public int TotalExecutions { get; set; }

    /// <summary>
    /// Successful executions count
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Failed executions count
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Last 30 days execution count
    /// </summary>
    public int Last30DaysExecutions { get; set; }

    /// <summary>
    /// Average result count
    /// </summary>
    public double AverageResultCount { get; set; }
}

/// <summary>
/// Indicator scheduler information
/// </summary>
public class IndicatorSchedulerInfo
{
    /// <summary>
    /// Scheduler ID
    /// </summary>
    public int SchedulerId { get; set; }

    /// <summary>
    /// Scheduler name
    /// </summary>
    public string SchedulerName { get; set; } = string.Empty;

    /// <summary>
    /// Scheduler description
    /// </summary>
    public string? SchedulerDescription { get; set; }

    /// <summary>
    /// Schedule type
    /// </summary>
    public string ScheduleType { get; set; } = string.Empty;

    /// <summary>
    /// Interval in minutes
    /// </summary>
    public int? IntervalMinutes { get; set; }

    /// <summary>
    /// Cron expression
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Whether the scheduler is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Next execution time
    /// </summary>
    public DateTime? NextExecution { get; set; }
}

/// <summary>
/// Indicator collector information
/// </summary>
public class IndicatorCollectorInfo
{
    /// <summary>
    /// Collector ID
    /// </summary>
    public long CollectorId { get; set; }

    /// <summary>
    /// Collector name
    /// </summary>
    public string CollectorName { get; set; } = string.Empty;

    /// <summary>
    /// Collector description
    /// </summary>
    public string? CollectorDescription { get; set; }

    /// <summary>
    /// Connection string
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Database type
    /// </summary>
    public string? DatabaseType { get; set; }

    /// <summary>
    /// Whether the collector is active
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Indicator execution summary
/// </summary>
public class IndicatorExecutionSummary
{
    /// <summary>
    /// Execution ID
    /// </summary>
    public long ExecutionId { get; set; }

    /// <summary>
    /// Execution time
    /// </summary>
    public DateTime ExecutionTime { get; set; }

    /// <summary>
    /// Execution status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Result count
    /// </summary>
    public int ResultCount { get; set; }

    /// <summary>
    /// Error message (if any)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution context
    /// </summary>
    public string? ExecutionContext { get; set; }
}

/// <summary>
/// Paginated indicators response
/// </summary>
public class PaginatedIndicatorsResponse
{
    /// <summary>
    /// List of indicators
    /// </summary>
    public List<IndicatorResponse> Indicators { get; set; } = new();

    /// <summary>
    /// Total count of indicators (before pagination)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
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

    /// <summary>
    /// Indicators summary statistics
    /// </summary>
    public IndicatorsSummary Summary { get; set; } = new();

    /// <summary>
    /// Query performance metrics
    /// </summary>
    public QueryMetrics QueryMetrics { get; set; } = new();
}

/// <summary>
/// Indicators summary statistics
/// </summary>
public class IndicatorsSummary
{
    /// <summary>
    /// Total indicators
    /// </summary>
    public int TotalIndicators { get; set; }

    /// <summary>
    /// Active indicators count
    /// </summary>
    public int ActiveIndicators { get; set; }

    /// <summary>
    /// Inactive indicators count
    /// </summary>
    public int InactiveIndicators { get; set; }

    /// <summary>
    /// Indicators with schedulers count
    /// </summary>
    public int IndicatorsWithSchedulers { get; set; }

    /// <summary>
    /// Indicators without schedulers count
    /// </summary>
    public int IndicatorsWithoutSchedulers { get; set; }

    /// <summary>
    /// Indicators executed today count
    /// </summary>
    public int ExecutedTodayIndicators { get; set; }
}

/// <summary>
/// Enhanced indicator execution result response
/// </summary>
public class IndicatorExecutionResultResponse
{
    /// <summary>
    /// Execution ID
    /// </summary>
    public long ExecutionId { get; set; }

    /// <summary>
    /// Indicator ID
    /// </summary>
    public long IndicatorId { get; set; }

    /// <summary>
    /// Indicator name
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Execution status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Whether execution was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Execution start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Execution end time
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Execution duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Number of results returned
    /// </summary>
    public int ResultCount { get; set; }

    /// <summary>
    /// Execution results (if requested)
    /// </summary>
    public List<Dictionary<string, object>>? Results { get; set; }

    /// <summary>
    /// Error message (if any)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution context
    /// </summary>
    public string? ExecutionContext { get; set; }

    /// <summary>
    /// SQL query executed
    /// </summary>
    public string? SqlQuery { get; set; }

    /// <summary>
    /// Alert triggered information
    /// </summary>
    public bool AlertTriggered { get; set; }

    /// <summary>
    /// Alert details (if triggered)
    /// </summary>
    public Dictionary<string, object>? AlertDetails { get; set; }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public ExecutionPerformanceMetrics? PerformanceMetrics { get; set; }

    /// <summary>
    /// Additional execution details
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Execution performance metrics
/// </summary>
public class ExecutionPerformanceMetrics
{
    /// <summary>
    /// Query compilation time in milliseconds
    /// </summary>
    public long CompilationTimeMs { get; set; }

    /// <summary>
    /// Query execution time in milliseconds
    /// </summary>
    public long QueryExecutionTimeMs { get; set; }

    /// <summary>
    /// Data processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// CPU usage percentage
    /// </summary>
    public double CpuUsagePercent { get; set; }
}

/// <summary>
/// Enhanced indicator dashboard response
/// </summary>
public class IndicatorDashboardResponse
{
    /// <summary>
    /// Total indicators count
    /// </summary>
    public int TotalIndicators { get; set; }

    /// <summary>
    /// Active indicators count
    /// </summary>
    public int ActiveIndicators { get; set; }

    /// <summary>
    /// Indicators executed today
    /// </summary>
    public int ExecutedTodayIndicators { get; set; }

    /// <summary>
    /// Failed executions today
    /// </summary>
    public int FailedExecutionsToday { get; set; }

    /// <summary>
    /// Indicators due for execution
    /// </summary>
    public int DueIndicators { get; set; }

    /// <summary>
    /// Currently running indicators
    /// </summary>
    public int RunningIndicators { get; set; }

    /// <summary>
    /// Average execution time today (milliseconds)
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Success rate today (percentage)
    /// </summary>
    public double SuccessRateToday { get; set; }

    /// <summary>
    /// Recent executions
    /// </summary>
    public List<IndicatorExecutionSummary> RecentExecutions { get; set; } = new();

    /// <summary>
    /// Top performing indicators
    /// </summary>
    public List<IndicatorPerformanceSummary> TopPerformingIndicators { get; set; } = new();

    /// <summary>
    /// Indicators with issues
    /// </summary>
    public List<IndicatorIssueSummary> IndicatorsWithIssues { get; set; } = new();

    /// <summary>
    /// Execution trend data
    /// </summary>
    public List<ExecutionTrendData> ExecutionTrend { get; set; } = new();

    /// <summary>
    /// System health score
    /// </summary>
    public double SystemHealthScore { get; set; }

    /// <summary>
    /// Dashboard generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Dashboard generation duration
    /// </summary>
    public long GenerationTimeMs { get; set; }
}

/// <summary>
/// Indicator performance summary
/// </summary>
public class IndicatorPerformanceSummary
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
    /// Execution count
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Last execution time
    /// </summary>
    public DateTime? LastExecution { get; set; }
}

/// <summary>
/// Indicator issue summary
/// </summary>
public class IndicatorIssueSummary
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
    /// Issue type
    /// </summary>
    public string IssueType { get; set; } = string.Empty;

    /// <summary>
    /// Issue description
    /// </summary>
    public string IssueDescription { get; set; } = string.Empty;

    /// <summary>
    /// Last failure time
    /// </summary>
    public DateTime? LastFailure { get; set; }

    /// <summary>
    /// Failure count in last 24 hours
    /// </summary>
    public int FailureCount24h { get; set; }
}

/// <summary>
/// Execution trend data point
/// </summary>
public class ExecutionTrendData
{
    /// <summary>
    /// Date/time of the data point
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total executions count
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Successful executions count
    /// </summary>
    public int SuccessfulCount { get; set; }

    /// <summary>
    /// Failed executions count
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }
}

/// <summary>
/// Indicator operation response
/// </summary>
public class IndicatorOperationResponse
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
    /// Indicator ID(s) involved in the operation
    /// </summary>
    public List<long> IndicatorIds { get; set; } = new();

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

    /// <summary>
    /// User who performed the operation
    /// </summary>
    public string? PerformedBy { get; set; }
}


