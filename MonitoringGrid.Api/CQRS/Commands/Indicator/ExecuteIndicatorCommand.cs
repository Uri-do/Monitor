using MediatR;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Api.CQRS.Commands.Indicator;

/// <summary>
/// Command to execute an indicator
/// </summary>
public class ExecuteIndicatorCommand : IRequest<Result<IndicatorExecutionResultDto>>
{
    public int IndicatorId { get; set; }
    public string ExecutionContext { get; set; } = "Manual";
    public bool SaveResults { get; set; } = true;

    public ExecuteIndicatorCommand(int indicatorId, string executionContext = "Manual", bool saveResults = true)
    {
        IndicatorId = indicatorId;
        ExecutionContext = executionContext;
        SaveResults = saveResults;
    }
}
