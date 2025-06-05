using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Core.Services;

/// <summary>
/// Domain service for KPI business logic
/// </summary>
public class KpiDomainService
{
    private readonly IRepository<KPI> _kpiRepository;
    private readonly IRepository<AlertLog> _alertRepository;

    public KpiDomainService(IRepository<KPI> kpiRepository, IRepository<AlertLog> alertRepository)
    {
        _kpiRepository = kpiRepository;
        _alertRepository = alertRepository;
    }

    /// <summary>
    /// Gets KPIs that are due for execution
    /// </summary>
    public async Task<IEnumerable<KPI>> GetDueKpisAsync(int batchSize = 10, CancellationToken cancellationToken = default)
    {
        var currentTime = DateTime.UtcNow;
        
        var allKpis = await _kpiRepository.GetAsync(
            k => k.IsActive && 
                 (k.LastRun == null || k.LastRun < currentTime.AddMinutes(-k.Frequency)),
            cancellationToken);

        return allKpis
            .OrderBy(k => k.LastRun ?? DateTime.MinValue) // Process oldest first
            .Take(batchSize);
    }

    /// <summary>
    /// Validates KPI configuration
    /// </summary>
    public List<string> ValidateKpi(KPI kpi)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(kpi.Indicator))
            errors.Add("Indicator is required");

        if (string.IsNullOrWhiteSpace(kpi.Owner))
            errors.Add("Owner is required");

        if (kpi.Priority < 1 || kpi.Priority > 2)
            errors.Add("Priority must be 1 (SMS + Email) or 2 (Email Only)");

        if (kpi.Frequency <= 0)
            errors.Add("Frequency must be greater than 0");

        if (kpi.Deviation < 0 || kpi.Deviation > 100)
            errors.Add("Deviation must be between 0 and 100");

        if (string.IsNullOrWhiteSpace(kpi.SpName))
            errors.Add("Stored procedure name is required");

        if (string.IsNullOrWhiteSpace(kpi.SubjectTemplate))
            errors.Add("Subject template is required");

        if (string.IsNullOrWhiteSpace(kpi.DescriptionTemplate))
            errors.Add("Description template is required");

        if (kpi.CooldownMinutes < 0)
            errors.Add("Cooldown minutes cannot be negative");

        if (kpi.MinimumThreshold.HasValue && kpi.MinimumThreshold < 0)
            errors.Add("Minimum threshold cannot be negative");

        return errors;
    }

    /// <summary>
    /// Checks if a KPI indicator name is unique
    /// </summary>
    public async Task<bool> IsIndicatorUniqueAsync(string indicator, int? excludeKpiId = null, CancellationToken cancellationToken = default)
    {
        var existingKpi = await _kpiRepository.GetFirstOrDefaultAsync(
            k => k.Indicator == indicator && (excludeKpiId == null || k.KpiId != excludeKpiId),
            cancellationToken);

        return existingKpi == null;
    }

    /// <summary>
    /// Gets KPI statistics
    /// </summary>
    public async Task<KpiStatistics> GetKpiStatisticsAsync(int kpiId, int days = 30, CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        
        var alerts = await _alertRepository.GetAsync(
            a => a.KpiId == kpiId && a.TriggerTime >= startDate,
            cancellationToken);

        var alertList = alerts.ToList();
        var resolvedAlerts = alertList.Where(a => a.IsResolved && a.ResolvedTime.HasValue).ToList();

        return new KpiStatistics
        {
            KpiId = kpiId,
            TotalAlerts = alertList.Count,
            ResolvedAlerts = resolvedAlerts.Count,
            UnresolvedAlerts = alertList.Count - resolvedAlerts.Count,
            AverageResolutionTimeHours = resolvedAlerts.Any() 
                ? resolvedAlerts.Average(a => (a.ResolvedTime!.Value - a.TriggerTime).TotalHours)
                : 0,
            LastAlertTime = alertList.OrderByDescending(a => a.TriggerTime).FirstOrDefault()?.TriggerTime
        };
    }

    /// <summary>
    /// Updates KPI last run time
    /// </summary>
    public async Task UpdateLastRunAsync(int kpiId, CancellationToken cancellationToken = default)
    {
        var kpi = await _kpiRepository.GetByIdAsync(kpiId, cancellationToken);
        if (kpi != null)
        {
            kpi.UpdateLastRun();
            await _kpiRepository.UpdateAsync(kpi, cancellationToken);
            await _kpiRepository.SaveChangesAsync(cancellationToken);
        }
    }
}

/// <summary>
/// KPI statistics model
/// </summary>
public class KpiStatistics
{
    public int KpiId { get; set; }
    public int TotalAlerts { get; set; }
    public int ResolvedAlerts { get; set; }
    public int UnresolvedAlerts { get; set; }
    public double AverageResolutionTimeHours { get; set; }
    public DateTime? LastAlertTime { get; set; }
}
