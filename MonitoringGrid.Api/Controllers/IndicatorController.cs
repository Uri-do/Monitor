using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.DTOs.Common;
using MonitoringGrid.Api.DTOs.Indicators;
using MonitoringGrid.Core.Interfaces;
using System.Diagnostics;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Enhanced API controller for managing Indicators using CQRS pattern with enterprise-grade features
/// </summary>
[ApiController]
[Route("api/indicator")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
public class IndicatorController : BaseApiController
{
    private readonly IMapper _mapper;
    private readonly IProgressPlayDbService _progressPlayDbService;

    public IndicatorController(
        IMediator mediator,
        IMapper mapper,
        IProgressPlayDbService progressPlayDbService,
        ILogger<IndicatorController> logger,
        IPerformanceMetricsService? performanceMetrics = null)
        : base(mediator, logger)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _progressPlayDbService = progressPlayDbService ?? throw new ArgumentNullException(nameof(progressPlayDbService));
    }

    /// <summary>
    /// Get all indicators with optional filtering and pagination
    /// </summary>
    /// <param name="request">Get indicators request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated indicators with enhanced information</returns>
    [HttpGet]
    [AllowAnonymous] // Temporarily allow anonymous access for development
    [ProducesResponseType(typeof(PaginatedIndicatorsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaginatedIndicatorsResponse>> GetIndicators(
        [FromQuery] GetIndicatorsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetIndicatorsRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Getting indicators with filters - Page: {Page}, PageSize: {PageSize}",
                request.Page, request.PageSize);

            var query = _mapper.Map<GetIndicatorsQuery>(request);
            var result = await Mediator.Send(query, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                var response = _mapper.Map<PaginatedIndicatorsResponse>(result.Value);
                response.QueryMetrics = new QueryMetrics
                {
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryCount = 1,
                    CacheHit = false
                };

                Logger.LogInformation("Retrieved {Count} indicators in {Duration}ms",
                    response.Indicators.Count, stopwatch.ElapsedMilliseconds);

                // 🔍 DETAILED RESPONSE LOGGING FOR DEBUGGING
                var finalResponse = CreateSuccessResponse(response, $"Retrieved {response.Indicators.Count} indicators");
                Logger.LogInformation("🔍 SENDING RESPONSE TO FRONTEND:");
                Logger.LogInformation("📊 Response Type: {ResponseType}", finalResponse.GetType().Name);
                Logger.LogInformation("📊 Indicators Count: {Count}", response.Indicators.Count);
                Logger.LogInformation("📊 Total Count: {TotalCount}", response.TotalCount);
                Logger.LogInformation("📊 Page: {Page}, PageSize: {PageSize}", response.Page, response.PageSize);

                // Log first few indicator names for verification
                var indicatorNames = response.Indicators.Take(5).Select(i => $"{i.IndicatorID}:{i.IndicatorName}").ToList();
                Logger.LogInformation("📊 First 5 Indicators: {IndicatorNames}", string.Join(", ", indicatorNames));

                // Log the actual JSON structure being sent
                try
                {
                    var jsonResponse = System.Text.Json.JsonSerializer.Serialize(finalResponse, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });
                    Logger.LogInformation("📊 JSON Response Length: {Length} characters", jsonResponse.Length);
                    Logger.LogInformation("📊 JSON Response Preview: {JsonPreview}...", jsonResponse.Length > 500 ? jsonResponse.Substring(0, 500) : jsonResponse);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Failed to serialize response for logging: {Error}", ex.Message);
                }

                return Ok(finalResponse);
            }

            return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Failed to retrieve indicators", "GET_INDICATORS_ERROR"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get indicators operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Get indicators operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving indicators: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to retrieve indicators", "GET_INDICATORS_ERROR"));
        }
    }

    /// <summary>
    /// Get indicator by ID
    /// </summary>
    /// <param name="id">Indicator ID</param>
    /// <param name="request">Get indicator request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced indicator information</returns>
    [HttpGet("{id:long}")]
    [AllowAnonymous] // Temporarily allow anonymous access for development
    [ProducesResponseType(typeof(IndicatorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndicatorResponse>> GetIndicator(
        long id,
        [FromQuery] bool includeDetails = true,
        [FromQuery] bool includeHistory = false,
        [FromQuery] bool includeScheduler = true,
        [FromQuery] bool includeCollector = true,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Create request object manually to avoid model binding issues
            var request = new GetIndicatorRequest
            {
                IndicatorId = id,
                IncludeDetails = includeDetails,
                IncludeHistory = includeHistory,
                IncludeScheduler = includeScheduler,
                IncludeCollector = includeCollector
            };

            // Validate the manually created request
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(request);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(request, validationContext, validationResults, true))
            {
                var errors = validationResults.ToDictionary(
                    vr => vr.MemberNames.FirstOrDefault() ?? "Unknown",
                    vr => new[] { vr.ErrorMessage ?? "Validation error" }
                );
                return BadRequest(CreateValidationErrorResponse(errors));
            }

            // Additional validation for ID
            var paramValidation = ValidateParameter(id, nameof(id),
                indicatorId => indicatorId > 0, "Indicator ID must be positive");
            if (paramValidation != null) return BadRequest(paramValidation);

            Logger.LogDebug("Getting indicator {IndicatorId}", id);

            var query = new GetIndicatorByIdQuery(id);
            var result = await Mediator.Send(query, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                var indicator = result.Value;

                // Fetch collector name if needed
                string? collectorName = null;
                try
                {
                    var collector = await _progressPlayDbService.GetCollectorByIdAsync(indicator.CollectorID, cancellationToken);
                    collectorName = collector?.CollectorDesc ?? collector?.CollectorCode ?? "Unknown";
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to fetch collector name for collector {CollectorId}", indicator.CollectorID);
                    collectorName = "Unknown";
                }

                // Create a custom response object with all the fields the frontend expects
                var response = new
                {
                    indicatorID = indicator.IndicatorID,
                    indicatorName = indicator.IndicatorName,
                    indicatorCode = indicator.IndicatorCode,
                    indicatorDescription = indicator.IndicatorDesc,
                    collectorId = indicator.CollectorID,
                    collectorName = collectorName,
                    collectorItemName = indicator.CollectorItemName,
                    schedulerId = indicator.SchedulerID,
                    isActive = indicator.IsActive,
                    lastMinutes = indicator.LastMinutes,
                    thresholdType = indicator.ThresholdType,
                    thresholdField = indicator.ThresholdField,
                    thresholdComparison = indicator.ThresholdComparison,
                    thresholdValue = indicator.ThresholdValue,
                    alertThreshold = indicator.ThresholdValue,
                    alertOperator = indicator.ThresholdComparison,
                    priority = indicator.Priority,
                    ownerContactId = indicator.OwnerContactId,
                    ownerName = indicator.OwnerContact?.Name,
                    averageLastDays = indicator.AverageLastDays,
                    createdDate = indicator.CreatedDate,
                    modifiedDate = indicator.UpdatedDate,
                    lastRun = indicator.LastRun,
                    lastRunResult = indicator.LastRunResult,
                    isCurrentlyRunning = indicator.IsCurrentlyRunning,
                    executionStartTime = indicator.ExecutionStartTime,
                    executionContext = indicator.ExecutionContext,
                    ownerContact = indicator.OwnerContact != null ? new
                    {
                        contactID = indicator.OwnerContact.ContactId,
                        name = indicator.OwnerContact.Name,
                        email = indicator.OwnerContact.Email,
                        phone = indicator.OwnerContact.Phone,
                        isActive = indicator.OwnerContact.IsActive,
                        createdDate = indicator.OwnerContact.CreatedDate,
                        modifiedDate = indicator.OwnerContact.ModifiedDate
                    } : null,
                    contacts = indicator.IndicatorContacts?.Where(ic => ic.IsActive && ic.Contact != null)
                        .Select(ic => new
                        {
                            contactID = ic.Contact.ContactId,
                            name = ic.Contact.Name,
                            email = ic.Contact.Email,
                            phone = ic.Contact.Phone,
                            isActive = ic.Contact.IsActive,
                            createdDate = ic.Contact.CreatedDate,
                            modifiedDate = ic.Contact.ModifiedDate
                        }).ToArray() ?? new object[0],
                    scheduler = indicator.Scheduler != null ? new
                    {
                        schedulerId = indicator.Scheduler.SchedulerID,
                        schedulerName = indicator.Scheduler.SchedulerName,
                        schedulerDescription = indicator.Scheduler.SchedulerDescription,
                        scheduleType = indicator.Scheduler.ScheduleType,
                        intervalMinutes = indicator.Scheduler.IntervalMinutes,
                        cronExpression = indicator.Scheduler.CronExpression,
                        isEnabled = indicator.Scheduler.IsEnabled,
                        timezone = indicator.Scheduler.Timezone,
                        executionDateTime = indicator.Scheduler.ExecutionDateTime,
                        startDate = indicator.Scheduler.StartDate,
                        endDate = indicator.Scheduler.EndDate,
                        createdDate = indicator.Scheduler.CreatedDate,
                        modifiedDate = indicator.Scheduler.ModifiedDate,
                        nextExecution = indicator.Scheduler.GetNextExecutionTime(indicator.LastRun)
                    } : null
                };

                Logger.LogInformation("Retrieved indicator {IndicatorId} in {Duration}ms",
                    id, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(response, $"Retrieved indicator {indicator.IndicatorName}"));
            }

            Logger.LogWarning("Indicator {IndicatorId} not found", id);
            return NotFound(CreateErrorResponse($"Indicator {id} not found", "INDICATOR_NOT_FOUND"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get indicator operation was cancelled for indicator {IndicatorId}", id);
            return StatusCode(499, CreateErrorResponse("Get indicator operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving indicator {IndicatorId}: {Message}", id, ex.Message);
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve indicator {id}", "GET_INDICATOR_ERROR"));
        }
    }

    /// <summary>
    /// Create a new indicator
    /// </summary>
    /// <param name="request">Create indicator request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced indicator creation response</returns>
    [HttpPost]
    [ProducesResponseType(typeof(IndicatorOperationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndicatorOperationResponse>> CreateIndicator(
        [FromBody] CreateIndicatorRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Creating indicator {IndicatorName}", request.IndicatorName);

            var command = _mapper.Map<CreateIndicatorCommand>(request);
            var result = await Mediator.Send(command, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                var response = new IndicatorOperationResponse
                {
                    Success = true,
                    Message = $"Indicator '{request.IndicatorName}' created successfully",
                    IndicatorIds = new List<long> { result.Value.IndicatorID },
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Details = new Dictionary<string, object>
                    {
                        ["IndicatorName"] = request.IndicatorName,
                        ["OwnerContactId"] = request.OwnerContactId,
                        ["CollectorId"] = request.CollectorId,
                        ["IsActive"] = request.IsActive,
                        ["LastMinutes"] = request.LastMinutes
                    }
                };

                Logger.LogInformation("Created indicator {IndicatorName} with ID {IndicatorId} in {Duration}ms",
                    request.IndicatorName, result.Value.IndicatorID, stopwatch.ElapsedMilliseconds);

                return CreatedAtAction(nameof(GetIndicator),
                    new { id = result.Value.IndicatorID },
                    CreateSuccessResponse(response, response.Message));
            }

            return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Failed to create indicator", "CREATE_INDICATOR_ERROR"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Create indicator operation was cancelled for indicator {IndicatorName}", request.IndicatorName);
            return StatusCode(499, CreateErrorResponse("Create indicator operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error creating indicator {IndicatorName}: {Message}", request.IndicatorName, ex.Message);
            return StatusCode(500, CreateErrorResponse($"Failed to create indicator '{request.IndicatorName}'", "CREATE_INDICATOR_ERROR"));
        }
    }

    /// <summary>
    /// Update an existing indicator
    /// </summary>
    /// <param name="id">Indicator ID</param>
    /// <param name="request">Update indicator request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced indicator update response</returns>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(IndicatorOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndicatorOperationResponse>> UpdateIndicator(
        long id,
        [FromBody] UpdateIndicatorRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate ID matches route parameter
            if (id != request.IndicatorID)
            {
                request.IndicatorID = id; // Use route parameter
            }

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Additional validation for ID
            var paramValidation = ValidateParameter(id, nameof(id),
                indicatorId => indicatorId > 0, "Indicator ID must be positive");
            if (paramValidation != null) return BadRequest(paramValidation);

            Logger.LogDebug("Updating indicator {IndicatorId} - {IndicatorName}", id, request.IndicatorName);

            var command = _mapper.Map<UpdateIndicatorCommand>(request);
            var result = await Mediator.Send(command, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                var response = new IndicatorOperationResponse
                {
                    Success = true,
                    Message = $"Indicator '{request.IndicatorName}' updated successfully",
                    IndicatorIds = new List<long> { id },
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Details = new Dictionary<string, object>
                    {
                        ["IndicatorName"] = request.IndicatorName,
                        ["UpdateReason"] = request.UpdateReason ?? "No reason provided",
                        ["OwnerContactId"] = request.OwnerContactId,
                        ["CollectorId"] = request.CollectorId,
                        ["IsActive"] = request.IsActive,
                        ["LastMinutes"] = request.LastMinutes
                    }
                };

                Logger.LogInformation("Updated indicator {IndicatorId} ({IndicatorName}) in {Duration}ms. Reason: {Reason}",
                    id, request.IndicatorName, stopwatch.ElapsedMilliseconds, request.UpdateReason);

                return Ok(CreateSuccessResponse(response, response.Message));
            }

            // Check if it's a not found error
            if (result.Error?.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                Logger.LogWarning("Indicator {IndicatorId} not found for update", id);
                return NotFound(CreateErrorResponse($"Indicator {id} not found", "INDICATOR_NOT_FOUND"));
            }

            return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Failed to update indicator", "UPDATE_INDICATOR_ERROR"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Update indicator operation was cancelled for indicator {IndicatorId}", id);
            return StatusCode(499, CreateErrorResponse("Update indicator operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error updating indicator {IndicatorId}: {Message}", id, ex.Message);
            return StatusCode(500, CreateErrorResponse($"Failed to update indicator {id}", "UPDATE_INDICATOR_ERROR"));
        }
    }

    /// <summary>
    /// Delete an indicator
    /// </summary>
    /// <param name="id">Indicator ID</param>
    /// <param name="request">Delete indicator request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced indicator deletion response</returns>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(IndicatorOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndicatorOperationResponse>> DeleteIndicator(
        long id,
        [FromQuery] DeleteIndicatorRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new DeleteIndicatorRequest { IndicatorId = id };
            request.IndicatorId = id; // Ensure route parameter takes precedence

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Additional validation for ID
            var paramValidation = ValidateParameter(id, nameof(id),
                indicatorId => indicatorId > 0, "Indicator ID must be positive");
            if (paramValidation != null) return BadRequest(paramValidation);

            Logger.LogDebug("Deleting indicator {IndicatorId}. Force: {Force}, Archive: {Archive}, Reason: {Reason}",
                id, request.Force, request.ArchiveData, request.DeletionReason);

            var command = new DeleteIndicatorCommand(id);
            var result = await Mediator.Send(command, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                var response = new IndicatorOperationResponse
                {
                    Success = true,
                    Message = $"Indicator {id} deleted successfully",
                    IndicatorIds = new List<long> { id },
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Details = new Dictionary<string, object>
                    {
                        ["DeletionReason"] = request.DeletionReason ?? "No reason provided",
                        ["Force"] = request.Force,
                        ["ArchiveData"] = request.ArchiveData,
                        ["DeletionTime"] = DateTime.UtcNow
                    }
                };

                Logger.LogInformation("Deleted indicator {IndicatorId} in {Duration}ms. Reason: {Reason}",
                    id, stopwatch.ElapsedMilliseconds, request.DeletionReason);

                return Ok(CreateSuccessResponse(response, response.Message));
            }

            // Check if it's a not found error
            if (result.Error?.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                Logger.LogWarning("Indicator {IndicatorId} not found for deletion", id);
                return NotFound(CreateErrorResponse($"Indicator {id} not found", "INDICATOR_NOT_FOUND"));
            }

            return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Failed to delete indicator", "DELETE_INDICATOR_ERROR"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Delete indicator operation was cancelled for indicator {IndicatorId}", id);
            return StatusCode(499, CreateErrorResponse("Delete indicator operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error deleting indicator {IndicatorId}: {Message}", id, ex.Message);
            return StatusCode(500, CreateErrorResponse($"Failed to delete indicator {id}", "DELETE_INDICATOR_ERROR"));
        }
    }

    /// <summary>
    /// Execute an indicator manually
    /// </summary>
    /// <param name="id">Indicator ID</param>
    /// <param name="request">Execute indicator request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced indicator execution result</returns>
    [HttpPost("{id:long}/execute")]
    [ProducesResponseType(typeof(IndicatorExecutionResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndicatorExecutionResultResponse>> ExecuteIndicator(
        long id,
        [FromBody] ExecuteIndicatorRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new ExecuteIndicatorRequest { IndicatorId = id };
            request.IndicatorId = id; // Ensure route parameter takes precedence

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Additional validation for ID
            var paramValidation = ValidateParameter(id, nameof(id),
                indicatorId => indicatorId > 0, "Indicator ID must be positive");
            if (paramValidation != null) return BadRequest(paramValidation);

            Logger.LogDebug("Executing indicator {IndicatorId} - Context: {Context}, SaveResults: {SaveResults}, Timeout: {Timeout}s",
                id, request.ExecutionContext, request.SaveResults, request.TimeoutSeconds);

            var command = new ExecuteIndicatorCommand(
                id,
                request.ExecutionContext ?? "Manual",
                request.SaveResults);

            var result = await Mediator.Send(command, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                var response = _mapper.Map<IndicatorExecutionResultResponse>(result.Value);

                // Add execution metadata
                response.Details ??= new Dictionary<string, object>();
                response.Details["RequestDurationMs"] = stopwatch.ElapsedMilliseconds;
                response.Details["ExecutionContext"] = request.ExecutionContext ?? "Manual";
                response.Details["SaveResults"] = request.SaveResults;
                response.Details["TimeoutSeconds"] = request.TimeoutSeconds;
                response.Details["SendAlerts"] = request.SendAlerts;

                Logger.LogInformation("Executed indicator {IndicatorId} in {Duration}ms - Status: {Status}, Results: {ResultCount}",
                    id, stopwatch.ElapsedMilliseconds, response.Status, response.ResultCount);

                return Ok(CreateSuccessResponse(response, $"Indicator executed successfully - {response.Status}"));
            }

            // Check if it's a not found error
            if (result.Error?.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
            {
                Logger.LogWarning("Indicator {IndicatorId} not found for execution", id);
                return NotFound(CreateErrorResponse($"Indicator {id} not found", "INDICATOR_NOT_FOUND"));
            }

            return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Failed to execute indicator", "EXECUTE_INDICATOR_ERROR"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Execute indicator operation was cancelled for indicator {IndicatorId}", id);
            return StatusCode(499, CreateErrorResponse("Execute indicator operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error executing indicator {IndicatorId}: {Message}", id, ex.Message);
            return StatusCode(500, CreateErrorResponse($"Failed to execute indicator {id}", "EXECUTE_INDICATOR_ERROR"));
        }
    }

    /// <summary>
    /// Get indicator dashboard data
    /// </summary>
    /// <param name="request">Get dashboard request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced indicator dashboard with analytics</returns>
    [HttpGet("dashboard")]
    [AllowAnonymous] // Temporarily allow anonymous access for development
    [ProducesResponseType(typeof(IndicatorDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndicatorDashboardResponse>> GetDashboard(
        [FromQuery] GetIndicatorDashboardRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetIndicatorDashboardRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Getting indicator dashboard - TrendHours: {TrendHours}, IncludeStatistics: {IncludeStatistics}, RefreshCache: {RefreshCache}",
                request.TrendHours, request.IncludeStatistics, request.RefreshCache);

            var query = new GetIndicatorDashboardQuery();
            var result = await Mediator.Send(query, cancellationToken);

            stopwatch.Stop();

            if (result.IsSuccess)
            {
                var response = _mapper.Map<IndicatorDashboardResponse>(result.Value);

                // Add generation metadata
                response.GeneratedAt = DateTime.UtcNow;
                response.GenerationTimeMs = stopwatch.ElapsedMilliseconds;

                Logger.LogInformation("Generated indicator dashboard in {Duration}ms - Total: {Total}, Active: {Active}, Running: {Running}",
                    stopwatch.ElapsedMilliseconds, response.TotalIndicators, response.ActiveIndicators, response.RunningIndicators);

                return Ok(CreateSuccessResponse(response, "Indicator dashboard generated successfully"));
            }

            return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Failed to generate dashboard", "GET_DASHBOARD_ERROR"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get dashboard operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Get dashboard operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error getting indicator dashboard: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to generate indicator dashboard", "GET_DASHBOARD_ERROR"));
        }
    }
}
