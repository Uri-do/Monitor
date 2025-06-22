using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.Security;

namespace MonitoringGrid.Core.Interfaces.Security;

/// <summary>
/// Interface for security audit services
/// </summary>
public interface ISecurityAuditService
{
    /// <summary>
    /// Logs a security event
    /// </summary>
    Task LogSecurityEventAsync(string eventType, string description, int? userId = null, string? ipAddress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security audit events for a time range
    /// </summary>
    Task<IEnumerable<AuditLog>> GetAuditEventsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security audit events for a specific user
    /// </summary>
    Task<IEnumerable<AuditLog>> GetUserAuditEventsAsync(int userId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed login attempts
    /// </summary>
    Task<IEnumerable<AuditLog>> GetFailedLoginAttemptsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks for suspicious activity patterns
    /// </summary>
    Task<Result<bool>> AnalyzeSecurityPatternsAsync(DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security events with simple filtering (backward compatibility)
    /// </summary>
    Task<List<SecurityAuditEvent>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null, CancellationToken cancellationToken = default);
}
