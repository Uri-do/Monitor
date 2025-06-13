using EnterpriseApp.Core.Enums;

namespace EnterpriseApp.Api.DTOs;

/// <summary>
/// Data transfer object for AuditLog
/// </summary>
public class AuditLogDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the entity that was changed
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity that was changed
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Action that was performed
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Description of the action
    /// </summary>
    public string? ActionDescription { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username of the user who performed the action
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// IP address from which the action was performed
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the action was performed
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Old values before the change
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values after the change
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Additional metadata about the change
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Correlation ID for tracking related operations
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Session ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Application or module that made the change
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Severity level of the audit event
    /// </summary>
    public string Severity { get; set; } = "Information";

    /// <summary>
    /// Computed properties
    /// </summary>
    public AuditLogComputedDto Computed { get; set; } = new();
}

/// <summary>
/// Computed properties for AuditLog
/// </summary>
public class AuditLogComputedDto
{
    /// <summary>
    /// Action display name
    /// </summary>
    public string ActionDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Severity display name with color
    /// </summary>
    public string SeverityDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Time ago display (e.g., "2 hours ago")
    /// </summary>
    public string TimeAgo { get; set; } = string.Empty;

    /// <summary>
    /// Formatted timestamp
    /// </summary>
    public string FormattedTimestamp { get; set; } = string.Empty;

    /// <summary>
    /// Parsed old values as dictionary
    /// </summary>
    public Dictionary<string, object>? OldValuesDict { get; set; }

    /// <summary>
    /// Parsed new values as dictionary
    /// </summary>
    public Dictionary<string, object>? NewValuesDict { get; set; }

    /// <summary>
    /// Changes summary
    /// </summary>
    public List<ChangeDto> Changes { get; set; } = new();

    /// <summary>
    /// Indicates if this is a security-related event
    /// </summary>
    public bool IsSecurityEvent { get; set; }

    /// <summary>
    /// Risk level of the action
    /// </summary>
    public RiskLevel RiskLevel { get; set; }
}

/// <summary>
/// Individual change in an audit log
/// </summary>
public class ChangeDto
{
    /// <summary>
    /// Property name that changed
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Old value
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// New value
    /// </summary>
    public object? NewValue { get; set; }

    /// <summary>
    /// Display name for the property
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Type of change
    /// </summary>
    public ChangeType ChangeType { get; set; }
}

/// <summary>
/// Type of change in audit log
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// Value was added
    /// </summary>
    Added = 0,

    /// <summary>
    /// Value was modified
    /// </summary>
    Modified = 1,

    /// <summary>
    /// Value was removed
    /// </summary>
    Removed = 2,

    /// <summary>
    /// No change
    /// </summary>
    NoChange = 3
}

/// <summary>
/// Risk level of an audit event
/// </summary>
public enum RiskLevel
{
    /// <summary>
    /// Low risk
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium risk
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High risk
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical risk
    /// </summary>
    Critical = 3
}

/// <summary>
/// Audit statistics DTO
/// </summary>
public class AuditStatisticsDto
{
    /// <summary>
    /// Total number of audit logs
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Count by action type
    /// </summary>
    public Dictionary<AuditAction, int> ActionCounts { get; set; } = new();

    /// <summary>
    /// Count by entity type
    /// </summary>
    public Dictionary<string, int> EntityCounts { get; set; } = new();

    /// <summary>
    /// Top user activity
    /// </summary>
    public Dictionary<string, int> TopUserActivity { get; set; } = new();

    /// <summary>
    /// Count by severity level
    /// </summary>
    public Dictionary<string, int> SeverityCounts { get; set; } = new();

    /// <summary>
    /// Daily activity counts
    /// </summary>
    public Dictionary<DateTime, int> DailyActivity { get; set; } = new();

    /// <summary>
    /// Hourly activity pattern
    /// </summary>
    public Dictionary<int, int> HourlyActivity { get; set; } = new();

    /// <summary>
    /// Security events count
    /// </summary>
    public int SecurityEventsCount { get; set; }

    /// <summary>
    /// High-risk events count
    /// </summary>
    public int HighRiskEventsCount { get; set; }

    /// <summary>
    /// Date range for the statistics
    /// </summary>
    public DateRangeDto DateRange { get; set; } = new();

    /// <summary>
    /// Most active users
    /// </summary>
    public List<UserActivityDto> MostActiveUsers { get; set; } = new();

    /// <summary>
    /// Most audited entities
    /// </summary>
    public List<EntityActivityDto> MostAuditedEntities { get; set; } = new();
}

/// <summary>
/// Date range DTO
/// </summary>
public class DateRangeDto
{
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Number of days in the range
    /// </summary>
    public int Days { get; set; }
}

/// <summary>
/// User activity DTO
/// </summary>
public class UserActivityDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Activity count
    /// </summary>
    public int ActivityCount { get; set; }

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// Activity breakdown by action
    /// </summary>
    public Dictionary<AuditAction, int> ActionBreakdown { get; set; } = new();
}

/// <summary>
/// Entity activity DTO
/// </summary>
public class EntityActivityDto
{
    /// <summary>
    /// Entity name
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Entity ID
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Activity count
    /// </summary>
    public int ActivityCount { get; set; }

    /// <summary>
    /// Last activity timestamp
    /// </summary>
    public DateTime LastActivity { get; set; }

    /// <summary>
    /// Activity breakdown by action
    /// </summary>
    public Dictionary<AuditAction, int> ActionBreakdown { get; set; } = new();
}
