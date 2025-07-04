using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Api.CQRS.Queries.Collector;

/// <summary>
/// Query to get available collectors from ProgressPlayDB
/// </summary>
public class GetCollectorsQuery : IRequest<Result<List<CollectorDto>>>
{
    public bool? IsActive { get; set; } = true;
}
