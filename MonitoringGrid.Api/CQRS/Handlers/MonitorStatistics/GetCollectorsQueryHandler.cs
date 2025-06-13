using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Queries.MonitorStatistics;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.MonitorStatistics;

/// <summary>
/// Handler for getting statistics collectors
/// </summary>
public class GetCollectorsQueryHandler : IRequestHandler<GetCollectorsQuery, Result<List<MonitorStatisticsCollectorDto>>>
{
    private readonly IMonitorStatisticsService _statisticsService;
    private readonly ILogger<GetCollectorsQueryHandler> _logger;

    public GetCollectorsQueryHandler(
        IMonitorStatisticsService statisticsService,
        ILogger<GetCollectorsQueryHandler> logger)
    {
        _statisticsService = statisticsService;
        _logger = logger;
    }

    public async Task<Result<List<MonitorStatisticsCollectorDto>>> Handle(GetCollectorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting statistics collectors, ActiveOnly: {ActiveOnly}", request.ActiveOnly);

            var collectors = request.ActiveOnly 
                ? await _statisticsService.GetActiveCollectorsAsync(cancellationToken)
                : await _statisticsService.GetCollectorsWithStatisticsAsync(cancellationToken);

            var dtos = collectors.Select(c => new MonitorStatisticsCollectorDto
            {
                ID = c.ID,
                CollectorID = c.CollectorID,
                CollectorCode = c.CollectorCode,
                CollectorDesc = c.CollectorDesc,
                FrequencyMinutes = c.FrequencyMinutes,
                LastMinutes = c.LastMinutes,
                StoreProcedure = c.StoreProcedure,
                IsActive = c.IsActive,
                UpdatedDate = c.UpdatedDate,
                LastRun = c.LastRun,
                LastRunResult = c.LastRunResult,
                StatisticsCount = c.Statistics?.Count ?? 0
            }).ToList();

            _logger.LogDebug("Retrieved {Count} collectors", dtos.Count);
            return Result<List<MonitorStatisticsCollectorDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics collectors");
            return Result.Failure<List<MonitorStatisticsCollectorDto>>("GET_COLLECTORS_FAILED", "Failed to retrieve collectors");
        }
    }
}
