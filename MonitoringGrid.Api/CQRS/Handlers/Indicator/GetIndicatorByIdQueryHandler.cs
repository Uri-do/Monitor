using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs.Indicators;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Indicator;

/// <summary>
/// Handler for getting an indicator by ID
/// </summary>
public class GetIndicatorByIdQueryHandler : IRequestHandler<GetIndicatorByIdQuery, Result<IndicatorResponse>>
{
    private readonly IIndicatorService _indicatorService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIndicatorByIdQueryHandler> _logger;

    public GetIndicatorByIdQueryHandler(
        IIndicatorService indicatorService,
        IMapper mapper,
        ILogger<GetIndicatorByIdQueryHandler> logger)
    {
        _indicatorService = indicatorService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IndicatorResponse>> Handle(GetIndicatorByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting indicator by ID: {IndicatorId}", request.IndicatorID);

            var indicatorResult = await _indicatorService.GetIndicatorByIdAsync(request.IndicatorID, cancellationToken);

            if (!indicatorResult.IsSuccess)
            {
                _logger.LogWarning("Indicator not found: {IndicatorId}", request.IndicatorID);
                return Result.Failure<IndicatorResponse>("INDICATOR_NOT_FOUND", $"Indicator with ID {request.IndicatorID} not found");
            }

            var indicator = indicatorResult.Value;
            var indicatorDto = _mapper.Map<IndicatorResponse>(indicator);

            _logger.LogDebug("Successfully retrieved indicator {IndicatorId}: {IndicatorName}",
                indicator.IndicatorID, indicator.IndicatorName);

            return Result<IndicatorResponse>.Success(indicatorDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indicator by ID: {IndicatorId}", request.IndicatorID);
            return Result.Failure<IndicatorResponse>("GET_FAILED", $"Failed to get indicator: {ex.Message}");
        }
    }
}
