using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.DTOs.Alerts;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.DTOs.Common;
using MonitoringGrid.Core.DTOs;
using Swashbuckle.AspNetCore.Annotations;
using AutoMapper;
using System.Diagnostics;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Alert management API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
[ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
public class AlertController : BaseApiController
{
    private readonly MonitoringGrid.Core.Interfaces.IAlertRepository _alertRepository;
    private readonly IMapper _mapper;
    private readonly MonitoringGrid.Core.Interfaces.ICacheService _cacheService;

    public AlertController(
        IMediator mediator,
        MonitoringGrid.Core.Interfaces.IAlertRepository alertRepository,
        IMapper mapper,
        ILogger<AlertController> logger,
        MonitoringGrid.Core.Interfaces.ICacheService cacheService)
        : base(mediator, logger)
    {
        _alertRepository = alertRepository;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Get alerts with filtering and pagination
    /// </summary>
    /// <param name="request">Get alerts request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of alerts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedAlertsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedAlertsResponse>> GetAlerts(
        [FromQuery] GetAlertsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetAlertsRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Getting alerts with filters - IsResolved: {IsResolved}, Search: {SearchText}",
                request.IsResolved, request.SearchText);

            // Create filter object
            var filter = new Core.Models.AlertFilter
            {
                IsResolved = request.IsResolved,
                SearchText = request.SearchText,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Page = request.Page,
                PageSize = request.PageSize,
                SortBy = request.SortBy ?? "TriggerTime",
                SortDirection = request.SortDirection ?? "desc"
            };

            // Get alerts from repository
            var alertsResult = await _alertRepository.GetAlertsWithFilteringAsync(filter);

            // Map to enhanced response DTOs
            var alertResponses = alertsResult.Alerts.Select(alert => MapToAlertResponse(alert, request.IncludeDetails)).ToList();

            // Calculate summary statistics
            var summary = new AlertSummary
            {
                TotalAlerts = alertsResult.TotalCount,
                UnresolvedAlerts = alertResponses.Count(a => !a.IsResolved),
                ResolvedAlerts = alertResponses.Count(a => a.IsResolved),
                CriticalAlerts = alertResponses.Count(a => a.Severity == "Critical"),
                HighAlerts = alertResponses.Count(a => a.Severity == "High"),
                MediumAlerts = alertResponses.Count(a => a.Severity == "Medium"),
                LowAlerts = alertResponses.Count(a => a.Severity == "Low"),
                AverageTimeToResolution = CalculateAverageTimeToResolution(alertResponses.Where(a => a.IsResolved))
            };

            stopwatch.Stop();

            var response = new PaginatedAlertsResponse
            {
                Alerts = alertResponses,
                TotalCount = alertsResult.TotalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling((double)alertsResult.TotalCount / request.PageSize),
                HasNextPage = request.Page < (int)Math.Ceiling((double)alertsResult.TotalCount / request.PageSize),
                HasPreviousPage = request.Page > 1,
                Summary = summary,
                QueryMetrics = new QueryMetrics
                {
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryCount = 1,
                    CacheHit = false
                }
            };

            Logger.LogDebug("Retrieved {Count} of {Total} alerts in {Duration}ms",
                alertResponses.Count, alertsResult.TotalCount, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, $"Retrieved {alertResponses.Count} alerts"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get alerts operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Get alerts operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (ObjectDisposedException ex)
        {
            Logger.LogError(ex, "Service provider disposed while accessing alerts");
            return StatusCode(503, CreateErrorResponse("Service temporarily unavailable. Please try again.", "SERVICE_UNAVAILABLE"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving alerts");
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve alerts: {ex.Message}", "GET_ALERTS_ERROR"));
        }
    }

    /// <summary>
    /// Get alert by ID
    /// </summary>
    /// <param name="id">Alert ID</param>
    /// <param name="request">Get alert request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alert details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertResponse>> GetAlert(
        int id,
        [FromQuery] GetAlertRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetAlertRequest { AlertId = id };

            // Validate alert ID matches route parameter
            if (request.AlertId != id)
            {
                request.AlertId = id; // Use route parameter
            }

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Additional validation for alert ID
            var paramValidation = ValidateParameter(id, nameof(id),
                alertId => alertId > 0, "Alert ID must be a positive integer");
            if (paramValidation != null) return BadRequest(paramValidation);

            Logger.LogDebug("Getting alert with ID: {AlertId}", id);

            var alert = await _alertRepository.GetByIdAsync(id, cancellationToken);

            if (alert == null)
            {
                Logger.LogWarning("Alert with ID {AlertId} not found", id);
                return NotFound(CreateErrorResponse($"Alert {id} not found", "ALERT_NOT_FOUND"));
            }

            stopwatch.Stop();

            var response = MapToAlertResponse(alert, request.IncludeDetails);

            // Add additional details if requested
            if (request.IncludeDetails)
            {
                response.AdditionalDetails ??= new Dictionary<string, object>();
                response.AdditionalDetails["QueryDurationMs"] = stopwatch.ElapsedMilliseconds;
                response.AdditionalDetails["RequestedIndicator"] = request.IncludeIndicator;
                response.AdditionalDetails["RequestedHistory"] = request.IncludeHistory;
            }

            Logger.LogDebug("Retrieved alert {AlertId} in {Duration}ms",
                id, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, $"Retrieved alert {id}"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get alert operation was cancelled for alert {AlertId}", id);
            return StatusCode(499, CreateErrorResponse("Get alert operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (ObjectDisposedException ex)
        {
            Logger.LogError(ex, "Service provider disposed while accessing alert {AlertId}", id);
            return StatusCode(503, CreateErrorResponse("Service temporarily unavailable. Please try again.", "SERVICE_UNAVAILABLE"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving alert {AlertId}", id);
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve alert {id}: {ex.Message}", "GET_ALERT_ERROR"));
        }
    }

    /// <summary>
    /// Get alert dashboard data
    /// </summary>
    /// <param name="request">Dashboard request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced alert dashboard data</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AlertDashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertDashboardResponse>> GetDashboard(
        [FromQuery] GetAlertDashboardRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetAlertDashboardRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Getting alert dashboard data with trend hours: {TrendHours}", request.TrendHours);

            // Use caching to reduce database load - cache for 30 seconds unless refresh requested
            var cacheKey = $"alert-dashboard-{request.TrendHours}-{request.IncludeTrend}-{request.IncludeDetails}";
            var cacheExpiry = TimeSpan.FromSeconds(30);

            if (request.RefreshCache)
            {
                await _cacheService.RemoveAsync(cacheKey, cancellationToken);
            }

            var dashboardResponse = await _cacheService.GetOrSetAsync(
                cacheKey,
                async () =>
                {
                    var dashboard = await _alertRepository.GetDashboardAsync();

                    // Get additional data for enhanced dashboard
                    var recentCriticalAlerts = new List<AlertResponse>();
                    var topAlertingIndicators = new List<IndicatorAlertCount>();

                    if (request.IncludeDetails)
                    {
                        // Get recent critical alerts
                        var criticalFilter = new Core.Models.AlertFilter
                        {
                            StartDate = DateTime.UtcNow.AddHours(-24),
                            SortBy = "TriggerTime",
                            SortDirection = "desc",
                            PageSize = 5
                        };

                        var criticalAlertsResult = await _alertRepository.GetAlertsWithFilteringAsync(criticalFilter);
                        recentCriticalAlerts = criticalAlertsResult.Alerts
                            .Where(a => (a.DeviationPercent ?? 0) >= 50)
                            .Select(a => MapToAlertResponse(a, false))
                            .ToList();

                        // Get top alerting indicators (this would need to be implemented in repository)
                        // For now, we'll create a placeholder
                        topAlertingIndicators = new List<IndicatorAlertCount>();
                    }

                    var response = new AlertDashboardResponse
                    {
                        TotalAlertsToday = dashboard.TotalAlertsToday,
                        UnresolvedAlerts = dashboard.UnresolvedAlerts,
                        CriticalAlerts = dashboard.CriticalAlerts,
                        AlertsLastHour = dashboard.AlertsLastHour,
                        AlertTrendPercentage = dashboard.AlertTrendPercentage,
                        HourlyTrend = request.IncludeTrend ? dashboard.HourlyTrend.Select(h => new AlertTrendData
                        {
                            Date = h.Date,
                            AlertCount = h.AlertCount,
                            CriticalCount = h.CriticalCount,
                            HighCount = h.HighCount,
                            MediumCount = h.MediumCount,
                            LowCount = h.LowCount
                        }).ToList() : new List<AlertTrendData>(),
                        TopAlertingIndicators = topAlertingIndicators,
                        RecentCriticalAlerts = recentCriticalAlerts,
                        SystemHealthScore = CalculateSystemHealthScore(dashboard),
                        GeneratedAt = DateTime.UtcNow
                    };

                    return response;
                },
                cacheExpiry,
                cancellationToken);

            stopwatch.Stop();
            dashboardResponse.GenerationTimeMs = stopwatch.ElapsedMilliseconds;

            Logger.LogDebug("Retrieved alert dashboard in {Duration}ms (cached: {Cached})",
                stopwatch.ElapsedMilliseconds, stopwatch.ElapsedMilliseconds < 10);

            return Ok(CreateSuccessResponse(dashboardResponse, "Retrieved alert dashboard"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get alert dashboard operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Get alert dashboard operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (ObjectDisposedException ex)
        {
            Logger.LogError(ex, "Service provider disposed while accessing alert dashboard");
            return StatusCode(503, CreateErrorResponse("Service temporarily unavailable. Please try again.", "SERVICE_UNAVAILABLE"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving alert dashboard");
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve alert dashboard: {ex.Message}", "GET_DASHBOARD_ERROR"));
        }
    }

    /// <summary>
    /// Resolve an alert
    /// </summary>
    /// <param name="id">Alert ID</param>
    /// <param name="request">Resolve alert request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Alert resolution result</returns>
    [HttpPost("{id}/resolve")]
    [ProducesResponseType(typeof(AlertOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AlertOperationResponse>> ResolveAlert(
        int id,
        [FromBody] MonitoringGrid.Api.DTOs.Alerts.ResolveAlertRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate alert ID matches route parameter
            if (request.AlertId != id)
            {
                request.AlertId = id; // Use route parameter
            }

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Additional validation for alert ID
            var paramValidation = ValidateParameter(id, nameof(id),
                alertId => alertId > 0, "Alert ID must be a positive integer");
            if (paramValidation != null) return BadRequest(paramValidation);

            Logger.LogDebug("Resolving alert {AlertId} by {ResolvedBy}", id, request.ResolvedBy);

            var alert = await _alertRepository.GetByIdAsync(id, cancellationToken);

            if (alert == null)
            {
                return NotFound(CreateErrorResponse($"Alert {id} not found", "ALERT_NOT_FOUND"));
            }

            if (alert.IsResolved)
            {
                return BadRequest(CreateErrorResponse("Alert is already resolved", "ALERT_ALREADY_RESOLVED"));
            }

            var resolvedTime = DateTime.UtcNow;
            var timeToResolution = resolvedTime - alert.TriggerTime;

            // Update alert
            alert.IsResolved = true;
            alert.ResolvedTime = resolvedTime;
            alert.ResolvedBy = request.ResolvedBy;

            // Add resolution notes if provided (assuming these fields exist or can be added)
            // alert.ResolutionNotes = request.ResolutionNotes;
            // alert.ResolutionCategory = request.ResolutionCategory;

            await _alertRepository.UpdateAsync(alert, cancellationToken);

            stopwatch.Stop();

            var response = new AlertOperationResponse
            {
                Success = true,
                Message = $"Alert {id} resolved successfully by {request.ResolvedBy}",
                AlertIds = new List<int> { id },
                ProcessedCount = 1,
                SuccessCount = 1,
                FailureCount = 0,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, object>
                {
                    ["ResolvedBy"] = request.ResolvedBy,
                    ["ResolvedAt"] = resolvedTime,
                    ["TimeToResolution"] = timeToResolution.ToString(@"dd\.hh\:mm\:ss"),
                    ["TimeToResolutionHours"] = timeToResolution.TotalHours,
                    ["ResolutionNotes"] = request.ResolutionNotes ?? "None",
                    ["ResolutionCategory"] = request.ResolutionCategory ?? "General",
                    ["AlertSeverity"] = alert.DeviationPercent switch
                    {
                        >= 50 => "Critical",
                        >= 25 => "High",
                        >= 10 => "Medium",
                        _ => "Low"
                    }
                }
            };

            Logger.LogInformation("Alert {AlertId} resolved by {ResolvedBy} in {Duration}ms (resolution time: {ResolutionTime})",
                id, request.ResolvedBy, stopwatch.ElapsedMilliseconds, timeToResolution);

            return Ok(CreateSuccessResponse(response, response.Message));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Resolve alert operation was cancelled for alert {AlertId}", id);
            return StatusCode(499, CreateErrorResponse("Resolve alert operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (ObjectDisposedException ex)
        {
            Logger.LogError(ex, "Service provider disposed while resolving alert {AlertId}", id);
            return StatusCode(503, CreateErrorResponse("Service temporarily unavailable. Please try again.", "SERVICE_UNAVAILABLE"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error resolving alert {AlertId}", id);
            return StatusCode(500, CreateErrorResponse($"Failed to resolve alert {id}: {ex.Message}", "RESOLVE_ALERT_ERROR"));
        }
    }

    /// <summary>
    /// Bulk resolve alerts
    /// </summary>
    /// <param name="request">Bulk resolve alerts request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk resolution operation result</returns>
    [HttpPost("bulk-resolve")]
    [ProducesResponseType(typeof(AlertOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AlertOperationResponse>> BulkResolveAlerts(
        [FromBody] MonitoringGrid.Api.DTOs.Alerts.BulkResolveAlertsRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Additional validation for alert IDs
            var paramValidation = ValidateParameter(request.AlertIds, nameof(request.AlertIds),
                ids => ids != null && ids.Any(), "Alert IDs list cannot be empty");
            if (paramValidation != null) return BadRequest(paramValidation);

            Logger.LogDebug("Bulk resolving {Count} alerts by {ResolvedBy}", request.AlertIds.Count, request.ResolvedBy);

            var resolvedTime = DateTime.UtcNow;
            var processedAlerts = new List<int>();
            var successfulAlerts = new List<int>();
            var failedAlerts = new List<int>();
            var errors = new List<string>();

            foreach (var alertId in request.AlertIds)
            {
                try
                {
                    processedAlerts.Add(alertId);

                    var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);

                    if (alert == null)
                    {
                        var error = $"Alert {alertId} not found";
                        errors.Add(error);
                        failedAlerts.Add(alertId);

                        if (!request.ContinueOnError)
                        {
                            return BadRequest(CreateErrorResponse(error, "ALERT_NOT_FOUND"));
                        }
                        continue;
                    }

                    if (alert.IsResolved)
                    {
                        var error = $"Alert {alertId} is already resolved";
                        errors.Add(error);
                        failedAlerts.Add(alertId);

                        if (!request.ContinueOnError)
                        {
                            return BadRequest(CreateErrorResponse(error, "ALERT_ALREADY_RESOLVED"));
                        }
                        continue;
                    }

                    // Update alert
                    alert.IsResolved = true;
                    alert.ResolvedTime = resolvedTime;
                    alert.ResolvedBy = request.ResolvedBy;

                    // Add resolution notes if provided
                    // alert.ResolutionNotes = request.ResolutionNotes;
                    // alert.ResolutionCategory = request.ResolutionCategory;

                    await _alertRepository.UpdateAsync(alert, cancellationToken);
                    successfulAlerts.Add(alertId);
                }
                catch (Exception ex)
                {
                    var error = $"Failed to resolve alert {alertId}: {ex.Message}";
                    errors.Add(error);
                    failedAlerts.Add(alertId);

                    Logger.LogError(ex, "Error resolving alert {AlertId} during bulk operation", alertId);

                    if (!request.ContinueOnError)
                    {
                        return StatusCode(500, CreateErrorResponse(error, "BULK_RESOLVE_PARTIAL_FAILURE"));
                    }
                }
            }

            stopwatch.Stop();

            var response = new AlertOperationResponse
            {
                Success = successfulAlerts.Any(),
                Message = $"Bulk resolution completed: {successfulAlerts.Count} resolved, {failedAlerts.Count} failed",
                AlertIds = request.AlertIds,
                ProcessedCount = processedAlerts.Count,
                SuccessCount = successfulAlerts.Count,
                FailureCount = failedAlerts.Count,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, object>
                {
                    ["ResolvedBy"] = request.ResolvedBy,
                    ["ResolvedAt"] = resolvedTime,
                    ["SuccessfulAlerts"] = successfulAlerts,
                    ["FailedAlerts"] = failedAlerts,
                    ["Errors"] = errors,
                    ["ResolutionNotes"] = request.ResolutionNotes ?? "None",
                    ["ResolutionCategory"] = request.ResolutionCategory ?? "General",
                    ["ContinueOnError"] = request.ContinueOnError
                }
            };

            // Set error code if there were failures
            if (failedAlerts.Any())
            {
                response.ErrorCode = "BULK_RESOLVE_PARTIAL_FAILURE";
            }

            Logger.LogInformation("Bulk resolved {SuccessCount}/{TotalCount} alerts by {ResolvedBy} in {Duration}ms",
                successfulAlerts.Count, request.AlertIds.Count, request.ResolvedBy, stopwatch.ElapsedMilliseconds);

            // Return appropriate status based on results
            if (!successfulAlerts.Any())
            {
                return BadRequest(CreateErrorResponse(response.Message, "BULK_RESOLVE_ALL_FAILED"));
            }

            if (failedAlerts.Any())
            {
                return StatusCode(207, CreateSuccessResponse(response, response.Message)); // 207 Multi-Status for partial success
            }

            return Ok(CreateSuccessResponse(response, response.Message));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Bulk resolve alerts operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Bulk resolve alerts operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (ObjectDisposedException ex)
        {
            Logger.LogError(ex, "Service provider disposed while bulk resolving alerts");
            return StatusCode(503, CreateErrorResponse("Service temporarily unavailable. Please try again.", "SERVICE_UNAVAILABLE"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error bulk resolving alerts");
            return StatusCode(500, CreateErrorResponse($"Failed to bulk resolve alerts: {ex.Message}", "BULK_RESOLVE_ERROR"));
        }
    }

    /// <summary>
    /// Maps an alert entity to an enhanced alert response DTO
    /// </summary>
    private AlertResponse MapToAlertResponse(Core.Entities.AlertLog alert, bool includeDetails = false)
    {
        var now = DateTime.UtcNow;
        var timeSinceTriggered = now - alert.TriggerTime;
        var timeToResolution = alert.IsResolved && alert.ResolvedTime.HasValue
            ? alert.ResolvedTime.Value - alert.TriggerTime
            : (TimeSpan?)null;

        var severity = alert.DeviationPercent switch
        {
            >= 50 => "Critical",
            >= 25 => "High",
            >= 10 => "Medium",
            _ => "Low"
        };

        var response = new AlertResponse
        {
            AlertId = (int)alert.AlertId,
            IndicatorId = alert.IndicatorId,
            IndicatorName = alert.Indicator?.IndicatorName ?? "Unknown",
            IndicatorOwner = alert.Indicator?.OwnerContactId.ToString() ?? "Unknown",
            TriggerTime = alert.TriggerTime,
            Message = alert.Message ?? "No message",
            Details = alert.Details,
            SentVia = alert.SentVia.ToString() ?? "Unknown",
            SentTo = alert.SentTo,
            CurrentValue = alert.CurrentValue,
            HistoricalValue = alert.HistoricalValue,
            DeviationPercent = alert.DeviationPercent,
            IsResolved = alert.IsResolved,
            ResolvedTime = alert.ResolvedTime,
            ResolvedBy = alert.ResolvedBy,
            Severity = severity,
            TimeSinceTriggered = timeSinceTriggered,
            TimeToResolution = timeToResolution
        };

        if (includeDetails && alert.Indicator != null)
        {
            response.Indicator = new AlertIndicatorInfo
            {
                IndicatorId = (int)alert.Indicator.IndicatorID,
                IndicatorName = alert.Indicator.IndicatorName,
                IndicatorDescription = alert.Indicator.IndicatorDesc,
                OwnerContactId = alert.Indicator.OwnerContactId,
                IsActive = alert.Indicator.IsActive,
                LastRun = alert.Indicator.LastRun
            };

            response.AdditionalDetails = new Dictionary<string, object>
            {
                ["AlertAge"] = timeSinceTriggered.TotalHours,
                ["SeverityScore"] = GetSeverityScore(severity),
                ["IsOverdue"] = !alert.IsResolved && timeSinceTriggered.TotalHours > 24
            };
        }

        return response;
    }

    /// <summary>
    /// Calculates average time to resolution for resolved alerts
    /// </summary>
    private TimeSpan? CalculateAverageTimeToResolution(IEnumerable<AlertResponse> resolvedAlerts)
    {
        var resolutionTimes = resolvedAlerts
            .Where(a => a.TimeToResolution.HasValue)
            .Select(a => a.TimeToResolution!.Value)
            .ToList();

        if (!resolutionTimes.Any())
            return null;

        var averageTicks = (long)resolutionTimes.Average(t => t.Ticks);
        return new TimeSpan(averageTicks);
    }

    /// <summary>
    /// Gets numeric severity score for sorting/calculations
    /// </summary>
    private int GetSeverityScore(string severity) => severity switch
    {
        "Critical" => 4,
        "High" => 3,
        "Medium" => 2,
        "Low" => 1,
        _ => 0
    };

    /// <summary>
    /// Calculates system health score based on alert dashboard data
    /// </summary>
    private double CalculateSystemHealthScore(dynamic dashboard)
    {
        var score = 100.0; // Start with perfect score

        // Deduct points for unresolved alerts
        if (dashboard.UnresolvedAlerts > 0)
        {
            score -= Math.Min(dashboard.UnresolvedAlerts * 2, 30); // Max 30 points deduction
        }

        // Deduct points for critical alerts
        if (dashboard.CriticalAlerts > 0)
        {
            score -= Math.Min(dashboard.CriticalAlerts * 5, 40); // Max 40 points deduction
        }

        // Deduct points for recent alert trend
        if (dashboard.AlertTrendPercentage > 0)
        {
            score -= Math.Min(dashboard.AlertTrendPercentage / 2, 20); // Max 20 points deduction
        }

        // Deduct points for high alert volume today
        if (dashboard.TotalAlertsToday > 10)
        {
            score -= Math.Min((dashboard.TotalAlertsToday - 10) * 0.5, 10); // Max 10 points deduction
        }

        return Math.Max(score, 0); // Ensure score doesn't go below 0
    }
}
