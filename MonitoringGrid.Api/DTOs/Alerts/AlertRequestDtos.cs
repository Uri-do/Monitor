using System.ComponentModel.DataAnnotations;
using MonitoringGrid.Api.Validation;

namespace MonitoringGrid.Api.DTOs.Alerts;

/// <summary>
/// Request DTO for getting alerts with filtering and pagination
/// </summary>
public class GetAlertsRequest
{
    /// <summary>
    /// Filter by resolution status
    /// </summary>
    [BooleanFlag]
    public bool? IsResolved { get; set; }

    /// <summary>
    /// Search text for filtering alerts
    /// </summary>
    [SearchTerm(0, 200)]
    public string? SearchText { get; set; }

    /// <summary>
    /// Start date for filtering alerts
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for filtering alerts
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    [PositiveInteger]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    [PageSize(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field
    /// </summary>
    [SearchTerm(0, 50)]
    public string? SortBy { get; set; } = "triggerTime";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    [SearchTerm(0, 10)]
    public string? SortDirection { get; set; } = "desc";

    /// <summary>
    /// Filter by severity level
    /// </summary>
    [SearchTerm(0, 20)]
    public string? Severity { get; set; }

    /// <summary>
    /// Filter by indicator ID
    /// </summary>
    [PositiveInteger]
    public long? IndicatorId { get; set; }

    /// <summary>
    /// Include detailed information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = false;
}

/// <summary>
/// Request DTO for getting alert by ID
/// </summary>
public class GetAlertRequest
{
    /// <summary>
    /// Alert ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public int AlertId { get; set; }

    /// <summary>
    /// Include detailed information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Include related indicator information
    /// </summary>
    [BooleanFlag]
    public bool IncludeIndicator { get; set; } = true;

    /// <summary>
    /// Include resolution history
    /// </summary>
    [BooleanFlag]
    public bool IncludeHistory { get; set; } = false;
}

/// <summary>
/// Request DTO for resolving an alert
/// </summary>
public class ResolveAlertRequest
{
    /// <summary>
    /// Alert ID to resolve
    /// </summary>
    [Required]
    [PositiveInteger]
    public int AlertId { get; set; }

    /// <summary>
    /// User resolving the alert
    /// </summary>
    [Required]
    [SearchTerm(1, 100)]
    public string ResolvedBy { get; set; } = string.Empty;

    /// <summary>
    /// Resolution notes
    /// </summary>
    [SearchTerm(0, 500)]
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Resolution category
    /// </summary>
    [SearchTerm(0, 50)]
    public string? ResolutionCategory { get; set; }
}

/// <summary>
/// Request DTO for bulk resolving alerts
/// </summary>
public class BulkResolveAlertsRequest
{
    /// <summary>
    /// List of alert IDs to resolve
    /// </summary>
    [Required]
    public List<int> AlertIds { get; set; } = new();

    /// <summary>
    /// User resolving the alerts
    /// </summary>
    [Required]
    [SearchTerm(1, 100)]
    public string ResolvedBy { get; set; } = string.Empty;

    /// <summary>
    /// Resolution notes for all alerts
    /// </summary>
    [SearchTerm(0, 500)]
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Resolution category for all alerts
    /// </summary>
    [SearchTerm(0, 50)]
    public string? ResolutionCategory { get; set; }

    /// <summary>
    /// Continue processing even if some alerts fail to resolve
    /// </summary>
    [BooleanFlag]
    public bool ContinueOnError { get; set; } = true;
}

/// <summary>
/// Request DTO for getting alert dashboard
/// </summary>
public class GetAlertDashboardRequest
{
    /// <summary>
    /// Include hourly trend data
    /// </summary>
    [BooleanFlag]
    public bool IncludeTrend { get; set; } = true;

    /// <summary>
    /// Number of hours for trend analysis
    /// </summary>
    [Range(1, 168)] // 1 hour to 1 week
    public int TrendHours { get; set; } = 24;

    /// <summary>
    /// Include detailed statistics
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = false;

    /// <summary>
    /// Refresh cache data
    /// </summary>
    [BooleanFlag]
    public bool RefreshCache { get; set; } = false;
}

/// <summary>
/// Request DTO for alert analytics
/// </summary>
public class GetAlertAnalyticsRequest
{
    /// <summary>
    /// Start date for analytics period
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for analytics period
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Number of days to analyze (used when dates not specified)
    /// </summary>
    [Range(1, 365)]
    public int Days { get; set; } = 30;

    /// <summary>
    /// Group analytics by time period
    /// </summary>
    public AnalyticsGroupBy? GroupBy { get; set; }

    /// <summary>
    /// Include trend analysis
    /// </summary>
    [BooleanFlag]
    public bool IncludeTrends { get; set; } = true;

    /// <summary>
    /// Include indicator breakdown
    /// </summary>
    [BooleanFlag]
    public bool IncludeIndicatorBreakdown { get; set; } = false;
}

/// <summary>
/// Enumeration for analytics grouping options
/// </summary>
public enum AnalyticsGroupBy
{
    Hour,
    Day,
    Week,
    Month
}
