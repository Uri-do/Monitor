using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Indicator;

/// <summary>
/// Handler for getting an indicator by ID
/// </summary>
public class GetIndicatorByIdQueryHandler : IRequestHandler<GetIndicatorByIdQuery, Result<IndicatorDto>>
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

    public async Task<Result<IndicatorDto>> Handle(GetIndicatorByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting indicator by ID: {IndicatorId}", request.IndicatorId);

            var indicator = await _indicatorService.GetIndicatorByIdAsync(request.IndicatorId, cancellationToken);

            if (indicator == null)
            {
                _logger.LogWarning("Indicator not found: {IndicatorId}", request.IndicatorId);
                return Result.Failure<IndicatorDto>("INDICATOR_NOT_FOUND", $"Indicator with ID {request.IndicatorId} not found");
            }

            var indicatorDto = _mapper.Map<IndicatorDto>(indicator);

            _logger.LogDebug("Successfully retrieved indicator {IndicatorId}: {IndicatorName}", 
                indicator.IndicatorId, indicator.IndicatorName);

            return Result<IndicatorDto>.Success(indicatorDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indicator by ID: {IndicatorId}", request.IndicatorId);
            return Result.Failure<IndicatorDto>("GET_FAILED", $"Failed to get indicator: {ex.Message}");
        }
    }
}
