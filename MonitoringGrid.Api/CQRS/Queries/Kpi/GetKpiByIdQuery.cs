using MonitoringGrid.Api.CQRS.Queries;
using MonitoringGrid.Api.DTOs;

namespace MonitoringGrid.Api.CQRS.Queries.Kpi;

/// <summary>
/// Query to get a KPI by ID
/// </summary>
public class GetKpiByIdQuery : IQuery<KpiDto?>
{
    public int KpiId { get; set; }

    public GetKpiByIdQuery(int kpiId)
    {
        KpiId = kpiId;
    }
}
