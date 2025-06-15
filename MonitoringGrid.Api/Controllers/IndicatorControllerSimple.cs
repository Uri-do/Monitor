using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Simplified API controller for managing Indicators using CQRS pattern with MediatR
/// </summary>
[ApiController]
[Route("api/indicator")]
public class IndicatorControllerSimple : BaseApiController
{
    private readonly IMapper _mapper;

    public IndicatorControllerSimple(
        IMediator mediator,
        IMapper mapper,
        ILogger<IndicatorControllerSimple> logger,
        IPerformanceMetricsService? performanceMetrics = null)
        : base(mediator, logger, performanceMetrics)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Get all indicators with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<IndicatorDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetIndicators([FromQuery] IndicatorFilterRequest request)
    {
        var query = _mapper.Map<GetIndicatorsQuery>(request);
        return await ExecuteQueryAsync<GetIndicatorsQuery, List<IndicatorDto>>(query, "get_all");
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
        var query = new GetIndicatorByIdQuery(id);
        return await ExecuteQueryAsync<GetIndicatorByIdQuery, IndicatorDto>(query, "get_by_id");
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
            Logger.LogDebug("Creating indicator {IndicatorName}", request.IndicatorName);
            PerformanceMetrics?.IncrementCounter("indicator.create.requests");

            var command = _mapper.Map<CreateIndicatorCommand>(request);
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                PerformanceMetrics?.IncrementCounter("indicator.create.success");
                return CreatedAtAction(nameof(GetIndicator), new { id = result.Value.IndicatorID }, result.Value);
            }

            PerformanceMetrics?.IncrementCounter("indicator.create.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating indicator {IndicatorName}", request.IndicatorName);
            PerformanceMetrics?.IncrementCounter("indicator.create.error");
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
            if (id != request.IndicatorID)
            {
                return BadRequest("ID mismatch");
            }

            Logger.LogDebug("Updating indicator {IndicatorId}", id);
            PerformanceMetrics?.IncrementCounter("indicator.update.requests");

            var command = _mapper.Map<UpdateIndicatorCommand>(request);
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                PerformanceMetrics?.IncrementCounter("indicator.update.success");
                return Ok(result.Value);
            }

            PerformanceMetrics?.IncrementCounter("indicator.update.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating indicator {IndicatorId}", id);
            PerformanceMetrics?.IncrementCounter("indicator.update.error");
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
            Logger.LogDebug("Deleting indicator {IndicatorId}", id);
            PerformanceMetrics?.IncrementCounter("indicator.delete.requests");

            var command = new DeleteIndicatorCommand(id);
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                PerformanceMetrics?.IncrementCounter("indicator.delete.success");
                return NoContent();
            }

            PerformanceMetrics?.IncrementCounter("indicator.delete.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error deleting indicator {IndicatorId}", id);
            PerformanceMetrics?.IncrementCounter("indicator.delete.error");
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
            Logger.LogDebug("Executing indicator {IndicatorId}", id);
            PerformanceMetrics?.IncrementCounter("indicator.execute.requests");

            var command = new ExecuteIndicatorCommand(
                id, 
                request?.ExecutionContext ?? "Manual", 
                request?.SaveResults ?? true);
            
            var result = await Mediator.Send(command);

            if (result.IsSuccess)
            {
                PerformanceMetrics?.IncrementCounter("indicator.execute.success");
                return Ok(result.Value);
            }

            PerformanceMetrics?.IncrementCounter("indicator.execute.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing indicator {IndicatorId}", id);
            PerformanceMetrics?.IncrementCounter("indicator.execute.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get indicator dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IndicatorDashboardDto), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            Logger.LogDebug("Getting indicator dashboard");
            PerformanceMetrics?.IncrementCounter("indicator.dashboard.requests");

            var query = new GetIndicatorDashboardQuery();
            var result = await Mediator.Send(query);

            if (result.IsSuccess)
            {
                PerformanceMetrics?.IncrementCounter("indicator.dashboard.success");
                return Ok(result.Value);
            }

            PerformanceMetrics?.IncrementCounter("indicator.dashboard.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting indicator dashboard");
            PerformanceMetrics?.IncrementCounter("indicator.dashboard.error");
            return StatusCode(500, "Internal server error");
        }
    }
}
