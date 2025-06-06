using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for audit logging service
/// </summary>
public interface IAuditService
{
    Task LogActionAsync(string userId, string action, string resource, object? details = null, CancellationToken cancellationToken = default);
    Task LogLoginAsync(string userId, string ipAddress, bool success, string? reason = null, CancellationToken cancellationToken = default);
    Task LogConfigurationChangeAsync(string userId, string configType, object oldValue, object newValue, CancellationToken cancellationToken = default);
    Task LogAlertActionAsync(string userId, int alertId, string action, string? notes = null, CancellationToken cancellationToken = default);
    Task<List<AuditLogEntry>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
}
