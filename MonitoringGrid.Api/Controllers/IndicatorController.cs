using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs;
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

    public IndicatorController(
        IMediator mediator,
        IMapper mapper,
        ILogger<IndicatorController> logger,
        IPerformanceMetricsService? performanceMetrics = null)
        : base(mediator, logger)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Get all indicators with optional filtering and pagination
    /// </summary>
    /// <param name="request">Get indicators request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated indicators with enhanced information</returns>
    [HttpGet]
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

                return Ok(CreateSuccessResponse(response, $"Retrieved {response.Indicators.Count} indicators"));
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
    [ProducesResponseType(typeof(IndicatorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IndicatorResponse>> GetIndicator(
        long id,
        [FromQuery] GetIndicatorRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetIndicatorRequest { IndicatorId = id };
            request.IndicatorId = id; // Ensure route parameter takes precedence

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

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
                var response = _mapper.Map<IndicatorResponse>(result.Value);

                // Add query metrics if details requested
                if (request.IncludeDetails)
                {
                    response.Details ??= new Dictionary<string, object>();
                    response.Details["QueryDurationMs"] = stopwatch.ElapsedMilliseconds;
                    response.Details["RequestedDetails"] = request.IncludeDetails;
                    response.Details["RequestedHistory"] = request.IncludeHistory;
                }

                Logger.LogInformation("Retrieved indicator {IndicatorId} in {Duration}ms",
                    id, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(response, $"Retrieved indicator {response.IndicatorName}"));
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
