using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Specifications;

/// <summary>
/// Specification for KPIs that are due for execution
/// </summary>
public class KpisDueForExecutionSpecification : BaseSpecification<KPI>
{
    public KpisDueForExecutionSpecification() 
        : base(k => k.IsActive && 
                   (k.LastRun == null || k.LastRun < DateTime.UtcNow.AddMinutes(-k.Frequency)))
    {
        AddInclude(k => k.KpiContacts);
        ApplyOrderBy(k => k.LastRun ?? DateTime.MinValue);
    }
}

/// <summary>
/// Specification for KPIs by owner
/// </summary>
public class KpisByOwnerSpecification : BaseSpecification<KPI>
{
    public KpisByOwnerSpecification(string owner) 
        : base(k => k.Owner == owner && k.IsActive)
    {
        AddInclude(k => k.KpiContacts);
        ApplyOrderBy(k => k.Indicator);
    }
}

/// <summary>
/// Specification for KPIs with high priority (SMS alerts)
/// </summary>
public class HighPriorityKpisSpecification : BaseSpecification<KPI>
{
    public HighPriorityKpisSpecification() 
        : base(k => k.Priority == 1 && k.IsActive)
    {
        AddInclude(k => k.KpiContacts);
        AddInclude(k => k.AlertLogs);
        ApplyOrderBy(k => k.Indicator);
    }
}

/// <summary>
/// Specification for KPIs that haven't run recently
/// </summary>
public class StaleKpisSpecification : BaseSpecification<KPI>
{
    public StaleKpisSpecification(int hoursThreshold = 24) 
        : base(k => k.IsActive && 
                   k.LastRun.HasValue && 
                   k.LastRun < DateTime.UtcNow.AddHours(-hoursThreshold))
    {
        ApplyOrderBy(k => k.LastRun);
    }
}

/// <summary>
/// Specification for KPIs with frequent alerts
/// </summary>
public class FrequentAlertKpisSpecification : BaseSpecification<KPI>
{
    public FrequentAlertKpisSpecification(int alertCountThreshold = 10, int daysBack = 7) 
        : base(k => k.IsActive && 
                   k.AlertLogs.Count(a => a.TriggerTime >= DateTime.UtcNow.AddDays(-daysBack)) >= alertCountThreshold)
    {
        AddInclude(k => k.AlertLogs);
        ApplyOrderByDescending(k => k.AlertLogs.Count);
    }
}

/// <summary>
/// Specification for KPIs by frequency range
/// </summary>
public class KpisByFrequencyRangeSpecification : BaseSpecification<KPI>
{
    public KpisByFrequencyRangeSpecification(int minFrequency, int maxFrequency) 
        : base(k => k.IsActive && k.Frequency >= minFrequency && k.Frequency <= maxFrequency)
    {
        ApplyOrderBy(k => k.Frequency);
    }
}

/// <summary>
/// Specification for KPIs with specific deviation threshold
/// </summary>
public class KpisByDeviationThresholdSpecification : BaseSpecification<KPI>
{
    public KpisByDeviationThresholdSpecification(decimal minDeviation, decimal maxDeviation) 
        : base(k => k.IsActive && k.Deviation >= minDeviation && k.Deviation <= maxDeviation)
    {
        ApplyOrderBy(k => k.Deviation);
    }
}

/// <summary>
/// Specification for KPIs that are in cooldown period
/// </summary>
public class KpisInCooldownSpecification : BaseSpecification<KPI>
{
    public KpisInCooldownSpecification() 
        : base(k => k.IsActive && 
                   k.LastRun.HasValue && 
                   k.LastRun > DateTime.UtcNow.AddMinutes(-k.CooldownMinutes))
    {
        ApplyOrderByDescending(k => k.LastRun);
    }
}

/// <summary>
/// Specification for searching KPIs by indicator name
/// </summary>
public class KpiSearchSpecification : BaseSpecification<KPI>
{
    public KpiSearchSpecification(string searchTerm) 
        : base(k => k.IsActive && 
                   (k.Indicator.Contains(searchTerm) || 
                    k.Owner.Contains(searchTerm) || 
                    k.SubjectTemplate.Contains(searchTerm)))
    {
        ApplyOrderBy(k => k.Indicator);
    }
}
