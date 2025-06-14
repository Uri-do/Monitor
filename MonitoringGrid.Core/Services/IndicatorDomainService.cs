using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.ValueObjects;
using MonitoringGrid.Core.Events;

namespace MonitoringGrid.Core.Services;

/// <summary>
/// Domain service for complex Indicator business logic
/// </summary>
public class IndicatorDomainService
{
    /// <summary>
    /// Determines if an indicator should be executed based on its schedule and current state
    /// </summary>
    public bool ShouldExecuteIndicator(Indicator indicator, DateTime currentTime)
    {
        if (!indicator.IsActive)
            return false;

        if (indicator.IsCurrentlyRunning)
            return false;

        if (indicator.Scheduler == null || !indicator.Scheduler.IsEnabled)
            return false;

        // Check if enough time has passed since last execution
        if (indicator.LastRun.HasValue)
        {
            var nextExecutionTime = indicator.Scheduler.GetNextExecutionTime(indicator.LastRun);
            if (!nextExecutionTime.HasValue || currentTime < nextExecutionTime.Value)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Calculates the priority score for indicator execution ordering
    /// </summary>
    public int CalculatePriorityScore(Indicator indicator)
    {
        var priority = Priority.FromString(indicator.Priority);
        var baseScore = priority.NumericValue * 1000;

        // Add urgency based on how overdue the indicator is
        if (indicator.LastRun.HasValue && indicator.Scheduler != null)
        {
            var nextExecution = indicator.Scheduler.GetNextExecutionTime(indicator.LastRun);
            if (nextExecution.HasValue)
            {
                var overdueMinutes = (DateTime.UtcNow - nextExecution.Value).TotalMinutes;
                if (overdueMinutes > 0)
                {
                    baseScore -= (int)Math.Min(overdueMinutes * 10, 500); // Max 500 point bonus for being overdue
                }
            }
        }

        return baseScore;
    }

    /// <summary>
    /// Validates indicator configuration for business rules
    /// </summary>
    public ValidationResult ValidateIndicatorConfiguration(Indicator indicator)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate threshold configuration
        if (indicator.ThresholdValue > 0)
        {
            try
            {
                var threshold = new ThresholdValue(
                    indicator.ThresholdValue,
                    indicator.ThresholdComparison,
                    indicator.ThresholdType);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"Invalid threshold configuration: {ex.Message}");
            }
        }

        // Validate priority
        try
        {
            Priority.FromString(indicator.Priority);
        }
        catch (ArgumentException)
        {
            errors.Add($"Invalid priority: {indicator.Priority}");
        }

        // Validate LastMinutes
        if (indicator.LastMinutes <= 0)
        {
            errors.Add("LastMinutes must be greater than 0");
        }
        else if (indicator.LastMinutes > 10080) // 7 days
        {
            warnings.Add("LastMinutes is very large (> 7 days), consider if this is intentional");
        }

        // Validate AverageLastDays
        if (indicator.AverageLastDays <= 0)
        {
            errors.Add("AverageLastDays must be greater than 0");
        }
        else if (indicator.AverageLastDays > 365)
        {
            warnings.Add("AverageLastDays is very large (> 1 year), consider performance impact");
        }

        // Validate scheduler relationship
        if (indicator.SchedulerID.HasValue && indicator.Scheduler == null)
        {
            warnings.Add("Scheduler ID is set but Scheduler entity is not loaded");
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Determines the appropriate escalation level for an indicator alert
    /// </summary>
    public EscalationLevel DetermineEscalationLevel(Indicator indicator, decimal currentValue, decimal? historicalValue)
    {
        if (indicator.ThresholdValue <= 0)
            return EscalationLevel.None;

        var threshold = new ThresholdValue(
            indicator.ThresholdValue,
            indicator.ThresholdComparison,
            indicator.ThresholdType);

        if (!threshold.IsBreached(currentValue, historicalValue ?? currentValue))
            return EscalationLevel.None;

        var severity = threshold.GetBreachSeverity(currentValue, historicalValue);
        var priority = Priority.FromString(indicator.Priority);

        return (severity, priority.Value) switch
        {
            ("Critical", "high") => EscalationLevel.Immediate,
            ("Critical", _) => EscalationLevel.High,
            ("High", "high") => EscalationLevel.High,
            ("High", _) => EscalationLevel.Medium,
            ("Medium", "high") => EscalationLevel.Medium,
            ("Medium", _) => EscalationLevel.Low,
            _ => EscalationLevel.Low
        };
    }

    /// <summary>
    /// Calculates the recommended execution frequency for an indicator
    /// </summary>
    public TimeSpan CalculateRecommendedFrequency(Indicator indicator, List<decimal> recentValues)
    {
        var priority = Priority.FromString(indicator.Priority);
        var baseFrequency = priority switch
        {
            var p when p == Priority.High => TimeSpan.FromMinutes(5),
            var p when p == Priority.Medium => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromMinutes(30)
        };

        // Adjust based on value volatility
        if (recentValues.Count >= 5)
        {
            var volatility = CalculateVolatility(recentValues);
            if (volatility > 0.5) // High volatility
            {
                baseFrequency = TimeSpan.FromTicks(baseFrequency.Ticks / 2); // More frequent
            }
            else if (volatility < 0.1) // Low volatility
            {
                baseFrequency = TimeSpan.FromTicks(baseFrequency.Ticks * 2); // Less frequent
            }
        }

        return baseFrequency;
    }

    /// <summary>
    /// Creates domain events for indicator state changes
    /// </summary>
    public List<IDomainEvent> CreateIndicatorStateChangeEvents(Indicator indicator, IndicatorStateChange change)
    {
        var events = new List<IDomainEvent>();

        switch (change.ChangeType)
        {
            case IndicatorChangeType.Executed:
                events.Add(new IndicatorExecutedEvent(
                    indicator.IndicatorID,
                    indicator.IndicatorName,
                    indicator.OwnerContact?.Name ?? "Unknown",
                    change.WasSuccessful,
                    change.CurrentValue,
                    change.HistoricalValue,
                    change.ErrorMessage,
                    change.ExecutionDuration,
                    change.CollectorName));
                break;

            case IndicatorChangeType.ThresholdBreached:
                if (indicator.ThresholdValue > 0 && change.CurrentValue.HasValue)
                {
                    events.Add(new IndicatorThresholdBreachedEvent(
                        indicator.IndicatorID,
                        indicator.IndicatorName,
                        indicator.OwnerContact?.Name ?? "Unknown",
                        change.CurrentValue.Value,
                        indicator.ThresholdValue,
                        indicator.ThresholdComparison,
                        indicator.Priority));
                }
                break;

            case IndicatorChangeType.Created:
                events.Add(new IndicatorCreatedEvent(
                    indicator.IndicatorID,
                    indicator.IndicatorName,
                    indicator.OwnerContact?.Name ?? "Unknown"));
                break;

            case IndicatorChangeType.Updated:
                events.Add(new IndicatorUpdatedEvent(
                    indicator.IndicatorID,
                    indicator.IndicatorName,
                    indicator.OwnerContact?.Name ?? "Unknown"));
                break;

            case IndicatorChangeType.Deleted:
                events.Add(new IndicatorDeletedEvent(
                    indicator.IndicatorID,
                    indicator.IndicatorName,
                    indicator.OwnerContact?.Name ?? "Unknown"));
                break;
        }

        return events;
    }

    private static double CalculateVolatility(List<decimal> values)
    {
        if (values.Count < 2)
            return 0;

        var mean = values.Average();
        var variance = values.Sum(v => Math.Pow((double)(v - mean), 2)) / values.Count;
        return Math.Sqrt(variance) / (double)mean;
    }
}

/// <summary>
/// Validation result for indicator configuration
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Escalation levels for indicator alerts
/// </summary>
public enum EscalationLevel
{
    None,
    Low,
    Medium,
    High,
    Immediate
}

/// <summary>
/// Indicator state change information
/// </summary>
public class IndicatorStateChange
{
    public IndicatorChangeType ChangeType { get; set; }
    public bool WasSuccessful { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? HistoricalValue { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan? ExecutionDuration { get; set; }
    public string? CollectorName { get; set; }
}

/// <summary>
/// Types of indicator changes
/// </summary>
public enum IndicatorChangeType
{
    Created,
    Updated,
    Deleted,
    Executed,
    ThresholdBreached
}
