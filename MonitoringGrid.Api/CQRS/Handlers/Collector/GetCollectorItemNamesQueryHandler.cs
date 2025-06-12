using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Queries.Collector;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Collector;

/// <summary>
/// Handler for getting available item names for a specific collector
/// </summary>
public class GetCollectorItemNamesQueryHandler : IRequestHandler<GetCollectorItemNamesQuery, Result<List<string>>>
{
    private readonly IProgressPlayDbService _progressPlayDbService;
    private readonly ILogger<GetCollectorItemNamesQueryHandler> _logger;

    public GetCollectorItemNamesQueryHandler(
        IProgressPlayDbService progressPlayDbService,
        ILogger<GetCollectorItemNamesQueryHandler> logger)
    {
        _progressPlayDbService = progressPlayDbService;
        _logger = logger;
    }

    public async Task<Result<List<string>>> Handle(GetCollectorItemNamesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting item names for collector {CollectorId}", request.CollectorId);

            // First verify the collector exists
            var collector = await _progressPlayDbService.GetCollectorByIdAsync(request.CollectorId, cancellationToken);
            if (collector == null)
            {
                return Result.Failure<List<string>>("COLLECTOR_NOT_FOUND", $"Collector with ID {request.CollectorId} not found");
            }

            var itemNames = await _progressPlayDbService.GetCollectorItemNamesAsync(request.CollectorId, cancellationToken);

            _logger.LogDebug("Retrieved {Count} item names for collector {CollectorId}", 
                itemNames.Count, request.CollectorId);

            return Result<List<string>>.Success(itemNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get item names for collector {CollectorId}", request.CollectorId);
            return Result.Failure<List<string>>("GET_FAILED", $"Failed to get item names: {ex.Message}");
        }
    }
}
