using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Indicator;

/// <summary>
/// Handler for getting an indicator by ID
/// </summary>
public class GetIndicatorByIdQueryHandler : IRequestHandler<GetIndicatorByIdQuery, Result<MonitoringGrid.Core.Entities.Indicator>>
{
    private readonly IIndicatorService _indicatorService;
    private readonly IProgressPlayDbService _progressPlayDbService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIndicatorByIdQueryHandler> _logger;

    public GetIndicatorByIdQueryHandler(
        IIndicatorService indicatorService,
        IProgressPlayDbService progressPlayDbService,
        IMapper mapper,
        ILogger<GetIndicatorByIdQueryHandler> logger)
    {
        _indicatorService = indicatorService;
        _progressPlayDbService = progressPlayDbService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<MonitoringGrid.Core.Entities.Indicator>> Handle(GetIndicatorByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting indicator by ID: {IndicatorId}", request.IndicatorID);

            var indicatorResult = await _indicatorService.GetIndicatorByIdAsync(request.IndicatorID, cancellationToken);

            if (!indicatorResult.IsSuccess)
            {
                _logger.LogWarning("Indicator not found: {IndicatorId}", request.IndicatorID);
                return Result.Failure<MonitoringGrid.Core.Entities.Indicator>("INDICATOR_NOT_FOUND", $"Indicator with ID {request.IndicatorID} not found");
            }

            var indicator = indicatorResult.Value;

            _logger.LogDebug("Successfully retrieved indicator {IndicatorId}: {IndicatorName}",
                indicator.IndicatorID, indicator.IndicatorName);

            return Result<MonitoringGrid.Core.Entities.Indicator>.Success(indicator);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indicator by ID: {IndicatorId}", request.IndicatorID);
            return Result.Failure<MonitoringGrid.Core.Entities.Indicator>("GET_FAILED", $"Failed to get indicator: {ex.Message}");
        }
    }
}
