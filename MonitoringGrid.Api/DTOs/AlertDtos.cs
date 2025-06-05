using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.DTOs;

/// <summary>
/// Alert log data transfer object for API responses
/// </summary>
public class AlertLogDto
{
    public long AlertId { get; set; }
    public int KpiId { get; set; }
    public string KpiIndicator { get; set; } = string.Empty;
    public string KpiOwner { get; set; } = string.Empty;
    public DateTime TriggerTime { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public byte SentVia { get; set; }
    public string SentViaName => SentVia switch
    {
        1 => "SMS",
        2 => "Email",
        3 => "SMS + Email",
        _ => "Unknown"
    };
    public string SentTo { get; set; } = string.Empty;
    public decimal? CurrentValue { get; set; }
    public decimal? HistoricalValue { get; set; }
    public decimal? DeviationPercent { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedTime { get; set; }
    public string? ResolvedBy { get; set; }
    public string Severity => DeviationPercent switch
    {
        >= 50 => "Critical",
        >= 25 => "High",
        >= 10 => "Medium",
        _ => "Low"
    };
}

/// <summary>
/// Alert resolution request
/// </summary>
public class ResolveAlertRequest
{
    [Required]
    public long AlertId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string ResolvedBy { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// Bulk alert resolution request
/// </summary>
public class BulkResolveAlertsRequest
{
    public List<long> AlertIds { get; set; } = new();
    
    [Required]
    [StringLength(100)]
    public string ResolvedBy { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// Alert statistics
/// </summary>
public class AlertStatisticsDto
{
    public int TotalAlerts { get; set; }
    public int UnresolvedAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int AlertsToday { get; set; }
    public int AlertsThisWeek { get; set; }
    public int AlertsThisMonth { get; set; }
    public int CriticalAlerts { get; set; }
    public int HighPriorityAlerts { get; set; }
    public decimal AverageResolutionTimeHours { get; set; }
    public List<AlertTrendDto> DailyTrend { get; set; } = new();
    public List<KpiAlertSummaryDto> TopAlertingKpis { get; set; } = new();
}

/// <summary>
/// Alert trend data
/// </summary>
public class AlertTrendDto
{
    public DateTime Date { get; set; }
    public int AlertCount { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
}

/// <summary>
/// KPI alert summary
/// </summary>
public class KpiAlertSummaryDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int AlertCount { get; set; }
    public int UnresolvedCount { get; set; }
    public DateTime? LastAlert { get; set; }
    public decimal? AverageDeviation { get; set; }
}

/// <summary>
/// Alert filter parameters
/// </summary>
public class AlertFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<int>? KpiIds { get; set; }
    public List<string>? Owners { get; set; }
    public bool? IsResolved { get; set; }
    public List<byte>? SentVia { get; set; }
    public decimal? MinDeviation { get; set; }
    public decimal? MaxDeviation { get; set; }
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "TriggerTime";
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Paginated alert response
/// </summary>
public class PaginatedAlertsDto
{
    public List<AlertLogDto> Alerts { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Alert dashboard summary
/// </summary>
public class AlertDashboardDto
{
    public int TotalAlertsToday { get; set; }
    public int UnresolvedAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int AlertsLastHour { get; set; }
    public decimal AlertTrendPercentage { get; set; }
    public List<AlertLogDto> RecentAlerts { get; set; } = new();
    public List<KpiAlertSummaryDto> TopAlertingKpis { get; set; } = new();
    public List<AlertTrendDto> HourlyTrend { get; set; } = new();
}

/// <summary>
/// Manual alert request
/// </summary>
public class ManualAlertRequest
{
    [Required]
    public int KpiId { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Details { get; set; }
    
    [Range(1, 3)]
    public byte Priority { get; set; } = 2; // Default to email
    
    public List<int>? SpecificContactIds { get; set; }
}

/// <summary>
/// Alert export request
/// </summary>
public class AlertExportRequest
{
    public AlertFilterDto Filter { get; set; } = new();
    public string Format { get; set; } = "csv"; // csv, excel, json
    public bool IncludeDetails { get; set; } = true;
}
