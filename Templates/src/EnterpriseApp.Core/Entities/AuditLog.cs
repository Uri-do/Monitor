using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EnterpriseApp.Core.Enums;

namespace EnterpriseApp.Core.Entities;

/// <summary>
/// Represents an audit log entry for tracking changes
/// </summary>
[Table("AuditLogs")]
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Name of the entity that was changed
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// ID of the entity that was changed
    /// </summary>
    [Required]
    [StringLength(50)]
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Action that was performed
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// Description of the action
    /// </summary>
    [StringLength(500)]
    public string? ActionDescription { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    [Required]
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username of the user who performed the action
    /// </summary>
    [StringLength(100)]
    public string? Username { get; set; }

    /// <summary>
    /// IP address from which the action was performed
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the client
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the action was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Old values before the change (JSON format)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// New values after the change (JSON format)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Additional metadata about the change
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Correlation ID for tracking related operations
    /// </summary>
    [StringLength(100)]
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Session ID
    /// </summary>
    [StringLength(100)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Application or module that made the change
    /// </summary>
    [StringLength(100)]
    public string? Source { get; set; }

    /// <summary>
    /// Severity level of the audit event
    /// </summary>
    [StringLength(20)]
    public string Severity { get; set; } = "Information";

    // Navigation properties (if linking to specific entities)
    /// <summary>
    /// Foreign key to DomainEntity (if applicable)
    /// </summary>
    public int? DomainEntityId { get; set; }

    /// <summary>
    /// Related DomainEntity (if applicable)
    /// </summary>
    [ForeignKey(nameof(DomainEntityId))]
    public virtual DomainEntity? DomainEntity { get; set; }

    // Business logic methods
    /// <summary>
    /// Creates an audit log entry for entity creation
    /// </summary>
    public static AuditLog ForCreation(string entityName, string entityId, string userId, string? username = null, string? ipAddress = null)
    {
        return new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = AuditAction.Created,
            ActionDescription = $"{entityName} created",
            UserId = userId,
            Username = username,
            IpAddress = ipAddress,
            Severity = "Information"
        };
    }

    /// <summary>
    /// Creates an audit log entry for entity update
    /// </summary>
    public static AuditLog ForUpdate(string entityName, string entityId, string userId, string? oldValues = null, string? newValues = null, string? username = null, string? ipAddress = null)
    {
        return new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = AuditAction.Updated,
            ActionDescription = $"{entityName} updated",
            UserId = userId,
            Username = username,
            IpAddress = ipAddress,
            OldValues = oldValues,
            NewValues = newValues,
            Severity = "Information"
        };
    }

    /// <summary>
    /// Creates an audit log entry for entity deletion
    /// </summary>
    public static AuditLog ForDeletion(string entityName, string entityId, string userId, string? username = null, string? ipAddress = null)
    {
        return new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = AuditAction.Deleted,
            ActionDescription = $"{entityName} deleted",
            UserId = userId,
            Username = username,
            IpAddress = ipAddress,
            Severity = "Warning"
        };
    }

    /// <summary>
    /// Creates a custom audit log entry
    /// </summary>
    public static AuditLog ForCustomAction(string entityName, string entityId, string actionDescription, string userId, string? username = null, string? ipAddress = null, string severity = "Information")
    {
        return new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = AuditAction.Custom,
            ActionDescription = actionDescription,
            UserId = userId,
            Username = username,
            IpAddress = ipAddress,
            Severity = severity
        };
    }

    /// <summary>
    /// Sets correlation ID for tracking related operations
    /// </summary>
    public void SetCorrelationId(string correlationId)
    {
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Sets additional metadata
    /// </summary>
    public void SetMetadata(string metadata)
    {
        Metadata = metadata;
    }
}
