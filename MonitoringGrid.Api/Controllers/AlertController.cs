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
    private readonly IAlertRepository _alertRepository;
    private readonly IRepository<KPI> _kpiRepository;
    private readonly IMapper _mapper;
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertController> _logger;

    public AlertController(
        IAlertRepository alertRepository,
        IRepository<KPI> kpiRepository,
        IMapper mapper,
        IAlertService alertService,
        ILogger<AlertController> logger)
    {
        _alertRepository = alertRepository;
        _kpiRepository = kpiRepository;
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
        // Convert API DTO to Core model
        var coreFilter = _mapper.Map<Core.Models.AlertFilter>(filter);

        // Get data from repository
        var coreResult = await _alertRepository.GetAlertsWithFilteringAsync(coreFilter);

        // Convert Core model back to API DTO
        var result = new PaginatedAlertsDto
        {
            Alerts = _mapper.Map<List<AlertLogDto>>(coreResult.Alerts),
            TotalCount = coreResult.TotalCount,
            Page = coreResult.Page,
            PageSize = coreResult.PageSize,
            TotalPages = coreResult.TotalPages,
            HasNextPage = coreResult.HasNextPage,
            HasPreviousPage = coreResult.HasPreviousPage
        };

        return Ok(result);
    }

    /// <summary>
    /// Get alert by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AlertLogDto>> GetAlert(long id)
    {
        var alert = await _alertRepository.GetByIdWithIncludesAsync(id, a => a.KPI);

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

        var alert = await _alertRepository.GetByIdAsync(id);
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

        await _alertRepository.UpdateAsync(alert);
        await _alertRepository.SaveChangesAsync();

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

        var resolvedCount = await _alertRepository.BulkResolveAlertsAsync(
            request.AlertIds,
            request.ResolvedBy,
            request.ResolutionNotes);

        if (resolvedCount == 0)
            return NotFound("No unresolved alerts found with the provided IDs");

        _logger.LogInformation("Bulk resolved {Count} alerts by {ResolvedBy}", resolvedCount, request.ResolvedBy);

        return Ok(new { Message = $"Resolved {resolvedCount} alerts successfully" });
    }

    /// <summary>
    /// Get alert statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<AlertStatisticsDto>> GetStatistics([FromQuery] int days = 30)
    {
        var coreStatistics = await _alertRepository.GetStatisticsAsync(days);
        var statistics = _mapper.Map<AlertStatisticsDto>(coreStatistics);
        return Ok(statistics);
    }

    /// <summary>
    /// Get alert dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<AlertDashboardDto>> GetDashboard()
    {
        var coreDashboard = await _alertRepository.GetDashboardAsync();

        // Get recent alerts separately since they're not included in the core dashboard
        var recentAlerts = await _alertRepository.GetWithIncludesAsync(
            a => a.TriggerTime >= DateTime.UtcNow.AddHours(-24),
            a => a.TriggerTime,
            false,
            a => a.KPI);

        var dashboard = new AlertDashboardDto
        {
            TotalAlertsToday = coreDashboard.TotalAlertsToday,
            UnresolvedAlerts = coreDashboard.UnresolvedAlerts,
            CriticalAlerts = coreDashboard.CriticalAlerts,
            AlertsLastHour = coreDashboard.AlertsLastHour,
            AlertTrendPercentage = coreDashboard.AlertTrendPercentage,
            RecentAlerts = _mapper.Map<List<AlertLogDto>>(recentAlerts.Take(10)),
            TopAlertingKpis = new List<KpiAlertSummaryDto>(), // Will be populated from statistics
            HourlyTrend = _mapper.Map<List<AlertTrendDto>>(coreDashboard.HourlyTrend)
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

        var kpi = await _kpiRepository.GetByIdAsync(request.KpiId);
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

        var createdAlert = await _alertRepository.AddAsync(alertLog);
        await _alertRepository.SaveChangesAsync();

        _logger.LogInformation("Manual alert sent for KPI {KpiId}: {Message}", request.KpiId, request.Message);

        return Ok(new { Message = "Manual alert sent successfully", AlertId = createdAlert.AlertId });
    }

}
