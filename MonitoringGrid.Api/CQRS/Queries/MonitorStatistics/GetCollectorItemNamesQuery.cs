using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.Common;

namespace MonitoringGrid.Api.CQRS.Queries.MonitorStatistics;

/// <summary>
/// Query to get item names for a specific collector
/// </summary>
public class GetCollectorItemNamesQuery : IRequest<Result<List<string>>>
{
    public long CollectorId { get; set; }

    public GetCollectorItemNamesQuery(long collectorId)
    {
        CollectorId = collectorId;
    }
}
