using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Queries.Collector;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Collector;

/// <summary>
/// Handler for getting available collectors from ProgressPlayDB
/// </summary>
public class GetCollectorsQueryHandler : IRequestHandler<GetCollectorsQuery, Result<List<CollectorDto>>>
{
    private readonly IProgressPlayDbService _progressPlayDbService;
    private readonly ILogger<GetCollectorsQueryHandler> _logger;

    public GetCollectorsQueryHandler(
        IProgressPlayDbService progressPlayDbService,
        ILogger<GetCollectorsQueryHandler> logger)
    {
        _progressPlayDbService = progressPlayDbService;
        _logger = logger;
    }

    public async Task<Result<List<CollectorDto>>> Handle(GetCollectorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting collectors from ProgressPlayDB, IsActive: {IsActive}", request.IsActive);

            var collectors = await _progressPlayDbService.GetCollectorsAsync(cancellationToken);

            // Filter by active status if specified
            if (request.IsActive.HasValue)
            {
                collectors = collectors.Where(c => c.IsActive == request.IsActive.Value).ToList();
            }

            _logger.LogDebug("Retrieved {Count} collectors", collectors.Count);

            return Result<List<CollectorDto>>.Success(collectors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get collectors from ProgressPlayDB");
            return Result.Failure<List<CollectorDto>>("GET_FAILED", $"Failed to get collectors: {ex.Message}");
        }
    }
}
