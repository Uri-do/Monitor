using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.DTOs.Common;
using MonitoringGrid.Api.DTOs.MonitorStatistics;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.CQRS.Queries.MonitorStatistics;
using System.Diagnostics;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API Controller for managing monitor statistics and collectors
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
public class MonitorStatisticsController : BaseApiController
{
    private readonly IMonitorStatisticsService _statisticsService;

    public MonitorStatisticsController(
        IMediator mediator,
        IMonitorStatisticsService statisticsService,
        ILogger<MonitorStatisticsController> logger)
        : base(mediator, logger)
    {
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// Get all statistics collectors
    /// </summary>
    /// <param name="request">Get collectors request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of collectors</returns>
    [HttpGet("collectors")]
    [ProducesResponseType(typeof(List<CollectorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CollectorResponse>>> GetCollectors(
        [FromQuery] GetCollectorsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetCollectorsRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            var query = new GetCollectorsQuery { ActiveOnly = request.ActiveOnly };
            var result = await Mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return StatusCode(500, CreateErrorResponse(result.Error?.Message ?? "Failed to get collectors", "GET_COLLECTORS_FAILED"));
            }

            stopwatch.Stop();

            // Map to enhanced response DTOs
            var collectors = result.Value.Select(c => new CollectorResponse
            {
                ID = c.ID,
                CollectorID = c.CollectorID,
                CollectorCode = c.CollectorCode,
                CollectorDesc = c.CollectorDesc,
                FrequencyMinutes = c.FrequencyMinutes,
                LastMinutes = c.LastMinutes ?? 0,
                StoreProcedure = c.StoreProcedure,
                IsActive = c.IsActive ?? false,
                UpdatedDate = c.UpdatedDate,
                LastRun = c.LastRun,
                LastRunResult = c.LastRunResult,
                Details = request.IncludeDetails ? new Dictionary<string, object>
                {
                    ["QueryDurationMs"] = stopwatch.ElapsedMilliseconds,
                    ["FilterApplied"] = request.ActiveOnly ? "ActiveOnly" : "All",
                    ["SearchTerm"] = request.SearchTerm ?? "None"
                } : null
            }).ToList();

            Logger.LogDebug("Retrieved {Count} collectors in {Duration}ms", collectors.Count, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(collectors, $"Retrieved {collectors.Count} collectors"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get collectors operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Get collectors operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error getting collectors");
            return StatusCode(500, CreateErrorResponse($"Error getting collectors: {ex.Message}", "GET_COLLECTORS_ERROR"));
        }
    }

    /// <summary>
    /// Get collector by ID
    /// </summary>
    /// <param name="collectorId">Collector ID</param>
    /// <param name="request">Get collector request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collector details</returns>
    [HttpGet("collectors/{collectorId}")]
    [ProducesResponseType(typeof(CollectorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectorResponse>> GetCollector(
        long collectorId,
        [FromQuery] GetCollectorRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetCollectorRequest { CollectorId = collectorId };

            // Validate collector ID matches route parameter
            if (request.CollectorId != collectorId)
            {
                request.CollectorId = collectorId; // Use route parameter
            }

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Additional validation for collector ID
            var paramValidation = ValidateParameter(collectorId, nameof(collectorId),
                id => id > 0, "Collector ID must be a positive integer");
            if (paramValidation != null) return BadRequest(paramValidation);

            Logger.LogDebug("Getting collector {CollectorId}", collectorId);

            var collector = await _statisticsService.GetCollectorByCollectorIdAsync(collectorId, cancellationToken);

            if (collector == null)
            {
                return NotFound(CreateErrorResponse($"Collector {collectorId} not found", "COLLECTOR_NOT_FOUND"));
            }

            stopwatch.Stop();

            var response = new CollectorResponse
            {
                ID = collector.ID,
                CollectorID = collector.CollectorID,
                CollectorCode = collector.CollectorCode,
                CollectorDesc = collector.CollectorDesc,
                FrequencyMinutes = collector.FrequencyMinutes,
                LastMinutes = collector.LastMinutes ?? 0,
                StoreProcedure = collector.StoreProcedure,
                IsActive = collector.IsActive ?? false,
                UpdatedDate = collector.UpdatedDate,
                LastRun = collector.LastRun,
                LastRunResult = collector.LastRunResult,
                Details = request.IncludeDetails ? new Dictionary<string, object>
                {
                    ["QueryDurationMs"] = stopwatch.ElapsedMilliseconds,
                    ["LastAccessTime"] = DateTime.UtcNow,
                    ["RequestedMetrics"] = request.IncludeMetrics
                } : null
            };

            // Add metrics if requested
            if (request.IncludeMetrics)
            {
                response.Metrics = new CollectorMetrics
                {
                    LastSuccessfulExecution = collector.LastRun,
                    NextScheduledExecution = collector.LastRun?.AddMinutes(collector.FrequencyMinutes)
                };
            }

            Logger.LogDebug("Retrieved collector {CollectorId} in {Duration}ms",
                collectorId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, $"Retrieved collector {collectorId}"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get collector operation was cancelled for collector {CollectorId}", collectorId);
            return StatusCode(499, CreateErrorResponse("Get collector operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error getting collector {CollectorId}", collectorId);
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve collector {collectorId}: {ex.Message}", "GET_COLLECTOR_ERROR"));
        }
    }

    /// <summary>
    /// Get item names for a specific collector
    /// </summary>
    /// <param name="collectorId">Collector ID</param>
    /// <param name="request">Get collector item names request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of item names</returns>
    [HttpGet("collectors/{collectorId}/items")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<string>>> GetCollectorItemNames(
        long collectorId,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? maxItems = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Additional validation for collector ID
            var paramValidation = ValidateParameter(collectorId, nameof(collectorId),
                id => id > 0, "Collector ID must be a positive integer");
            if (paramValidation != null) return BadRequest(paramValidation);

            // Create request object from parameters
            var request = new GetCollectorItemNamesRequest
            {
                CollectorId = collectorId,
                SearchTerm = searchTerm,
                MaxItems = maxItems
            };

            // Validate query parameters if provided
            if (!string.IsNullOrEmpty(searchTerm) && searchTerm.Length > 50)
            {
                return BadRequest(CreateErrorResponse("Search term cannot exceed 50 characters", "INVALID_SEARCH_TERM"));
            }

            if (maxItems.HasValue && (maxItems.Value < 1 || maxItems.Value > 1000))
            {
                return BadRequest(CreateErrorResponse("Max items must be between 1 and 1000", "INVALID_MAX_ITEMS"));
            }

            var query = new GetCollectorItemNamesQuery(collectorId);
            var result = await Mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                return StatusCode(500, CreateErrorResponse(result.Error?.Message ?? "Failed to get item names", "GET_ITEM_NAMES_FAILED"));
            }

            var itemNames = result.Value;

            // Apply search filter if specified
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                itemNames = itemNames
                    .Where(name => name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Apply max items limit if specified
            if (request.MaxItems.HasValue && itemNames.Count > request.MaxItems.Value)
            {
                itemNames = itemNames.Take(request.MaxItems.Value).ToList();
            }

            stopwatch.Stop();

            Logger.LogDebug("Retrieved {Count} item names for collector {CollectorId} in {Duration}ms",
                itemNames.Count, collectorId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(itemNames, $"Retrieved {itemNames.Count} item names for collector {collectorId}"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get collector item names operation was cancelled for collector {CollectorId}", collectorId);
            return StatusCode(499, CreateErrorResponse("Get collector item names operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error getting item names for collector {CollectorId}", collectorId);
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve item names for collector {collectorId}: {ex.Message}", "GET_ITEM_NAMES_ERROR"));
        }
    }

    /// <summary>
    /// Get statistics for a collector
    /// </summary>
    /// <param name="collectorId">Collector ID</param>
    /// <param name="request">Get statistics request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Statistics data</returns>
    [HttpGet("collectors/{collectorId}/statistics")]
    [ProducesResponseType(typeof(BulkStatisticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BulkStatisticsResponse>> GetStatistics(
        long collectorId,
        [FromQuery] GetStatisticsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetStatisticsRequest { CollectorId = collectorId };

            // Validate collector ID matches route parameter
            if (request.CollectorId != collectorId)
            {
                request.CollectorId = collectorId; // Use route parameter
            }

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Additional validation for collector ID
            var paramValidation = ValidateParameter(collectorId, nameof(collectorId),
                id => id > 0, "Collector ID must be a positive integer");
            if (paramValidation != null) return BadRequest(paramValidation);

            Logger.LogDebug("Getting statistics for collector {CollectorId}", collectorId);

            List<Core.Entities.MonitorStatistics> statistics;

            if (request.FromDate.HasValue && request.ToDate.HasValue)
            {
                statistics = await _statisticsService.GetStatisticsAsync(collectorId, request.FromDate.Value, request.ToDate.Value, cancellationToken);
            }
            else
            {
                statistics = await _statisticsService.GetLatestStatisticsAsync(collectorId, request.Hours, cancellationToken);
            }

            // Apply item name filter if specified
            if (!string.IsNullOrEmpty(request.ItemName))
            {
                statistics = statistics
                    .Where(s => s.ItemName.Contains(request.ItemName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            stopwatch.Stop();

            // Map to enhanced response DTOs
            var statisticsResponse = statistics.Select(s => new StatisticsResponse
            {
                Day = s.Day,
                Hour = s.Hour,
                CollectorID = s.CollectorID,
                ItemName = s.ItemName,
                Total = (long)(s.Total ?? 0),
                Marked = (long)(s.Marked ?? 0),
                MarkedPercent = s.MarkedPercent ?? 0,
                UpdatedDate = s.UpdatedDate ?? DateTime.UtcNow
            }).ToList();

            var response = new BulkStatisticsResponse
            {
                CollectorId = collectorId,
                TotalCount = statisticsResponse.Count(),
                DateRange = new DateRange
                {
                    StartDate = request.FromDate ?? DateTime.UtcNow.AddHours(-request.Hours),
                    EndDate = request.ToDate ?? DateTime.UtcNow,
                    TotalHours = request.Hours
                },
                Statistics = statisticsResponse,
                QueryMetrics = new QueryMetrics
                {
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryCount = 1,
                    CacheHit = false
                }
            };

            // Add aggregates if requested
            if (request.IncludeAggregates && statisticsResponse.Any())
            {
                response.Aggregates = new StatisticsAggregates
                {
                    TotalSum = statisticsResponse.Sum(s => s.Total),
                    MarkedSum = statisticsResponse.Sum(s => s.Marked),
                    AverageMarkedPercent = statisticsResponse.Average(s => s.MarkedPercent),
                    Peak = new StatisticsPeak
                    {
                        HighestTotal = statisticsResponse.Max(s => s.Total),
                        HighestMarked = statisticsResponse.Max(s => s.Marked),
                        HighestMarkedPercent = statisticsResponse.Max(s => s.MarkedPercent),
                        PeakTime = statisticsResponse.OrderByDescending(s => s.Total).First().Day
                    },
                    ItemAggregates = statisticsResponse
                        .GroupBy(s => s.ItemName)
                        .Select(g => new ItemAggregate
                        {
                            ItemName = g.Key,
                            Total = g.Sum(s => s.Total),
                            Marked = g.Sum(s => s.Marked),
                            AverageMarkedPercent = g.Average(s => s.MarkedPercent)
                        }).ToList()
                };
            }

            Logger.LogDebug("Retrieved {Count} statistics for collector {CollectorId} in {Duration}ms",
                statisticsResponse.Count, collectorId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, $"Retrieved {statisticsResponse.Count} statistics for collector {collectorId}"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get statistics operation was cancelled for collector {CollectorId}", collectorId);
            return StatusCode(499, CreateErrorResponse("Get statistics operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error getting statistics for collector {CollectorId}", collectorId);
            return StatusCode(500, CreateErrorResponse($"Failed to retrieve statistics for collector {collectorId}: {ex.Message}", "GET_STATISTICS_ERROR"));
        }
    }
}
