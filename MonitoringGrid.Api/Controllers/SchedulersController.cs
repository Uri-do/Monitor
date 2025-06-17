using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.DTOs.Schedulers;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Interfaces;
using System.Diagnostics;

namespace MonitoringGrid.Api.Controllers
{
    /// <summary>
    /// API controller for managing scheduler configurations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public class SchedulersController : BaseApiController
    {
        private readonly ISchedulerService _schedulerService;

        public SchedulersController(
            IMediator mediator,
            ISchedulerService schedulerService,
            ILogger<SchedulersController> logger)
            : base(mediator, logger)
        {
            _schedulerService = schedulerService;
        }

        /// <summary>
        /// Get all schedulers
        /// </summary>
        /// <param name="request">Get schedulers request parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of schedulers</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedSchedulersResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedSchedulersResponse>> GetSchedulers(
            [FromQuery] GetSchedulersRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                request ??= new GetSchedulersRequest();

                var validationError = ValidateModelState();
                if (validationError != null) return BadRequest(validationError);

                var result = await _schedulerService.GetSchedulersAsync(!request.EnabledOnly);

                if (!result.IsSuccess)
                {
                    return StatusCode(500, CreateErrorResponse(result.Error?.Message ?? "Unknown error", "GET_SCHEDULERS_FAILED"));
                }

                var schedulers = result.Value;

                // Apply search filter if specified
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    schedulers = schedulers
                        .Where(s => s.SchedulerName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                   (s.SchedulerDescription?.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) == true))
                        .ToList();
                }

                // Apply pagination
                var totalCount = schedulers.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
                var skip = (request.Page - 1) * request.PageSize;
                var pagedSchedulers = schedulers.Skip(skip).Take(request.PageSize).ToList();

                stopwatch.Stop();

                // Map to enhanced response DTOs
                var schedulerResponses = pagedSchedulers.Select(s => new SchedulerResponse
                {
                    SchedulerID = s.SchedulerID,
                    SchedulerName = s.SchedulerName,
                    SchedulerDescription = s.SchedulerDescription,
                    ScheduleType = s.ScheduleType,
                    IntervalMinutes = s.IntervalMinutes,
                    CronExpression = s.CronExpression,
                    IsEnabled = s.IsEnabled,
                    Timezone = s.Timezone,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    CreatedDate = s.CreatedDate,
                    CreatedBy = s.CreatedBy,
                    ModifiedDate = s.ModifiedDate,
                    ModifiedBy = s.ModifiedBy,
                    Details = request.IncludeDetails ? new Dictionary<string, object>
                    {
                        ["QueryDurationMs"] = stopwatch.ElapsedMilliseconds,
                        ["FilterApplied"] = request.EnabledOnly ? "EnabledOnly" : "All",
                        ["SearchTerm"] = request.SearchTerm ?? "None"
                    } : null
                }).ToList();

                var response = new PaginatedSchedulersResponse
                {
                    Schedulers = schedulerResponses,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1,
                    QueryMetrics = new QueryMetrics
                    {
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        QueryCount = 1,
                        CacheHit = false
                    }
                };

                Logger.LogDebug("Retrieved {Count} schedulers (page {Page}/{TotalPages}) in {Duration}ms", schedulerResponses.Count, request.Page, totalPages, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(response, $"Retrieved {schedulerResponses.Count} schedulers"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Get schedulers operation was cancelled");
                return StatusCode(499, CreateErrorResponse("Get schedulers operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error getting schedulers");
                return StatusCode(500, CreateErrorResponse($"Error getting schedulers: {ex.Message}", "GET_SCHEDULERS_ERROR"));
            }
        }

        /// <summary>
        /// Get scheduler by ID
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <param name="request">Get scheduler request parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Scheduler details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(SchedulerResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SchedulerResponse>> GetScheduler(
            int id,
            [FromQuery] GetSchedulerRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                request ??= new GetSchedulerRequest { SchedulerId = id };

                // Validate scheduler ID matches route parameter
                if (request.SchedulerId != id)
                {
                    request.SchedulerId = id; // Use route parameter
                }

                var validationError = ValidateModelState();
                if (validationError != null) return BadRequest(validationError);

                // Additional validation for scheduler ID
                var paramValidation = ValidateParameter(id, nameof(id),
                    schedulerId => schedulerId > 0, "Scheduler ID must be a positive integer");
                if (paramValidation != null) return BadRequest(paramValidation);

                var result = await _schedulerService.GetSchedulerByIdAsync(id);

                if (!result.IsSuccess)
                {
                    return NotFound(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "SCHEDULER_NOT_FOUND"));
                }

                var scheduler = result.Value;
                stopwatch.Stop();

                var response = new SchedulerResponse
                {
                    SchedulerID = scheduler.SchedulerID,
                    SchedulerName = scheduler.SchedulerName,
                    SchedulerDescription = scheduler.SchedulerDescription,
                    ScheduleType = scheduler.ScheduleType,
                    IntervalMinutes = scheduler.IntervalMinutes,
                    CronExpression = scheduler.CronExpression,
                    IsEnabled = scheduler.IsEnabled,
                    Timezone = scheduler.Timezone,
                    StartDate = scheduler.StartDate,
                    EndDate = scheduler.EndDate,
                    CreatedDate = scheduler.CreatedDate,
                    CreatedBy = scheduler.CreatedBy,
                    ModifiedDate = scheduler.ModifiedDate,
                    ModifiedBy = scheduler.ModifiedBy,
                    Details = request.IncludeDetails ? new Dictionary<string, object>
                    {
                        ["QueryDurationMs"] = stopwatch.ElapsedMilliseconds,
                        ["RequestedIndicators"] = request.IncludeIndicators,
                        ["RequestedHistory"] = request.IncludeHistory
                    } : null
                };

                // Add indicators if requested
                if (request.IncludeIndicators)
                {
                    var indicatorsResult = await _schedulerService.GetIndicatorsBySchedulerAsync(id);
                    if (indicatorsResult.IsSuccess)
                    {
                        response.Indicators = indicatorsResult.Value.Select(i => new SchedulerIndicatorInfo
                        {
                            IndicatorID = i.IndicatorID,
                            IndicatorName = i.IndicatorName,
                            IsActive = i.IsActive,
                            LastRun = i.LastRun,
                            IsCurrentlyRunning = false // Core DTO doesn't have IsCurrentlyRunning property
                        }).ToList();
                    }
                }

                Logger.LogDebug("Retrieved scheduler {SchedulerId} in {Duration}ms",
                    id, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(response, $"Retrieved scheduler {id}"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Get scheduler operation was cancelled for scheduler {SchedulerId}", id);
                return StatusCode(499, CreateErrorResponse("Get scheduler operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error getting scheduler {SchedulerId}", id);
                return StatusCode(500, CreateErrorResponse($"Failed to retrieve scheduler {id}: {ex.Message}", "GET_SCHEDULER_ERROR"));
            }
        }

        /// <summary>
        /// Create a new scheduler
        /// </summary>
        /// <param name="request">Scheduler creation request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created scheduler</returns>
        [HttpPost]
        [ProducesResponseType(typeof(SchedulerOperationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SchedulerOperationResponse>> CreateScheduler(
            [FromBody] MonitoringGrid.Api.DTOs.Schedulers.CreateSchedulerRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var validationError = ValidateModelState();
                if (validationError != null) return BadRequest(validationError);

                // Map API DTO to Core DTO
                var coreRequest = new MonitoringGrid.Core.DTOs.CreateSchedulerRequest
                {
                    SchedulerName = request.SchedulerName,
                    SchedulerDescription = request.SchedulerDescription,
                    ScheduleType = request.ScheduleType,
                    IntervalMinutes = request.IntervalMinutes,
                    CronExpression = request.CronExpression,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Timezone = request.Timezone,
                    IsEnabled = request.IsEnabled
                };

                var result = await _schedulerService.CreateSchedulerAsync(coreRequest, GetCurrentUser());

                if (!result.IsSuccess)
                {
                    return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "CREATE_SCHEDULER_FAILED"));
                }

                var createdScheduler = result.Value;
                stopwatch.Stop();

                var schedulerResponse = new SchedulerResponse
                {
                    SchedulerID = createdScheduler.SchedulerID,
                    SchedulerName = createdScheduler.SchedulerName,
                    SchedulerDescription = createdScheduler.SchedulerDescription,
                    ScheduleType = createdScheduler.ScheduleType,
                    IntervalMinutes = createdScheduler.IntervalMinutes,
                    CronExpression = createdScheduler.CronExpression,
                    IsEnabled = createdScheduler.IsEnabled,
                    Timezone = createdScheduler.Timezone,
                    StartDate = createdScheduler.StartDate,
                    EndDate = createdScheduler.EndDate,
                    CreatedDate = createdScheduler.CreatedDate,
                    CreatedBy = createdScheduler.CreatedBy,
                    ModifiedDate = createdScheduler.ModifiedDate,
                    ModifiedBy = createdScheduler.ModifiedBy
                };

                var response = new SchedulerOperationResponse
                {
                    Success = true,
                    Message = $"Scheduler '{createdScheduler.SchedulerName}' created successfully",
                    SchedulerId = createdScheduler.SchedulerID,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Scheduler = schedulerResponse,
                    Details = new Dictionary<string, object>
                    {
                        ["CreatedBy"] = GetCurrentUser(),
                        ["ScheduleType"] = createdScheduler.ScheduleType,
                        ["IsEnabled"] = createdScheduler.IsEnabled
                    }
                };

                Logger.LogInformation("Created scheduler {SchedulerId} '{SchedulerName}' in {Duration}ms",
                    createdScheduler.SchedulerID, createdScheduler.SchedulerName, stopwatch.ElapsedMilliseconds);

                return CreatedAtAction(
                    nameof(GetScheduler),
                    new { id = createdScheduler.SchedulerID },
                    CreateSuccessResponse(response, response.Message));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Create scheduler operation was cancelled");
                return StatusCode(499, CreateErrorResponse("Create scheduler operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error creating scheduler");
                return StatusCode(500, CreateErrorResponse($"Failed to create scheduler: {ex.Message}", "CREATE_SCHEDULER_ERROR"));
            }
        }

        /// <summary>
        /// Update an existing scheduler
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <param name="request">Scheduler update request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated scheduler</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(SchedulerOperationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SchedulerOperationResponse>> UpdateScheduler(
            int id,
            [FromBody] MonitoringGrid.Api.DTOs.Schedulers.UpdateSchedulerRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate scheduler ID matches route parameter
                if (request.SchedulerId != id)
                {
                    request.SchedulerId = id; // Use route parameter
                }

                var validationError = ValidateModelState();
                if (validationError != null) return BadRequest(validationError);

                // Additional validation for scheduler ID
                var paramValidation = ValidateParameter(id, nameof(id),
                    schedulerId => schedulerId > 0, "Scheduler ID must be a positive integer");
                if (paramValidation != null) return BadRequest(paramValidation);

                // Map API DTO to Core DTO
                var coreRequest = new MonitoringGrid.Core.DTOs.UpdateSchedulerRequest
                {
                    SchedulerName = request.SchedulerName,
                    SchedulerDescription = request.SchedulerDescription,
                    ScheduleType = request.ScheduleType,
                    IntervalMinutes = request.IntervalMinutes,
                    CronExpression = request.CronExpression,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Timezone = request.Timezone,
                    IsEnabled = request.IsEnabled ?? false
                };

                var result = await _schedulerService.UpdateSchedulerAsync(id, coreRequest, GetCurrentUser());

                if (!result.IsSuccess)
                {
                    if (result.Error?.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return NotFound(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "SCHEDULER_NOT_FOUND"));
                    }
                    return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "UPDATE_SCHEDULER_FAILED"));
                }

                var updatedScheduler = result.Value;
                stopwatch.Stop();

                var schedulerResponse = new SchedulerResponse
                {
                    SchedulerID = updatedScheduler.SchedulerID,
                    SchedulerName = updatedScheduler.SchedulerName,
                    SchedulerDescription = updatedScheduler.SchedulerDescription,
                    ScheduleType = updatedScheduler.ScheduleType,
                    IntervalMinutes = updatedScheduler.IntervalMinutes,
                    CronExpression = updatedScheduler.CronExpression,
                    IsEnabled = updatedScheduler.IsEnabled,
                    Timezone = updatedScheduler.Timezone,
                    StartDate = updatedScheduler.StartDate,
                    EndDate = updatedScheduler.EndDate,
                    CreatedDate = updatedScheduler.CreatedDate,
                    CreatedBy = updatedScheduler.CreatedBy,
                    ModifiedDate = updatedScheduler.ModifiedDate,
                    ModifiedBy = updatedScheduler.ModifiedBy
                };

                var response = new SchedulerOperationResponse
                {
                    Success = true,
                    Message = $"Scheduler '{updatedScheduler.SchedulerName}' updated successfully",
                    SchedulerId = updatedScheduler.SchedulerID,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Scheduler = schedulerResponse,
                    Details = new Dictionary<string, object>
                    {
                        ["ModifiedBy"] = GetCurrentUser(),
                        ["ModifiedFields"] = GetModifiedFields(request),
                        ["PreviouslyEnabled"] = updatedScheduler.IsEnabled
                    }
                };

                Logger.LogInformation("Updated scheduler {SchedulerId} '{SchedulerName}' in {Duration}ms",
                    updatedScheduler.SchedulerID, updatedScheduler.SchedulerName, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(response, response.Message));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Update scheduler operation was cancelled for scheduler {SchedulerId}", id);
                return StatusCode(499, CreateErrorResponse("Update scheduler operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error updating scheduler {SchedulerId}", id);
                return StatusCode(500, CreateErrorResponse($"Failed to update scheduler {id}: {ex.Message}", "UPDATE_SCHEDULER_ERROR"));
            }
        }

        private List<string> GetModifiedFields(MonitoringGrid.Api.DTOs.Schedulers.UpdateSchedulerRequest request)
        {
            var modifiedFields = new List<string>();
            if (!string.IsNullOrEmpty(request.SchedulerName)) modifiedFields.Add("SchedulerName");
            if (!string.IsNullOrEmpty(request.SchedulerDescription)) modifiedFields.Add("SchedulerDescription");
            if (!string.IsNullOrEmpty(request.ScheduleType)) modifiedFields.Add("ScheduleType");
            if (request.IntervalMinutes.HasValue) modifiedFields.Add("IntervalMinutes");
            if (!string.IsNullOrEmpty(request.CronExpression)) modifiedFields.Add("CronExpression");
            if (request.IsEnabled.HasValue) modifiedFields.Add("IsEnabled");
            if (!string.IsNullOrEmpty(request.Timezone)) modifiedFields.Add("Timezone");
            if (request.StartDate.HasValue) modifiedFields.Add("StartDate");
            if (request.EndDate.HasValue) modifiedFields.Add("EndDate");
            return modifiedFields;
        }

        /// <summary>
        /// Delete a scheduler
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <param name="request">Delete scheduler request parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Delete operation result</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(SchedulerOperationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SchedulerOperationResponse>> DeleteScheduler(
            int id,
            [FromQuery] DeleteSchedulerRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                request ??= new DeleteSchedulerRequest { SchedulerId = id };

                // Validate scheduler ID matches route parameter
                if (request.SchedulerId != id)
                {
                    request.SchedulerId = id; // Use route parameter
                }

                var validationError = ValidateModelState();
                if (validationError != null) return BadRequest(validationError);

                // Additional validation for scheduler ID
                var paramValidation = ValidateParameter(id, nameof(id),
                    schedulerId => schedulerId > 0, "Scheduler ID must be a positive integer");
                if (paramValidation != null) return BadRequest(paramValidation);

                // Check if scheduler exists and get its details before deletion
                var getResult = await _schedulerService.GetSchedulerByIdAsync(id);
                if (!getResult.IsSuccess)
                {
                    return NotFound(CreateErrorResponse($"Scheduler {id} not found", "SCHEDULER_NOT_FOUND"));
                }

                var schedulerToDelete = getResult.Value;

                // Check for associated indicators if not forcing deletion
                if (!request.Force)
                {
                    var indicatorsResult = await _schedulerService.GetIndicatorsBySchedulerAsync(id);
                    if (indicatorsResult.IsSuccess && indicatorsResult.Value.Any())
                    {
                        return BadRequest(CreateErrorResponse(
                            $"Cannot delete scheduler '{schedulerToDelete.SchedulerName}' because it has {indicatorsResult.Value.Count} associated indicators. Use force=true to delete anyway or reassign indicators first.",
                            "SCHEDULER_HAS_INDICATORS"));
                    }
                }

                // Handle reassignment if specified
                if (request.ReassignToSchedulerId.HasValue)
                {
                    var reassignResult = await _schedulerService.BulkAssignSchedulerAsync(
                        new List<long>(), // Will be populated by service based on current scheduler
                        (int)request.ReassignToSchedulerId.Value,
                        GetCurrentUser());

                    if (!reassignResult.IsSuccess)
                    {
                        return BadRequest(CreateErrorResponse(
                            $"Failed to reassign indicators to scheduler {request.ReassignToSchedulerId}: {reassignResult.Error}",
                            "REASSIGNMENT_FAILED"));
                    }
                }

                var result = await _schedulerService.DeleteSchedulerAsync(id);

                if (!result.IsSuccess)
                {
                    return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "DELETE_SCHEDULER_FAILED"));
                }

                stopwatch.Stop();

                var response = new SchedulerOperationResponse
                {
                    Success = true,
                    Message = $"Scheduler '{schedulerToDelete.SchedulerName}' deleted successfully",
                    SchedulerId = id,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Details = new Dictionary<string, object>
                    {
                        ["DeletedBy"] = GetCurrentUser(),
                        ["ForceDelete"] = request.Force,
                        ["ReassignedTo"] = request.ReassignToSchedulerId?.ToString() ?? "None",
                        ["SchedulerName"] = schedulerToDelete.SchedulerName
                    }
                };

                Logger.LogInformation("Deleted scheduler {SchedulerId} '{SchedulerName}' in {Duration}ms",
                    id, schedulerToDelete.SchedulerName, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(response, response.Message));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Delete scheduler operation was cancelled for scheduler {SchedulerId}", id);
                return StatusCode(499, CreateErrorResponse("Delete scheduler operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error deleting scheduler {SchedulerId}", id);
                return StatusCode(500, CreateErrorResponse($"Failed to delete scheduler {id}: {ex.Message}", "DELETE_SCHEDULER_ERROR"));
            }
        }

        /// <summary>
        /// Toggle scheduler enabled/disabled status
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <param name="isEnabled">New enabled status</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Toggle operation result</returns>
        [HttpPatch("{id}/toggle")]
        [ProducesResponseType(typeof(SchedulerOperationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SchedulerOperationResponse>> ToggleScheduler(
            int id,
            [FromBody] bool isEnabled,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Additional validation for scheduler ID
                var paramValidation = ValidateParameter(id, nameof(id),
                    schedulerId => schedulerId > 0, "Scheduler ID must be a positive integer");
                if (paramValidation != null) return BadRequest(paramValidation);

                // Get current scheduler state
                var getResult = await _schedulerService.GetSchedulerByIdAsync(id);
                if (!getResult.IsSuccess)
                {
                    return NotFound(CreateErrorResponse($"Scheduler {id} not found", "SCHEDULER_NOT_FOUND"));
                }

                var currentScheduler = getResult.Value;
                var previousState = currentScheduler.IsEnabled;

                var result = await _schedulerService.ToggleSchedulerAsync(id, isEnabled, GetCurrentUser());

                if (!result.IsSuccess)
                {
                    return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "TOGGLE_SCHEDULER_FAILED"));
                }

                stopwatch.Stop();

                var action = isEnabled ? "enabled" : "disabled";
                var response = new SchedulerOperationResponse
                {
                    Success = true,
                    Message = $"Scheduler '{currentScheduler.SchedulerName}' {action} successfully",
                    SchedulerId = id,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Details = new Dictionary<string, object>
                    {
                        ["ModifiedBy"] = GetCurrentUser(),
                        ["PreviousState"] = previousState,
                        ["NewState"] = isEnabled,
                        ["StateChanged"] = previousState != isEnabled,
                        ["SchedulerName"] = currentScheduler.SchedulerName
                    }
                };

                Logger.LogInformation("Toggled scheduler {SchedulerId} '{SchedulerName}' from {PreviousState} to {NewState} in {Duration}ms",
                    id, currentScheduler.SchedulerName, previousState, isEnabled, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(response, response.Message));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Toggle scheduler operation was cancelled for scheduler {SchedulerId}", id);
                return StatusCode(499, CreateErrorResponse("Toggle scheduler operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error toggling scheduler {SchedulerId}", id);
                return StatusCode(500, CreateErrorResponse($"Failed to toggle scheduler {id}: {ex.Message}", "TOGGLE_SCHEDULER_ERROR"));
            }
        }

        /// <summary>
        /// Validate scheduler configuration
        /// </summary>
        /// <param name="request">Scheduler configuration to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(MonitoringGrid.Api.DTOs.Schedulers.SchedulerValidationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<MonitoringGrid.Api.DTOs.Schedulers.SchedulerValidationResult>> ValidateScheduler(
            [FromBody] MonitoringGrid.Api.DTOs.Schedulers.CreateSchedulerRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var validationError = ValidateModelState();
                if (validationError != null) return BadRequest(validationError);

                // Map API DTO to Core DTO
                var coreRequest = new MonitoringGrid.Core.DTOs.CreateSchedulerRequest
                {
                    SchedulerName = request.SchedulerName,
                    SchedulerDescription = request.SchedulerDescription,
                    ScheduleType = request.ScheduleType,
                    IntervalMinutes = request.IntervalMinutes,
                    CronExpression = request.CronExpression,
                    // ExecutionDateTime property doesn't exist in Core DTO
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Timezone = request.Timezone,
                    IsEnabled = request.IsEnabled
                };

                var result = await _schedulerService.ValidateSchedulerConfigurationAsync(coreRequest);

                if (!result.IsSuccess)
                {
                    return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "SCHEDULER_VALIDATION_FAILED"));
                }

                stopwatch.Stop();

                var validationResult = result.Value;

                // Enhance validation result with additional metadata
                var enhancedResult = new MonitoringGrid.Api.DTOs.Schedulers.SchedulerValidationResult
                {
                    IsValid = validationResult.IsValid,
                    ValidationErrors = validationResult.ValidationErrors,
                    ValidationWarnings = new List<string>(), // Core DTO doesn't have ValidationWarnings
                    SuggestedImprovements = new List<string>(), // Core DTO doesn't have SuggestedImprovements
                    ValidationDurationMs = stopwatch.ElapsedMilliseconds,
                    ValidatedAt = DateTime.UtcNow,
                    ValidatedBy = GetCurrentUser()
                };

                Logger.LogDebug("Validated scheduler configuration in {Duration}ms. Valid: {IsValid}, Errors: {ErrorCount}, Warnings: {WarningCount}",
                    stopwatch.ElapsedMilliseconds, validationResult.IsValid,
                    validationResult.ValidationErrors?.Count ?? 0,
                    0); // Core DTO doesn't have ValidationWarnings

                return Ok(CreateSuccessResponse(enhancedResult,
                    validationResult.IsValid ? "Scheduler configuration is valid" : "Scheduler configuration has validation issues"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Validate scheduler operation was cancelled");
                return StatusCode(499, CreateErrorResponse("Validate scheduler operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error validating scheduler configuration");
                return StatusCode(500, CreateErrorResponse($"Failed to validate scheduler configuration: {ex.Message}", "SCHEDULER_VALIDATION_ERROR"));
            }
        }

        /// <summary>
        /// Get scheduler preset options
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of scheduler presets</returns>
        [HttpGet("presets")]
        [ProducesResponseType(typeof(List<MonitoringGrid.Api.DTOs.Schedulers.SchedulerPresetDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<MonitoringGrid.Api.DTOs.Schedulers.SchedulerPresetDto>>> GetSchedulerPresets(
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await _schedulerService.GetSchedulerPresetsAsync();

                if (!result.IsSuccess)
                {
                    return StatusCode(500, CreateErrorResponse(result.Error?.Message ?? "Unknown error", "GET_SCHEDULER_PRESETS_FAILED"));
                }

                stopwatch.Stop();

                var presets = result.Value;

                // Enhance presets with additional metadata
                var enhancedPresets = presets.Select(preset => new MonitoringGrid.Api.DTOs.Schedulers.SchedulerPresetDto
                {
                    PresetId = 0, // Core DTO doesn't have PresetId
                    PresetName = preset.Name, // Core DTO uses Name
                    PresetDescription = preset.Description, // Core DTO uses Description
                    ScheduleType = preset.ScheduleType,
                    IntervalMinutes = preset.IntervalMinutes,
                    CronExpression = preset.CronExpression,
                    IsRecommended = false, // Core DTO doesn't have IsRecommended
                    Category = preset.Category,
                    UsageCount = 0, // Core DTO doesn't have UsageCount
                    LastUsed = null, // Core DTO doesn't have LastUsed
                    CreatedDate = DateTime.UtcNow, // Core DTO doesn't have CreatedDate
                    // Add computed fields
                    NextExecutionExample = CalculateNextExecutionExample(preset.IntervalMinutes, preset.CronExpression),
                    IsPopular = false // Cannot calculate without UsageCount
                }).ToList();

                Logger.LogDebug("Retrieved {Count} scheduler presets in {Duration}ms",
                    enhancedPresets.Count, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(enhancedPresets, $"Retrieved {enhancedPresets.Count} scheduler presets"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Get scheduler presets operation was cancelled");
                return StatusCode(499, CreateErrorResponse("Get scheduler presets operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error getting scheduler presets");
                return StatusCode(500, CreateErrorResponse($"Failed to get scheduler presets: {ex.Message}", "GET_SCHEDULER_PRESETS_ERROR"));
            }
        }

        private DateTime? CalculateNextExecutionExample(int? intervalMinutes, string? cronExpression)
        {
            try
            {
                var now = DateTime.UtcNow;
                if (intervalMinutes.HasValue)
                {
                    return now.AddMinutes(intervalMinutes.Value);
                }
                // For cron expressions, you would use a cron parser library
                // This is a simplified example
                return now.AddHours(1); // Default example
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get scheduler statistics
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Enhanced scheduler statistics</returns>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(MonitoringGrid.Api.DTOs.Schedulers.SchedulerStatsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<MonitoringGrid.Api.DTOs.Schedulers.SchedulerStatsDto>> GetSchedulerStatistics(
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await _schedulerService.GetSchedulerStatisticsAsync();

                if (!result.IsSuccess)
                {
                    return StatusCode(500, CreateErrorResponse(result.Error?.Message ?? "Unknown error", "GET_SCHEDULER_STATISTICS_FAILED"));
                }

                stopwatch.Stop();

                var stats = result.Value;

                // Enhance statistics with additional computed metrics
                var enhancedStats = new MonitoringGrid.Api.DTOs.Schedulers.SchedulerStatsDto
                {
                    TotalSchedulers = stats.TotalSchedulers,
                    EnabledSchedulers = stats.ActiveSchedulers, // Core DTO uses ActiveSchedulers
                    DisabledSchedulers = stats.InactiveSchedulers, // Core DTO uses InactiveSchedulers
                    TotalIndicators = 0, // Not available in Core DTO
                    IndicatorsWithSchedulers = stats.SchedulersWithIndicators, // Core DTO has SchedulersWithIndicators
                    IndicatorsWithoutSchedulers = stats.UnusedSchedulers, // Core DTO has UnusedSchedulers
                    DueIndicators = 0, // Not available in Core DTO
                    RunningIndicators = 0, // Not available in Core DTO
                    LastExecutionTime = null, // Not available in Core DTO
                    NextExecutionTime = stats.NextScheduledExecution,
                    AverageExecutionTimeMs = null, // Not available in Core DTO
                    TotalExecutionsToday = 0, // Not available in Core DTO
                    SuccessfulExecutionsToday = 0, // Not available in Core DTO
                    FailedExecutionsToday = 0, // Not available in Core DTO
                    // Add computed fields
                    SchedulerUtilizationRate = stats.TotalSchedulers > 0 ? (double)stats.ActiveSchedulers / stats.TotalSchedulers * 100 : 0,
                    IndicatorCoverageRate = 0, // Cannot calculate without TotalIndicators
                    TodaySuccessRate = 0, // Cannot calculate without execution data
                    SystemHealthScore = CalculateSystemHealthScore(stats),
                    StatisticsGeneratedAt = DateTime.UtcNow,
                    StatisticsGenerationTimeMs = stopwatch.ElapsedMilliseconds
                };

                Logger.LogDebug("Generated scheduler statistics in {Duration}ms. Health Score: {HealthScore:F1}%",
                    stopwatch.ElapsedMilliseconds, enhancedStats.SystemHealthScore);

                return Ok(CreateSuccessResponse(enhancedStats, "Retrieved scheduler statistics"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Get scheduler statistics operation was cancelled");
                return StatusCode(499, CreateErrorResponse("Get scheduler statistics operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error getting scheduler statistics");
                return StatusCode(500, CreateErrorResponse($"Failed to get scheduler statistics: {ex.Message}", "GET_SCHEDULER_STATISTICS_ERROR"));
            }
        }

        private double CalculateSystemHealthScore(MonitoringGrid.Core.DTOs.SchedulerStatsDto stats)
        {
            var score = 0.0;
            var factors = 0;

            // Factor 1: Scheduler utilization (30% weight)
            if (stats.TotalSchedulers > 0)
            {
                score += (double)stats.ActiveSchedulers / stats.TotalSchedulers * 30;
                factors++;
            }

            // Factor 2: Scheduler usage (25% weight) - using SchedulersWithIndicators
            if (stats.TotalSchedulers > 0)
            {
                score += (double)stats.SchedulersWithIndicators / stats.TotalSchedulers * 25;
                factors++;
            }

            // Factor 3: Basic health check (25% weight) - simplified since execution data not available
            if (stats.TotalSchedulers > 0)
            {
                score += (stats.ActiveSchedulers > 0 ? 25 : 0);
                factors++;
            }

            // Factor 4: System activity (20% weight) - simplified since LastExecutionTime not available in Core DTO
            if (stats.NextScheduledExecution.HasValue)
            {
                score += 20; // Give points if there's a next execution scheduled
            }
            factors++;

            return factors > 0 ? score : 0;
        }

        /// <summary>
        /// Get indicators with their scheduler information
        /// </summary>
        /// <param name="request">Get indicators request parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of indicators with scheduler details</returns>
        [HttpGet("indicators")]
        [ProducesResponseType(typeof(PaginatedIndicatorsWithSchedulersResponse), StatusCodes.Status200OK)]
        public async Task<ActionResult<PaginatedIndicatorsWithSchedulersResponse>> GetIndicatorsWithSchedulers(
            [FromQuery] GetSchedulersRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                request ??= new GetSchedulersRequest();

                var validationError = ValidateModelState();
                if (validationError != null) return BadRequest(validationError);

                var result = await _schedulerService.GetIndicatorsWithSchedulersAsync();

                if (!result.IsSuccess)
                {
                    return StatusCode(500, CreateErrorResponse(result.Error?.Message ?? "Unknown error", "GET_INDICATORS_WITH_SCHEDULERS_FAILED"));
                }

                var indicators = result.Value;

                // Apply search filter if specified
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    indicators = indicators
                        .Where(i => i.IndicatorName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                   (i.SchedulerName?.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) == true))
                        .ToList();
                }

                // Apply enabled filter if specified
                if (request.EnabledOnly)
                {
                    indicators = indicators.Where(i => i.IsActive && (i.SchedulerEnabled ?? false)).ToList();
                }

                // Apply pagination
                var totalCount = indicators.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
                var skip = (request.Page - 1) * request.PageSize;
                var pagedIndicators = indicators.Skip(skip).Take(request.PageSize).ToList();

                stopwatch.Stop();

                var response = new PaginatedIndicatorsWithSchedulersResponse
                {
                    Indicators = pagedIndicators,
                    TotalCount = totalCount,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1,
                    Summary = new IndicatorSchedulerSummary
                    {
                        TotalIndicators = totalCount,
                        IndicatorsWithSchedulers = indicators.Count(i => i.SchedulerID.HasValue),
                        IndicatorsWithoutSchedulers = indicators.Count(i => !i.SchedulerID.HasValue),
                        ActiveIndicators = indicators.Count(i => i.IsActive),
                        DueIndicators = 0, // Core DTO doesn't have IsDue property
                        RunningIndicators = 0 // Core DTO doesn't have IsCurrentlyRunning property
                    },
                    QueryMetrics = new QueryMetrics
                    {
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        QueryCount = 1,
                        CacheHit = false
                    }
                };

                Logger.LogDebug("Retrieved {Count} indicators with schedulers (page {Page}/{TotalPages}) in {Duration}ms",
                    pagedIndicators.Count, request.Page, totalPages, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(response, $"Retrieved {pagedIndicators.Count} indicators with scheduler information"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Get indicators with schedulers operation was cancelled");
                return StatusCode(499, CreateErrorResponse("Get indicators with schedulers operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error getting indicators with schedulers");
                return StatusCode(500, CreateErrorResponse($"Failed to get indicators with schedulers: {ex.Message}", "GET_INDICATORS_WITH_SCHEDULERS_ERROR"));
            }
        }

        /// <summary>
        /// Assign scheduler to indicator
        /// </summary>
        /// <param name="request">Assignment request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Assignment result</returns>
        [HttpPost("assign")]
        [ProducesResponseType(typeof(AssignSchedulerResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AssignSchedulerResponse>> AssignScheduler(
            [FromBody] AssignSchedulerRequest request,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var validationError = ValidateModelState();
                if (validationError != null) return BadRequest(validationError);

                var result = await _schedulerService.AssignSchedulerToIndicatorAsync(request, GetCurrentUser());

                if (!result.IsSuccess)
                {
                    return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "ASSIGN_SCHEDULER_FAILED"));
                }

                stopwatch.Stop();

                var response = result.Value;

                // Core DTO doesn't have additional metadata properties, use response as-is

                Logger.LogInformation("Assigned scheduler {SchedulerId} to indicator {IndicatorId} in {Duration}ms",
                    request.SchedulerID, request.IndicatorID, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(response, "Scheduler assigned successfully"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Assign scheduler operation was cancelled");
                return StatusCode(499, CreateErrorResponse("Assign scheduler operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error assigning scheduler");
                return StatusCode(500, CreateErrorResponse($"Failed to assign scheduler: {ex.Message}", "ASSIGN_SCHEDULER_ERROR"));
            }
        }

        /// <summary>
        /// Get indicators assigned to a specific scheduler
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of indicators using this scheduler</returns>
        [HttpGet("{id}/indicators")]
        [ProducesResponseType(typeof(List<IndicatorWithSchedulerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<IndicatorWithSchedulerDto>>> GetIndicatorsByScheduler(
            int id,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var paramValidation = ValidateParameter(id, nameof(id),
                    schedulerId => schedulerId > 0, "Scheduler ID must be a positive integer");
                if (paramValidation != null) return BadRequest(paramValidation);

                var result = await _schedulerService.GetIndicatorsBySchedulerAsync(id);

                if (!result.IsSuccess)
                {
                    return StatusCode(500, CreateErrorResponse(result.Error?.Message ?? "Unknown error", "GET_INDICATORS_BY_SCHEDULER_FAILED"));
                }

                stopwatch.Stop();

                var indicators = result.Value;

                Logger.LogDebug("Retrieved {Count} indicators for scheduler {SchedulerId} in {Duration}ms",
                    indicators.Count, id, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(indicators, $"Retrieved {indicators.Count} indicators for scheduler {id}"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Get indicators by scheduler operation was cancelled for scheduler {SchedulerId}", id);
                return StatusCode(499, CreateErrorResponse("Get indicators by scheduler operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error getting indicators for scheduler {SchedulerId}", id);
                return StatusCode(500, CreateErrorResponse($"Failed to get indicators for scheduler {id}: {ex.Message}", "GET_INDICATORS_BY_SCHEDULER_ERROR"));
            }
        }

        /// <summary>
        /// Get indicators that are due for execution
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of due indicators</returns>
        [HttpGet("due-indicators")]
        [ProducesResponseType(typeof(List<IndicatorWithSchedulerDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<IndicatorWithSchedulerDto>>> GetDueIndicators(
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await _schedulerService.GetDueIndicatorsAsync();

                if (!result.IsSuccess)
                {
                    return StatusCode(500, CreateErrorResponse(result.Error?.Message ?? "Unknown error", "GET_DUE_INDICATORS_FAILED"));
                }

                stopwatch.Stop();

                var indicators = result.Value;

                Logger.LogDebug("Retrieved {Count} due indicators in {Duration}ms",
                    indicators.Count, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(indicators, $"Retrieved {indicators.Count} due indicators"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Get due indicators operation was cancelled");
                return StatusCode(499, CreateErrorResponse("Get due indicators operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error getting due indicators");
                return StatusCode(500, CreateErrorResponse($"Failed to get due indicators: {ex.Message}", "GET_DUE_INDICATORS_ERROR"));
            }
        }

        /// <summary>
        /// Get upcoming executions for the next specified hours
        /// </summary>
        /// <param name="hours">Number of hours to look ahead (default: 24)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of upcoming executions</returns>
        [HttpGet("upcoming")]
        [ProducesResponseType(typeof(List<IndicatorWithSchedulerDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<IndicatorWithSchedulerDto>>> GetUpcomingExecutions(
            [FromQuery] int hours = 24,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var paramValidation = ValidateParameter(hours, nameof(hours),
                    h => h > 0 && h <= 168, "Hours must be between 1 and 168 (1 week)");
                if (paramValidation != null) return BadRequest(paramValidation);

                var result = await _schedulerService.GetUpcomingExecutionsAsync(hours);

                if (!result.IsSuccess)
                {
                    return StatusCode(500, CreateErrorResponse(result.Error?.Message ?? "Unknown error", "GET_UPCOMING_EXECUTIONS_FAILED"));
                }

                stopwatch.Stop();

                var executions = result.Value;

                Logger.LogDebug("Retrieved {Count} upcoming executions for next {Hours} hours in {Duration}ms",
                    executions.Count, hours, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(executions, $"Retrieved {executions.Count} upcoming executions for next {hours} hours"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Get upcoming executions operation was cancelled");
                return StatusCode(499, CreateErrorResponse("Get upcoming executions operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error getting upcoming executions");
                return StatusCode(500, CreateErrorResponse($"Failed to get upcoming executions: {ex.Message}", "GET_UPCOMING_EXECUTIONS_ERROR"));
            }
        }

        /// <summary>
        /// Bulk assign scheduler to multiple indicators
        /// </summary>
        /// <param name="indicatorIds">List of indicator IDs</param>
        /// <param name="schedulerId">Scheduler ID to assign (null to remove assignment)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bulk operation result</returns>
        [HttpPost("bulk-assign")]
        [ProducesResponseType(typeof(SchedulerOperationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SchedulerOperationResponse>> BulkAssignScheduler(
            [FromBody] List<long> indicatorIds,
            [FromQuery] int? schedulerId = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var paramValidation = ValidateParameter(indicatorIds, nameof(indicatorIds),
                    ids => ids != null && ids.Any(), "Indicator IDs list cannot be empty");
                if (paramValidation != null) return BadRequest(paramValidation);

                var result = await _schedulerService.BulkAssignSchedulerAsync(indicatorIds, schedulerId, GetCurrentUser());

                if (!result.IsSuccess)
                {
                    return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "BULK_ASSIGN_SCHEDULER_FAILED"));
                }

                stopwatch.Stop();

                var action = schedulerId.HasValue ? $"assigned scheduler {schedulerId}" : "removed scheduler assignment";
                var response = new SchedulerOperationResponse
                {
                    Success = true,
                    Message = $"Successfully {action} for {indicatorIds.Count} indicators",
                    SchedulerId = schedulerId,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Details = new Dictionary<string, object>
                    {
                        ["ProcessedIndicators"] = indicatorIds.Count,
                        ["SchedulerId"] = schedulerId?.ToString() ?? "None",
                        ["AssignedBy"] = GetCurrentUser(),
                        ["IndicatorIds"] = indicatorIds
                    }
                };

                Logger.LogInformation("Bulk assigned scheduler {SchedulerId} to {Count} indicators in {Duration}ms",
                    schedulerId, indicatorIds.Count, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(response, response.Message));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Bulk assign scheduler operation was cancelled");
                return StatusCode(499, CreateErrorResponse("Bulk assign scheduler operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error in bulk assign scheduler");
                return StatusCode(500, CreateErrorResponse($"Failed to bulk assign scheduler: {ex.Message}", "BULK_ASSIGN_SCHEDULER_ERROR"));
            }
        }

        /// <summary>
        /// Bulk update scheduler status
        /// </summary>
        /// <param name="schedulerIds">List of scheduler IDs</param>
        /// <param name="isEnabled">New enabled status</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bulk operation result</returns>
        [HttpPost("bulk-toggle")]
        [ProducesResponseType(typeof(SchedulerOperationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SchedulerOperationResponse>> BulkUpdateSchedulerStatus(
            [FromBody] List<int> schedulerIds,
            [FromQuery] bool isEnabled = true,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var paramValidation = ValidateParameter(schedulerIds, nameof(schedulerIds),
                    ids => ids != null && ids.Any(), "Scheduler IDs list cannot be empty");
                if (paramValidation != null) return BadRequest(paramValidation);

                var result = await _schedulerService.BulkUpdateSchedulerStatusAsync(schedulerIds, isEnabled, GetCurrentUser());

                if (!result.IsSuccess)
                {
                    return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "BULK_UPDATE_SCHEDULER_STATUS_FAILED"));
                }

                stopwatch.Stop();

                var action = isEnabled ? "enabled" : "disabled";
                var response = new SchedulerOperationResponse
                {
                    Success = true,
                    Message = $"Successfully {action} {schedulerIds.Count} schedulers",
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Details = new Dictionary<string, object>
                    {
                        ["ProcessedSchedulers"] = schedulerIds.Count,
                        ["NewStatus"] = isEnabled,
                        ["ModifiedBy"] = GetCurrentUser(),
                        ["SchedulerIds"] = schedulerIds
                    }
                };

                Logger.LogInformation("Bulk {Action} {Count} schedulers in {Duration}ms",
                    action, schedulerIds.Count, stopwatch.ElapsedMilliseconds);

                return Ok(CreateSuccessResponse(response, response.Message));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Bulk update scheduler status operation was cancelled");
                return StatusCode(499, CreateErrorResponse("Bulk update scheduler status operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error in bulk update scheduler status");
                return StatusCode(500, CreateErrorResponse($"Failed to bulk update scheduler status: {ex.Message}", "BULK_UPDATE_SCHEDULER_STATUS_ERROR"));
            }
        }

        /// <summary>
        /// Get next execution time for a scheduler
        /// </summary>
        /// <param name="id">Scheduler ID</param>
        /// <param name="lastExecution">Last execution time (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Next execution time calculation result</returns>
        [HttpGet("{id}/next-execution")]
        [ProducesResponseType(typeof(NextExecutionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<NextExecutionResponse>> GetNextExecutionTime(
            int id,
            [FromQuery] DateTime? lastExecution = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var paramValidation = ValidateParameter(id, nameof(id),
                    schedulerId => schedulerId > 0, "Scheduler ID must be a positive integer");
                if (paramValidation != null) return BadRequest(paramValidation);

                var result = await _schedulerService.GetNextExecutionTimeAsync(id, lastExecution);

                if (!result.IsSuccess)
                {
                    if (result.Error?.Message?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return NotFound(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "SCHEDULER_NOT_FOUND"));
                    }
                    return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Unknown error", "GET_NEXT_EXECUTION_TIME_FAILED"));
                }

                stopwatch.Stop();

                var nextExecution = result.Value;
                var response = new NextExecutionResponse
                {
                    SchedulerId = id,
                    LastExecution = lastExecution,
                    NextExecution = nextExecution,
                    CalculatedAt = DateTime.UtcNow,
                    CalculationDurationMs = stopwatch.ElapsedMilliseconds,
                    TimeUntilExecution = nextExecution.HasValue ? nextExecution.Value - DateTime.UtcNow : null
                };

                Logger.LogDebug("Calculated next execution time for scheduler {SchedulerId} in {Duration}ms: {NextExecution}",
                    id, stopwatch.ElapsedMilliseconds, nextExecution);

                return Ok(CreateSuccessResponse(response, $"Calculated next execution time for scheduler {id}"));
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Get next execution time operation was cancelled for scheduler {SchedulerId}", id);
                return StatusCode(499, CreateErrorResponse("Get next execution time operation was cancelled", "OPERATION_CANCELLED"));
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogError(ex, "Error getting next execution time for scheduler {SchedulerId}", id);
                return StatusCode(500, CreateErrorResponse($"Failed to get next execution time for scheduler {id}: {ex.Message}", "GET_NEXT_EXECUTION_TIME_ERROR"));
            }
        }

        private string GetCurrentUser()
        {
            // TODO: Implement proper user context when authentication is added
            return User?.Identity?.Name ?? "system";
        }
    }
}
