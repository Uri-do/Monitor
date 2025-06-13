using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.DTOs;

namespace MonitoringGrid.Api.CQRS.Queries.MonitorStatistics;

/// <summary>
/// Query to get all active statistics collectors
/// </summary>
public class GetCollectorsQuery : IRequest<Result<List<MonitorStatisticsCollectorDto>>>
{
    public bool ActiveOnly { get; set; } = true;
}
