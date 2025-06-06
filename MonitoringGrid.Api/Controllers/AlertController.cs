using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.ValueObjects;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertController> _logger;

    public AlertController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAlertService alertService,
        ILogger<AlertController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Get alerts with filtering and pagination using enhanced patterns
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedAlertsDto>> GetAlerts([FromQuery] AlertFilterDto filter)
    {
        try
        {
            // Validate pagination parameters
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize < 1) filter.PageSize = 20;
            if (filter.PageSize > 100) filter.PageSize = 100; // Limit to max 100 items per page

            var alertRepository = _unitOfWork.Repository<AlertLog>();
            var allAlerts = await alertRepository.GetAllAsync();

            // Apply filters
            var filteredAlerts = allAlerts.AsQueryable();

            if (filter.StartDate.HasValue)
                filteredAlerts = filteredAlerts.Where(a => a.TriggerTime >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                filteredAlerts = filteredAlerts.Where(a => a.TriggerTime <= filter.EndDate.Value);

            if (filter.KpiIds?.Any() == true)
                filteredAlerts = filteredAlerts.Where(a => filter.KpiIds.Contains(a.KpiId));

            if (filter.IsResolved.HasValue)
                filteredAlerts = filteredAlerts.Where(a => a.IsResolved == filter.IsResolved.Value);

            if (filter.MinDeviation.HasValue)
                filteredAlerts = filteredAlerts.Where(a => a.DeviationPercent >= filter.MinDeviation.Value);

            if (filter.MaxDeviation.HasValue)
                filteredAlerts = filteredAlerts.Where(a => a.DeviationPercent <= filter.MaxDeviation.Value);

            if (!string.IsNullOrEmpty(filter.SearchText))
                filteredAlerts = filteredAlerts.Where(a => a.Message.Contains(filter.SearchText) ||
                                                          (a.Details != null && a.Details.Contains(filter.SearchText)));

            // Apply sorting
            filteredAlerts = filter.SortDirection.ToLower() == "asc"
                ? filteredAlerts.OrderBy(a => a.TriggerTime)
                : filteredAlerts.OrderByDescending(a => a.TriggerTime);

            // Apply pagination
            var totalCount = filteredAlerts.Count();
            var alerts = filteredAlerts
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var result = new PaginatedAlertsDto
            {
                Alerts = _mapper.Map<List<AlertLogDto>>(alerts),
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize),
                HasNextPage = filter.Page < Math.Ceiling((double)totalCount / filter.PageSize),
                HasPreviousPage = filter.Page > 1
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts with filter: {@Filter}", filter);
            return StatusCode(500, "An error occurred while retrieving alerts");
        }
    }

    /// <summary>
    /// Get alert by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AlertLogDto>> GetAlert(long id)
    {
        try
        {
            var alertRepository = _unitOfWork.Repository<AlertLog>();
            var alert = await alertRepository.GetByIdAsync(id);

            if (alert == null)
                return NotFound($"Alert with ID {id} not found");

            return Ok(_mapper.Map<AlertLogDto>(alert));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert with ID: {AlertId}", id);
            return StatusCode(500, "An error occurred while retrieving the alert");
        }
    }

    /// <summary>
    /// Resolve an alert
    /// </summary>
    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> ResolveAlert(long id, [FromBody] ResolveAlertRequest request)
    {
        try
        {
            if (id != request.AlertId)
                return BadRequest("ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var alertRepository = _unitOfWork.Repository<AlertLog>();
            var alert = await alertRepository.GetByIdAsync(id);

            if (alert == null)
                return NotFound($"Alert with ID {id} not found");

            if (alert.IsResolved)
                return BadRequest("Alert is already resolved");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                alert.IsResolved = true;
                alert.ResolvedTime = DateTime.UtcNow;
                alert.ResolvedBy = request.ResolvedBy;

                if (!string.IsNullOrEmpty(request.ResolutionNotes))
                {
                    alert.Details = string.IsNullOrEmpty(alert.Details)
                        ? $"Resolution: {request.ResolutionNotes}"
                        : $"{alert.Details}\n\nResolution: {request.ResolutionNotes}";
                }

                await alertRepository.UpdateAsync(alert);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Alert {AlertId} resolved by {ResolvedBy}", id, request.ResolvedBy);

                return Ok(new { Message = "Alert resolved successfully" });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving alert {AlertId}", id);
            return StatusCode(500, "An error occurred while resolving the alert");
        }
    }

    /// <summary>
    /// Bulk resolve alerts
    /// </summary>
    [HttpPost("resolve-bulk")]
    public async Task<IActionResult> BulkResolveAlerts([FromBody] BulkResolveAlertsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!request.AlertIds.Any())
                return BadRequest("No alert IDs provided");

            var alertRepository = _unitOfWork.Repository<AlertLog>();
            var allAlerts = await alertRepository.GetAllAsync();
            var alertsToResolve = allAlerts.Where(a => request.AlertIds.Contains(a.AlertId) && !a.IsResolved).ToList();

            if (!alertsToResolve.Any())
                return NotFound("No unresolved alerts found with the provided IDs");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var alert in alertsToResolve)
                {
                    alert.IsResolved = true;
                    alert.ResolvedTime = DateTime.UtcNow;
                    alert.ResolvedBy = request.ResolvedBy;

                    if (!string.IsNullOrEmpty(request.ResolutionNotes))
                    {
                        alert.Details = string.IsNullOrEmpty(alert.Details)
                            ? $"Resolution: {request.ResolutionNotes}"
                            : $"{alert.Details}\n\nResolution: {request.ResolutionNotes}";
                    }

                    await alertRepository.UpdateAsync(alert);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Bulk resolved {Count} alerts by {ResolvedBy}", alertsToResolve.Count, request.ResolvedBy);

                return Ok(new { Message = $"Resolved {alertsToResolve.Count} alerts successfully" });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk resolving alerts");
            return StatusCode(500, "An error occurred while resolving alerts");
        }
    }

    /// <summary>
    /// Get alert statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<AlertStatisticsDto>> GetStatistics([FromQuery] int days = 30)
    {
        try
        {
            var alertRepository = _unitOfWork.Repository<AlertLog>();
            var startDate = DateTime.UtcNow.AddDays(-days);
            var allAlerts = await alertRepository.GetAllAsync();
            var alerts = allAlerts.Where(a => a.TriggerTime >= startDate).ToList();

            var statistics = new AlertStatisticsDto
            {
                TotalAlerts = alerts.Count,
                ResolvedAlerts = alerts.Count(a => a.IsResolved),
                UnresolvedAlerts = alerts.Count(a => !a.IsResolved),
                CriticalAlerts = alerts.Count(a => a.DeviationPercent.HasValue && Math.Abs(a.DeviationPercent.Value) >= 50),
                AverageResolutionTimeHours = (decimal)alerts.Where(a => a.IsResolved && a.ResolvedTime.HasValue)
                    .Select(a => (a.ResolvedTime!.Value - a.TriggerTime).TotalHours)
                    .DefaultIfEmpty(0)
                    .Average()
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert statistics");
            return StatusCode(500, "An error occurred while retrieving alert statistics");
        }
    }

    /// <summary>
    /// Get alert dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<AlertDashboardDto>> GetDashboard()
    {
        try
        {
            var alertRepository = _unitOfWork.Repository<AlertLog>();
            var now = DateTime.UtcNow;
            var today = now.Date;
            var lastHour = now.AddHours(-1);
            var last24Hours = now.AddHours(-24);

            var allAlerts = await alertRepository.GetAllAsync();
            var recentAlerts = allAlerts.Where(a => a.TriggerTime >= last24Hours).ToList();

            var dashboard = new AlertDashboardDto
            {
                TotalAlertsToday = allAlerts.Count(a => a.TriggerTime.Date == today),
                UnresolvedAlerts = allAlerts.Count(a => !a.IsResolved),
                CriticalAlerts = allAlerts.Count(a => a.DeviationPercent.HasValue && Math.Abs(a.DeviationPercent.Value) >= 50),
                AlertsLastHour = allAlerts.Count(a => a.TriggerTime >= lastHour),
                AlertTrendPercentage = (decimal)CalculateAlertTrend(allAlerts.ToList(), today),
                RecentAlerts = _mapper.Map<List<AlertLogDto>>(recentAlerts.OrderByDescending(a => a.TriggerTime).Take(10).ToList()),
                TopAlertingKpis = new List<KpiAlertSummaryDto>(), // Would need KPI data to populate
                HourlyTrend = new List<AlertTrendDto>() // Would need to calculate hourly trends
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert dashboard");
            return StatusCode(500, "An error occurred while retrieving alert dashboard");
        }
    }

    /// <summary>
    /// Send manual alert
    /// </summary>
    [HttpPost("manual")]
    public async Task<IActionResult> SendManualAlert([FromBody] ManualAlertRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdAsync(request.KpiId);

            if (kpi == null)
                return NotFound($"KPI with ID {request.KpiId} not found");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
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

                var alertRepository = _unitOfWork.Repository<AlertLog>();
                var createdAlert = await alertRepository.AddAsync(alertLog);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Manual alert sent for KPI {KpiId}: {Message}", request.KpiId, request.Message);

                return Ok(new { Message = "Manual alert sent successfully", AlertId = createdAlert.AlertId });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending manual alert for KPI {KpiId}", request.KpiId);
            return StatusCode(500, "An error occurred while sending the manual alert");
        }
    }

    #region Helper Methods

    private double CalculateAlertTrend(List<AlertLog> allAlerts, DateTime today)
    {
        var yesterday = today.AddDays(-1);
        var todayCount = allAlerts.Count(a => a.TriggerTime.Date == today);
        var yesterdayCount = allAlerts.Count(a => a.TriggerTime.Date == yesterday);

        if (yesterdayCount == 0) return todayCount > 0 ? 100 : 0;
        return ((double)(todayCount - yesterdayCount) / yesterdayCount) * 100;
    }

    #endregion

}
