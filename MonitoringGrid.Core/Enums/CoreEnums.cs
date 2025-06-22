namespace MonitoringGrid.Core.Enums;

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Emergency = 5
}

/// <summary>
/// Notification channels
/// </summary>
public enum NotificationChannel
{
    Email = 1,
    Sms = 2,
    Push = 3,
    Webhook = 4,
    Slack = 5,
    Teams = 6
}

/// <summary>
/// Notification priority levels
/// </summary>
public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

/// <summary>
/// Indicator execution status
/// </summary>
public enum IndicatorExecutionStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

/// <summary>
/// System status values
/// </summary>
public enum SystemStatusType
{
    Unknown = 0,
    Starting = 1,
    Running = 2,
    Stopping = 3,
    Stopped = 4,
    Error = 5
}

/// <summary>
/// Report formats
/// </summary>
public enum ReportFormat
{
    Pdf = 1,
    Excel = 2,
    Csv = 3,
    Json = 4,
    Xml = 5
}

/// <summary>
/// Export formats
/// </summary>
public enum ExportFormat
{
    Csv = 1,
    Excel = 2,
    Json = 3,
    Xml = 4
}

/// <summary>
/// Trend directions for analytics
/// </summary>
public enum TrendDirection
{
    Unknown = 0,
    Increasing = 1,
    Decreasing = 2,
    Stable = 3,
    Volatile = 4
}

/// <summary>
/// Correlation strength levels
/// </summary>
public enum CorrelationStrength
{
    None = 0,
    Weak = 1,
    Moderate = 2,
    Strong = 3,
    VeryStrong = 4
}

/// <summary>
/// Seasonality types
/// </summary>
public enum SeasonalityType
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4,
    Yearly = 5
}

/// <summary>
/// Anomaly types
/// </summary>
public enum AnomalyType
{
    Outlier = 1,
    Spike = 2,
    Drop = 3,
    Trend = 4,
    Seasonal = 5
}

/// <summary>
/// Time series decomposition methods
/// </summary>
public enum DecompositionMethod
{
    Additive = 1,
    Multiplicative = 2,
    STL = 3,
    X13 = 4
}

/// <summary>
/// User account status
/// </summary>
public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Locked = 3,
    Suspended = 4,
    PendingActivation = 5
}

/// <summary>
/// Permission types
/// </summary>
public enum PermissionType
{
    Read = 1,
    Write = 2,
    Delete = 3,
    Admin = 4,
    Execute = 5
}

/// <summary>
/// Audit action types
/// </summary>
public enum AuditActionType
{
    Create = 1,
    Read = 2,
    Update = 3,
    Delete = 4,
    Login = 5,
    Logout = 6,
    Execute = 7,
    Export = 8,
    Import = 9,
    Configure = 10
}
