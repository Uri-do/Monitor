using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Services;
using MonitoringGrid.Core.Specifications;
using MonitoringGrid.Core.ValueObjects;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Advanced analytics and reporting API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly KpiDomainService _kpiDomainService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        KpiDomainService kpiDomainService,
        ILogger<AnalyticsController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _kpiDomainService = kpiDomainService;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive system analytics
    /// </summary>
    [HttpGet("system")]
    public async Task<ActionResult<SystemAnalyticsDto>> GetSystemAnalytics([FromQuery] int days = 30)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var alertRepository = _unitOfWork.Repository<AlertLog>();
            var historicalRepository = _unitOfWork.Repository<HistoricalData>();

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);

            var kpis = (await kpiRepository.GetAllAsync()).ToList();
            var alerts = (await alertRepository.GetAllAsync())
                .Where(a => a.TriggerTime >= startDate)
                .ToList();
            var historicalData = (await historicalRepository.GetAllAsync())
                .Where(h => h.Timestamp >= startDate)
                .ToList();

            var analytics = new SystemAnalyticsDto
            {
                Period = days,
                StartDate = startDate,
                EndDate = endDate,
                TotalKpis = kpis.Count,
                ActiveKpis = kpis.Count(k => k.IsActive),
                InactiveKpis = kpis.Count(k => !k.IsActive),
                TotalExecutions = historicalData.Count,
                TotalAlerts = alerts.Count,
                ResolvedAlerts = alerts.Count(a => a.IsResolved),
                UnresolvedAlerts = alerts.Count(a => !a.IsResolved),
                CriticalAlerts = alerts.Count(a => a.DeviationPercent.HasValue && 
                    Math.Abs(a.DeviationPercent.Value) >= 50),
                AverageExecutionsPerDay = (double)historicalData.Count / Math.Max(days, 1),
                AverageAlertsPerDay = (double)alerts.Count / Math.Max(days, 1),
                TopPerformingKpis = GetTopPerformingKpis(kpis, historicalData, alerts),
                WorstPerformingKpis = GetWorstPerformingKpis(kpis, historicalData, alerts),
                AlertTrends = GetAlertTrends(alerts, days),
                ExecutionTrends = GetExecutionTrends(historicalData, days),
                KpiHealthDistribution = GetKpiHealthDistribution(kpis, alerts, historicalData)
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system analytics");
            return StatusCode(500, "An error occurred while retrieving system analytics");
        }
    }

    /// <summary>
    /// Get KPI performance analytics
    /// </summary>
    [HttpGet("kpi/{id}/performance")]
    public async Task<ActionResult<KpiPerformanceAnalyticsDto>> GetKpiPerformanceAnalytics(
        int id, [FromQuery] int days = 30)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdAsync(id);
            
            if (kpi == null)
                return NotFound($"KPI with ID {id} not found");

            var statistics = await _kpiDomainService.GetKpiStatisticsAsync(id, days);
            
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);

            var alertRepository = _unitOfWork.Repository<AlertLog>();
            var historicalRepository = _unitOfWork.Repository<HistoricalData>();

            var alerts = (await alertRepository.GetAllAsync())
                .Where(a => a.KpiId == id && a.TriggerTime >= startDate)
                .ToList();
            
            var historicalData = (await historicalRepository.GetAllAsync())
                .Where(h => h.KpiId == id && h.Timestamp >= startDate)
                .OrderBy(h => h.Timestamp)
                .ToList();

            var analytics = new KpiPerformanceAnalyticsDto
            {
                KpiId = id,
                Indicator = kpi.Indicator,
                Owner = kpi.Owner,
                Period = days,
                StartDate = startDate,
                EndDate = endDate,
                TotalExecutions = historicalData.Count,
                SuccessfulExecutions = historicalData.Count(h => h.IsSuccessful),
                FailedExecutions = historicalData.Count(h => !h.IsSuccessful),
                SuccessRate = historicalData.Count > 0 
                    ? (double)historicalData.Count(h => h.IsSuccessful) / historicalData.Count * 100 
                    : 0,
                TotalAlerts = alerts.Count,
                CriticalAlerts = alerts.Count(a => a.DeviationPercent.HasValue && 
                    Math.Abs(a.DeviationPercent.Value) >= 50),
                AverageDeviation = (double)historicalData.Where(h => h.DeviationPercent.HasValue)
                    .Select(h => Math.Abs(h.DeviationPercent!.Value))
                    .DefaultIfEmpty(0)
                    .Average(),
                MaxDeviation = (double)historicalData.Where(h => h.DeviationPercent.HasValue)
                    .Select(h => Math.Abs(h.DeviationPercent!.Value))
                    .DefaultIfEmpty(0)
                    .Max(),
                AverageExecutionTime = (double)historicalData.Where(h => h.ExecutionTimeMs.HasValue)
                    .Select(h => h.ExecutionTimeMs!.Value)
                    .DefaultIfEmpty(0)
                    .Average(),
                TrendDirection = CalculateTrendDirection(historicalData),
                PerformanceScore = CalculatePerformanceScore(kpi, alerts, historicalData),
                Recommendations = GenerateRecommendations(kpi, alerts, historicalData),
                DetailedTrends = historicalData.Select(h => new KpiTrendPointDto
                {
                    Timestamp = h.Timestamp,
                    Value = h.Value,
                    DeviationPercent = h.DeviationPercent ?? 0,
                    ExecutionTimeMs = h.ExecutionTimeMs ?? 0,
                    IsSuccessful = h.IsSuccessful,
                    TriggeredAlert = alerts.Any(a => Math.Abs((a.TriggerTime - h.Timestamp).TotalMinutes) < 5)
                }).ToList()
            };

            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI performance analytics for ID: {KpiId}", id);
            return StatusCode(500, "An error occurred while retrieving KPI performance analytics");
        }
    }

    /// <summary>
    /// Get owner-based analytics
    /// </summary>
    [HttpGet("owners")]
    public async Task<ActionResult<List<OwnerAnalyticsDto>>> GetOwnerAnalytics([FromQuery] int days = 30)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var alertRepository = _unitOfWork.Repository<AlertLog>();
            var historicalRepository = _unitOfWork.Repository<HistoricalData>();

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);

            var kpis = (await kpiRepository.GetAllAsync()).ToList();
            var alerts = (await alertRepository.GetAllAsync())
                .Where(a => a.TriggerTime >= startDate)
                .ToList();
            var historicalData = (await historicalRepository.GetAllAsync())
                .Where(h => h.Timestamp >= startDate)
                .ToList();

            var ownerAnalytics = kpis
                .GroupBy(k => k.Owner)
                .Select(g => new OwnerAnalyticsDto
                {
                    Owner = g.Key,
                    OwnerDomain = GetEmailDomain(g.Key),
                    TotalKpis = g.Count(),
                    ActiveKpis = g.Count(k => k.IsActive),
                    InactiveKpis = g.Count(k => !k.IsActive),
                    TotalAlerts = alerts.Count(a => g.Any(k => k.KpiId == a.KpiId)),
                    CriticalAlerts = alerts.Count(a => g.Any(k => k.KpiId == a.KpiId) && 
                        a.DeviationPercent.HasValue && Math.Abs(a.DeviationPercent.Value) >= 50),
                    TotalExecutions = historicalData.Count(h => g.Any(k => k.KpiId == h.KpiId)),
                    SuccessfulExecutions = historicalData.Count(h => g.Any(k => k.KpiId == h.KpiId) && h.IsSuccessful),
                    SuccessRate = historicalData.Count(h => g.Any(k => k.KpiId == h.KpiId)) > 0
                        ? (double)historicalData.Count(h => g.Any(k => k.KpiId == h.KpiId) && h.IsSuccessful) / 
                          historicalData.Count(h => g.Any(k => k.KpiId == h.KpiId)) * 100
                        : 0,
                    AverageDeviation = (double)historicalData
                        .Where(h => g.Any(k => k.KpiId == h.KpiId) && h.DeviationPercent.HasValue)
                        .Select(h => Math.Abs(h.DeviationPercent!.Value))
                        .DefaultIfEmpty(0)
                        .Average(),
                    PerformanceScore = CalculateOwnerPerformanceScore(g.ToList(), alerts, historicalData)
                })
                .OrderByDescending(o => o.PerformanceScore)
                .ToList();

            return Ok(ownerAnalytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving owner analytics");
            return StatusCode(500, "An error occurred while retrieving owner analytics");
        }
    }

    /// <summary>
    /// Get real-time system health
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<SystemHealthDto>> GetSystemHealth()
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var alertRepository = _unitOfWork.Repository<AlertLog>();

            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);
            var lastHour = now.AddHours(-1);

            var kpis = (await kpiRepository.GetAllAsync()).ToList();
            var recentAlerts = (await alertRepository.GetAllAsync())
                .Where(a => a.TriggerTime >= last24Hours)
                .ToList();

            // Calculate due KPIs using specifications
            var dueKpisSpec = new KpisDueForExecutionSpecification();
            var dueKpis = await kpiRepository.GetAsync(dueKpisSpec);

            // Calculate stale KPIs
            var staleKpisSpec = new StaleKpisSpecification(24);
            var staleKpis = await kpiRepository.GetAsync(staleKpisSpec);

            var health = new SystemHealthDto
            {
                Timestamp = now,
                OverallHealthScore = CalculateOverallHealthScore(kpis, recentAlerts),
                TotalKpis = kpis.Count,
                ActiveKpis = kpis.Count(k => k.IsActive),
                HealthyKpis = kpis.Count(k => k.IsActive && !IsKpiInError(k, recentAlerts)),
                WarningKpis = dueKpis.Count(),
                CriticalKpis = staleKpis.Count(),
                AlertsLast24Hours = recentAlerts.Count,
                AlertsLastHour = recentAlerts.Count(a => a.TriggerTime >= lastHour),
                UnresolvedAlerts = recentAlerts.Count(a => !a.IsResolved),
                CriticalAlerts = recentAlerts.Count(a => a.DeviationPercent.HasValue && 
                    Math.Abs(a.DeviationPercent.Value) >= 50),
                SystemStatus = DetermineSystemStatus(kpis, recentAlerts, dueKpis.Count(), staleKpis.Count()),
                Issues = IdentifySystemIssues(kpis, recentAlerts, dueKpis.ToList(), staleKpis.ToList()),
                Recommendations = GenerateSystemRecommendations(kpis, recentAlerts)
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system health");
            return StatusCode(500, "An error occurred while retrieving system health");
        }
    }

    #region Helper Methods

    private List<KpiSummaryDto> GetTopPerformingKpis(List<KPI> kpis, List<HistoricalData> historicalData, List<AlertLog> alerts)
    {
        return kpis
            .Where(k => k.IsActive)
            .Select(k => new KpiSummaryDto
            {
                KpiId = k.KpiId,
                Indicator = k.Indicator,
                Owner = k.Owner,
                PerformanceScore = CalculatePerformanceScore(k, alerts, historicalData)
            })
            .OrderByDescending(k => k.PerformanceScore)
            .Take(5)
            .ToList();
    }

    private List<KpiSummaryDto> GetWorstPerformingKpis(List<KPI> kpis, List<HistoricalData> historicalData, List<AlertLog> alerts)
    {
        return kpis
            .Where(k => k.IsActive)
            .Select(k => new KpiSummaryDto
            {
                KpiId = k.KpiId,
                Indicator = k.Indicator,
                Owner = k.Owner,
                PerformanceScore = CalculatePerformanceScore(k, alerts, historicalData)
            })
            .OrderBy(k => k.PerformanceScore)
            .Take(5)
            .ToList();
    }

    private List<TrendDataDto> GetAlertTrends(List<AlertLog> alerts, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        return Enumerable.Range(0, days)
            .Select(i => endDate.AddDays(-i))
            .Select(date => new TrendDataDto
            {
                Date = date,
                Value = alerts.Count(a => a.TriggerTime.Date == date)
            })
            .OrderBy(t => t.Date)
            .ToList();
    }

    private List<TrendDataDto> GetExecutionTrends(List<HistoricalData> historicalData, int days)
    {
        var endDate = DateTime.UtcNow.Date;
        return Enumerable.Range(0, days)
            .Select(i => endDate.AddDays(-i))
            .Select(date => new TrendDataDto
            {
                Date = date,
                Value = historicalData.Count(h => h.Timestamp.Date == date)
            })
            .OrderBy(t => t.Date)
            .ToList();
    }

    private HealthDistributionDto GetKpiHealthDistribution(List<KPI> kpis, List<AlertLog> alerts, List<HistoricalData> historicalData)
    {
        var activeKpis = kpis.Where(k => k.IsActive).ToList();
        var recentAlerts = alerts.Where(a => a.TriggerTime >= DateTime.UtcNow.AddHours(-24)).ToList();

        return new HealthDistributionDto
        {
            Healthy = activeKpis.Count(k => !IsKpiInError(k, recentAlerts)),
            Warning = activeKpis.Count(k => IsKpiWarning(k, recentAlerts)),
            Critical = activeKpis.Count(k => IsKpiCritical(k, recentAlerts)),
            Inactive = kpis.Count(k => !k.IsActive)
        };
    }

    private string GetEmailDomain(string email)
    {
        try
        {
            var emailAddress = new EmailAddress(email);
            return emailAddress.GetDomain();
        }
        catch
        {
            return "Unknown";
        }
    }

    private double CalculatePerformanceScore(KPI kpi, List<AlertLog> alerts, List<HistoricalData> historicalData)
    {
        var kpiAlerts = alerts.Where(a => a.KpiId == kpi.KpiId).ToList();
        var kpiHistory = historicalData.Where(h => h.KpiId == kpi.KpiId).ToList();

        var score = 100.0;

        // Deduct points for alerts
        score -= kpiAlerts.Count * 5;

        // Deduct points for critical alerts
        score -= kpiAlerts.Count(a => a.DeviationPercent.HasValue && Math.Abs(a.DeviationPercent.Value) >= 50) * 10;

        // Deduct points for failed executions
        score -= kpiHistory.Count(h => !h.IsSuccessful) * 2;

        // Deduct points for high average deviation
        var avgDeviation = kpiHistory.Where(h => h.DeviationPercent.HasValue)
            .Select(h => Math.Abs(h.DeviationPercent!.Value))
            .DefaultIfEmpty(0)
            .Average();
        score -= (double)avgDeviation * 0.5;

        return Math.Max(0, score);
    }

    private double CalculateOwnerPerformanceScore(List<KPI> ownerKpis, List<AlertLog> alerts, List<HistoricalData> historicalData)
    {
        return ownerKpis.Average(k => CalculatePerformanceScore(k, alerts, historicalData));
    }

    private string CalculateTrendDirection(List<HistoricalData> historicalData)
    {
        if (historicalData.Count < 2) return "Stable";

        var recentData = historicalData.TakeLast(10).ToList();
        var olderData = historicalData.Take(Math.Max(1, historicalData.Count - 10)).ToList();

        var recentAvg = recentData.Where(h => h.DeviationPercent.HasValue)
            .Select(h => Math.Abs(h.DeviationPercent!.Value))
            .DefaultIfEmpty(0)
            .Average();

        var olderAvg = olderData.Where(h => h.DeviationPercent.HasValue)
            .Select(h => Math.Abs(h.DeviationPercent!.Value))
            .DefaultIfEmpty(0)
            .Average();

        var difference = recentAvg - olderAvg;
        return difference > 5 ? "Deteriorating" : difference < -5 ? "Improving" : "Stable";
    }

    private List<string> GenerateRecommendations(KPI kpi, List<AlertLog> alerts, List<HistoricalData> historicalData)
    {
        var recommendations = new List<string>();

        var recentAlerts = alerts.Where(a => a.TriggerTime >= DateTime.UtcNow.AddDays(-7)).ToList();
        if (recentAlerts.Count > 5)
        {
            recommendations.Add("Consider adjusting deviation threshold - frequent alerts detected");
        }

        var failedExecutions = historicalData.Count(h => !h.IsSuccessful);
        if (failedExecutions > historicalData.Count * 0.1)
        {
            recommendations.Add("Review stored procedure reliability - high failure rate detected");
        }

        var avgExecutionTime = historicalData.Where(h => h.ExecutionTimeMs.HasValue)
            .Select(h => h.ExecutionTimeMs!.Value)
            .DefaultIfEmpty(0)
            .Average();
        if (avgExecutionTime > 30000) // 30 seconds
        {
            recommendations.Add("Optimize stored procedure performance - slow execution detected");
        }

        if (kpi.CooldownMinutes < 5)
        {
            recommendations.Add("Consider increasing cooldown period to reduce alert spam");
        }

        return recommendations;
    }

    private double CalculateOverallHealthScore(List<KPI> kpis, List<AlertLog> recentAlerts)
    {
        var activeKpis = kpis.Where(k => k.IsActive).ToList();
        if (!activeKpis.Any()) return 0;

        var healthyKpis = activeKpis.Count(k => !IsKpiInError(k, recentAlerts));
        return (double)healthyKpis / activeKpis.Count * 100;
    }

    private bool IsKpiInError(KPI kpi, List<AlertLog> recentAlerts)
    {
        return recentAlerts.Any(a => a.KpiId == kpi.KpiId && !a.IsResolved);
    }

    private bool IsKpiWarning(KPI kpi, List<AlertLog> recentAlerts)
    {
        var lastRun = kpi.LastRun ?? DateTime.MinValue;
        var nextDue = lastRun.AddMinutes(kpi.Frequency);
        return DateTime.UtcNow > nextDue && DateTime.UtcNow <= nextDue.AddMinutes(kpi.Frequency);
    }

    private bool IsKpiCritical(KPI kpi, List<AlertLog> recentAlerts)
    {
        var lastRun = kpi.LastRun ?? DateTime.MinValue;
        var nextDue = lastRun.AddMinutes(kpi.Frequency);
        return DateTime.UtcNow > nextDue.AddMinutes(kpi.Frequency);
    }

    private string DetermineSystemStatus(List<KPI> kpis, List<AlertLog> recentAlerts, int warningCount, int criticalCount)
    {
        var healthScore = CalculateOverallHealthScore(kpis, recentAlerts);
        
        if (criticalCount > 0 || healthScore < 70) return "Critical";
        if (warningCount > 0 || healthScore < 90) return "Warning";
        return "Healthy";
    }

    private List<string> IdentifySystemIssues(List<KPI> kpis, List<AlertLog> recentAlerts, List<KPI> dueKpis, List<KPI> staleKpis)
    {
        var issues = new List<string>();

        if (staleKpis.Any())
            issues.Add($"{staleKpis.Count} KPIs are severely overdue for execution");

        if (dueKpis.Any())
            issues.Add($"{dueKpis.Count} KPIs are due for execution");

        var unresolvedAlerts = recentAlerts.Count(a => !a.IsResolved);
        if (unresolvedAlerts > 0)
            issues.Add($"{unresolvedAlerts} unresolved alerts require attention");

        var criticalAlerts = recentAlerts.Count(a => a.DeviationPercent.HasValue && Math.Abs(a.DeviationPercent.Value) >= 50);
        if (criticalAlerts > 0)
            issues.Add($"{criticalAlerts} critical alerts detected in the last 24 hours");

        return issues;
    }

    private List<string> GenerateSystemRecommendations(List<KPI> kpis, List<AlertLog> recentAlerts)
    {
        var recommendations = new List<string>();

        var inactiveKpis = kpis.Count(k => !k.IsActive);
        if (inactiveKpis > 0)
            recommendations.Add($"Review {inactiveKpis} inactive KPIs for potential reactivation or cleanup");

        var frequentAlerters = recentAlerts
            .GroupBy(a => a.KpiId)
            .Where(g => g.Count() > 10)
            .ToList();
        
        if (frequentAlerters.Any())
            recommendations.Add($"Review {frequentAlerters.Count} KPIs with frequent alerts for threshold adjustments");

        var unresolvedCount = recentAlerts.Count(a => !a.IsResolved);
        if (unresolvedCount > 5)
            recommendations.Add("Implement alert resolution workflow to reduce unresolved alert backlog");

        return recommendations;
    }

    #endregion
}

#region Analytics DTOs

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
    public List<KpiSummaryDto> TopPerformingKpis { get; set; } = new();
    public List<KpiSummaryDto> WorstPerformingKpis { get; set; } = new();
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
/// System health DTO
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
/// KPI summary DTO
/// </summary>
public class KpiSummaryDto
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

#endregion
