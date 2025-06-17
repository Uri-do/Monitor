using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.DTOs.Indicators;

namespace MonitoringGrid.Api.CQRS.Queries.Indicator;

/// <summary>
/// Query to get an indicator by ID
/// </summary>
public class GetIndicatorByIdQuery : IRequest<Result<IndicatorResponse>>
{
    public long IndicatorID { get; set; }

    public GetIndicatorByIdQuery(long indicatorID)
    {
        IndicatorID = indicatorID;
    }
}
