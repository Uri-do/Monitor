using MediatR;
using MonitoringGrid.Api.CQRS.Queries;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Queries.Kpi;

/// <summary>
/// Optimized query for getting KPIs with projections and pagination
/// </summary>
public class GetKpisOptimizedQuery : IRequest<PagedResult<KpiListItemDto>>
{
    public bool? IsActive { get; set; }
    public string? Owner { get; set; }
    public byte? Priority { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "Indicator";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// Lightweight KPI summary DTO for list views
/// </summary>
public class KpiListItemDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public byte Priority { get; set; }
    public int Frequency { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastRun { get; set; }
    public string? KpiType { get; set; }
    public decimal Deviation { get; set; }
    public int? CooldownMinutes { get; set; }
}

/// <summary>
/// Query for getting KPI dashboard data with aggregations
/// </summary>
public class GetKpiDashboardOptimizedQuery : IRequest<KpiDashboardOptimizedDto>
{
    public int Days { get; set; } = 30;
}

/// <summary>
/// Optimized dashboard DTO with aggregated data
/// </summary>
public class KpiDashboardOptimizedDto
{
    public int TotalKpis { get; set; }
    public int ActiveKpis { get; set; }
    public int InactiveKpis { get; set; }
    public int KpisWithRecentAlerts { get; set; }
    public DateTime? LastExecutionTime { get; set; }
    public string SystemHealth { get; set; } = "Unknown";
    public List<KpiExecutionSummaryDto> RecentExecutions { get; set; } = new();
    public KpiPerformanceMetricsDto PerformanceMetrics { get; set; } = new();
    public List<KpiTrendDto> ExecutionTrend { get; set; } = new();
}

/// <summary>
/// KPI execution summary for dashboard
/// </summary>
public class KpiExecutionSummaryDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; }
    public bool Success { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? DeviationPercent { get; set; }
    public int ExecutionTimeMs { get; set; }
}

/// <summary>
/// KPI performance metrics
/// </summary>
public class KpiPerformanceMetricsDto
{
    public double AverageExecutionTimeMs { get; set; }
    public int TotalExecutionsToday { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public int AlertsTriggered { get; set; }
}

/// <summary>
/// KPI execution trend data
/// </summary>
public class KpiTrendDto
{
    public DateTime Date { get; set; }
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public double AverageExecutionTime { get; set; }
    public int AlertCount { get; set; }
}

/// <summary>
/// Query for getting KPI execution history with optimized projections
/// </summary>
public class GetKpiExecutionHistoryOptimizedQuery : IRequest<PagedResult<KpiExecutionHistoryDto>>
{
    public int KpiId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? SuccessOnly { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Optimized KPI execution history DTO
/// </summary>
public class KpiExecutionHistoryDto
{
    public int HistoricalId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public decimal? HistoricalValue { get; set; }
    public decimal? DeviationPercent { get; set; }
    public bool IsSuccessful { get; set; }
    public bool ShouldAlert { get; set; }
    public int? ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Query for getting KPI statistics and analytics
/// </summary>
public class GetKpiAnalyticsQuery : IRequest<KpiAnalyticsDto>
{
    public int KpiId { get; set; }
    public int Days { get; set; } = 30;
}

/// <summary>
/// KPI analytics DTO with statistical data
/// </summary>
public class KpiAnalyticsDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double AverageValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public double StandardDeviation { get; set; }
    public int AlertsTriggered { get; set; }
    public double AverageExecutionTime { get; set; }
    public List<KpiValueTrendDto> ValueTrend { get; set; } = new();
    public List<KpiPerformanceTrendDto> PerformanceTrend { get; set; } = new();
}

/// <summary>
/// KPI value trend data
/// </summary>
public class KpiValueTrendDto
{
    public DateTime Date { get; set; }
    public decimal AverageValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public int ExecutionCount { get; set; }
}

/// <summary>
/// KPI performance trend data
/// </summary>
public class KpiPerformanceTrendDto
{
    public DateTime Date { get; set; }
    public double AverageExecutionTime { get; set; }
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

/// <summary>
/// Query for getting top performing/problematic KPIs
/// </summary>
public class GetTopKpisQuery : IRequest<TopKpisDto>
{
    public int Count { get; set; } = 10;
    public int Days { get; set; } = 7;
    public TopKpiType Type { get; set; } = TopKpiType.MostProblematic;
}

/// <summary>
/// Type of top KPIs to retrieve
/// </summary>
public enum TopKpiType
{
    MostProblematic,
    BestPerforming,
    MostFrequent,
    Slowest,
    Fastest
}

/// <summary>
/// Top KPIs DTO
/// </summary>
public class TopKpisDto
{
    public TopKpiType Type { get; set; }
    public List<TopKpiItemDto> Items { get; set; } = new();
}

/// <summary>
/// Individual top KPI item
/// </summary>
public class TopKpiItemDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public double Score { get; set; }
    public string ScoreDescription { get; set; } = string.Empty;
    public int ExecutionCount { get; set; }
    public double SuccessRate { get; set; }
    public double AverageExecutionTime { get; set; }
    public int AlertCount { get; set; }
}
