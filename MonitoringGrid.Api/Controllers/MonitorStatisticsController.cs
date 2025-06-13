using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.CQRS.Queries.MonitorStatistics;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API Controller for managing monitor statistics and collectors
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MonitorStatisticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMonitorStatisticsService _statisticsService;
    private readonly ILogger<MonitorStatisticsController> _logger;

    public MonitorStatisticsController(
        IMediator mediator,
        IMonitorStatisticsService statisticsService,
        ILogger<MonitorStatisticsController> logger)
    {
        _mediator = mediator;
        _statisticsService = statisticsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active statistics collectors
    /// </summary>
    [HttpGet("collectors")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<List<MonitorStatisticsCollectorDto>>>> GetActiveCollectors(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCollectorsQuery { ActiveOnly = activeOnly };
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(500, result);
    }

    /// <summary>
    /// Get collector by ID
    /// </summary>
    [HttpGet("collectors/{collectorId}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<MonitorStatisticsCollectorDto>>> GetCollector(long collectorId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting collector {CollectorId}", collectorId);
            
            var collector = await _statisticsService.GetCollectorByCollectorIdAsync(collectorId, cancellationToken);
            
            if (collector == null)
            {
                return NotFound(Result<MonitorStatisticsCollectorDto>.Failure("COLLECTOR_NOT_FOUND", $"Collector {collectorId} not found"));
            }

            var dto = new MonitorStatisticsCollectorDto
            {
                ID = collector.ID,
                CollectorID = collector.CollectorID,
                CollectorCode = collector.CollectorCode,
                CollectorDesc = collector.CollectorDesc,
                FrequencyMinutes = collector.FrequencyMinutes,
                LastMinutes = collector.LastMinutes,
                StoreProcedure = collector.StoreProcedure,
                IsActive = collector.IsActive,
                UpdatedDate = collector.UpdatedDate,
                LastRun = collector.LastRun,
                LastRunResult = collector.LastRunResult
            };

            return Ok(Result<MonitorStatisticsCollectorDto>.Success(dto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collector {CollectorId}", collectorId);
            return StatusCode(500, Result<MonitorStatisticsCollectorDto>.Failure("GET_COLLECTOR_FAILED", "Failed to retrieve collector"));
        }
    }

    /// <summary>
    /// Get item names for a specific collector
    /// </summary>
    [HttpGet("collectors/{collectorId}/items")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<List<string>>>> GetCollectorItemNames(long collectorId, CancellationToken cancellationToken = default)
    {
        var query = new GetCollectorItemNamesQuery(collectorId);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsSuccess)
            return Ok(result);

        return StatusCode(500, result);
    }

    /// <summary>
    /// Get statistics for a collector
    /// </summary>
    [HttpGet("collectors/{collectorId}/statistics")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<List<MonitorStatisticsDto>>>> GetStatistics(
        long collectorId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int hours = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting statistics for collector {CollectorId}", collectorId);
            
            List<Core.Entities.MonitorStatistics> statistics;
            
            if (fromDate.HasValue && toDate.HasValue)
            {
                statistics = await _statisticsService.GetStatisticsAsync(collectorId, fromDate.Value, toDate.Value, cancellationToken);
            }
            else
            {
                statistics = await _statisticsService.GetLatestStatisticsAsync(collectorId, hours, cancellationToken);
            }
            
            var dtos = statistics.Select(s => new MonitorStatisticsDto
            {
                Day = s.Day,
                Hour = s.Hour,
                CollectorID = s.CollectorID,
                ItemName = s.ItemName,
                Total = s.Total,
                Marked = s.Marked,
                MarkedPercent = s.MarkedPercent,
                UpdatedDate = s.UpdatedDate
            }).ToList();

            return Ok(Result<List<MonitorStatisticsDto>>.Success(dtos));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting statistics for collector {CollectorId}", collectorId);
            return StatusCode(500, Result<List<MonitorStatisticsDto>>.Failure("GET_STATISTICS_FAILED", "Failed to retrieve statistics"));
        }
    }
}
