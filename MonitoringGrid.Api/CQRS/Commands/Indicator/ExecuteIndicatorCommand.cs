using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.DTOs;

namespace MonitoringGrid.Api.CQRS.Commands.Indicator;

/// <summary>
/// Command to execute an indicator
/// </summary>
public class ExecuteIndicatorCommand : IRequest<Result<IndicatorExecutionResultDto>>
{
    public long IndicatorID { get; set; }
    public string ExecutionContext { get; set; } = "Manual";
    public bool SaveResults { get; set; } = true;

    public ExecuteIndicatorCommand(long indicatorID, string executionContext = "Manual", bool saveResults = true)
    {
        IndicatorID = indicatorID;
        ExecutionContext = executionContext;
        SaveResults = saveResults;
    }
}
