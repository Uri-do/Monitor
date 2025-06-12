using MediatR;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Api.CQRS.Queries.Indicator;

/// <summary>
/// Query to get indicator dashboard data
/// </summary>
public class GetIndicatorDashboardQuery : IRequest<Result<IndicatorDashboardDto>>
{
    // No parameters needed for dashboard query
}
