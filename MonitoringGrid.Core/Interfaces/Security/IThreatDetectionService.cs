using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Core.Interfaces.Security;

/// <summary>
/// Interface for threat detection services
/// </summary>
public interface IThreatDetectionService
{
    /// <summary>
    /// Analyzes login attempts for suspicious patterns
    /// </summary>
    Task<Result<bool>> AnalyzeLoginPatternAsync(string username, string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects brute force attacks
    /// </summary>
    Task<Result<bool>> DetectBruteForceAttackAsync(string ipAddress, DateTime timeWindow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes API usage patterns for anomalies
    /// </summary>
    Task<Result<bool>> AnalyzeApiUsageAsync(int userId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active security threats
    /// </summary>
    Task<IEnumerable<AuditLog>> GetActiveThreatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reports a security threat
    /// </summary>
    Task<Result<int>> ReportThreatAsync(string threatType, string description, string source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a security threat
    /// </summary>
    Task<Result<bool>> ResolveThreatAsync(int threatId, string resolution, CancellationToken cancellationToken = default);
}
