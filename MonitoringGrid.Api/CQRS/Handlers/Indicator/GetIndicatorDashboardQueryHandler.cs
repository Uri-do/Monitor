using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.CQRS.Handlers.Indicator;

/// <summary>
/// Handler for getting indicator dashboard data
/// </summary>
public class GetIndicatorDashboardQueryHandler : IRequestHandler<GetIndicatorDashboardQuery, Result<IndicatorDashboardDto>>
{
    private readonly IIndicatorService _indicatorService;
    private readonly IMapper _mapper;
    private readonly ILogger<GetIndicatorDashboardQueryHandler> _logger;

    public GetIndicatorDashboardQueryHandler(
        IIndicatorService indicatorService,
        IMapper mapper,
        ILogger<GetIndicatorDashboardQueryHandler> logger)
    {
        _indicatorService = indicatorService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IndicatorDashboardDto>> Handle(GetIndicatorDashboardQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting indicator dashboard data");

            var dashboardData = await _indicatorService.GetIndicatorDashboardAsync(cancellationToken);
            var dashboardDto = _mapper.Map<IndicatorDashboardDto>(dashboardData);

            // Add indicator statuses
            var allIndicators = await _indicatorService.GetAllIndicatorsAsync(cancellationToken);
            dashboardDto.IndicatorStatuses = allIndicators.Select(i => new IndicatorStatusDto
            {
                IndicatorId = i.IndicatorId,
                IndicatorName = i.IndicatorName,
                IsActive = i.IsActive,
                IsCurrentlyRunning = i.IsCurrentlyRunning,
                LastRun = i.LastRun,
                NextRun = i.GetNextRunTime(),
                Status = GetIndicatorStatus(i),
                LastError = i.LastRunResult?.Contains("Error") == true ? i.LastRunResult : null
            }).ToList();

            _logger.LogDebug("Successfully retrieved indicator dashboard data");

            return Result<IndicatorDashboardDto>.Success(dashboardDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indicator dashboard data");
            return Result<IndicatorDashboardDto>.Failure($"Failed to get dashboard data: {ex.Message}");
        }
    }

    private static string GetIndicatorStatus(Core.Entities.Indicator indicator)
    {
        if (!indicator.IsActive)
            return "inactive";

        if (indicator.IsCurrentlyRunning)
            return "running";

        if (indicator.LastRun == null)
            return "never_run";

        if (indicator.IsDue())
            return "due";

        if (indicator.LastRunResult?.Contains("Error") == true)
            return "error";

        return "healthy";
    }
}
