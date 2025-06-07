using MonitoringGrid.Api.CQRS.Commands;

namespace MonitoringGrid.Api.CQRS.Commands.Kpi;

/// <summary>
/// Command to delete a KPI
/// </summary>
public class DeleteKpiCommand : ICommand<bool>
{
    public int KpiId { get; set; }

    public DeleteKpiCommand(int kpiId)
    {
        KpiId = kpiId;
    }
}
