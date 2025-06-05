namespace MonitoringGrid.Core.Models;

/// <summary>
/// Slack message model
/// </summary>
public class SlackMessage
{
    public string Channel { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? IconEmoji { get; set; }
    public List<SlackAttachment> Attachments { get; set; } = new();
    public List<SlackBlock> Blocks { get; set; } = new();
}

/// <summary>
/// Slack attachment model
/// </summary>
public class SlackAttachment
{
    public string? Color { get; set; }
    public string? Title { get; set; }
    public string? Text { get; set; }
    public List<SlackField> Fields { get; set; } = new();
    public string? Footer { get; set; }
    public long? Timestamp { get; set; }
}

/// <summary>
/// Slack field model
/// </summary>
public class SlackField
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Short { get; set; } = true;
}

/// <summary>
/// Slack block model
/// </summary>
public class SlackBlock
{
    public string Type { get; set; } = string.Empty;
    public object? Text { get; set; }
    public List<object>? Elements { get; set; }
}

/// <summary>
/// Teams adaptive card model
/// </summary>
public class TeamsAdaptiveCard
{
    public string Type { get; set; } = "AdaptiveCard";
    public string Version { get; set; } = "1.4";
    public List<object> Body { get; set; } = new();
    public List<object>? Actions { get; set; }
}

/// <summary>
/// Webhook delivery log
/// </summary>
public class WebhookDeliveryLog
{
    public int LogId { get; set; }
    public int WebhookId { get; set; }
    public DateTime DeliveryTime { get; set; }
    public int StatusCode { get; set; }
    public string? Response { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public double ResponseTimeMs { get; set; }
}

/// <summary>
/// Webhook test result
/// </summary>
public class WebhookTestResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? Response { get; set; }
    public string? ErrorMessage { get; set; }
    public double ResponseTimeMs { get; set; }
    public DateTime TestTime { get; set; }
}

/// <summary>
/// LDAP user model
/// </summary>
public class LdapUser
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string? Phone { get; set; }
    public List<string> Groups { get; set; } = new();
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>
/// LDAP group model
/// </summary>
public class LdapGroup
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Members { get; set; } = new();
}

/// <summary>
/// SSO authentication result
/// </summary>
public class SsoAuthResult
{
    public bool IsSuccess { get; set; }
    public SsoUser? User { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// SSO user model
/// </summary>
public class SsoUser
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, object> Claims { get; set; } = new();
}

/// <summary>
/// Audit log entry
/// </summary>
public class AuditLogEntry
{
    public int LogId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Audit log filter
/// </summary>
public class AuditLogFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public string? Resource { get; set; }
    public bool? IsSuccess { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Report request models
/// </summary>
public class KpiReportRequest
{
    public List<int>? KpiIds { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Format { get; set; } = "PDF"; // PDF, Excel, CSV
    public bool IncludeCharts { get; set; } = true;
    public bool IncludeTrends { get; set; } = true;
    public string? TemplateId { get; set; }
}

public class AlertReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<int>? KpiIds { get; set; }
    public List<string>? Severities { get; set; }
    public bool? IsResolved { get; set; }
    public string Format { get; set; } = "PDF";
    public bool IncludeStatistics { get; set; } = true;
}

public class PerformanceReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string ReportType { get; set; } = "Summary"; // Summary, Detailed, Trends
    public string Format { get; set; } = "PDF";
    public bool IncludeBenchmarks { get; set; } = true;
}

public class CustomReportRequest
{
    public string ReportName { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string Format { get; set; } = "PDF";
    public string? TemplateId { get; set; }
}

/// <summary>
/// Report template
/// </summary>
public class ReportTemplate
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public List<ReportParameter> Parameters { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Report parameter
/// </summary>
public class ReportParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public object? DefaultValue { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Report schedule
/// </summary>
public class ReportSchedule
{
    public int ScheduleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public List<string> Recipients { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
}

public class ReportScheduleRequest
{
    public string Name { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public List<string> Recipients { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Notification models
/// </summary>
public class NotificationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public List<NotificationChannel> PreferredChannels { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
}

public class NotificationResult
{
    public bool IsSuccess { get; set; }
    public List<string> DeliveredChannels { get; set; } = new();
    public List<string> FailedChannels { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class NotificationPreferences
{
    public string UserId { get; set; } = string.Empty;
    public List<NotificationChannel> EnabledChannels { get; set; } = new();
    public Dictionary<string, object> ChannelSettings { get; set; } = new();
    public bool EnableQuietHours { get; set; }
    public TimeSpan QuietHoursStart { get; set; }
    public TimeSpan QuietHoursEnd { get; set; }
}

public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

/// <summary>
/// Data export models
/// </summary>
public class DataExportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Format { get; set; } = "CSV"; // CSV, Excel, JSON, XML
    public List<string>? Columns { get; set; }
    public Dictionary<string, object> Filters { get; set; } = new();
    public bool IncludeHeaders { get; set; } = true;
}

public class ScheduledExportRequest
{
    public string Name { get; set; } = string.Empty;
    public string ExportType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public DataExportRequest ExportRequest { get; set; } = new();
    public List<string> Recipients { get; set; } = new();
}

public class ExportJob
{
    public int JobId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Backup and restore models
/// </summary>
public class BackupRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string> IncludedTables { get; set; } = new();
    public bool IncludeConfiguration { get; set; } = true;
    public bool IncludeHistoricalData { get; set; } = true;
    public string? Description { get; set; }
}

public class BackupResult
{
    public string BackupId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? FilePath { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RestoreRequest
{
    public string BackupId { get; set; } = string.Empty;
    public bool RestoreConfiguration { get; set; } = true;
    public bool RestoreHistoricalData { get; set; } = true;
    public List<string>? SpecificTables { get; set; }
}

public class RestoreResult
{
    public bool IsSuccess { get; set; }
    public DateTime RestoredAt { get; set; }
    public List<string> RestoredTables { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class BackupInfo
{
    public string BackupId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public List<string> IncludedTables { get; set; } = new();
}

public class BackupValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
}
