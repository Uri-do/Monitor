using MonitoringGrid.Api.CQRS.Commands;
using MonitoringGrid.Api.DTOs;

namespace MonitoringGrid.Api.CQRS.Commands.Kpi;

/// <summary>
/// Command to execute a KPI manually
/// </summary>
public class ExecuteKpiCommand : ICommand<KpiExecutionResultDto>
{
    public int KpiId { get; set; }
    public int? CustomFrequency { get; set; }

    public ExecuteKpiCommand(int kpiId, int? customFrequency = null)
    {
        KpiId = kpiId;
        CustomFrequency = customFrequency;
    }
}
