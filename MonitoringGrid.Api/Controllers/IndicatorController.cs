using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.CQRS.Queries.Collector;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API controller for managing Indicators using CQRS pattern with MediatR
/// Replaces KpiController with enhanced functionality for ProgressPlayDB integration
/// </summary>
[ApiController]
[Route("api/indicator")]
[Produces("application/json")]
[PerformanceMonitor(slowThresholdMs: 2000)]
public class IndicatorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<IndicatorController> _logger;
    private readonly IPerformanceMetricsService? _performanceMetrics;

    public IndicatorController(
        IMediator mediator,
        IMapper mapper,
        ILogger<IndicatorController> logger,
        IPerformanceMetricsService? performanceMetrics = null)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
        _performanceMetrics = performanceMetrics;
    }

    /// <summary>
    /// Get all indicators with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<IndicatorDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetIndicators([FromQuery] IndicatorFilterRequest request)
    {
        try
        {
            _logger.LogDebug("Getting indicators with filters");
            _performanceMetrics?.IncrementCounter("indicator.get_all.requests");

            var query = _mapper.Map<GetIndicatorsQuery>(request);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.get_all.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.get_all.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indicators");
            _performanceMetrics?.IncrementCounter("indicator.get_all.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get indicator by ID
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(IndicatorDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetIndicator(long id)
    {
        try
        {
            _logger.LogDebug("Getting indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.get_by_id.requests");

            var query = new GetIndicatorByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.get_by_id.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.get_by_id.failure");
            return NotFound(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.get_by_id.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new indicator
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IndicatorDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateIndicator([FromBody] CreateIndicatorRequest request)
    {
        try
        {
            _logger.LogDebug("Creating indicator {IndicatorName}", request.IndicatorName);
            _performanceMetrics?.IncrementCounter("indicator.create.requests");

            var command = _mapper.Map<CreateIndicatorCommand>(request);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.create.success");
                return CreatedAtAction(nameof(GetIndicator), new { id = result.Value.IndicatorId }, result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.create.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating indicator {IndicatorName}", request.IndicatorName);
            _performanceMetrics?.IncrementCounter("indicator.create.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing indicator
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(IndicatorDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateIndicator(long id, [FromBody] UpdateIndicatorRequest request)
    {
        try
        {
            if (id != request.IndicatorId)
            {
                return BadRequest("ID mismatch");
            }

            _logger.LogDebug("Updating indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.update.requests");

            var command = _mapper.Map<UpdateIndicatorCommand>(request);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.update.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.update.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.update.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete an indicator
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteIndicator(long id)
    {
        try
        {
            _logger.LogDebug("Deleting indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.delete.requests");

            var command = new DeleteIndicatorCommand(id);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.delete.success");
                return NoContent();
            }

            _performanceMetrics?.IncrementCounter("indicator.delete.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.delete.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Execute an indicator manually
    /// </summary>
    [HttpPost("{id:long}/execute")]
    [ProducesResponseType(typeof(IndicatorExecutionResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ExecuteIndicator(long id, [FromBody] ExecuteIndicatorRequest? request = null)
    {
        try
        {
            _logger.LogDebug("Executing indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.execute.requests");

            var command = new ExecuteIndicatorCommand(
                id, 
                request?.ExecutionContext ?? "Manual", 
                request?.SaveResults ?? true);
            
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.execute.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.execute.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.execute.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Test an indicator without saving results
    /// </summary>
    [HttpPost("{id:long}/test")]
    [ProducesResponseType(typeof(IndicatorExecutionResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> TestIndicator(long id, [FromBody] TestIndicatorRequest? request = null)
    {
        try
        {
            _logger.LogDebug("Testing indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.test.requests");

            var command = new ExecuteIndicatorCommand(id, "Test", saveResults: false);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.test.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.test.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.test.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get indicator dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(IndicatorDashboardDto), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            _logger.LogDebug("Getting indicator dashboard");
            _performanceMetrics?.IncrementCounter("indicator.dashboard.requests");

            var query = new GetIndicatorDashboardQuery();
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.dashboard.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.dashboard.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indicator dashboard");
            _performanceMetrics?.IncrementCounter("indicator.dashboard.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get available collectors from ProgressPlayDB
    /// </summary>
    [HttpGet("collectors")]
    [ProducesResponseType(typeof(List<CollectorDto>), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetCollectors([FromQuery] bool? isActive = true)
    {
        try
        {
            _logger.LogDebug("Getting collectors");
            _performanceMetrics?.IncrementCounter("indicator.collectors.requests");

            var query = new GetCollectorsQuery { IsActive = isActive };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.collectors.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.collectors.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collectors");
            _performanceMetrics?.IncrementCounter("indicator.collectors.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get available item names for a specific collector
    /// </summary>
    [HttpGet("collectors/{collectorId:int}/items")]
    [ProducesResponseType(typeof(List<string>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetCollectorItems(int collectorId)
    {
        try
        {
            _logger.LogDebug("Getting items for collector {CollectorId}", collectorId);
            _performanceMetrics?.IncrementCounter("indicator.collector_items.requests");

            var query = new GetCollectorItemNamesQuery(collectorId);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.collector_items.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.collector_items.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting items for collector {CollectorId}", collectorId);
            _performanceMetrics?.IncrementCounter("indicator.collector_items.error");
            return StatusCode(500, "Internal server error");
        }
    }
}
