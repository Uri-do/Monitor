using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.Common;

namespace MonitoringGrid.Api.CQRS.Commands.Indicator;

/// <summary>
/// Command to delete an indicator
/// </summary>
public class DeleteIndicatorCommand : IRequest<Result<bool>>
{
    public long IndicatorID { get; set; }

    public DeleteIndicatorCommand(long indicatorID)
    {
        IndicatorID = indicatorID;
    }
}
