using MonitoringGrid.Core.DTOs;

namespace MonitoringGrid.Api.DTOs.Schedulers;

/// <summary>
/// Enhanced scheduler response DTO
/// </summary>
public class SchedulerResponse
{
    /// <summary>
    /// Scheduler ID
    /// </summary>
    public long SchedulerID { get; set; }

    /// <summary>
    /// Scheduler name
    /// </summary>
    public string SchedulerName { get; set; } = string.Empty;

    /// <summary>
    /// Scheduler description
    /// </summary>
    public string? SchedulerDescription { get; set; }

    /// <summary>
    /// Schedule type (Interval, Cron, etc.)
    /// </summary>
    public string ScheduleType { get; set; } = string.Empty;

    /// <summary>
    /// Interval in minutes (for interval-based schedules)
    /// </summary>
    public int? IntervalMinutes { get; set; }

    /// <summary>
    /// Cron expression (for cron-based schedules)
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Whether the scheduler is enabled
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Timezone for the scheduler
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// Start date for the scheduler
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for the scheduler
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Created by user
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Modified date
    /// </summary>
    public DateTime? ModifiedDate { get; set; }

    /// <summary>
    /// Modified by user
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Last execution time
    /// </summary>
    public DateTime? LastExecution { get; set; }

    /// <summary>
    /// Next scheduled execution time
    /// </summary>
    public DateTime? NextExecution { get; set; }

    /// <summary>
    /// Associated indicators (if requested)
    /// </summary>
    public List<SchedulerIndicatorInfo>? Indicators { get; set; }

    /// <summary>
    /// Performance metrics (if requested)
    /// </summary>
    public SchedulerMetrics? Metrics { get; set; }

    /// <summary>
    /// Execution history (if requested)
    /// </summary>
    public List<SchedulerExecutionHistory>? ExecutionHistory { get; set; }

    /// <summary>
    /// Additional details (if requested)
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Scheduler indicator information
/// </summary>
public class SchedulerIndicatorInfo
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
    /// Whether the indicator is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Last run time
    /// </summary>
    public DateTime? LastRun { get; set; }

    /// <summary>
    /// Whether currently running
    /// </summary>
    public bool IsCurrentlyRunning { get; set; }
}

/// <summary>
/// Scheduler performance metrics
/// </summary>
public class SchedulerMetrics
{
    /// <summary>
    /// Total executions in the analyzed period
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Successful executions
    /// </summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>
    /// Failed executions
    /// </summary>
    public long FailedExecutions { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double? AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Total indicators managed by this scheduler
    /// </summary>
    public int TotalIndicators { get; set; }

    /// <summary>
    /// Active indicators managed by this scheduler
    /// </summary>
    public int ActiveIndicators { get; set; }

    /// <summary>
    /// Last successful execution
    /// </summary>
    public DateTime? LastSuccessfulExecution { get; set; }

    /// <summary>
    /// Last failed execution
    /// </summary>
    public DateTime? LastFailedExecution { get; set; }
}

/// <summary>
/// Scheduler execution history item
/// </summary>
public class SchedulerExecutionHistory
{
    /// <summary>
    /// Execution timestamp
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Number of indicators executed
    /// </summary>
    public int IndicatorsExecuted { get; set; }

    /// <summary>
    /// Number of successful executions
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of failed executions
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Total execution duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Execution context
    /// </summary>
    public string Context { get; set; } = string.Empty;
}

/// <summary>
/// Paginated schedulers response
/// </summary>
public class PaginatedSchedulersResponse
{
    /// <summary>
    /// List of schedulers
    /// </summary>
    public List<SchedulerResponse> Schedulers { get; set; } = new();

    /// <summary>
    /// Total count of schedulers (before pagination)
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
    /// Query performance metrics
    /// </summary>
    public QueryMetrics QueryMetrics { get; set; } = new();
}

/// <summary>
/// Scheduler operation response
/// </summary>
public class SchedulerOperationResponse
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
    /// Scheduler ID involved in the operation
    /// </summary>
    public long? SchedulerId { get; set; }

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
    /// Affected scheduler data (for create/update operations)
    /// </summary>
    public SchedulerResponse? Scheduler { get; set; }
}

/// <summary>
/// Scheduler test execution response
/// </summary>
public class SchedulerTestResponse
{
    /// <summary>
    /// Scheduler ID
    /// </summary>
    public long SchedulerId { get; set; }

    /// <summary>
    /// Scheduler name
    /// </summary>
    public string SchedulerName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the test was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Test execution duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Number of indicators tested
    /// </summary>
    public int IndicatorsTested { get; set; }

    /// <summary>
    /// Number of successful indicator executions
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of failed indicator executions
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Test execution context
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// Test execution timestamp
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Individual indicator test results
    /// </summary>
    public List<IndicatorTestResult> IndicatorResults { get; set; } = new();

    /// <summary>
    /// Error message if test failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Individual indicator test result
/// </summary>
public class IndicatorTestResult
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
    /// Whether the test was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Test execution duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Error message if test failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Enhanced scheduler validation result
/// </summary>
public class SchedulerValidationResult
{
    /// <summary>
    /// Whether the configuration is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string>? ValidationErrors { get; set; }

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string>? ValidationWarnings { get; set; }

    /// <summary>
    /// Suggested improvements
    /// </summary>
    public List<string>? SuggestedImprovements { get; set; }

    /// <summary>
    /// Validation duration in milliseconds
    /// </summary>
    public long ValidationDurationMs { get; set; }

    /// <summary>
    /// When validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; set; }

    /// <summary>
    /// Who performed the validation
    /// </summary>
    public string? ValidatedBy { get; set; }
}

/// <summary>
/// Enhanced scheduler preset DTO
/// </summary>
public class SchedulerPresetDto
{
    /// <summary>
    /// Preset ID
    /// </summary>
    public int PresetId { get; set; }

    /// <summary>
    /// Preset name
    /// </summary>
    public string PresetName { get; set; } = string.Empty;

    /// <summary>
    /// Preset description
    /// </summary>
    public string? PresetDescription { get; set; }

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
    /// Whether this preset is recommended
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// Preset category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// How many times this preset has been used
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// When this preset was last used
    /// </summary>
    public DateTime? LastUsed { get; set; }

    /// <summary>
    /// When this preset was created
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Example of next execution time
    /// </summary>
    public DateTime? NextExecutionExample { get; set; }

    /// <summary>
    /// Whether this preset is popular (computed field)
    /// </summary>
    public bool IsPopular { get; set; }
}

/// <summary>
/// Enhanced scheduler statistics DTO
/// </summary>
public class SchedulerStatsDto
{
    /// <summary>
    /// Total number of schedulers
    /// </summary>
    public int TotalSchedulers { get; set; }

    /// <summary>
    /// Number of enabled schedulers
    /// </summary>
    public int EnabledSchedulers { get; set; }

    /// <summary>
    /// Number of disabled schedulers
    /// </summary>
    public int DisabledSchedulers { get; set; }

    /// <summary>
    /// Total number of indicators
    /// </summary>
    public int TotalIndicators { get; set; }

    /// <summary>
    /// Number of indicators with schedulers
    /// </summary>
    public int IndicatorsWithSchedulers { get; set; }

    /// <summary>
    /// Number of indicators without schedulers
    /// </summary>
    public int IndicatorsWithoutSchedulers { get; set; }

    /// <summary>
    /// Number of indicators due for execution
    /// </summary>
    public int DueIndicators { get; set; }

    /// <summary>
    /// Number of currently running indicators
    /// </summary>
    public int RunningIndicators { get; set; }

    /// <summary>
    /// Last execution time
    /// </summary>
    public DateTime? LastExecutionTime { get; set; }

    /// <summary>
    /// Next scheduled execution time
    /// </summary>
    public DateTime? NextExecutionTime { get; set; }

    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double? AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Total executions today
    /// </summary>
    public int TotalExecutionsToday { get; set; }

    /// <summary>
    /// Successful executions today
    /// </summary>
    public int SuccessfulExecutionsToday { get; set; }

    /// <summary>
    /// Failed executions today
    /// </summary>
    public int FailedExecutionsToday { get; set; }

    /// <summary>
    /// Scheduler utilization rate percentage
    /// </summary>
    public double SchedulerUtilizationRate { get; set; }

    /// <summary>
    /// Indicator coverage rate percentage
    /// </summary>
    public double IndicatorCoverageRate { get; set; }

    /// <summary>
    /// Today's success rate percentage
    /// </summary>
    public double TodaySuccessRate { get; set; }

    /// <summary>
    /// Overall system health score (0-100)
    /// </summary>
    public double SystemHealthScore { get; set; }

    /// <summary>
    /// When statistics were generated
    /// </summary>
    public DateTime StatisticsGeneratedAt { get; set; }

    /// <summary>
    /// Statistics generation time in milliseconds
    /// </summary>
    public long StatisticsGenerationTimeMs { get; set; }
}

/// <summary>
/// Paginated indicators with schedulers response
/// </summary>
public class PaginatedIndicatorsWithSchedulersResponse
{
    /// <summary>
    /// List of indicators with scheduler information
    /// </summary>
    public List<IndicatorWithSchedulerDto> Indicators { get; set; } = new();

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
    /// Summary statistics
    /// </summary>
    public IndicatorSchedulerSummary Summary { get; set; } = new();

    /// <summary>
    /// Query performance metrics
    /// </summary>
    public QueryMetrics QueryMetrics { get; set; } = new();
}

/// <summary>
/// Indicator scheduler summary
/// </summary>
public class IndicatorSchedulerSummary
{
    /// <summary>
    /// Total indicators
    /// </summary>
    public int TotalIndicators { get; set; }

    /// <summary>
    /// Indicators with schedulers
    /// </summary>
    public int IndicatorsWithSchedulers { get; set; }

    /// <summary>
    /// Indicators without schedulers
    /// </summary>
    public int IndicatorsWithoutSchedulers { get; set; }

    /// <summary>
    /// Active indicators
    /// </summary>
    public int ActiveIndicators { get; set; }

    /// <summary>
    /// Due indicators
    /// </summary>
    public int DueIndicators { get; set; }

    /// <summary>
    /// Running indicators
    /// </summary>
    public int RunningIndicators { get; set; }
}

/// <summary>
/// Next execution time calculation response
/// </summary>
public class NextExecutionResponse
{
    /// <summary>
    /// Scheduler ID
    /// </summary>
    public int SchedulerId { get; set; }

    /// <summary>
    /// Last execution time used for calculation
    /// </summary>
    public DateTime? LastExecution { get; set; }

    /// <summary>
    /// Calculated next execution time
    /// </summary>
    public DateTime? NextExecution { get; set; }

    /// <summary>
    /// When the calculation was performed
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// Calculation duration in milliseconds
    /// </summary>
    public long CalculationDurationMs { get; set; }

    /// <summary>
    /// Time until next execution
    /// </summary>
    public TimeSpan? TimeUntilExecution { get; set; }
}

/// <summary>
/// Query performance metrics (reused from MonitorStatistics)
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
