using MediatR;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Api.CQRS.Queries.Indicator;

/// <summary>
/// Query to get an indicator by ID
/// </summary>
public class GetIndicatorByIdQuery : IRequest<Result<IndicatorDto>>
{
    public int IndicatorId { get; set; }

    public GetIndicatorByIdQuery(int indicatorId)
    {
        IndicatorId = indicatorId;
    }
}
