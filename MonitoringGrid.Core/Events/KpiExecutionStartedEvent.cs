using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Core.Events;

/// <summary>
/// Domain event raised when a KPI execution starts
/// </summary>
public record KpiExecutionStartedEvent(
    int KpiId,
    string Indicator,
    string Owner,
    string ExecutionContext,
    DateTime StartTime
) : DomainEvent
{
    public KpiExecutionStartedEvent(int kpiId, string indicator, string owner, string executionContext)
        : this(kpiId, indicator, owner, executionContext, DateTime.UtcNow)
    {
    }
}
