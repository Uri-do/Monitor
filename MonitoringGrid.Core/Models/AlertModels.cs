namespace MonitoringGrid.Core.Models;

/// <summary>
/// Filter criteria for alert queries
/// </summary>
public class AlertFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<long>? IndicatorIds { get; set; }
    public List<string>? Owners { get; set; }
    public bool? IsResolved { get; set; }
    public List<string>? SentVia { get; set; }
    public decimal? MinDeviation { get; set; }
    public decimal? MaxDeviation { get; set; }
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "TriggerTime";
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Paginated alert results
/// </summary>
public class PaginatedAlerts<T>
{
    public List<T> Alerts { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// Alert statistics model
/// </summary>
public class AlertStatistics
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
    public List<AlertTrend> DailyTrend { get; set; } = new();
    public List<IndicatorAlertSummary> TopAlertingIndicators { get; set; } = new();
}

/// <summary>
/// Alert dashboard model
/// </summary>
public class AlertDashboard
{
    public int TotalAlertsToday { get; set; }
    public int UnresolvedAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int AlertsLastHour { get; set; }
    public decimal AlertTrendPercentage { get; set; }
    public List<AlertTrend> HourlyTrend { get; set; } = new();
}

/// <summary>
/// Alert trend data
/// </summary>
public class AlertTrend
{
    public DateTime Date { get; set; }
    public int AlertCount { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
}

/// <summary>
/// Indicator alert summary
/// </summary>
public class IndicatorAlertSummary
{
    public long IndicatorId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int AlertCount { get; set; }
    public int UnresolvedCount { get; set; }
    public DateTime LastAlert { get; set; }
    public decimal AverageDeviation { get; set; }
}
