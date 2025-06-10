using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MonitoringGrid.Api.CQRS.Commands.Kpi;
using MonitoringGrid.Api.CQRS.Queries.Kpi;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Core.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Main API controller for managing KPIs using CQRS pattern with MediatR
/// Clean architecture principles with root-level API endpoints
/// </summary>
[ApiController]
[Route("api/kpi")]
[Produces("application/json")]
[PerformanceMonitor(slowThresholdMs: 2000)]
public class KpiController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<KpiController> _logger;
    private readonly IPerformanceMetricsService? _performanceMetrics;

    public KpiController(
        IMediator mediator,
        ILogger<KpiController> logger,
        IPerformanceMetricsService? performanceMetrics = null)
    {
        _mediator = mediator;
        _logger = logger;
        _performanceMetrics = performanceMetrics;
    }

    /// <summary>
    /// Get all KPIs with optional filtering
    /// </summary>
    /// <param name="isActive">Filter by active status (true/false)</param>
    /// <param name="owner">Filter by owner email or name</param>
    /// <param name="priority">Filter by priority level (1=SMS+Email, 2=Email only)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of KPIs matching the filter criteria</returns>
    /// <response code="200">Returns the list of KPIs</response>
    /// <response code="400">Invalid filter parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "isActive", "owner", "priority" })]
    [DatabasePerformanceMonitor]
    [SwaggerOperation(
        Summary = "Get all KPIs with optional filtering",
        Description = "Retrieves all KPIs from the system with optional filtering by active status, owner, and priority level. Results are cached for 5 minutes.",
        OperationId = "GetKpis",
        Tags = new[] { "KPI Management" }
    )]
    [SwaggerResponse(200, "Successfully retrieved KPIs", typeof(List<KpiDto>))]
    [SwaggerResponse(400, "Invalid filter parameters", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<List<KpiDto>>> GetKpis(
        [FromQuery, SwaggerParameter("Filter by active status")] bool? isActive = null,
        [FromQuery, SwaggerParameter("Filter by owner email or name")] string? owner = null,
        [FromQuery, SwaggerParameter("Filter by priority level (1=SMS+Email, 2=Email only)")] byte? priority = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetKpisQuery(isActive, owner, priority);
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI query failed: {Error}", result.Error);
                return StatusCode(500, result.Error.Message);
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPIs");
            return StatusCode(500, "An error occurred while retrieving KPIs");
        }
    }

    /// <summary>
    /// Get KPI by ID
    /// </summary>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "id" })]
    [DatabasePerformanceMonitor]
    public async Task<ActionResult<KpiDto>> GetKpi(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetKpiByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI query failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            if (result.Value == null)
                return NotFound($"KPI with ID {id} not found");

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI with ID: {KpiId}", id);
            return StatusCode(500, "An error occurred while retrieving the KPI");
        }
    }

    /// <summary>
    /// Create a new KPI
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<KpiDto>> CreateKpi([FromBody] CreateKpiCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI creation failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.Validation
                    ? BadRequest(result.Error.Message)
                    : result.Error.Type == ErrorType.Conflict
                    ? Conflict(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            return CreatedAtAction(nameof(GetKpi), new { id = result.Value.KpiId }, result.Value);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid KPI creation request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating KPI: {@Command}", command);
            return StatusCode(500, "An error occurred while creating the KPI");
        }
    }

    /// <summary>
    /// Update an existing KPI
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<KpiDto>> UpdateKpi(int id, [FromBody] UpdateKpiCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != command.KpiId)
                return BadRequest("ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI update failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : result.Error.Type == ErrorType.Validation
                    ? BadRequest(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            return Ok(result.Value);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid KPI update request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating KPI {KpiId}: {@Command}", id, command);
            return StatusCode(500, "An error occurred while updating the KPI");
        }
    }

    /// <summary>
    /// Delete a KPI
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKpi(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DeleteKpiCommand(id);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI deletion failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            if (!result.Value)
                return NotFound($"KPI with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting KPI {KpiId}", id);
            return StatusCode(500, "An error occurred while deleting the KPI");
        }
    }

    /// <summary>
    /// Execute a KPI manually
    /// </summary>
    [HttpPost("{id}/execute")]
    [KpiPerformanceMonitor]
    public async Task<ActionResult<KpiExecutionResultDto>> ExecuteKpi(int id, [FromBody] TestKpiRequest? request = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new ExecuteKpiCommand(id, request?.CustomFrequency);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI execution failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : result.Error.Type == ErrorType.BusinessRule
                    ? BadRequest(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            return Ok(result.Value);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid KPI execution request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("KPI execution not allowed: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing KPI {KpiId}", id);
            return StatusCode(500, "An error occurred while executing the KPI");
        }
    }

    /// <summary>
    /// Get KPI dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = new string[] { })]
    [DatabasePerformanceMonitor]
    public async Task<ActionResult<KpiDashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetKpiDashboardQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Dashboard query failed: {Error}", result.Error);
                return StatusCode(500, result.Error.Message);
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data");
            return StatusCode(500, "An error occurred while retrieving dashboard data");
        }
    }

    /// <summary>
    /// Perform bulk operations on KPIs
    /// </summary>
    [HttpPost("bulk")]
    [DatabasePerformanceMonitor(slowQueryThresholdMs: 1000)]
    public async Task<IActionResult> BulkOperation([FromBody] BulkKpiOperationCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Bulk operation failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.Validation
                    ? BadRequest(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            return Ok(new { Message = result.Value });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid bulk operation request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation: {Operation}", command.Operation);
            return StatusCode(500, "An error occurred while performing the bulk operation");
        }
    }

    /// <summary>
    /// Gets KPIs with optimized projections and pagination (v3.0+)
    /// </summary>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="owner">Filter by owner</param>
    /// <param name="priority">Filter by priority</param>
    /// <param name="searchTerm">Search term for indicator, owner, or description</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="sortBy">Sort field (default: Indicator)</param>
    /// <param name="sortDescending">Sort direction (default: false)</param>
    /// <returns>Paginated list of KPI summaries</returns>
    [HttpGet("optimized")]
    [ProducesResponseType(typeof(PagedResult<MonitoringGrid.Api.CQRS.Queries.Kpi.KpiListItemDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PagedResult<MonitoringGrid.Api.CQRS.Queries.Kpi.KpiListItemDto>>> GetKpisOptimized(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? owner = null,
        [FromQuery] byte? priority = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "Indicator",
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting optimized KPIs with filters: IsActive={IsActive}, Owner={Owner}, Priority={Priority}, Search={SearchTerm}",
            isActive, owner, priority, searchTerm);

        var query = new GetKpisOptimizedQuery
        {
            IsActive = isActive,
            Owner = owner,
            Priority = priority,
            SearchTerm = searchTerm,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogDebug("Retrieved {Count} of {Total} KPIs in page {Page}",
            result.Items.Count(), result.TotalCount, result.PageNumber);

        return Ok(result);
    }

    /// <summary>
    /// Gets performance metrics for the KPI system (v3.0+)
    /// </summary>
    /// <param name="from">Start date for metrics</param>
    /// <param name="to">End date for metrics</param>
    /// <returns>Performance report</returns>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(PerformanceReport), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PerformanceReport>> GetPerformanceMetrics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (_performanceMetrics == null)
        {
            return BadRequest("Performance metrics service not available");
        }

        var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
        var toDate = to ?? DateTime.UtcNow;

        _logger.LogDebug("Getting performance metrics from {From} to {To}", fromDate, toDate);

        var report = await _performanceMetrics.GetPerformanceReportAsync(fromDate, toDate);

        _logger.LogDebug("Retrieved performance report: {TotalRequests} requests, {AverageResponseTime}ms avg response time",
            report.TotalRequests, report.AverageResponseTime);

        return Ok(report);
    }





    /// <summary>
    /// Validate and test scheduling calculations (v3.0+)
    /// </summary>
    [HttpGet("scheduling/validate")]
    [ProducesResponseType(200)]
    public IActionResult ValidateScheduling(
        [FromQuery] int frequencyMinutes = 5,
        [FromQuery] DateTime? lastRun = null)
    {
        var now = DateTime.UtcNow;
        var testLastRun = lastRun ?? now.AddMinutes(-frequencyMinutes - 1);

        var nextExecution = MonitoringGrid.Core.Utilities.WholeTimeScheduler.GetNextWholeTimeExecution(frequencyMinutes, testLastRun);
        var isDue = MonitoringGrid.Core.Utilities.WholeTimeScheduler.IsKpiDueForWholeTimeExecution(testLastRun, frequencyMinutes, now);
        var description = MonitoringGrid.Core.Utilities.WholeTimeScheduler.GetScheduleDescription(frequencyMinutes);

        return Ok(new
        {
            CurrentTime = now,
            LastRun = testLastRun,
            FrequencyMinutes = frequencyMinutes,
            NextExecution = nextExecution,
            MinutesUntilNext = (nextExecution - now).TotalMinutes,
            IsDue = isDue,
            ScheduleDescription = description
        });
    }





    /// <summary>
    /// Converts Error objects to appropriate HTTP responses (for Result pattern support)
    /// </summary>
    private IActionResult HandleError(Error error)
    {
        var (statusCode, message) = error.Type switch
        {
            ErrorType.NotFound => (404, error.Message),
            ErrorType.Validation => (400, error.Message),
            ErrorType.Conflict => (409, error.Message),
            ErrorType.Unauthorized => (401, error.Message),
            ErrorType.Forbidden => (403, error.Message),
            ErrorType.BusinessRule => (400, error.Message),
            ErrorType.External => (502, "External service error"),
            ErrorType.Timeout => (408, "Request timeout"),
            _ => (500, "An internal error occurred")
        };

        _logger.LogWarning("API error: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);

        return StatusCode(statusCode, new
        {
            Error = error.Code,
            Message = message,
            Type = error.Type.ToString()
        });
    }
}
