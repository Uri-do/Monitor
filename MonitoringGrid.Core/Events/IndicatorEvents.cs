using MediatR;
using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Core.Events;

/// <summary>
/// Domain event raised when an indicator execution starts
/// </summary>
public record IndicatorExecutionStartedEvent : DomainEvent, INotification
{
    public int IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }
    public string ExecutionContext { get; }

    public IndicatorExecutionStartedEvent(int indicatorId, string indicatorName, string owner, string executionContext)
    {
        IndicatorId = indicatorId;
        IndicatorName = indicatorName;
        Owner = owner;
        ExecutionContext = executionContext;
    }
}

/// <summary>
/// Domain event raised when an indicator execution completes
/// </summary>
public record IndicatorExecutionCompletedEvent : DomainEvent, INotification
{
    public int IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }

    public IndicatorExecutionCompletedEvent(int indicatorId, string indicatorName, string owner)
    {
        IndicatorId = indicatorId;
        IndicatorName = indicatorName;
        Owner = owner;
    }
}

/// <summary>
/// Domain event raised when an indicator is executed
/// </summary>
public record IndicatorExecutedEvent : DomainEvent, INotification
{
    public int IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }
    public bool WasSuccessful { get; }
    public decimal? CurrentValue { get; }
    public decimal? HistoricalValue { get; }
    public string? ErrorMessage { get; }

    public IndicatorExecutedEvent(int indicatorId, string indicatorName, string owner, bool wasSuccessful,
        decimal? currentValue = null, decimal? historicalValue = null, string? errorMessage = null)
    {
        IndicatorId = indicatorId;
        IndicatorName = indicatorName;
        Owner = owner;
        WasSuccessful = wasSuccessful;
        CurrentValue = currentValue;
        HistoricalValue = historicalValue;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Domain event raised when an indicator threshold is breached
/// </summary>
public record IndicatorThresholdBreachedEvent : DomainEvent, INotification
{
    public int IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }
    public decimal CurrentValue { get; }
    public decimal ThresholdValue { get; }
    public string ThresholdComparison { get; }
    public string Priority { get; }

    public IndicatorThresholdBreachedEvent(int indicatorId, string indicatorName, string owner,
        decimal currentValue, decimal thresholdValue, string thresholdComparison, string priority)
    {
        IndicatorId = indicatorId;
        IndicatorName = indicatorName;
        Owner = owner;
        CurrentValue = currentValue;
        ThresholdValue = thresholdValue;
        ThresholdComparison = thresholdComparison;
        Priority = priority;
    }
}

/// <summary>
/// Domain event raised when an indicator is created
/// </summary>
public record IndicatorCreatedEvent : DomainEvent, INotification
{
    public int IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }

    public IndicatorCreatedEvent(int indicatorId, string indicatorName, string owner)
    {
        IndicatorId = indicatorId;
        IndicatorName = indicatorName;
        Owner = owner;
    }
}

/// <summary>
/// Domain event raised when an indicator is updated
/// </summary>
public record IndicatorUpdatedEvent : DomainEvent, INotification
{
    public int IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }

    public IndicatorUpdatedEvent(int indicatorId, string indicatorName, string owner)
    {
        IndicatorId = indicatorId;
        IndicatorName = indicatorName;
        Owner = owner;
    }
}

/// <summary>
/// Domain event raised when an indicator is deleted
/// </summary>
public record IndicatorDeletedEvent : DomainEvent, INotification
{
    public int IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }

    public IndicatorDeletedEvent(int indicatorId, string indicatorName, string owner)
    {
        IndicatorId = indicatorId;
        IndicatorName = indicatorName;
        Owner = owner;
    }
}
