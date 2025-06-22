using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Events;

/// <summary>
/// Event raised when an alert is triggered
/// </summary>
public record AlertTriggeredEvent : DomainEvent
{
    public AlertTriggeredEvent(AlertLog alert)
    {
        AlertId = (int)alert.AlertId;
        IndicatorId = alert.IndicatorID;
        Severity = alert.GetSeverity();
        Subject = alert.Subject ?? string.Empty;
        Message = alert.Message;
        CurrentValue = alert.CurrentValue ?? 0;
        HistoricalValue = alert.HistoricalValue ?? 0;
        Deviation = alert.DeviationPercent;
    }

    public int AlertId { get; init; }
    public long IndicatorId { get; init; }
    public string Severity { get; init; }
    public string Subject { get; init; }
    public string Message { get; init; }
    public decimal CurrentValue { get; init; }
    public decimal HistoricalValue { get; init; }
    public decimal? Deviation { get; init; }
}

/// <summary>
/// Event raised when an alert is resolved
/// </summary>
public record AlertResolvedEvent : DomainEvent
{
    public AlertResolvedEvent(int alertId, long indicatorId, string resolvedBy, string? resolution = null)
    {
        AlertId = alertId;
        IndicatorId = indicatorId;
        ResolvedBy = resolvedBy;
        Resolution = resolution;
    }

    public int AlertId { get; init; }
    public long IndicatorId { get; init; }
    public string ResolvedBy { get; init; }
    public string? Resolution { get; init; }
}

/// <summary>
/// Event raised when an alert is acknowledged
/// </summary>
public record AlertAcknowledgedEvent : DomainEvent
{
    public AlertAcknowledgedEvent(int alertId, long indicatorId, string acknowledgedBy, string? notes = null)
    {
        AlertId = alertId;
        IndicatorId = indicatorId;
        AcknowledgedBy = acknowledgedBy;
        Notes = notes;
    }

    public int AlertId { get; init; }
    public long IndicatorId { get; init; }
    public string AcknowledgedBy { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Event raised when an alert escalation is triggered
/// </summary>
public record AlertEscalationTriggeredEvent : DomainEvent
{
    public AlertEscalationTriggeredEvent(int alertId, long indicatorId, int escalationLevel, string reason)
    {
        AlertId = alertId;
        IndicatorId = indicatorId;
        EscalationLevel = escalationLevel;
        Reason = reason;
    }

    public int AlertId { get; init; }
    public long IndicatorId { get; init; }
    public int EscalationLevel { get; init; }
    public string Reason { get; init; }
}

/// <summary>
/// Event raised when an alert notification is sent
/// </summary>
public record AlertNotificationSentEvent : DomainEvent
{
    public AlertNotificationSentEvent(int alertId, long indicatorId, string channel,
        List<string> recipients, bool wasSuccessful, string? errorMessage = null)
    {
        AlertId = alertId;
        IndicatorId = indicatorId;
        Channel = channel;
        Recipients = recipients;
        WasSuccessful = wasSuccessful;
        ErrorMessage = errorMessage;
    }

    public int AlertId { get; init; }
    public long IndicatorId { get; init; }
    public string Channel { get; init; }
    public List<string> Recipients { get; init; }
    public bool WasSuccessful { get; init; }
    public string? ErrorMessage { get; init; }
}
