using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Core.Events;

/// <summary>
/// Domain event raised when a KPI execution completes
/// </summary>
public record KpiExecutionCompletedEvent(
    int KpiId,
    string Indicator,
    string Owner,
    DateTime CompletionTime
) : DomainEvent
{
    public KpiExecutionCompletedEvent(int kpiId, string indicator, string owner)
        : this(kpiId, indicator, owner, DateTime.UtcNow)
    {
    }
}
