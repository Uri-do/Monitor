using MediatR;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.DTOs;

namespace MonitoringGrid.Api.CQRS.Queries.Indicator;

/// <summary>
/// Query to get an indicator by ID
/// </summary>
public class GetIndicatorByIdQuery : IRequest<Result<IndicatorDto>>
{
    public long IndicatorId { get; set; }

    public GetIndicatorByIdQuery(long indicatorId)
    {
        IndicatorId = indicatorId;
    }
}
