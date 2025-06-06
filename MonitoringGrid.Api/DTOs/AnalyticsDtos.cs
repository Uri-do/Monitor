using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.DTOs;

/// <summary>
/// KPI performance summary for analytics
/// </summary>
public class KpiPerformanceSummaryDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public double PerformanceScore { get; set; }
}

/// <summary>
/// Trend data DTO
/// </summary>
public class TrendDataDto
{
    public DateTime Date { get; set; }
    public int Value { get; set; }
}

/// <summary>
/// Health distribution DTO
/// </summary>
public class HealthDistributionDto
{
    public int Healthy { get; set; }
    public int Warning { get; set; }
    public int Critical { get; set; }
    public int Inactive { get; set; }
}

/// <summary>
/// System health overview
/// </summary>
public class SystemHealthDto
{
    public DateTime Timestamp { get; set; }
    public double OverallHealthScore { get; set; }
    public int TotalKpis { get; set; }
    public int ActiveKpis { get; set; }
    public int HealthyKpis { get; set; }
    public int WarningKpis { get; set; }
    public int CriticalKpis { get; set; }
    public int AlertsLast24Hours { get; set; }
    public int AlertsLastHour { get; set; }
    public int UnresolvedAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public string SystemStatus { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Performance analytics DTO
/// </summary>
public class PerformanceAnalyticsDto
{
    public double AverageExecutionTime { get; set; }
    public double AverageDeviation { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public List<TrendDataDto> ExecutionTrends { get; set; } = new();
    public List<TrendDataDto> DeviationTrends { get; set; } = new();
    public List<KpiPerformanceSummaryDto> TopPerformers { get; set; } = new();
    public List<KpiPerformanceSummaryDto> WorstPerformers { get; set; } = new();
}

/// <summary>
/// Alert analytics DTO
/// </summary>
public class AlertAnalyticsDto
{
    public int TotalAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int UnresolvedAlerts { get; set; }
    public double AverageResolutionTime { get; set; }
    public List<TrendDataDto> AlertTrends { get; set; } = new();
    public List<TrendDataDto> ResolutionTrends { get; set; } = new();
    public Dictionary<string, int> AlertsByPriority { get; set; } = new();
    public Dictionary<string, int> AlertsByOwner { get; set; } = new();
}

/// <summary>
/// System analytics DTO
/// </summary>
public class SystemAnalyticsDto
{
    public int Period { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalKpis { get; set; }
    public int ActiveKpis { get; set; }
    public int InactiveKpis { get; set; }
    public int TotalExecutions { get; set; }
    public int TotalAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int UnresolvedAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public double AverageExecutionsPerDay { get; set; }
    public double AverageAlertsPerDay { get; set; }
    public List<KpiPerformanceSummaryDto> TopPerformingKpis { get; set; } = new();
    public List<KpiPerformanceSummaryDto> WorstPerformingKpis { get; set; } = new();
    public List<TrendDataDto> AlertTrends { get; set; } = new();
    public List<TrendDataDto> ExecutionTrends { get; set; } = new();
    public HealthDistributionDto KpiHealthDistribution { get; set; } = new();
}

/// <summary>
/// KPI performance analytics DTO
/// </summary>
public class KpiPerformanceAnalyticsDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int Period { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public double AverageDeviation { get; set; }
    public double MaxDeviation { get; set; }
    public double AverageExecutionTime { get; set; }
    public string TrendDirection { get; set; } = string.Empty;
    public double PerformanceScore { get; set; }
    public List<string> Recommendations { get; set; } = new();
    public List<KpiTrendPointDto> DetailedTrends { get; set; } = new();
}

/// <summary>
/// Owner analytics DTO
/// </summary>
public class OwnerAnalyticsDto
{
    public string Owner { get; set; } = string.Empty;
    public string OwnerDomain { get; set; } = string.Empty;
    public int TotalKpis { get; set; }
    public int ActiveKpis { get; set; }
    public int InactiveKpis { get; set; }
    public int TotalAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDeviation { get; set; }
    public double PerformanceScore { get; set; }
}

/// <summary>
/// KPI trend point DTO
/// </summary>
public class KpiTrendPointDto
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public decimal DeviationPercent { get; set; }
    public int ExecutionTimeMs { get; set; }
    public bool IsSuccessful { get; set; }
    public bool TriggeredAlert { get; set; }
}
