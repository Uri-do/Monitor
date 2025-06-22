using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.DTOs.Security;
using MonitoringGrid.Core.Interfaces.Security;
using MonitoringGrid.Core.Security;
using System.Diagnostics;
using AutoMapper;
using MediatR;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Controller for security audit operations (events, logs, monitoring)
/// </summary>
[ApiController]
[Route("api/security/audit")]
[Authorize]
[Produces("application/json")]
public class SecurityAuditController : BaseApiController
{
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IMapper _mapper;

    public SecurityAuditController(
        IMediator mediator,
        ISecurityAuditService securityAuditService,
        IMapper mapper,
        ILogger<SecurityAuditController> logger)
        : base(mediator, logger)
    {
        _securityAuditService = securityAuditService;
        _mapper = mapper;
    }

    /// <summary>
    /// Get security events with filtering and pagination
    /// </summary>
    /// <param name="startDate">Start date filter</param>
    /// <param name="endDate">End date filter</param>
    /// <param name="userId">User ID filter</param>
    /// <param name="eventType">Event type filter</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of security events</returns>
    [HttpGet("events")]
    [ProducesResponseType(typeof(PagedResponse<SecurityEventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SecurityEventResponse>>> GetSecurityEvents(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogDebug("Retrieving security events with filters: StartDate={StartDate}, EndDate={EndDate}, UserId={UserId}, EventType={EventType}, Page={Page}, PageSize={PageSize}",
                startDate, endDate, userId, eventType, page, pageSize);

            // Set default date range if not provided
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            // Validate date range
            if (startDate > endDate)
            {
                return BadRequest(CreateErrorResponse("Start date cannot be after end date", "INVALID_DATE_RANGE"));
            }

            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 1000) pageSize = 50;

            var events = await _securityAuditService.GetSecurityEventsAsync(startDate, endDate, userId, cancellationToken);

            // Apply additional filtering
            if (!string.IsNullOrEmpty(eventType))
            {
                events = events.Where(e => e.EventType.Contains(eventType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Apply pagination
            var totalCount = events.Count;
            var pagedEvents = events
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var eventResponses = pagedEvents.Select(e => new SecurityEventResponse
            {
                EventId = e.EventId,
                EventType = e.EventType,
                Action = e.Action ?? string.Empty,
                Resource = e.Resource,
                UserId = e.UserId,
                Username = e.Username,
                IpAddress = e.IpAddress,
                UserAgent = e.UserAgent,
                Timestamp = e.Timestamp,
                IsSuccess = e.IsSuccess,
                AdditionalData = e.AdditionalData ?? new Dictionary<string, object>(),
                RiskScore = 0, // Calculate based on event type and success
                TimeSinceEvent = DateTime.UtcNow - e.Timestamp
            }).ToList();

            stopwatch.Stop();

            var response = new PagedResponse<SecurityEventResponse>
            {
                Data = eventResponses,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1
            };

            Logger.LogInformation("Retrieved {Count} security events (page {Page}/{TotalPages}) in {Duration}ms",
                eventResponses.Count, page, response.TotalPages, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, $"Retrieved {eventResponses.Count} security events"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving security events: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to retrieve security events", "SECURITY_EVENTS_ERROR"));
        }
    }

    /// <summary>
    /// Get security event by ID
    /// </summary>
    /// <param name="id">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Security event details</returns>
    [HttpGet("events/{id}")]
    [ProducesResponseType(typeof(SecurityEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SecurityEventResponse>> GetSecurityEvent(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _securityAuditService.GetSecurityEventsAsync(null, null, null, cancellationToken);
            var securityEvent = events.FirstOrDefault(e => e.EventId == id.ToString());

            if (securityEvent == null)
            {
                return NotFound(CreateErrorResponse("Security event not found", "EVENT_NOT_FOUND"));
            }

            var response = new SecurityEventResponse
            {
                EventId = securityEvent.EventId,
                EventType = securityEvent.EventType,
                Action = securityEvent.Action ?? string.Empty,
                Resource = securityEvent.Resource,
                UserId = securityEvent.UserId,
                Username = securityEvent.Username,
                IpAddress = securityEvent.IpAddress,
                UserAgent = securityEvent.UserAgent,
                Timestamp = securityEvent.Timestamp,
                IsSuccess = securityEvent.IsSuccess,
                AdditionalData = securityEvent.AdditionalData ?? new Dictionary<string, object>(),
                RiskScore = 0, // Calculate based on event type and success
                TimeSinceEvent = DateTime.UtcNow - securityEvent.Timestamp
            };

            return Ok(CreateSuccessResponse(response, "Security event retrieved successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving security event {Id}: {Message}", id, ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to retrieve security event", "SECURITY_EVENT_ERROR"));
        }
    }

    /// <summary>
    /// Analyze security patterns for suspicious activity
    /// </summary>
    /// <param name="startTime">Analysis start time</param>
    /// <param name="endTime">Analysis end time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Security analysis results</returns>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(SecurityAnalysisResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SecurityAnalysisResponse>> AnalyzeSecurityPatterns(
        [FromBody] SecurityAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Starting security pattern analysis from {StartTime} to {EndTime}",
                request.StartTime, request.EndTime);

            var result = await _securityAuditService.AnalyzeSecurityPatternsAsync(
                request.StartTime, request.EndTime, cancellationToken);

            stopwatch.Stop();

            var response = new SecurityAnalysisResponse
            {
                AnalysisId = Guid.NewGuid().ToString(),
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                AnalysisDate = DateTime.UtcNow,
                HasSuspiciousActivity = result.IsSuccess && result.Value,
                AnalysisDurationMs = stopwatch.ElapsedMilliseconds,
                Summary = result.IsSuccess && result.Value 
                    ? "Suspicious activity patterns detected" 
                    : "No suspicious activity patterns found",
                Details = new Dictionary<string, object>
                {
                    ["AnalysisType"] = "SecurityPatterns",
                    ["TimeRange"] = $"{request.StartTime:yyyy-MM-dd HH:mm:ss} - {request.EndTime:yyyy-MM-dd HH:mm:ss}",
                    ["AnalysisResult"] = result.IsSuccess ? "Success" : "Failed",
                    ["ErrorMessage"] = result.Error?.Message
                }
            };

            if (result.IsSuccess && result.Value)
            {
                Logger.LogWarning("Suspicious security patterns detected in analysis from {StartTime} to {EndTime}",
                    request.StartTime, request.EndTime);
            }
            else
            {
                Logger.LogInformation("Security pattern analysis completed - no suspicious activity found from {StartTime} to {EndTime}",
                    request.StartTime, request.EndTime);
            }

            return Ok(CreateSuccessResponse(response, response.Summary));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error during security pattern analysis: {Message}", ex.Message);

            var errorResponse = new SecurityAnalysisResponse
            {
                AnalysisId = Guid.NewGuid().ToString(),
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                AnalysisDate = DateTime.UtcNow,
                HasSuspiciousActivity = false,
                AnalysisDurationMs = stopwatch.ElapsedMilliseconds,
                Summary = "Analysis failed due to error",
                Details = new Dictionary<string, object>
                {
                    ["Error"] = ex.Message,
                    ["AnalysisResult"] = "Failed"
                }
            };

            return StatusCode(500, CreateErrorResponse("Security analysis failed", "SECURITY_ANALYSIS_ERROR"));
        }
    }

    /// <summary>
    /// Get security statistics summary
    /// </summary>
    /// <param name="days">Number of days to analyze (default: 7)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Security statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(SecurityStatisticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SecurityStatisticsResponse>> GetSecurityStatistics(
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (days < 1 || days > 365) days = 7;

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-days);

            Logger.LogDebug("Generating security statistics for the last {Days} days", days);

            var events = await _securityAuditService.GetSecurityEventsAsync(startDate, endDate, null, cancellationToken);

            stopwatch.Stop();

            var response = new SecurityStatisticsResponse
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalEvents = events.Count,
                SuccessfulEvents = events.Count(e => e.IsSuccess),
                FailedEvents = events.Count(e => !e.IsSuccess),
                UniqueUsers = events.Where(e => !string.IsNullOrEmpty(e.UserId)).Select(e => e.UserId!).Distinct().Count(),
                UniqueIpAddresses = events.Where(e => !string.IsNullOrEmpty(e.IpAddress)).Select(e => e.IpAddress).Distinct().Count(),
                EventsByType = events.GroupBy(e => e.EventType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByDay = events.GroupBy(e => e.Timestamp.Date)
                    .ToDictionary(g => g.Key, g => g.Count()),
                GeneratedAt = DateTime.UtcNow,
                GenerationDurationMs = stopwatch.ElapsedMilliseconds
            };

            Logger.LogInformation("Generated security statistics for {Days} days: {TotalEvents} total events, {SuccessfulEvents} successful, {FailedEvents} failed",
                days, response.TotalEvents, response.SuccessfulEvents, response.FailedEvents);

            return Ok(CreateSuccessResponse(response, $"Security statistics for the last {days} days"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error generating security statistics: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to generate security statistics", "SECURITY_STATISTICS_ERROR"));
        }
    }
}
