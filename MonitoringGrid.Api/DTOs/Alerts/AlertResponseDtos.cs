using MonitoringGrid.Api.DTOs.Common;

namespace MonitoringGrid.Api.DTOs.Alerts;

/// <summary>
/// Enhanced alert response DTO
/// </summary>
public class AlertResponse
{
    /// <summary>
    /// Alert ID
    /// </summary>
    public int AlertId { get; set; }

    /// <summary>
    /// Indicator ID that triggered the alert
    /// </summary>
    public long IndicatorId { get; set; }

    /// <summary>
    /// Indicator name
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Indicator owner
    /// </summary>
    public string IndicatorOwner { get; set; } = string.Empty;

    /// <summary>
    /// When the alert was triggered
    /// </summary>
    public DateTime TriggerTime { get; set; }

    /// <summary>
    /// Alert message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional alert details
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// How the alert was sent
    /// </summary>
    public string? SentVia { get; set; }

    /// <summary>
    /// Who the alert was sent to
    /// </summary>
    public string? SentTo { get; set; }

    /// <summary>
    /// Current value that triggered the alert
    /// </summary>
    public decimal? CurrentValue { get; set; }

    /// <summary>
    /// Historical value for comparison
    /// </summary>
    public decimal? HistoricalValue { get; set; }

    /// <summary>
    /// Deviation percentage
    /// </summary>
    public decimal? DeviationPercent { get; set; }

    /// <summary>
    /// Whether the alert is resolved
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// When the alert was resolved
    /// </summary>
    public DateTime? ResolvedTime { get; set; }

    /// <summary>
    /// Who resolved the alert
    /// </summary>
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Resolution notes
    /// </summary>
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Resolution category
    /// </summary>
    public string? ResolutionCategory { get; set; }

    /// <summary>
    /// Alert severity level
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Time since alert was triggered
    /// </summary>
    public TimeSpan TimeSinceTriggered { get; set; }

    /// <summary>
    /// Time to resolution (if resolved)
    /// </summary>
    public TimeSpan? TimeToResolution { get; set; }

    /// <summary>
    /// Indicator information (if requested)
    /// </summary>
    public AlertIndicatorInfo? Indicator { get; set; }

    /// <summary>
    /// Additional details (if requested)
    /// </summary>
    public Dictionary<string, object>? AdditionalDetails { get; set; }
}

/// <summary>
/// Alert indicator information
/// </summary>
public class AlertIndicatorInfo
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
    /// Whether the indicator is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Last run time
    /// </summary>
    public DateTime? LastRun { get; set; }
}

/// <summary>
/// Paginated alerts response
/// </summary>
public class PaginatedAlertsResponse
{
    /// <summary>
    /// List of alerts
    /// </summary>
    public List<AlertResponse> Alerts { get; set; } = new();

    /// <summary>
    /// Total count of alerts (before pagination)
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
    /// Alert summary statistics
    /// </summary>
    public AlertSummary Summary { get; set; } = new();

    /// <summary>
    /// Query performance metrics
    /// </summary>
    public QueryMetrics QueryMetrics { get; set; } = new();
}

/// <summary>
/// Alert summary statistics
/// </summary>
public class AlertSummary
{
    /// <summary>
    /// Total alerts in current filter
    /// </summary>
    public int TotalAlerts { get; set; }

    /// <summary>
    /// Unresolved alerts count
    /// </summary>
    public int UnresolvedAlerts { get; set; }

    /// <summary>
    /// Resolved alerts count
    /// </summary>
    public int ResolvedAlerts { get; set; }

    /// <summary>
    /// Critical alerts count
    /// </summary>
    public int CriticalAlerts { get; set; }

    /// <summary>
    /// High severity alerts count
    /// </summary>
    public int HighAlerts { get; set; }

    /// <summary>
    /// Medium severity alerts count
    /// </summary>
    public int MediumAlerts { get; set; }

    /// <summary>
    /// Low severity alerts count
    /// </summary>
    public int LowAlerts { get; set; }

    /// <summary>
    /// Average time to resolution
    /// </summary>
    public TimeSpan? AverageTimeToResolution { get; set; }
}

/// <summary>
/// Enhanced alert dashboard response
/// </summary>
public class AlertDashboardResponse
{
    /// <summary>
    /// Total alerts today
    /// </summary>
    public int TotalAlertsToday { get; set; }

    /// <summary>
    /// Unresolved alerts count
    /// </summary>
    public int UnresolvedAlerts { get; set; }

    /// <summary>
    /// Critical alerts count
    /// </summary>
    public int CriticalAlerts { get; set; }

    /// <summary>
    /// Alerts in the last hour
    /// </summary>
    public int AlertsLastHour { get; set; }

    /// <summary>
    /// Alert trend percentage (compared to previous period)
    /// </summary>
    public decimal AlertTrendPercentage { get; set; }

    /// <summary>
    /// Hourly trend data
    /// </summary>
    public List<AlertTrendData> HourlyTrend { get; set; } = new();

    /// <summary>
    /// Top indicators by alert count
    /// </summary>
    public List<IndicatorAlertCount> TopAlertingIndicators { get; set; } = new();

    /// <summary>
    /// Recent critical alerts
    /// </summary>
    public List<AlertResponse> RecentCriticalAlerts { get; set; } = new();

    /// <summary>
    /// System health score based on alerts
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
/// Alert trend data point
/// </summary>
public class AlertTrendData
{
    /// <summary>
    /// Date/time of the data point
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total alert count
    /// </summary>
    public int AlertCount { get; set; }

    /// <summary>
    /// Critical alerts count
    /// </summary>
    public int CriticalCount { get; set; }

    /// <summary>
    /// High severity alerts count
    /// </summary>
    public int HighCount { get; set; }

    /// <summary>
    /// Medium severity alerts count
    /// </summary>
    public int MediumCount { get; set; }

    /// <summary>
    /// Low severity alerts count
    /// </summary>
    public int LowCount { get; set; }
}

/// <summary>
/// Indicator alert count
/// </summary>
public class IndicatorAlertCount
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
    /// Alert count
    /// </summary>
    public int AlertCount { get; set; }

    /// <summary>
    /// Unresolved alert count
    /// </summary>
    public int UnresolvedCount { get; set; }
}

/// <summary>
/// Alert operation response
/// </summary>
public class AlertOperationResponse
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
    /// Alert ID(s) involved in the operation
    /// </summary>
    public List<int> AlertIds { get; set; } = new();

    /// <summary>
    /// Operation timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Operation duration in milliseconds
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Number of alerts processed
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// Number of successful operations
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed operations
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Additional operation details
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }

    /// <summary>
    /// Error code if operation failed
    /// </summary>
    public string? ErrorCode { get; set; }
}


