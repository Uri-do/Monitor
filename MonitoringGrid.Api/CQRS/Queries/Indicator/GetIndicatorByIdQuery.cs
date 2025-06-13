using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.DTOs;

namespace MonitoringGrid.Api.CQRS.Queries.Indicator;

/// <summary>
/// Query to get an indicator by ID
/// </summary>
public class GetIndicatorByIdQuery : IRequest<Result<IndicatorDto>>
{
    public long IndicatorID { get; set; }

    public GetIndicatorByIdQuery(long indicatorID)
    {
        IndicatorID = indicatorID;
    }
}
