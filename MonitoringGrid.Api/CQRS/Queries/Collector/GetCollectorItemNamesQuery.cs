using MediatR;
using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Api.CQRS.Queries.Collector;

/// <summary>
/// Query to get available item names for a specific collector
/// </summary>
public class GetCollectorItemNamesQuery : IRequest<Result<List<string>>>
{
    public int CollectorId { get; set; }

    public GetCollectorItemNamesQuery(int collectorId)
    {
        CollectorId = collectorId;
    }
}
