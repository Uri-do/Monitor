using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.DTOs.Indicators;

namespace MonitoringGrid.Api.CQRS.Queries.Indicator;

/// <summary>
/// Query to get indicator dashboard data
/// </summary>
public class GetIndicatorDashboardQuery : IRequest<Result<IndicatorDashboardResponse>>
{
    // No parameters needed for dashboard query
}
