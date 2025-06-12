using MediatR;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.DTOs;

namespace MonitoringGrid.Api.CQRS.Commands.Indicator;

/// <summary>
/// Command to execute an indicator
/// </summary>
public class ExecuteIndicatorCommand : IRequest<Result<IndicatorExecutionResultDto>>
{
    public long IndicatorId { get; set; }
    public string ExecutionContext { get; set; } = "Manual";
    public bool SaveResults { get; set; } = true;

    public ExecuteIndicatorCommand(long indicatorId, string executionContext = "Manual", bool saveResults = true)
    {
        IndicatorId = indicatorId;
        ExecutionContext = executionContext;
        SaveResults = saveResults;
    }
}
