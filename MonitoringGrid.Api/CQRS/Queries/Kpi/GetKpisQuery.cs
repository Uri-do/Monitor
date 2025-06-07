using MonitoringGrid.Api.CQRS.Queries;
using MonitoringGrid.Api.DTOs;

namespace MonitoringGrid.Api.CQRS.Queries.Kpi;

/// <summary>
/// Query to get KPIs with optional filtering
/// </summary>
public class GetKpisQuery : IQuery<List<KpiDto>>
{
    public bool? IsActive { get; set; }
    public string? Owner { get; set; }
    public byte? Priority { get; set; }

    public GetKpisQuery(bool? isActive = null, string? owner = null, byte? priority = null)
    {
        IsActive = isActive;
        Owner = owner;
        Priority = priority;
    }
}
