using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API controller for managing alerts
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AlertController : ControllerBase
{
    private readonly MonitoringContext _context;
    private readonly IMapper _mapper;
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertController> _logger;

    public AlertController(
        MonitoringContext context,
        IMapper mapper,
        IAlertService alertService,
        ILogger<AlertController> logger)
    {
        _context = context;
        _mapper = mapper;
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Get alerts with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedAlertsDto>> GetAlerts([FromQuery] AlertFilterDto filter)
    {
        var query = _context.AlertLogs
            .Include(a => a.KPI)
            .AsQueryable();

        // Apply filters
        if (filter.StartDate.HasValue)
            query = query.Where(a => a.TriggerTime >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(a => a.TriggerTime <= filter.EndDate.Value);

        if (filter.KpiIds?.Any() == true)
            query = query.Where(a => filter.KpiIds.Contains(a.KpiId));

        if (filter.Owners?.Any() == true)
            query = query.Where(a => filter.Owners.Contains(a.KPI.Owner));

        if (filter.IsResolved.HasValue)
            query = query.Where(a => a.IsResolved == filter.IsResolved.Value);

        if (filter.SentVia?.Any() == true)
            query = query.Where(a => filter.SentVia.Contains(a.SentVia));

        if (filter.MinDeviation.HasValue)
            query = query.Where(a => a.DeviationPercent >= filter.MinDeviation.Value);

        if (filter.MaxDeviation.HasValue)
            query = query.Where(a => a.DeviationPercent <= filter.MaxDeviation.Value);

        if (!string.IsNullOrEmpty(filter.SearchText))
            query = query.Where(a => a.Message.Contains(filter.SearchText) || 
                                   a.KPI.Indicator.Contains(filter.SearchText));

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = filter.SortDirection.ToLower() == "asc" 
            ? ApplySortingAscending(query, filter.SortBy)
            : ApplySortingDescending(query, filter.SortBy);

        // Apply pagination
        var alerts = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var alertDtos = _mapper.Map<List<AlertLogDto>>(alerts);

        var result = new PaginatedAlertsDto
        {
            Alerts = alertDtos,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize),
            HasNextPage = filter.Page * filter.PageSize < totalCount,
            HasPreviousPage = filter.Page > 1
        };

        return Ok(result);
    }

    /// <summary>
    /// Get alert by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AlertLogDto>> GetAlert(long id)
    {
        var alert = await _context.AlertLogs
            .Include(a => a.KPI)
            .FirstOrDefaultAsync(a => a.AlertId == id);

        if (alert == null)
            return NotFound($"Alert with ID {id} not found");

        return Ok(_mapper.Map<AlertLogDto>(alert));
    }

    /// <summary>
    /// Resolve an alert
    /// </summary>
    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> ResolveAlert(long id, [FromBody] ResolveAlertRequest request)
    {
        if (id != request.AlertId)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var alert = await _context.AlertLogs.FindAsync(id);
        if (alert == null)
            return NotFound($"Alert with ID {id} not found");

        if (alert.IsResolved)
            return BadRequest("Alert is already resolved");

        alert.IsResolved = true;
        alert.ResolvedTime = DateTime.UtcNow;
        alert.ResolvedBy = request.ResolvedBy;

        if (!string.IsNullOrEmpty(request.ResolutionNotes))
        {
            alert.Details = string.IsNullOrEmpty(alert.Details) 
                ? $"Resolution: {request.ResolutionNotes}"
                : $"{alert.Details}\n\nResolution: {request.ResolutionNotes}";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Alert {AlertId} resolved by {ResolvedBy}", id, request.ResolvedBy);

        return Ok(new { Message = "Alert resolved successfully" });
    }

    /// <summary>
    /// Bulk resolve alerts
    /// </summary>
    [HttpPost("resolve-bulk")]
    public async Task<IActionResult> BulkResolveAlerts([FromBody] BulkResolveAlertsRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!request.AlertIds.Any())
            return BadRequest("No alert IDs provided");

        var alerts = await _context.AlertLogs
            .Where(a => request.AlertIds.Contains(a.AlertId) && !a.IsResolved)
            .ToListAsync();

        if (!alerts.Any())
            return NotFound("No unresolved alerts found with the provided IDs");

        var resolvedTime = DateTime.UtcNow;
        foreach (var alert in alerts)
        {
            alert.IsResolved = true;
            alert.ResolvedTime = resolvedTime;
            alert.ResolvedBy = request.ResolvedBy;

            if (!string.IsNullOrEmpty(request.ResolutionNotes))
            {
                alert.Details = string.IsNullOrEmpty(alert.Details) 
                    ? $"Resolution: {request.ResolutionNotes}"
                    : $"{alert.Details}\n\nResolution: {request.ResolutionNotes}";
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk resolved {Count} alerts by {ResolvedBy}", alerts.Count, request.ResolvedBy);

        return Ok(new { Message = $"Resolved {alerts.Count} alerts successfully" });
    }

    /// <summary>
    /// Get alert statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<AlertStatisticsDto>> GetStatistics([FromQuery] int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        var now = DateTime.UtcNow;
        var today = now.Date;

        var alerts = await _context.AlertLogs
            .Include(a => a.KPI)
            .Where(a => a.TriggerTime >= startDate)
            .ToListAsync();

        var dailyTrend = alerts
            .GroupBy(a => a.TriggerTime.Date)
            .Select(g => new AlertTrendDto
            {
                Date = g.Key,
                AlertCount = g.Count(),
                CriticalCount = g.Count(a => a.DeviationPercent >= 50),
                HighCount = g.Count(a => a.DeviationPercent >= 25 && a.DeviationPercent < 50),
                MediumCount = g.Count(a => a.DeviationPercent >= 10 && a.DeviationPercent < 25),
                LowCount = g.Count(a => a.DeviationPercent < 10)
            })
            .OrderBy(t => t.Date)
            .ToList();

        var topAlertingKpis = alerts
            .GroupBy(a => new { a.KpiId, a.KPI.Indicator, a.KPI.Owner })
            .Select(g => new KpiAlertSummaryDto
            {
                KpiId = g.Key.KpiId,
                Indicator = g.Key.Indicator,
                Owner = g.Key.Owner,
                AlertCount = g.Count(),
                UnresolvedCount = g.Count(a => !a.IsResolved),
                LastAlert = g.Max(a => a.TriggerTime),
                AverageDeviation = g.Average(a => a.DeviationPercent ?? 0)
            })
            .OrderByDescending(k => k.AlertCount)
            .Take(10)
            .ToList();

        var resolvedAlerts = alerts.Where(a => a.IsResolved && a.ResolvedTime.HasValue).ToList();
        var averageResolutionTime = resolvedAlerts.Any() 
            ? resolvedAlerts.Average(a => (a.ResolvedTime!.Value - a.TriggerTime).TotalHours)
            : 0;

        var statistics = new AlertStatisticsDto
        {
            TotalAlerts = alerts.Count,
            UnresolvedAlerts = alerts.Count(a => !a.IsResolved),
            ResolvedAlerts = alerts.Count(a => a.IsResolved),
            AlertsToday = alerts.Count(a => a.TriggerTime >= today),
            AlertsThisWeek = alerts.Count(a => a.TriggerTime >= today.AddDays(-7)),
            AlertsThisMonth = alerts.Count(a => a.TriggerTime >= today.AddDays(-30)),
            CriticalAlerts = alerts.Count(a => a.DeviationPercent >= 50),
            HighPriorityAlerts = alerts.Count(a => a.DeviationPercent >= 25),
            AverageResolutionTimeHours = (decimal)averageResolutionTime,
            DailyTrend = dailyTrend,
            TopAlertingKpis = topAlertingKpis
        };

        return Ok(statistics);
    }

    /// <summary>
    /// Get alert dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<AlertDashboardDto>> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var lastHour = now.AddHours(-1);

        var recentAlerts = await _context.AlertLogs
            .Include(a => a.KPI)
            .Where(a => a.TriggerTime >= now.AddHours(-24))
            .OrderByDescending(a => a.TriggerTime)
            .Take(10)
            .ToListAsync();

        var alertsToday = await _context.AlertLogs.CountAsync(a => a.TriggerTime >= today);
        var unresolvedAlerts = await _context.AlertLogs.CountAsync(a => !a.IsResolved);
        var criticalAlerts = await _context.AlertLogs.CountAsync(a => !a.IsResolved && a.DeviationPercent >= 50);
        var alertsLastHour = await _context.AlertLogs.CountAsync(a => a.TriggerTime >= lastHour);

        // Calculate trend (compare with yesterday)
        var yesterday = today.AddDays(-1);
        var alertsYesterday = await _context.AlertLogs.CountAsync(a => a.TriggerTime >= yesterday && a.TriggerTime < today);
        var alertTrendPercentage = alertsYesterday > 0 
            ? ((decimal)(alertsToday - alertsYesterday) / alertsYesterday) * 100
            : alertsToday > 0 ? 100 : 0;

        // Hourly trend for last 24 hours
        var hourlyTrend = new List<AlertTrendDto>();
        for (int i = 23; i >= 0; i--)
        {
            var hourStart = now.AddHours(-i).Date.AddHours(now.AddHours(-i).Hour);
            var hourEnd = hourStart.AddHours(1);
            
            var hourlyAlerts = await _context.AlertLogs
                .Where(a => a.TriggerTime >= hourStart && a.TriggerTime < hourEnd)
                .ToListAsync();

            hourlyTrend.Add(new AlertTrendDto
            {
                Date = hourStart,
                AlertCount = hourlyAlerts.Count,
                CriticalCount = hourlyAlerts.Count(a => a.DeviationPercent >= 50),
                HighCount = hourlyAlerts.Count(a => a.DeviationPercent >= 25 && a.DeviationPercent < 50),
                MediumCount = hourlyAlerts.Count(a => a.DeviationPercent >= 10 && a.DeviationPercent < 25),
                LowCount = hourlyAlerts.Count(a => a.DeviationPercent < 10)
            });
        }

        var topAlertingKpis = await _context.AlertLogs
            .Include(a => a.KPI)
            .Where(a => a.TriggerTime >= today.AddDays(-7))
            .GroupBy(a => new { a.KpiId, a.KPI.Indicator, a.KPI.Owner })
            .Select(g => new KpiAlertSummaryDto
            {
                KpiId = g.Key.KpiId,
                Indicator = g.Key.Indicator,
                Owner = g.Key.Owner,
                AlertCount = g.Count(),
                UnresolvedCount = g.Count(a => !a.IsResolved),
                LastAlert = g.Max(a => a.TriggerTime)
            })
            .OrderByDescending(k => k.AlertCount)
            .Take(5)
            .ToListAsync();

        var dashboard = new AlertDashboardDto
        {
            TotalAlertsToday = alertsToday,
            UnresolvedAlerts = unresolvedAlerts,
            CriticalAlerts = criticalAlerts,
            AlertsLastHour = alertsLastHour,
            AlertTrendPercentage = alertTrendPercentage,
            RecentAlerts = _mapper.Map<List<AlertLogDto>>(recentAlerts),
            TopAlertingKpis = topAlertingKpis,
            HourlyTrend = hourlyTrend
        };

        return Ok(dashboard);
    }

    /// <summary>
    /// Send manual alert
    /// </summary>
    [HttpPost("manual")]
    public async Task<IActionResult> SendManualAlert([FromBody] ManualAlertRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var kpi = await _context.KPIs.FindAsync(request.KpiId);
        if (kpi == null)
            return NotFound($"KPI with ID {request.KpiId} not found");

        // Create a manual alert log entry
        var alertLog = new AlertLog
        {
            KpiId = request.KpiId,
            TriggerTime = DateTime.UtcNow,
            Message = request.Message,
            Details = request.Details,
            SentVia = request.Priority,
            SentTo = "Manual Alert",
            IsResolved = false
        };

        _context.AlertLogs.Add(alertLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Manual alert sent for KPI {KpiId}: {Message}", request.KpiId, request.Message);

        return Ok(new { Message = "Manual alert sent successfully", AlertId = alertLog.AlertId });
    }

    private static IQueryable<AlertLog> ApplySortingAscending(IQueryable<AlertLog> query, string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "triggertime" => query.OrderBy(a => a.TriggerTime),
            "kpi" => query.OrderBy(a => a.KPI.Indicator),
            "owner" => query.OrderBy(a => a.KPI.Owner),
            "deviation" => query.OrderBy(a => a.DeviationPercent),
            "resolved" => query.OrderBy(a => a.IsResolved),
            _ => query.OrderBy(a => a.TriggerTime)
        };
    }

    private static IQueryable<AlertLog> ApplySortingDescending(IQueryable<AlertLog> query, string sortBy)
    {
        return sortBy.ToLower() switch
        {
            "triggertime" => query.OrderByDescending(a => a.TriggerTime),
            "kpi" => query.OrderByDescending(a => a.KPI.Indicator),
            "owner" => query.OrderByDescending(a => a.KPI.Owner),
            "deviation" => query.OrderByDescending(a => a.DeviationPercent),
            "resolved" => query.OrderByDescending(a => a.IsResolved),
            _ => query.OrderByDescending(a => a.TriggerTime)
        };
    }
}
