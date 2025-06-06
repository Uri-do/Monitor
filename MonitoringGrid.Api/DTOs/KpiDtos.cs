using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.DTOs;

/// <summary>
/// KPI data transfer object for API responses
/// </summary>
public class KpiDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public byte Priority { get; set; }
    public string PriorityName => Priority == 1 ? "SMS + Email" : "Email Only";
    public int Frequency { get; set; }
    public int LastMinutes { get; set; }
    public decimal Deviation { get; set; }
    public string SpName { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string DescriptionTemplate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastRun { get; set; }
    public int CooldownMinutes { get; set; }
    public decimal? MinimumThreshold { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public List<ContactDto> Contacts { get; set; } = new();
}

/// <summary>
/// KPI creation/update request
/// </summary>
public class CreateKpiRequest
{
    [Required]
    [StringLength(255)]
    public string Indicator { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Owner { get; set; } = string.Empty;

    [Range(1, 2)]
    public byte Priority { get; set; }

    [Range(1, int.MaxValue)]
    public int Frequency { get; set; }

    [Range(1, int.MaxValue)]
    public int LastMinutes { get; set; } = 1440;

    [Range(0, 100)]
    public decimal Deviation { get; set; }

    [Required]
    [StringLength(255)]
    public string SpName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string SubjectTemplate { get; set; } = string.Empty;

    [Required]
    public string DescriptionTemplate { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int CooldownMinutes { get; set; } = 30;

    [Range(0, double.MaxValue)]
    public decimal? MinimumThreshold { get; set; }

    public List<int> ContactIds { get; set; } = new();
}

/// <summary>
/// KPI update request
/// </summary>
public class UpdateKpiRequest : CreateKpiRequest
{
    public int KpiId { get; set; }
}

/// <summary>
/// KPI execution result for API
/// </summary>
public class KpiExecutionResultDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal HistoricalValue { get; set; }
    public decimal DeviationPercent { get; set; }
    public bool ShouldAlert { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ExecutionTime { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public string? ExecutionDetails { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public bool IsSuccessful => string.IsNullOrEmpty(ErrorMessage);

    // Enhanced execution information
    public ExecutionTimingInfoDto? TimingInfo { get; set; }
    public DatabaseExecutionInfoDto? DatabaseInfo { get; set; }
    public List<ExecutionStepInfoDto>? ExecutionSteps { get; set; }
}

/// <summary>
/// KPI status summary
/// </summary>
public class KpiStatusDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
    public int Frequency { get; set; }
    public string Status { get; set; } = string.Empty; // "Running", "Due", "Error", "Inactive"
    public DateTime? LastAlert { get; set; }
    public int AlertsToday { get; set; }
    public decimal? LastCurrentValue { get; set; }
    public decimal? LastHistoricalValue { get; set; }
    public decimal? LastDeviation { get; set; }
}

/// <summary>
/// KPI performance metrics
/// </summary>
public class KpiMetricsDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public decimal SuccessRate { get; set; }
    public int TotalAlerts { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? LastAlert { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public List<KpiTrendDataDto> TrendData { get; set; } = new();
}

/// <summary>
/// KPI trend data point
/// </summary>
public class KpiTrendDataDto
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public int Period { get; set; }
}

/// <summary>
/// Bulk KPI operation request
/// </summary>
public class BulkKpiOperationRequest
{
    public List<int> KpiIds { get; set; } = new();
    public string Operation { get; set; } = string.Empty; // "activate", "deactivate", "delete", "execute"
}

/// <summary>
/// KPI test execution request
/// </summary>
public class TestKpiRequest
{
    public int KpiId { get; set; }
    public int? CustomFrequency { get; set; }
}

/// <summary>
/// KPI dashboard summary
/// </summary>
public class KpiDashboardDto
{
    public int TotalKpis { get; set; }
    public int ActiveKpis { get; set; }
    public int InactiveKpis { get; set; }
    public int KpisInErrorCount { get; set; }
    public int KpisDue { get; set; }
    public int AlertsToday { get; set; }
    public int AlertsThisWeek { get; set; }
    public DateTime LastUpdate { get; set; }
    public List<KpiStatusDto> RecentAlerts { get; set; } = new();
    public List<KpiStatusDto> KpisInError { get; set; } = new();
    public List<KpiStatusDto> DueKpis { get; set; } = new();
}

/// <summary>
/// KPI execution history record
/// </summary>
public class ExecutionHistoryDto
{
    public long HistoricalId { get; set; }
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string KpiOwner { get; set; } = string.Empty;
    public string SpName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? ExecutedBy { get; set; }
    public string? ExecutionMethod { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal? HistoricalValue { get; set; }
    public decimal? DeviationPercent { get; set; }
    public int Period { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public string? DatabaseName { get; set; }
    public string? ServerName { get; set; }
    public bool ShouldAlert { get; set; }
    public bool AlertSent { get; set; }
    public string? SessionId { get; set; }
    public string? IpAddress { get; set; }
    public string? SqlCommand { get; set; }
    public string? RawResponse { get; set; }
    public string? ExecutionContext { get; set; }
    public string PerformanceCategory { get; set; } = string.Empty;
    public string DeviationCategory { get; set; } = string.Empty;
}

/// <summary>
/// Detailed execution history with additional fields
/// </summary>
public class ExecutionHistoryDetailDto : ExecutionHistoryDto
{
    public string? UserAgent { get; set; }
    public string? SqlParameters { get; set; }
    public string? ConnectionString { get; set; }
}

/// <summary>
/// Paginated execution history response
/// </summary>
public class PaginatedExecutionHistoryDto
{
    public List<ExecutionHistoryDto> Executions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// KPI execution statistics
/// </summary>
public class ExecutionStatsDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double? AvgExecutionTimeMs { get; set; }
    public int? MinExecutionTimeMs { get; set; }
    public int? MaxExecutionTimeMs { get; set; }
    public int AlertsTriggered { get; set; }
    public int AlertsSent { get; set; }
    public DateTime? LastExecution { get; set; }
    public int UniqueExecutors { get; set; }
    public int ExecutionMethods { get; set; }
}

/// <summary>
/// Detailed timing information for KPI execution
/// </summary>
public class ExecutionTimingInfoDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TotalExecutionMs { get; set; }
    public int DatabaseConnectionMs { get; set; }
    public int StoredProcedureExecutionMs { get; set; }
    public int ResultProcessingMs { get; set; }
    public int HistoricalDataSaveMs { get; set; }
}

/// <summary>
/// Database execution information
/// </summary>
public class DatabaseExecutionInfoDto
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string SqlCommand { get; set; } = string.Empty;
    public string SqlParameters { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
    public int RowsReturned { get; set; }
    public int ResultSetsReturned { get; set; }
}

/// <summary>
/// Individual execution step information
/// </summary>
public class ExecutionStepInfoDto
{
    public string StepName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int DurationMs { get; set; }
    public string Status { get; set; } = string.Empty; // "Success", "Error", "Warning"
    public string? Details { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? StepMetadata { get; set; }
}
