using MediatR;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.CQRS.Commands.Kpi;
using MonitoringGrid.Api.CQRS.Queries.Kpi;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Filters;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Result Pattern-based API controller for managing KPIs using MediatR with Result&lt;T&gt;
/// </summary>
[ApiController]
[ApiVersion("3.0")]
[Route("api/v{version:apiVersion}/kpi")]
[Produces("application/json")]
[PerformanceMonitor(slowThresholdMs: 2000)]
public class KpiV3Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<KpiV3Controller> _logger;

    public KpiV3Controller(IMediator mediator, ILogger<KpiV3Controller> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all KPIs with optional filtering
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "isActive", "owner", "priority" })]
    [DatabasePerformanceMonitor]
    public async Task<IActionResult> GetKpis(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? owner = null,
        [FromQuery] byte? priority = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetKpisQuery(isActive, owner, priority);
        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: kpis => Ok(kpis),
            onFailure: error => HandleError(error)
        );
    }

    /// <summary>
    /// Get KPI by ID
    /// </summary>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "id" })]
    [DatabasePerformanceMonitor]
    public async Task<IActionResult> GetKpi(int id, CancellationToken cancellationToken = default)
    {
        var query = new GetKpiByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: kpi => kpi == null ? NotFound() : Ok(kpi),
            onFailure: error => HandleError(error)
        );
    }

    /// <summary>
    /// Create a new KPI
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateKpi([FromBody] CreateKpiCommand command, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: kpi => CreatedAtAction(nameof(GetKpi), new { id = kpi.KpiId }, kpi),
            onFailure: error => HandleError(error)
        );
    }

    /// <summary>
    /// Update an existing KPI
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateKpi(int id, [FromBody] UpdateKpiCommand command, CancellationToken cancellationToken = default)
    {
        if (id != command.KpiId)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: kpi => Ok(kpi),
            onFailure: error => HandleError(error)
        );
    }

    /// <summary>
    /// Delete a KPI
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKpi(int id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteKpiCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: success => success ? NoContent() : NotFound(),
            onFailure: error => HandleError(error)
        );
    }

    /// <summary>
    /// Execute a KPI manually
    /// </summary>
    [HttpPost("{id}/execute")]
    [KpiPerformanceMonitor]
    public async Task<IActionResult> ExecuteKpi(int id, [FromBody] TestKpiRequest? request = null, CancellationToken cancellationToken = default)
    {
        var command = new ExecuteKpiCommand(id, request?.CustomFrequency);
        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: executionResult => Ok(executionResult),
            onFailure: error => HandleError(error)
        );
    }

    /// <summary>
    /// Get KPI dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = new string[] { })]
    [DatabasePerformanceMonitor]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken = default)
    {
        var query = new GetKpiDashboardQuery();
        var result = await _mediator.Send(query, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: dashboard => Ok(dashboard),
            onFailure: error => HandleError(error)
        );
    }

    /// <summary>
    /// Perform bulk operations on KPIs
    /// </summary>
    [HttpPost("bulk")]
    [DatabasePerformanceMonitor(slowQueryThresholdMs: 1000)]
    public async Task<IActionResult> BulkOperation([FromBody] BulkKpiOperationCommand command, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);

        return result.Match<IActionResult>(
            onSuccess: message => Ok(new { Message = message }),
            onFailure: error => HandleError(error)
        );
    }

    /// <summary>
    /// Converts Error objects to appropriate HTTP responses
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
