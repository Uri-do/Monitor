using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Specifications;

/// <summary>
/// Specification for Indicators that are due for execution
/// </summary>
public class IndicatorsDueForExecutionSpecification : BaseSpecification<Indicator>
{
    public IndicatorsDueForExecutionSpecification()
        : base(i => i.IsActive && !i.IsCurrentlyRunning)
    {
        AddInclude(i => i.IndicatorContacts);
        AddInclude(i => i.OwnerContact!);
        ApplyOrderBy(i => i.LastRun ?? DateTime.MinValue);
    }
}

/// <summary>
/// Specification for Indicators by owner contact ID
/// </summary>
public class IndicatorsByOwnerSpecification : BaseSpecification<Indicator>
{
    public IndicatorsByOwnerSpecification(int ownerContactId)
        : base(i => i.OwnerContactId == ownerContactId && i.IsActive)
    {
        AddInclude(i => i.IndicatorContacts);
        AddInclude(i => i.OwnerContact!);
        ApplyOrderBy(i => i.IndicatorName);
    }
}

/// <summary>
/// Specification for Indicators by priority
/// </summary>
public class IndicatorsByPrioritySpecification : BaseSpecification<Indicator>
{
    public IndicatorsByPrioritySpecification(string priority)
        : base(i => i.Priority == priority && i.IsActive)
    {
        AddInclude(i => i.IndicatorContacts);
        AddInclude(i => i.OwnerContact!);
        ApplyOrderBy(i => i.IndicatorName);
    }
}

/// <summary>
/// Specification for Indicators by collector ID
/// </summary>
public class IndicatorsByCollectorSpecification : BaseSpecification<Indicator>
{
    public IndicatorsByCollectorSpecification(int collectorId)
        : base(i => i.CollectorID == collectorId && i.IsActive)
    {
        AddInclude(i => i.IndicatorContacts);
        AddInclude(i => i.OwnerContact!);
        ApplyOrderBy(i => i.IndicatorName);
    }
}

/// <summary>
/// Specification for active Indicators
/// </summary>
public class ActiveIndicatorsSpecification : BaseSpecification<Indicator>
{
    public ActiveIndicatorsSpecification()
        : base(i => i.IsActive)
    {
        AddInclude(i => i.IndicatorContacts);
        AddInclude(i => i.OwnerContact!);
        ApplyOrderBy(i => i.IndicatorName);
    }
}

/// <summary>
/// Specification for currently running Indicators
/// </summary>
public class RunningIndicatorsSpecification : BaseSpecification<Indicator>
{
    public RunningIndicatorsSpecification()
        : base(i => i.IsCurrentlyRunning)
    {
        AddInclude(i => i.IndicatorContacts);
        AddInclude(i => i.OwnerContact!);
        ApplyOrderBy(i => i.ExecutionStartTime!);
    }
}

/// <summary>
/// Specification for Indicators with specific threshold type
/// </summary>
public class IndicatorsByThresholdTypeSpecification : BaseSpecification<Indicator>
{
    public IndicatorsByThresholdTypeSpecification(string thresholdType)
        : base(i => i.ThresholdType == thresholdType && i.IsActive)
    {
        AddInclude(i => i.IndicatorContacts);
        AddInclude(i => i.OwnerContact!);
        ApplyOrderBy(i => i.IndicatorName);
    }
}

/// <summary>
/// Specification for Indicators by collector and item name
/// </summary>
public class IndicatorsByCollectorAndItemSpecification : BaseSpecification<Indicator>
{
    public IndicatorsByCollectorAndItemSpecification(int collectorId, string itemName)
        : base(i => i.CollectorID == collectorId && i.CollectorItemName == itemName && i.IsActive)
    {
        AddInclude(i => i.IndicatorContacts);
        AddInclude(i => i.OwnerContact!);
        ApplyOrderBy(i => i.IndicatorName);
    }
}

/// <summary>
/// Specification for Indicators that haven't run recently
/// </summary>
public class StaleIndicatorsSpecification : BaseSpecification<Indicator>
{
    public StaleIndicatorsSpecification(int hoursThreshold = 24)
        : base(i => i.IsActive &&
                   (i.LastRun == null || i.LastRun < DateTime.UtcNow.AddHours(-hoursThreshold)))
    {
        AddInclude(i => i.IndicatorContacts);
        AddInclude(i => i.OwnerContact!);
        ApplyOrderBy(i => i.LastRun ?? DateTime.MinValue);
    }
}
