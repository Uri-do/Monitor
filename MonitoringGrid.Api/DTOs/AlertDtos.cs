using System.ComponentModel.DataAnnotations;
using MonitoringGrid.Core.DTOs;

namespace MonitoringGrid.Api.DTOs;

// Note: AlertLogDto, AlertFilterDto, PaginatedAlertsDto, AlertStatisticsDto,
// AlertDashboardDto, AlertTrendDto, and IndicatorAlertSummaryDto are now
// imported from MonitoringGrid.Core.DTOs to avoid duplication

/// <summary>
/// Enhanced Alert DTO with value object insights
/// </summary>
public class EnhancedAlertDto : Core.DTOs.AlertLogDto
{
    public string SeverityColor { get; set; } = string.Empty;
    public bool RequiresImmediateAttention { get; set; }
    public string DeviationCategory { get; set; } = string.Empty;
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

// Removed duplicate DTOs - using Core.DTOs versions instead:
// - AlertStatisticsDto
// - AlertTrendDto
// - IndicatorAlertSummaryDto
// - AlertFilterDto
// - PaginatedAlertsDto

/// <summary>
/// Alert severity statistics
/// </summary>
public class AlertSeverityStatsDto
{
    public string Severity { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

// Removed AlertDashboardDto - using Core.DTOs version instead

/// <summary>
/// Manual alert request
/// </summary>
public class ManualAlertRequest
{
    [Required]
    public long IndicatorId { get; set; }
    
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
    public Core.DTOs.AlertFilterDto Filter { get; set; } = new();
    public string Format { get; set; } = "csv"; // csv, excel, json
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// DTO for real-time alert notifications
/// </summary>
public class AlertNotificationDto
{
    public int AlertId { get; set; }
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int Priority { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal HistoricalValue { get; set; }
    public decimal Deviation { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime TriggerTime { get; set; }
    public string Severity { get; set; } = string.Empty;
    public List<string> NotifiedContacts { get; set; } = new();
}

/// <summary>
/// DTO for real-time Indicator status updates
/// </summary>
public class IndicatorStatusUpdateDto
{
    public long IndicatorId { get; set; }
    public long IndicatorID { get; set; } // For backward compatibility
    public string IndicatorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastRun { get; set; }
    public DateTime? NextRun { get; set; }
    public decimal? LastValue { get; set; }
    public decimal? LastDeviation { get; set; }
    public bool IsSignificantChange { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentlyRunning { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ChangeReason { get; set; }
}

/// <summary>
/// DTO for real-time dashboard updates
/// </summary>
public class DashboardUpdateDto
{
    public int TotalIndicators { get; set; }
    public int ActiveIndicators { get; set; }
    public int IndicatorsInError { get; set; }
    public int IndicatorsDue { get; set; }
    public int AlertsToday { get; set; }
    public int AlertsThisWeek { get; set; }
    public DateTime LastUpdate { get; set; }
    public List<RecentAlertDto> RecentAlerts { get; set; } = new();
}

/// <summary>
/// DTO for recent alerts in dashboard
/// </summary>
public class RecentAlertDto
{
    public long IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public DateTime TriggerTime { get; set; }
    public decimal Deviation { get; set; }
    public string Severity { get; set; } = string.Empty;
}

/// <summary>
/// DTO for system status updates
/// </summary>
public class SystemStatusDto
{
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; }
    public int ProcessedIndicators { get; set; }
    public int AlertsSent { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}
