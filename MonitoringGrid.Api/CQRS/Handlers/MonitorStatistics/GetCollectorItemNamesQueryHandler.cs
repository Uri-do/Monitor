using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Queries.MonitorStatistics;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.MonitorStatistics;

/// <summary>
/// Handler for getting collector item names
/// </summary>
public class GetCollectorItemNamesQueryHandler : IRequestHandler<GetCollectorItemNamesQuery, Result<List<string>>>
{
    private readonly IMonitorStatisticsService _statisticsService;
    private readonly ILogger<GetCollectorItemNamesQueryHandler> _logger;

    public GetCollectorItemNamesQueryHandler(
        IMonitorStatisticsService statisticsService,
        ILogger<GetCollectorItemNamesQueryHandler> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    public async Task<Result<List<string>>> Handle(GetCollectorItemNamesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting item names for collector {CollectorId}", request.CollectorId);

            var itemNames = await _statisticsService.GetCollectorItemNamesAsync(request.CollectorId, cancellationToken);

            _logger.LogDebug("Retrieved {Count} item names for collector {CollectorId}", itemNames.Count, request.CollectorId);
            return Result<List<string>>.Success(itemNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get item names for collector {CollectorId}", request.CollectorId);
            return Result.Failure<List<string>>("GET_ITEM_NAMES_FAILED", "Failed to retrieve item names");
        }
    }
}
