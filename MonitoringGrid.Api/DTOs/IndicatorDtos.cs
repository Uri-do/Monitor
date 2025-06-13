using System.ComponentModel.DataAnnotations;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Api.DTOs;

/// <summary>
/// Indicator data transfer object
/// </summary>
public class IndicatorDto
{
    public long IndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string IndicatorCode { get; set; } = string.Empty;
    public string? IndicatorDesc { get; set; }
    public long? CollectorID { get; set; }
    public string CollectorItemName { get; set; } = string.Empty;
    public int? SchedulerID { get; set; }
    public bool IsActive { get; set; }
    public int LastMinutes { get; set; }
    public string ThresholdType { get; set; } = string.Empty;
    public string ThresholdField { get; set; } = string.Empty;
    public string ThresholdComparison { get; set; } = string.Empty;
    public decimal ThresholdValue { get; set; }
    public string Priority { get; set; } = string.Empty;
    public int OwnerContactId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public DateTime? LastRun { get; set; }
    public string? LastRunResult { get; set; }
    public decimal? AverageHour { get; set; }
    public int? AverageLastDays { get; set; }
    public bool IsCurrentlyRunning { get; set; }
    public DateTime? ExecutionStartTime { get; set; }
    public string? ExecutionContext { get; set; }

    // Navigation properties
    public ContactDto? OwnerContact { get; set; }
    public List<ContactDto> Contacts { get; set; } = new();
    public SchedulerDto? Scheduler { get; set; }
}

/// <summary>
/// Create indicator request
/// </summary>
public class CreateIndicatorRequest
{
    [Required]
    [MaxLength(255)]
    public string IndicatorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string IndicatorCode { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? IndicatorDesc { get; set; }

    [Required]
    public int CollectorID { get; set; }

    [Required]
    [MaxLength(255)]
    public string CollectorItemName { get; set; } = string.Empty;

    public int? SchedulerID { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int LastMinutes { get; set; } = 60;

    [Required]
    [MaxLength(50)]
    public string ThresholdType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ThresholdField { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string ThresholdComparison { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal ThresholdValue { get; set; }

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = "medium";

    [Required]
    public int OwnerContactId { get; set; }

    public int? AverageLastDays { get; set; }

    public List<int> ContactIds { get; set; } = new();
}

/// <summary>
/// Update indicator request
/// </summary>
public class UpdateIndicatorRequest : CreateIndicatorRequest
{
    public int IndicatorID { get; set; }
}

/// <summary>
/// Indicator execution request
/// </summary>
public class ExecuteIndicatorRequest
{
    public long IndicatorID { get; set; }
    public string ExecutionContext { get; set; } = "Manual";
    public bool SaveResults { get; set; } = true;
}

/// <summary>
/// Indicator test request
/// </summary>
public class TestIndicatorRequest
{
    public long IndicatorID { get; set; }
    public int? OverrideLastMinutes { get; set; }
}

/// <summary>
/// Bulk indicator operation request
/// </summary>
public class BulkIndicatorOperationRequest
{
    public List<long> IndicatorIDs { get; set; } = new();
    public string Operation { get; set; } = string.Empty; // "activate", "deactivate", "delete", "execute"
    public string? ExecutionContext { get; set; }
}

/// <summary>
/// Indicator filter request
/// </summary>
public class IndicatorFilterRequest
{
    public bool? IsActive { get; set; }
    public string? Priority { get; set; }
    public int? OwnerContactId { get; set; }
    public long? CollectorId { get; set; }
    public string? ThresholdType { get; set; }
    public bool? IsCurrentlyRunning { get; set; }
    public DateTime? LastRunAfter { get; set; }
    public DateTime? LastRunBefore { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string SortBy { get; set; } = "IndicatorName";
    public string SortDirection { get; set; } = "asc";
}

/// <summary>
/// Indicator execution result
/// </summary>
public class IndicatorExecutionResultDto
{
    public long IndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public bool WasSuccessful { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public bool ThresholdBreached { get; set; }
    public string? ErrorMessage { get; set; }
    public List<MonitoringGrid.Core.DTOs.CollectorStatisticDto> RawData { get; set; } = new();
    public TimeSpan ExecutionDuration { get; set; }
    public DateTime ExecutionTime { get; set; }
    public string ExecutionContext { get; set; } = string.Empty;
}

/// <summary>
/// Indicator dashboard response
/// </summary>
public class IndicatorDashboardDto
{
    public int TotalIndicators { get; set; }
    public int ActiveIndicators { get; set; }
    public int DueIndicators { get; set; }
    public int RunningIndicators { get; set; }
    public int IndicatorsExecutedToday { get; set; }
    public int AlertsTriggeredToday { get; set; }
    public List<IndicatorExecutionSummaryDto> RecentExecutions { get; set; } = new();
    public List<IndicatorCountByPriorityDto> CountByPriority { get; set; } = new();
    public List<IndicatorStatusDto> IndicatorStatuses { get; set; } = new();
}

/// <summary>
/// Indicator execution summary
/// </summary>
public class IndicatorExecutionSummaryDto
{
    public long IndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; }
    public bool WasSuccessful { get; set; }
    public decimal? Value { get; set; }
    public string? ErrorMessage { get; set; }
    public string Priority { get; set; } = string.Empty;
}

/// <summary>
/// Indicator count by priority
/// </summary>
public class IndicatorCountByPriorityDto
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
    public int ActiveCount { get; set; }
    public int DueCount { get; set; }
}

/// <summary>
/// Indicator status
/// </summary>
public class IndicatorStatusDto
{
    public long IndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsCurrentlyRunning { get; set; }
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
    public string Status { get; set; } = string.Empty; // "healthy", "warning", "error", "unknown"
    public string? LastError { get; set; }
}

/// <summary>
/// Indicator statistics response
/// </summary>
public class IndicatorStatisticsDto
{
    public long IndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int AlertsTriggered { get; set; }
    public decimal? AverageValue { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? NextExecution { get; set; }
    public List<IndicatorValueTrendDto> ValueTrend { get; set; } = new();
}

/// <summary>
/// Indicator value trend data
/// </summary>
public class IndicatorValueTrendDto
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public bool AlertTriggered { get; set; }
    public string? Note { get; set; }
}
