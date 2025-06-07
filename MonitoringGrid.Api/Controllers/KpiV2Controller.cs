using MediatR;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.CQRS.Commands.Kpi;
using MonitoringGrid.Api.CQRS.Queries.Kpi;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Filters;
using Swashbuckle.AspNetCore.Annotations;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// CQRS-based API controller for managing KPIs using MediatR
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/kpi")]
[Produces("application/json")]
[PerformanceMonitor(slowThresholdMs: 2000)]
public class KpiV2Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<KpiV2Controller> _logger;

    public KpiV2Controller(IMediator mediator, ILogger<KpiV2Controller> logger)
    {
        _mediator = mediator;
        _logger = logger;
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
            return Ok(result);
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

            if (result == null)
                return NotFound($"KPI with ID {id} not found");

            return Ok(result);
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
            return Ok(result);
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
            return Ok(result);
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
            return Ok(result);
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
            return Ok(new { Message = result });
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
}
