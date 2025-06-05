using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service responsible for managing alerts and notifications
/// </summary>
public interface IAlertService
{
    /// <summary>
    /// Sends alerts for a KPI based on its configuration and execution result
    /// </summary>
    Task<AlertResult> SendAlertsAsync(KPI kpi, KpiExecutionResult executionResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a KPI is currently in cooldown period
    /// </summary>
    Task<bool> IsInCooldownAsync(KPI kpi, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an alert to the database
    /// </summary>
    Task LogAlertAsync(KPI kpi, AlertResult alertResult, KpiExecutionResult executionResult, CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a message from a template by replacing placeholders
    /// </summary>
    string BuildMessageFromTemplate(string template, KPI kpi, KpiExecutionResult result);

    /// <summary>
    /// Resolves an alert with optional notes
    /// </summary>
    Task<bool> ResolveAlertAsync(long alertId, string resolvedBy, string? notes = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves multiple alerts in bulk
    /// </summary>
    Task<int> BulkResolveAlertsAsync(IEnumerable<long> alertIds, string resolvedBy, string? notes = null, CancellationToken cancellationToken = default);
}
