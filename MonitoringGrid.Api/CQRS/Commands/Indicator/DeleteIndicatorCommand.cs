using MediatR;
using MonitoringGrid.Api.Common;

namespace MonitoringGrid.Api.CQRS.Commands.Indicator;

/// <summary>
/// Command to delete an indicator
/// </summary>
public class DeleteIndicatorCommand : IRequest<Result<bool>>
{
    public long IndicatorId { get; set; }

    public DeleteIndicatorCommand(long indicatorId)
    {
        IndicatorId = indicatorId;
    }
}
