using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service interface for managing alerts
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Get alerts with filtering and pagination
    /// </summary>
    Task<Result<PaginatedAlertsDto>> GetAlertsAsync(AlertFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get alert by ID
    /// </summary>
    Task<Result<AlertLogDto>> GetAlertByIdAsync(long alertId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new alert
    /// </summary>
    Task<Result<AlertLogDto>> CreateAlertAsync(CreateAlertRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve an alert
    /// </summary>
    Task<Result<bool>> ResolveAlertAsync(long alertId, ResolveAlertRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk resolve alerts
    /// </summary>
    Task<Result<int>> BulkResolveAlertsAsync(BulkResolveAlertsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get alert statistics
    /// </summary>
    Task<Result<AlertStatisticsDto>> GetAlertStatisticsAsync(int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get alert dashboard data
    /// </summary>
    Task<Result<AlertDashboardDto>> GetAlertDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get alerts for a specific indicator
    /// </summary>
    Task<Result<List<AlertLogDto>>> GetAlertsByIndicatorAsync(long indicatorId, int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get unresolved alerts count
    /// </summary>
    Task<Result<int>> GetUnresolvedAlertsCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Process indicator execution result for alerts
    /// </summary>
    Task<Result<bool>> ProcessIndicatorExecutionAsync(long indicatorId, decimal value, bool thresholdExceeded, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for creating an alert
/// </summary>
public class CreateAlertRequest
{
    public long IndicatorID { get; set; }
    public string AlertMessage { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public decimal? TriggerValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public string? AdditionalData { get; set; }
}

/// <summary>
/// Request model for resolving an alert
/// </summary>
public class ResolveAlertRequest
{
    public string ResolvedBy { get; set; } = string.Empty;
    public string? ResolutionNotes { get; set; }
}

/// <summary>
/// Request model for bulk resolving alerts
/// </summary>
public class BulkResolveAlertsRequest
{
    public List<long> AlertIds { get; set; } = new();
    public string ResolvedBy { get; set; } = string.Empty;
    public string? ResolutionNotes { get; set; }
}
