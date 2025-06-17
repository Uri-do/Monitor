using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs.Indicators;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Api.CQRS.Handlers.Indicator;

/// <summary>
/// Handler for getting indicator dashboard data
/// </summary>
public class GetIndicatorDashboardQueryHandler : IRequestHandler<GetIndicatorDashboardQuery, Result<IndicatorDashboardResponse>>
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

    public async Task<Result<IndicatorDashboardResponse>> Handle(GetIndicatorDashboardQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Getting indicator dashboard data");

            var dashboardOptions = new DashboardOptions();
            var dashboardData = await _indicatorService.GetIndicatorDashboardAsync(dashboardOptions, cancellationToken);
            var dashboardDto = _mapper.Map<IndicatorDashboardResponse>(dashboardData);

            // Dashboard data is already populated by the service and mapper

            _logger.LogDebug("Successfully retrieved indicator dashboard data");

            return Result<IndicatorDashboardResponse>.Success(dashboardDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get indicator dashboard data");
            return Result.Failure<IndicatorDashboardResponse>("DASHBOARD_FAILED", $"Failed to get dashboard data: {ex.Message}");
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
