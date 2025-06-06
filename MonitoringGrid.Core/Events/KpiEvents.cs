using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Events;

/// <summary>
/// Event raised when a KPI is executed
/// </summary>
public record KpiExecutedEvent : DomainEvent
{
    public KpiExecutedEvent(int kpiId, string indicator, string owner, bool wasSuccessful, 
        decimal? currentValue = null, decimal? historicalValue = null, string? errorMessage = null)
    {
        KpiId = kpiId;
        Indicator = indicator;
        Owner = owner;
        WasSuccessful = wasSuccessful;
        CurrentValue = currentValue;
        HistoricalValue = historicalValue;
        ErrorMessage = errorMessage;
    }

    public int KpiId { get; init; }
    public string Indicator { get; init; }
    public string Owner { get; init; }
    public bool WasSuccessful { get; init; }
    public decimal? CurrentValue { get; init; }
    public decimal? HistoricalValue { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Event raised when a KPI configuration is created
/// </summary>
public record KpiCreatedEvent : DomainEvent
{
    public KpiCreatedEvent(KPI kpi)
    {
        KpiId = kpi.KpiId;
        Indicator = kpi.Indicator;
        Owner = kpi.Owner;
        Priority = kpi.Priority;
        Frequency = kpi.Frequency;
        CreatedBy = "System"; // TODO: Get from current user context
    }

    public int KpiId { get; init; }
    public string Indicator { get; init; }
    public string Owner { get; init; }
    public byte Priority { get; init; }
    public int Frequency { get; init; }
    public string CreatedBy { get; init; }
}

/// <summary>
/// Event raised when a KPI configuration is updated
/// </summary>
public record KpiUpdatedEvent : DomainEvent
{
    public KpiUpdatedEvent(KPI kpi, string updatedBy)
    {
        KpiId = kpi.KpiId;
        Indicator = kpi.Indicator;
        Owner = kpi.Owner;
        UpdatedBy = updatedBy;
    }

    public int KpiId { get; init; }
    public string Indicator { get; init; }
    public string Owner { get; init; }
    public string UpdatedBy { get; init; }
}

/// <summary>
/// Event raised when a KPI is deactivated
/// </summary>
public record KpiDeactivatedEvent : DomainEvent
{
    public KpiDeactivatedEvent(int kpiId, string indicator, string reason, string deactivatedBy)
    {
        KpiId = kpiId;
        Indicator = indicator;
        Reason = reason;
        DeactivatedBy = deactivatedBy;
    }

    public int KpiId { get; init; }
    public string Indicator { get; init; }
    public string Reason { get; init; }
    public string DeactivatedBy { get; init; }
}

/// <summary>
/// Event raised when a KPI threshold is breached
/// </summary>
public record KpiThresholdBreachedEvent : DomainEvent
{
    public KpiThresholdBreachedEvent(int kpiId, string indicator, decimal currentValue, 
        decimal historicalValue, decimal deviation, string severity)
    {
        KpiId = kpiId;
        Indicator = indicator;
        CurrentValue = currentValue;
        HistoricalValue = historicalValue;
        Deviation = deviation;
        Severity = severity;
    }

    public int KpiId { get; init; }
    public string Indicator { get; init; }
    public decimal CurrentValue { get; init; }
    public decimal HistoricalValue { get; init; }
    public decimal Deviation { get; init; }
    public string Severity { get; init; }
}
