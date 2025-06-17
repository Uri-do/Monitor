namespace MonitoringGrid.Core.Models;

/// <summary>
/// Enhanced dashboard data for indicators
/// </summary>
public class IndicatorDashboard
{
    public int TotalIndicators { get; set; }
    public int ActiveIndicators { get; set; }
    public int InactiveIndicators { get; set; }
    public int DueIndicators { get; set; }
    public int RunningIndicators { get; set; }
    public int IndicatorsExecutedToday { get; set; }
    public int AlertsTriggeredToday { get; set; }
    public int FailedExecutionsToday { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public double SuccessRateToday { get; set; }
    public List<IndicatorExecutionSummary> RecentExecutions { get; set; } = new();
    public List<IndicatorCountByPriority> CountByPriority { get; set; } = new();
    public List<IndicatorTrendData> ExecutionTrends { get; set; } = new();
    public List<IndicatorHealthStatus> HealthStatus { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan GenerationDuration { get; set; }
}

/// <summary>
/// Enhanced indicator execution summary
/// </summary>
public class IndicatorExecutionSummary
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; }
    public bool WasSuccessful { get; set; }
    public decimal? Value { get; set; }
    public decimal? ThresholdValue { get; set; }
    public bool AlertTriggered { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionDuration { get; set; }
    public string? ExecutionContext { get; set; }
    public int OwnerContactId { get; set; }
}

/// <summary>
/// Indicator count by priority with additional metrics
/// </summary>
public class IndicatorCountByPriority
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
    public int ActiveCount { get; set; }
    public int DueCount { get; set; }
    public int RunningCount { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Indicator trend data for analytics
/// </summary>
public class IndicatorTrendData
{
    public DateTime Date { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int AlertsTriggered { get; set; }
    public double AverageExecutionTime { get; set; }
}

/// <summary>
/// Indicator health status
/// </summary>
public class IndicatorHealthStatus
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public HealthLevel HealthLevel { get; set; }
    public List<string> Issues { get; set; } = new();
    public DateTime? LastSuccessfulExecution { get; set; }
    public int ConsecutiveFailures { get; set; }
}

/// <summary>
/// Health level enumeration
/// </summary>
public enum HealthLevel
{
    Healthy,
    Warning,
    Critical,
    Unknown
}

/// <summary>
/// Enhanced indicator test result with diagnostics
/// </summary>
public class IndicatorTestResult
{
    public bool WasSuccessful { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public bool ThresholdBreached { get; set; }
    public string? ErrorMessage { get; set; }
    public List<CollectorStatisticDto> RawData { get; set; } = new();
    public TimeSpan ExecutionDuration { get; set; }
    public TestDiagnostics? Diagnostics { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Test diagnostics information
/// </summary>
public class TestDiagnostics
{
    public string SqlQuery { get; set; } = string.Empty;
    public int RowsReturned { get; set; }
    public TimeSpan QueryExecutionTime { get; set; }
    public long MemoryUsed { get; set; }
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object>? PerformanceMetrics { get; set; }
}

/// <summary>
/// Enhanced indicator statistics with comprehensive metrics
/// </summary>
public class IndicatorStatistics
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int AlertsTriggered { get; set; }
    public decimal? AverageValue { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public decimal? StandardDeviation { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? NextExecution { get; set; }
    public TimeSpan? AverageExecutionTime { get; set; }
    public double SuccessRate { get; set; }
    public double AlertRate { get; set; }
    public List<IndicatorValueTrend> ValueTrend { get; set; } = new();
    public List<ExecutionFrequencyData> ExecutionFrequency { get; set; } = new();
    public PerformanceMetrics? Performance { get; set; }
}

/// <summary>
/// Enhanced indicator value trend data
/// </summary>
public class IndicatorValueTrend
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public bool AlertTriggered { get; set; }
    public bool WasSuccessful { get; set; }
    public TimeSpan? ExecutionDuration { get; set; }
    public string? ExecutionContext { get; set; }
}

/// <summary>
/// Execution frequency data for analytics
/// </summary>
public class ExecutionFrequencyData
{
    public DateTime Date { get; set; }
    public int ExecutionCount { get; set; }
    public TimeSpan TotalExecutionTime { get; set; }
    public double AverageExecutionTime { get; set; }
}

/// <summary>
/// Performance metrics for indicators
/// </summary>
public class PerformanceMetrics
{
    public TimeSpan AverageExecutionTime { get; set; }
    public TimeSpan MinExecutionTime { get; set; }
    public TimeSpan MaxExecutionTime { get; set; }
    public long AverageMemoryUsage { get; set; }
    public double AverageCpuUsage { get; set; }
    public int TotalQueries { get; set; }
    public TimeSpan TotalQueryTime { get; set; }
}

/// <summary>
/// Enhanced indicator execution result
/// </summary>
public class IndicatorExecutionResult
{
    public long ExecutionId { get; set; }
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public bool WasSuccessful { get; set; }
    public decimal? Value { get; set; }
    public decimal? ThresholdValue { get; set; }
    public bool ThresholdBreached { get; set; }
    public bool AlertTriggered { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan ExecutionDuration { get; set; }
    public string? ExecutionContext { get; set; }
    public List<CollectorStatisticDto> RawData { get; set; } = new();
    public ExecutionMetrics? Metrics { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Execution metrics for performance tracking
/// </summary>
public class ExecutionMetrics
{
    public TimeSpan QueryExecutionTime { get; set; }
    public TimeSpan DataProcessingTime { get; set; }
    public long MemoryUsed { get; set; }
    public double CpuUsage { get; set; }
    public int RowsProcessed { get; set; }
    public int QueriesExecuted { get; set; }
}

/// <summary>
/// Enhanced indicator performance metrics
/// </summary>
public class IndicatorPerformanceMetrics
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PerformanceMetrics Overall { get; set; } = new();
    public List<DailyPerformanceMetrics> DailyMetrics { get; set; } = new();
    public List<HourlyPerformanceMetrics> HourlyMetrics { get; set; } = new();
    public PerformanceTrends Trends { get; set; } = new();
    public List<PerformanceAlert> Alerts { get; set; } = new();
}

/// <summary>
/// Daily performance metrics
/// </summary>
public class DailyPerformanceMetrics
{
    public DateTime Date { get; set; }
    public int ExecutionCount { get; set; }
    public TimeSpan TotalExecutionTime { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public long AverageMemoryUsage { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Hourly performance metrics
/// </summary>
public class HourlyPerformanceMetrics
{
    public DateTime Hour { get; set; }
    public int ExecutionCount { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public double SuccessRate { get; set; }
}

/// <summary>
/// Performance trends analysis
/// </summary>
public class PerformanceTrends
{
    public TrendDirection ExecutionTimeTrend { get; set; }
    public TrendDirection MemoryUsageTrend { get; set; }
    public TrendDirection SuccessRateTrend { get; set; }
    public double ExecutionTimeChangePercent { get; set; }
    public double MemoryUsageChangePercent { get; set; }
    public double SuccessRateChangePercent { get; set; }
}

/// <summary>
/// Trend direction enumeration
/// </summary>
public enum TrendDirection
{
    Improving,
    Stable,
    Degrading,
    Unknown
}

/// <summary>
/// Performance alert information
/// </summary>
public class PerformanceAlert
{
    public string AlertType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Indicator execution status
/// </summary>
public class IndicatorExecutionStatus
{
    public long IndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public bool IsCurrentlyRunning { get; set; }
    public DateTime? ExecutionStartTime { get; set; }
    public string? ExecutionContext { get; set; }
    public TimeSpan? ExecutionDuration { get; set; }
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
    public string Status { get; set; } = string.Empty; // "idle", "running", "error", "completed"
}

/// <summary>
/// Indicator execution history entry
/// </summary>
public class IndicatorExecutionHistory
{
    public int HistoryId { get; set; }
    public long IndicatorID { get; set; }
    public DateTime ExecutionTime { get; set; }
    public bool WasSuccessful { get; set; }
    public decimal? Value { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionDuration { get; set; }
    public string ExecutionContext { get; set; } = string.Empty;
    public bool AlertTriggered { get; set; }
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Collector statistic DTO for raw data
/// </summary>
public class CollectorStatisticDto
{
    public long StatisticID { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public DateTime StatisticDate { get; set; }
    public int Total { get; set; }
    public int Marked { get; set; }
    public decimal MarkedPercent { get; set; }
    public DateTime CreatedDate { get; set; }
}
