using MediatR;
using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Api.CQRS.Commands.Indicator;

/// <summary>
/// Command to delete an indicator
/// </summary>
public class DeleteIndicatorCommand : IRequest<Result<bool>>
{
    public int IndicatorId { get; set; }

    public DeleteIndicatorCommand(int indicatorId)
    {
        IndicatorId = indicatorId;
    }
}
