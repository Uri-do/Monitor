using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Audit log entity for compliance and security tracking
/// </summary>
[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    public int LogId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string UserName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Resource { get; set; } = string.Empty;
    
    [StringLength(4000)]
    public string? Details { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    public DateTime Timestamp { get; set; }
    
    public bool IsSuccess { get; set; }
    
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Webhook configuration entity
/// </summary>
[Table("WebhookConfigurations")]
public class WebhookConfig
{
    [Key]
    public int WebhookId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string Url { get; set; } = string.Empty;
    
    [StringLength(10)]
    public string HttpMethod { get; set; } = "POST";
    
    [StringLength(4000)]
    public string? Headers { get; set; } // JSON serialized headers
    
    [StringLength(4000)]
    public string? PayloadTemplate { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public int TimeoutSeconds { get; set; } = 30;
    
    public int RetryCount { get; set; } = 3;
    
    [StringLength(1000)]
    public string? TriggerSeverities { get; set; } // JSON serialized list
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime ModifiedDate { get; set; }
    
    // Navigation properties
    public virtual ICollection<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();
}

/// <summary>
/// Webhook delivery log entity
/// </summary>
[Table("WebhookDeliveryLogs")]
public class WebhookDeliveryLog
{
    [Key]
    public int LogId { get; set; }
    
    public int WebhookId { get; set; }
    
    public DateTime DeliveryTime { get; set; }
    
    public int StatusCode { get; set; }
    
    [StringLength(4000)]
    public string? Response { get; set; }
    
    public bool IsSuccess { get; set; }
    
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
    
    public int RetryCount { get; set; }
    
    public double ResponseTimeMs { get; set; }
    
    // Navigation properties
    [ForeignKey("WebhookId")]
    public virtual WebhookConfig Webhook { get; set; } = null!;
}

/// <summary>
/// Report template entity
/// </summary>
[Table("ReportTemplates")]
public class ReportTemplate
{
    [Key]
    [StringLength(50)]
    public string TemplateId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string ReportType { get; set; } = string.Empty;
    
    [StringLength(4000)]
    public string Template { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? Parameters { get; set; } // JSON serialized parameters
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Report schedule entity
/// </summary>
[Table("ReportSchedules")]
public class ReportSchedule
{
    [Key]
    public int ScheduleId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string ReportType { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string CronExpression { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string Recipients { get; set; } = string.Empty; // JSON serialized list
    
    [StringLength(4000)]
    public string? Parameters { get; set; } // JSON serialized parameters
    
    public bool IsActive { get; set; } = true;
    
    public DateTime? LastRun { get; set; }
    
    public DateTime? NextRun { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Alert escalation entity
/// </summary>
[Table("AlertEscalations")]
public class AlertEscalation
{
    [Key]
    public int AlertEscalationId { get; set; }
    
    public int AlertId { get; set; }
    
    public int Level { get; set; }
    
    public DateTime ScheduledTime { get; set; }
    
    public DateTime? ExecutedTime { get; set; }
    
    public bool IsExecuted { get; set; }
    
    public bool IsCancelled { get; set; }
    
    [StringLength(1000)]
    public string? ExecutionResult { get; set; }
    
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
    
    // Navigation properties
    [ForeignKey("AlertId")]
    public virtual AlertLog Alert { get; set; } = null!;
}

/// <summary>
/// Alert acknowledgment entity
/// </summary>
[Table("AlertAcknowledgments")]
public class AlertAcknowledgment
{
    [Key]
    public int AlertAcknowledgmentId { get; set; }
    
    public int AlertId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string AcknowledgedBy { get; set; } = string.Empty;
    
    public DateTime AcknowledgedAt { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public bool StopEscalation { get; set; } = true;
    
    // Navigation properties
    [ForeignKey("AlertId")]
    public virtual AlertLog Alert { get; set; } = null!;
}

/// <summary>
/// Alert suppression rule entity
/// </summary>
[Table("AlertSuppressionRules")]
public class AlertSuppressionRule
{
    [Key]
    public int SuppressionRuleId { get; set; }
    
    public int? KpiId { get; set; } // null for global rules
    
    [StringLength(100)]
    public string? Owner { get; set; } // null for all owners
    
    public DateTime StartTime { get; set; }
    
    public DateTime EndTime { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    [ForeignKey("KpiId")]
    public virtual KPI? Kpi { get; set; }
}

/// <summary>
/// Notification preferences entity
/// </summary>
[Table("NotificationPreferences")]
public class NotificationPreferences
{
    [Key]
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string EnabledChannels { get; set; } = string.Empty; // JSON serialized list
    
    [StringLength(2000)]
    public string? ChannelSettings { get; set; } // JSON serialized settings
    
    public bool EnableQuietHours { get; set; }
    
    public TimeSpan QuietHoursStart { get; set; }
    
    public TimeSpan QuietHoursEnd { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Export job entity
/// </summary>
[Table("ExportJobs")]
public class ExportJob
{
    [Key]
    public int JobId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? UserId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    [StringLength(500)]
    public string? FilePath { get; set; }
    
    public long? FileSizeBytes { get; set; }
    
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }
    
    [StringLength(4000)]
    public string? Parameters { get; set; } // JSON serialized export parameters
}

/// <summary>
/// Backup info entity
/// </summary>
[Table("BackupInfo")]
public class BackupInfo
{
    [Key]
    [StringLength(50)]
    public string BackupId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public long FileSizeBytes { get; set; }
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(2000)]
    public string IncludedTables { get; set; } = string.Empty; // JSON serialized list
    
    [StringLength(500)]
    public string? FilePath { get; set; }
    
    [StringLength(100)]
    public string? CreatedBy { get; set; }
    
    public bool IsValid { get; set; } = true;
}
