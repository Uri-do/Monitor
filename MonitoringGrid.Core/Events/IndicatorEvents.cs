using MediatR;
using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Core.Events;

/// <summary>
/// Domain event raised when an indicator execution starts
/// </summary>
public record IndicatorExecutionStartedEvent : DomainEvent, INotification
{
    public long IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }
    public string ExecutionContext { get; }

    public IndicatorExecutionStartedEvent(long indicatorId, string indicatorName, string owner, string executionContext)
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
    public long IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }

    public IndicatorExecutionCompletedEvent(long indicatorId, string indicatorName, string owner)
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
    public long IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }
    public bool WasSuccessful { get; }
    public decimal? CurrentValue { get; }
    public decimal? HistoricalValue { get; }
    public string? ErrorMessage { get; }
    public TimeSpan? ExecutionDuration { get; }
    public string? CollectorName { get; }
    public long? ExecutionHistoryId { get; init; }

    public IndicatorExecutedEvent(long indicatorId, string indicatorName, string owner, bool wasSuccessful,
        decimal? currentValue = null, decimal? historicalValue = null, string? errorMessage = null,
        TimeSpan? executionDuration = null, string? collectorName = null, long? executionHistoryId = null)
    {
        IndicatorId = indicatorId;
        IndicatorName = indicatorName;
        Owner = owner;
        WasSuccessful = wasSuccessful;
        CurrentValue = currentValue;
        HistoricalValue = historicalValue;
        ErrorMessage = errorMessage;
        ExecutionDuration = executionDuration;
        CollectorName = collectorName;
        ExecutionHistoryId = executionHistoryId;
    }

    /// <summary>
    /// Gets the execution performance category
    /// </summary>
    public string GetPerformanceCategory()
    {
        if (!ExecutionDuration.HasValue)
            return "Unknown";

        return ExecutionDuration.Value.TotalSeconds switch
        {
            < 1 => "Excellent",
            < 5 => "Good",
            < 15 => "Acceptable",
            < 30 => "Slow",
            _ => "Very Slow"
        };
    }

    /// <summary>
    /// Determines if this execution indicates a performance issue
    /// </summary>
    public bool HasPerformanceIssue()
    {
        return ExecutionDuration?.TotalSeconds > 15;
    }

    /// <summary>
    /// Gets the deviation percentage if both values are available
    /// </summary>
    public decimal? GetDeviationPercentage()
    {
        if (!CurrentValue.HasValue || !HistoricalValue.HasValue || HistoricalValue.Value == 0)
            return null;

        return Math.Abs((CurrentValue.Value - HistoricalValue.Value) / HistoricalValue.Value) * 100;
    }

    /// <summary>
    /// Determines if this execution result indicates an anomaly
    /// </summary>
    public bool IsAnomaly()
    {
        var deviation = GetDeviationPercentage();
        return deviation.HasValue && deviation.Value > 50;
    }
}

/// <summary>
/// Domain event raised when an indicator threshold is breached
/// </summary>
public record IndicatorThresholdBreachedEvent : DomainEvent, INotification
{
    public long IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }
    public decimal CurrentValue { get; }
    public decimal ThresholdValue { get; }
    public string ThresholdComparison { get; }
    public string Priority { get; }

    public IndicatorThresholdBreachedEvent(long indicatorId, string indicatorName, string owner,
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
    public long IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }

    public IndicatorCreatedEvent(long indicatorId, string indicatorName, string owner)
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
    public long IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }

    public IndicatorUpdatedEvent(long indicatorId, string indicatorName, string owner)
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
    public long IndicatorId { get; }
    public string IndicatorName { get; }
    public string Owner { get; }

    public IndicatorDeletedEvent(long indicatorId, string indicatorName, string owner)
    {
        IndicatorId = indicatorId;
        IndicatorName = indicatorName;
        Owner = owner;
    }
}
