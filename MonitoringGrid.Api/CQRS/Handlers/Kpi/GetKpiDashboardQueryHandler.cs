using AutoMapper;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.CQRS.Queries;
using MonitoringGrid.Api.CQRS.Queries.Kpi;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Observability;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Kpi;

/// <summary>
/// Handler for getting KPI dashboard data
/// </summary>
public class GetKpiDashboardQueryHandler : IQueryHandler<GetKpiDashboardQuery, KpiDashboardDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly MetricsService _metricsService;
    private readonly ILogger<GetKpiDashboardQueryHandler> _logger;

    public GetKpiDashboardQueryHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        MetricsService metricsService,
        ILogger<GetKpiDashboardQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _metricsService = metricsService;
        _logger = logger;
    }

    public async Task<Result<KpiDashboardDto>> Handle(GetKpiDashboardQuery request, CancellationToken cancellationToken)
    {
        using var activity = ApiActivitySource.StartDashboardAggregation("KpiDashboard");
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("Getting KPI dashboard data");

        var now = DateTime.UtcNow;
        var today = now.Date;

        var kpiRepository = _unitOfWork.Repository<KPI>();
        var alertRepository = _unitOfWork.Repository<AlertLog>();
        var historicalRepository = _unitOfWork.Repository<HistoricalData>();

        var kpis = (await kpiRepository.GetAllAsync(cancellationToken)).ToList();
        var allAlerts = (await alertRepository.GetAllAsync(cancellationToken)).ToList();
        var allHistoricalData = (await historicalRepository.GetAllAsync(cancellationToken)).ToList();

        activity?.SetTag("dashboard.kpi_count", kpis.Count)
                ?.SetTag("dashboard.alert_count", allAlerts.Count);

        var alertsToday = allAlerts.Count(a => a.TriggerTime >= today);
        var alertsThisWeek = allAlerts.Count(a => a.TriggerTime >= today.AddDays(-7));

        var recentAlerts = allAlerts
            .Where(a => a.TriggerTime >= now.AddHours(-24))
            .OrderByDescending(a => a.TriggerTime)
            .Take(10)
            .Select(a => new KpiStatusDto
            {
                KpiId = a.KpiId,
                Indicator = kpis.FirstOrDefault(k => k.KpiId == a.KpiId)?.Indicator ?? "Unknown",
                Owner = kpis.FirstOrDefault(k => k.KpiId == a.KpiId)?.Owner ?? "Unknown",
                LastAlert = a.TriggerTime,
                LastDeviation = a.DeviationPercent
            })
            .ToList();

        var dueKpis = kpis
            .Where(k => k.IsActive && !k.IsCurrentlyRunning && (k.LastRun == null || k.LastRun < now.AddMinutes(-k.Frequency)))
            .Select(k => _mapper.Map<KpiStatusDto>(k))
            .ToList();

        var runningKpis = kpis
            .Where(k => k.IsCurrentlyRunning)
            .Select(k =>
            {
                var dto = _mapper.Map<KpiStatusDto>(k);
                dto.Status = "Running";
                dto.IsCurrentlyRunning = true;
                dto.ExecutionStartTime = k.ExecutionStartTime;
                dto.ExecutionContext = k.ExecutionContext;
                dto.ExecutionDurationSeconds = k.GetExecutionDuration()?.TotalSeconds is double duration ? (int)duration : null;
                return dto;
            })
            .ToList();

        // Find the next KPI due for execution using simple frequency-based calculation
        var nextKpiDue = kpis
            .Where(k => k.IsActive && !k.IsCurrentlyRunning && k.LastRun.HasValue)
            .Select(k => new
            {
                Kpi = k,
                NextRun = k.LastRun!.Value.AddMinutes(k.Frequency),
                MinutesUntilDue = (k.LastRun!.Value.AddMinutes(k.Frequency) - now).TotalMinutes
            })
            .Where(x => x.MinutesUntilDue > 0) // Only future executions
            .OrderBy(x => x.NextRun)
            .FirstOrDefault();

        // Get recent executions (last 10)
        var recentExecutions = allHistoricalData
            .Where(h => h.Timestamp >= now.AddHours(-24)) // Last 24 hours
            .OrderByDescending(h => h.Timestamp)
            .Take(10)
            .Select(h => new KpiExecutionStatusDto
            {
                KpiId = h.KpiId,
                Indicator = kpis.FirstOrDefault(k => k.KpiId == h.KpiId)?.Indicator ?? "Unknown",
                ExecutionTime = h.Timestamp,
                IsSuccessful = h.IsSuccessful,
                Value = h.Value,
                ExecutionTimeMs = h.ExecutionTimeMs
            })
            .ToList();

        var dashboard = new KpiDashboardDto
        {
            TotalKpis = kpis.Count,
            ActiveKpis = kpis.Count(k => k.IsActive),
            InactiveKpis = kpis.Count(k => !k.IsActive),
            KpisInErrorCount = recentAlerts.Count,
            KpisDue = dueKpis.Count,
            KpisRunning = runningKpis.Count,
            AlertsToday = alertsToday,
            AlertsThisWeek = alertsThisWeek,
            LastUpdate = now,
            RecentAlerts = recentAlerts,
            KpisInError = recentAlerts,
            DueKpis = dueKpis,
            RunningKpis = runningKpis,
            NextKpiDue = nextKpiDue != null ? new KpiStatusDto
            {
                KpiId = nextKpiDue.Kpi.KpiId,
                Indicator = nextKpiDue.Kpi.Indicator,
                Owner = nextKpiDue.Kpi.Owner,
                NextRun = nextKpiDue.NextRun,
                MinutesUntilDue = (int)Math.Ceiling(Math.Max(0, nextKpiDue.MinutesUntilDue)),
                Status = nextKpiDue.MinutesUntilDue <= 0 ? "Due Now" :
                         nextKpiDue.MinutesUntilDue <= 5 ? "Due Soon" : "Scheduled",
                IsActive = true,
                Frequency = nextKpiDue.Kpi.Frequency,
                LastRun = nextKpiDue.Kpi.LastRun
            } : null,
            RecentExecutions = recentExecutions
        };

        var duration = DateTime.UtcNow - startTime;

        // Update metrics
        _metricsService.UpdateKpiStatus(dashboard.ActiveKpis, dueKpis.Count);

        // Calculate and update system health score
        var healthScore = CalculateSystemHealthScore(dashboard);
        _metricsService.UpdateSystemHealth(healthScore);

        // Record performance
        KpiActivitySource.RecordSuccess(activity, $"Dashboard generated with {kpis.Count} KPIs");
        KpiActivitySource.RecordPerformanceMetrics(activity, (long)duration.TotalMilliseconds, kpis.Count);

            _logger.LogInformation("Dashboard data retrieved successfully in {Duration}ms " +
                "- {TotalKpis} KPIs, {ActiveKpis} active, {AlertsToday} alerts today",
                duration.TotalMilliseconds, dashboard.TotalKpis, dashboard.ActiveKpis, dashboard.AlertsToday);

            return Result.Success(dashboard);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Error retrieving dashboard data after {Duration}ms", duration.TotalMilliseconds);
            return Error.Failure("Dashboard.RetrieveFailed", "An error occurred while retrieving dashboard data");
        }
    }

    /// <summary>
    /// Calculate system health score based on dashboard metrics
    /// </summary>
    private static double CalculateSystemHealthScore(KpiDashboardDto dashboard)
    {
        if (dashboard.TotalKpis == 0) return 100.0;

        var activeRatio = (double)dashboard.ActiveKpis / dashboard.TotalKpis;
        var errorRatio = dashboard.ActiveKpis > 0 ? (double)dashboard.KpisInErrorCount / dashboard.ActiveKpis : 0;
        var dueRatio = dashboard.ActiveKpis > 0 ? (double)dashboard.KpisDue / dashboard.ActiveKpis : 0;

        // Health score calculation (0-100)
        var healthScore = 100.0;
        healthScore -= (1.0 - activeRatio) * 30; // Penalty for inactive KPIs
        healthScore -= errorRatio * 40; // Penalty for KPIs in error
        healthScore -= dueRatio * 30; // Penalty for overdue KPIs

        return Math.Max(0, Math.Min(100, healthScore));
    }
}
