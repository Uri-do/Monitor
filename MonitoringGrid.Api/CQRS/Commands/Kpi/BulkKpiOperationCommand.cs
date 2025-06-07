using MonitoringGrid.Api.CQRS.Commands;

namespace MonitoringGrid.Api.CQRS.Commands.Kpi;

/// <summary>
/// Command to perform bulk operations on KPIs
/// </summary>
public class BulkKpiOperationCommand : ICommand<string>
{
    public List<int> KpiIds { get; set; } = new();
    public string Operation { get; set; } = string.Empty; // "activate", "deactivate", "delete", "execute"

    public BulkKpiOperationCommand()
    {
    }

    public BulkKpiOperationCommand(List<int> kpiIds, string operation)
    {
        KpiIds = kpiIds;
        Operation = operation;
    }
}
