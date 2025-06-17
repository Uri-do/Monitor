using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.DTOs;
using Swashbuckle.AspNetCore.Annotations;
using AutoMapper;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Alert management API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AlertController : ControllerBase
{
    private readonly IAlertRepository _alertRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AlertController> _logger;
    private readonly ICacheService _cacheService;

    public AlertController(
        IAlertRepository alertRepository,
        IMapper mapper,
        ILogger<AlertController> logger,
        ICacheService cacheService)
    {
        _alertRepository = alertRepository;
        _mapper = mapper;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Get alerts with filtering and pagination
    /// </summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get alerts with filtering", Description = "Retrieve alerts with comprehensive filtering and pagination")]
    [SwaggerResponse(200, "Successfully retrieved alerts", typeof(PaginatedAlertsDto))]
    [SwaggerResponse(400, "Invalid filter parameters", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<PaginatedAlertsDto>> GetAlerts(
        [FromQuery] bool? isResolved = null,
        [FromQuery] string? searchText = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "triggerTime",
        [FromQuery] string? sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting alerts with filters - IsResolved: {IsResolved}, Search: {SearchText}", isResolved, searchText);

            // Create filter object
            var filter = new Core.Models.AlertFilter
            {
                IsResolved = isResolved,
                SearchText = searchText,
                StartDate = !string.IsNullOrEmpty(startDate) ? DateTime.Parse(startDate) : null,
                EndDate = !string.IsNullOrEmpty(endDate) ? DateTime.Parse(endDate) : null,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy ?? "TriggerTime",
                SortDirection = sortDirection ?? "desc"
            };

            // Get alerts from repository
            var alertsResult = await _alertRepository.GetAlertsWithFilteringAsync(filter);

            // Map to DTOs
            var alertDtos = alertsResult.Alerts.Select(alert => new AlertLogDto
            {
                AlertId = alert.AlertId,
                IndicatorId = alert.IndicatorId,
                IndicatorName = alert.Indicator?.IndicatorName ?? "Unknown",
                IndicatorOwner = alert.Indicator?.OwnerContactId.ToString() ?? "Unknown",
                TriggerTime = alert.TriggerTime,
                Message = alert.Message ?? "No message",
                Details = alert.Details,
                SentVia = alert.SentVia,
                SentTo = alert.SentTo,
                CurrentValue = alert.CurrentValue,
                HistoricalValue = alert.HistoricalValue,
                DeviationPercent = alert.DeviationPercent,
                IsResolved = alert.IsResolved,
                ResolvedTime = alert.ResolvedTime,
                ResolvedBy = alert.ResolvedBy
            }).ToList();

            var result = new PaginatedAlertsDto
            {
                Alerts = alertDtos,
                TotalCount = alertsResult.TotalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)alertsResult.TotalCount / pageSize)
            };

            _logger.LogDebug("Retrieved {Count} of {Total} alerts", alertDtos.Count, alertsResult.TotalCount);
            return Ok(result);
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "Service provider disposed while accessing alerts");
            return StatusCode(503, "Service temporarily unavailable. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts");
            return StatusCode(500, "An error occurred while retrieving alerts");
        }
    }

    /// <summary>
    /// Get alert by ID
    /// </summary>
    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Get alert by ID", Description = "Retrieve a specific alert by its ID")]
    [SwaggerResponse(200, "Successfully retrieved alert", typeof(AlertLogDto))]
    [SwaggerResponse(404, "Alert not found", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<AlertLogDto>> GetAlert(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting alert with ID: {AlertId}", id);

            var alert = await _alertRepository.GetByIdAsync(id, cancellationToken);

            if (alert == null)
            {
                _logger.LogWarning("Alert with ID {AlertId} not found", id);
                return NotFound($"Alert with ID {id} not found");
            }

            var alertDto = new AlertLogDto
            {
                AlertId = alert.AlertId,
                IndicatorId = alert.IndicatorId,
                IndicatorName = alert.Indicator?.IndicatorName ?? "Unknown",
                IndicatorOwner = alert.Indicator?.OwnerContactId.ToString() ?? "Unknown",
                TriggerTime = alert.TriggerTime,
                Message = alert.Message ?? "No message",
                Details = alert.Details,
                SentVia = alert.SentVia,
                SentTo = alert.SentTo,
                CurrentValue = alert.CurrentValue,
                HistoricalValue = alert.HistoricalValue,
                DeviationPercent = alert.DeviationPercent,
                IsResolved = alert.IsResolved,
                ResolvedTime = alert.ResolvedTime,
                ResolvedBy = alert.ResolvedBy
            };

            return Ok(alertDto);
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "Service provider disposed while accessing alert {AlertId}", id);
            return StatusCode(503, "Service temporarily unavailable. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert {AlertId}", id);
            return StatusCode(500, "An error occurred while retrieving the alert");
        }
    }

    /// <summary>
    /// Get alert dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    [SwaggerOperation(Summary = "Get alert dashboard", Description = "Retrieve alert dashboard summary data")]
    [SwaggerResponse(200, "Successfully retrieved dashboard data", typeof(AlertDashboardDto))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<AlertDashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting alert dashboard data");

            // Use caching to reduce database load - cache for 30 seconds
            const string cacheKey = "alert-dashboard";
            var dashboardDto = await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    var dashboard = await _alertRepository.GetDashboardAsync();

                    return new AlertDashboardDto
                    {
                        TotalAlertsToday = dashboard.TotalAlertsToday,
                        UnresolvedAlerts = dashboard.UnresolvedAlerts,
                        CriticalAlerts = dashboard.CriticalAlerts,
                        AlertsLastHour = dashboard.AlertsLastHour,
                        AlertTrendPercentage = dashboard.AlertTrendPercentage,
                        HourlyTrend = dashboard.HourlyTrend.Select(h => new AlertTrendDto
                        {
                            Date = h.Date,
                            AlertCount = h.AlertCount,
                            CriticalCount = h.CriticalCount,
                            HighCount = h.HighCount,
                            MediumCount = h.MediumCount,
                            LowCount = h.LowCount
                        }).ToList()
                    };
                },
                TimeSpan.FromSeconds(30), // Cache for 30 seconds to balance freshness with performance
                cancellationToken);

            return Ok(dashboardDto);
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "Service provider disposed while accessing alert dashboard");
            return StatusCode(503, "Service temporarily unavailable. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert dashboard");
            return StatusCode(500, "An error occurred while retrieving alert dashboard");
        }
    }

    /// <summary>
    /// Resolve an alert
    /// </summary>
    [HttpPost("{id}/resolve")]
    [SwaggerOperation(Summary = "Resolve alert", Description = "Mark an alert as resolved")]
    [SwaggerResponse(200, "Alert resolved successfully")]
    [SwaggerResponse(404, "Alert not found", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult> ResolveAlert(
        int id,
        [FromBody] MonitoringGrid.Api.DTOs.ResolveAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Resolving alert {AlertId} by {ResolvedBy}", id, request.ResolvedBy);

            var alert = await _alertRepository.GetByIdAsync(id, cancellationToken);

            if (alert == null)
            {
                return NotFound($"Alert with ID {id} not found");
            }

            if (alert.IsResolved)
            {
                return BadRequest("Alert is already resolved");
            }

            // Update alert
            alert.IsResolved = true;
            alert.ResolvedTime = DateTime.UtcNow;
            alert.ResolvedBy = request.ResolvedBy;

            await _alertRepository.UpdateAsync(alert, cancellationToken);

            _logger.LogInformation("Alert {AlertId} resolved by {ResolvedBy}", id, request.ResolvedBy);
            return Ok(new { message = "Alert resolved successfully" });
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "Service provider disposed while resolving alert {AlertId}", id);
            return StatusCode(503, "Service temporarily unavailable. Please try again.");
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
    [HttpPost("bulk-resolve")]
    [SwaggerOperation(Summary = "Bulk resolve alerts", Description = "Resolve multiple alerts at once")]
    [SwaggerResponse(200, "Alerts resolved successfully")]
    [SwaggerResponse(400, "Invalid request", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult> BulkResolveAlerts(
        [FromBody] MonitoringGrid.Api.DTOs.BulkResolveAlertsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Bulk resolving {Count} alerts by {ResolvedBy}", request.AlertIds.Count, request.ResolvedBy);

            if (!request.AlertIds.Any())
            {
                return BadRequest("No alert IDs provided");
            }

            var resolvedCount = 0;
            foreach (var alertId in request.AlertIds)
            {
                var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);
                if (alert != null && !alert.IsResolved)
                {
                    alert.IsResolved = true;
                    alert.ResolvedTime = DateTime.UtcNow;
                    alert.ResolvedBy = request.ResolvedBy;
                    await _alertRepository.UpdateAsync(alert, cancellationToken);
                    resolvedCount++;
                }
            }

            _logger.LogInformation("Bulk resolved {Count} alerts by {ResolvedBy}", resolvedCount, request.ResolvedBy);
            return Ok(new { message = $"{resolvedCount} alerts resolved successfully" });
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "Service provider disposed while bulk resolving alerts");
            return StatusCode(503, "Service temporarily unavailable. Please try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk resolving alerts");
            return StatusCode(500, "An error occurred while resolving alerts");
        }
    }
}
