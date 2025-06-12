using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Specifications;

/// <summary>
/// Specification for unresolved alerts
/// </summary>
public class UnresolvedAlertsSpecification : BaseSpecification<AlertLog>
{
    public UnresolvedAlertsSpecification()
        : base(a => !a.IsResolved)
    {
        AddInclude(a => a.Indicator);
        ApplyOrderByDescending(a => a.TriggerTime);
    }
}

/// <summary>
/// Specification for alerts by severity
/// </summary>
public class AlertsBySeveritySpecification : BaseSpecification<AlertLog>
{
    public AlertsBySeveritySpecification(string severity)
        : base(a => a.GetSeverity() == severity)
    {
        AddInclude(a => a.Indicator);
        ApplyOrderByDescending(a => a.TriggerTime);
    }
}

/// <summary>
/// Specification for alerts within date range
/// </summary>
public class AlertsByDateRangeSpecification : BaseSpecification<AlertLog>
{
    public AlertsByDateRangeSpecification(DateTime startDate, DateTime endDate)
        : base(a => a.TriggerTime >= startDate && a.TriggerTime <= endDate)
    {
        AddInclude(a => a.Indicator);
        ApplyOrderByDescending(a => a.TriggerTime);
    }
}

/// <summary>
/// Specification for alerts by Indicator
/// </summary>
public class AlertsByIndicatorSpecification : BaseSpecification<AlertLog>
{
    public AlertsByIndicatorSpecification(int indicatorId)
        : base(a => a.KpiId == indicatorId)
    {
        AddInclude(a => a.Indicator);
        ApplyOrderByDescending(a => a.TriggerTime);
    }
}

/// <summary>
/// Specification for recent alerts
/// </summary>
public class RecentAlertsSpecification : BaseSpecification<AlertLog>
{
    public RecentAlertsSpecification(int hoursBack = 24)
        : base(a => a.TriggerTime >= DateTime.UtcNow.AddHours(-hoursBack))
    {
        AddInclude(a => a.Indicator);
        ApplyOrderByDescending(a => a.TriggerTime);
    }
}

/// <summary>
/// Specification for critical alerts
/// </summary>
public class CriticalAlertsSpecification : BaseSpecification<AlertLog>
{
    public CriticalAlertsSpecification()
        : base(a => a.GetSeverity() == "Critical" || a.GetSeverity() == "Emergency")
    {
        AddInclude(a => a.Indicator);
        ApplyOrderByDescending(a => a.TriggerTime);
    }
}

/// <summary>
/// Specification for alerts requiring escalation
/// </summary>
public class AlertsRequiringEscalationSpecification : BaseSpecification<AlertLog>
{
    public AlertsRequiringEscalationSpecification(int minutesThreshold = 30)
        : base(a => !a.IsResolved &&
                   a.TriggerTime <= DateTime.UtcNow.AddMinutes(-minutesThreshold) &&
                   (a.GetSeverity() == "Critical" || a.GetSeverity() == "Emergency"))
    {
        AddInclude(a => a.Indicator);
        // Note: AlertEscalations navigation property may need to be added to AlertLog entity
        ApplyOrderBy(a => a.TriggerTime);
    }
}

/// <summary>
/// Specification for alerts by owner
/// </summary>
public class AlertsByOwnerSpecification : BaseSpecification<AlertLog>
{
    public AlertsByOwnerSpecification(string owner)
        : base(a => a.Indicator.OwnerContactId.ToString() == owner)
    {
        AddInclude(a => a.Indicator);
        ApplyOrderByDescending(a => a.TriggerTime);
    }
}
