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
    public bool IsSuccessful => string.IsNullOrEmpty(ErrorMessage);
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
