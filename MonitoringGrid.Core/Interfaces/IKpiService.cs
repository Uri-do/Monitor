using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service interface for KPI management operations
/// </summary>
public interface IKpiService
{
    /// <summary>
    /// Gets all KPIs
    /// </summary>
    Task<List<KPI>> GetAllKpisAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a KPI by ID
    /// </summary>
    Task<KPI?> GetKpiByIdAsync(int kpiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active KPIs only
    /// </summary>
    Task<List<KPI>> GetActiveKpisAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets KPIs that are due for execution
    /// </summary>
    Task<List<KPI>> GetDueKpisAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new KPI
    /// </summary>
    Task<KPI> CreateKpiAsync(KPI kpi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing KPI
    /// </summary>
    Task<KPI> UpdateKpiAsync(KPI kpi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a KPI
    /// </summary>
    Task<bool> DeleteKpiAsync(int kpiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets KPIs by owner
    /// </summary>
    Task<List<KPI>> GetKpisByOwnerAsync(string owner, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets KPIs by priority
    /// </summary>
    Task<List<KPI>> GetKpisByPriorityAsync(byte priority, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a KPI
    /// </summary>
    Task<bool> ActivateKpiAsync(int kpiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a KPI
    /// </summary>
    Task<bool> DeactivateKpiAsync(int kpiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets KPI statistics
    /// </summary>
    Task<KpiStatistics> GetKpiStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a KPI configuration
    /// </summary>
    Task<ValidationResult> ValidateKpiAsync(KPI kpi, CancellationToken cancellationToken = default);
}

/// <summary>
/// KPI statistics summary
/// </summary>
public class KpiStatistics
{
    public int TotalKpis { get; set; }
    public int ActiveKpis { get; set; }
    public int InactiveKpis { get; set; }
    public int RunningKpis { get; set; }
    public int DueKpis { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Validation result for KPI operations
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(params string[] errors) => new() { IsValid = false, Errors = errors.ToList() };
}
