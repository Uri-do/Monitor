namespace MonitoringGrid.Core.Models;

/// <summary>
/// Filter criteria for security event queries
/// </summary>
public class SecurityEventFilter
{
    /// <summary>
    /// Start date for filtering events
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for filtering events
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by event type (e.g., LOGIN_SUCCESS, LOGIN_FAILED, etc.)
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Filter by user ID
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Filter by IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Filter by success status
    /// </summary>
    public bool? IsSuccess { get; set; }

    /// <summary>
    /// Filter by severity level
    /// </summary>
    public string? Severity { get; set; }

    /// <summary>
    /// Filter by resource accessed
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// Filter by action performed
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int? MaxResults { get; set; } = 1000;

    /// <summary>
    /// Page number for pagination
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Sort field
    /// </summary>
    public string SortBy { get; set; } = "Timestamp";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Security health status information
/// </summary>
public class SecurityHealthStatus
{
    /// <summary>
    /// Overall security health status
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Time when the health check was performed
    /// </summary>
    public DateTime CheckTime { get; set; }

    /// <summary>
    /// Number of active security threats
    /// </summary>
    public int ActiveThreatsCount { get; set; }

    /// <summary>
    /// Number of failed login attempts in the last hour
    /// </summary>
    public int RecentFailedLogins { get; set; }

    /// <summary>
    /// Number of suspicious activities detected
    /// </summary>
    public int SuspiciousActivities { get; set; }

    /// <summary>
    /// List of security issues or concerns
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// Security recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Security metrics summary
    /// </summary>
    public SecurityMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Overall security score (0-100)
    /// </summary>
    public int SecurityScore { get; set; }

    /// <summary>
    /// Security level classification
    /// </summary>
    public SecurityLevel Level { get; set; } = SecurityLevel.Medium;
}

/// <summary>
/// Security metrics for health monitoring
/// </summary>
public class SecurityMetrics
{
    /// <summary>
    /// Total security events in the last 24 hours
    /// </summary>
    public int TotalEvents24h { get; set; }

    /// <summary>
    /// Successful authentication rate (percentage)
    /// </summary>
    public decimal AuthenticationSuccessRate { get; set; }

    /// <summary>
    /// Number of unique users active in the last 24 hours
    /// </summary>
    public int ActiveUsers24h { get; set; }

    /// <summary>
    /// Number of unique IP addresses in the last 24 hours
    /// </summary>
    public int UniqueIpAddresses24h { get; set; }

    /// <summary>
    /// Average time between security events (in minutes)
    /// </summary>
    public double AverageEventInterval { get; set; }

    /// <summary>
    /// Number of blocked IP addresses
    /// </summary>
    public int BlockedIpAddresses { get; set; }

    /// <summary>
    /// Number of users with 2FA enabled
    /// </summary>
    public int TwoFactorEnabledUsers { get; set; }

    /// <summary>
    /// Percentage of users with 2FA enabled
    /// </summary>
    public decimal TwoFactorAdoptionRate { get; set; }
}

/// <summary>
/// Security level classification
/// </summary>
public enum SecurityLevel
{
    /// <summary>
    /// Critical security issues detected
    /// </summary>
    Critical = 0,

    /// <summary>
    /// High security risk
    /// </summary>
    High = 1,

    /// <summary>
    /// Medium security risk (normal)
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Low security risk
    /// </summary>
    Low = 3,

    /// <summary>
    /// Optimal security status
    /// </summary>
    Optimal = 4
}

/// <summary>
/// Security threat summary for dashboard
/// </summary>
public class SecurityThreatSummary
{
    /// <summary>
    /// Total number of active threats
    /// </summary>
    public int TotalActiveThreats { get; set; }

    /// <summary>
    /// Number of critical threats
    /// </summary>
    public int CriticalThreats { get; set; }

    /// <summary>
    /// Number of high severity threats
    /// </summary>
    public int HighSeverityThreats { get; set; }

    /// <summary>
    /// Number of medium severity threats
    /// </summary>
    public int MediumSeverityThreats { get; set; }

    /// <summary>
    /// Number of low severity threats
    /// </summary>
    public int LowSeverityThreats { get; set; }

    /// <summary>
    /// Most recent threat detection time
    /// </summary>
    public DateTime? LastThreatDetected { get; set; }

    /// <summary>
    /// Most common threat type
    /// </summary>
    public string? MostCommonThreatType { get; set; }

    /// <summary>
    /// Threat detection trend (increasing/decreasing)
    /// </summary>
    public string ThreatTrend { get; set; } = "stable";
}

/// <summary>
/// Security audit summary for reporting
/// </summary>
public class SecurityAuditSummary
{
    /// <summary>
    /// Report period
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Total number of security events
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Number of successful events
    /// </summary>
    public int SuccessfulEvents { get; set; }

    /// <summary>
    /// Number of failed events
    /// </summary>
    public int FailedEvents { get; set; }

    /// <summary>
    /// Number of unique users
    /// </summary>
    public int UniqueUsers { get; set; }

    /// <summary>
    /// Number of unique IP addresses
    /// </summary>
    public int UniqueIpAddresses { get; set; }

    /// <summary>
    /// Events grouped by type
    /// </summary>
    public Dictionary<string, int> EventsByType { get; set; } = new();

    /// <summary>
    /// Events grouped by hour of day
    /// </summary>
    public Dictionary<int, int> EventsByHour { get; set; } = new();

    /// <summary>
    /// Top IP addresses by event count
    /// </summary>
    public Dictionary<string, int> TopIpAddresses { get; set; } = new();

    /// <summary>
    /// Top users by event count
    /// </summary>
    public Dictionary<string, int> TopUsers { get; set; } = new();
}
