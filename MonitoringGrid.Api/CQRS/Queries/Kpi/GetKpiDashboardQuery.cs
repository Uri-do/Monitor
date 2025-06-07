using MonitoringGrid.Api.CQRS.Queries;
using MonitoringGrid.Api.DTOs;

namespace MonitoringGrid.Api.CQRS.Queries.Kpi;

/// <summary>
/// Query to get KPI dashboard data
/// </summary>
public class GetKpiDashboardQuery : IQuery<KpiDashboardDto>
{
    public GetKpiDashboardQuery()
    {
    }
}
