using MediatR;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Api.DTOs;

namespace MonitoringGrid.Api.CQRS.Queries.Indicator;

/// <summary>
/// Query to get indicators with optional filtering
/// </summary>
public class GetIndicatorsQuery : IRequest<Result<List<IndicatorDto>>>
{
    public bool? IsActive { get; set; }
    public string? Priority { get; set; }
    public int? OwnerContactId { get; set; }
    public int? CollectorId { get; set; }
    public string? ThresholdType { get; set; }
    public bool? IsCurrentlyRunning { get; set; }
    public DateTime? LastRunAfter { get; set; }
    public DateTime? LastRunBefore { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string SortBy { get; set; } = "IndicatorName";
    public string SortDirection { get; set; } = "asc";
}
