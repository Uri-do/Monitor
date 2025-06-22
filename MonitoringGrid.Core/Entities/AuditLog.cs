using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Audit log entity for compliance and security tracking.
/// Records all significant user actions and system events for security monitoring,
/// compliance reporting, and forensic analysis.
/// </summary>
[Table("AuditLogs")]
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry.
    /// </summary>
    [Key]
    public int LogId { get; set; }

    // Alias for compatibility with services that expect AuditLogId
    public int AuditLogId => LogId;

    /// <summary>
    /// ID of the user who performed the action. Required for accountability.
    /// </summary>
    [Required(ErrorMessage = "User ID is required for audit trail")]
    [StringLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the user for easier identification in audit reports.
    /// </summary>
    [StringLength(200)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// The action that was performed (e.g., CREATE, UPDATE, DELETE, LOGIN).
    /// </summary>
    [Required(ErrorMessage = "Action is required for audit trail")]
    [StringLength(100, ErrorMessage = "Action cannot exceed 100 characters")]
    [MinLength(1, ErrorMessage = "Action cannot be empty")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// The resource or entity that was affected by the action.
    /// </summary>
    [Required(ErrorMessage = "Resource is required for audit trail")]
    [StringLength(200, ErrorMessage = "Resource cannot exceed 200 characters")]
    [MinLength(1, ErrorMessage = "Resource cannot be empty")]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Additional details about the action in JSON format.
    /// May include before/after values, parameters, etc.
    /// </summary>
    [StringLength(4000)]
    public string? Details { get; set; }

    /// <summary>
    /// IP address from which the action was performed.
    /// Supports both IPv4 and IPv6 addresses.
    /// </summary>
    [StringLength(45)] // IPv6 max length is 45 characters
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string from the client browser or application.
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the action occurred. Always stored in UTC.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the action completed successfully.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the action failed.
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    // Alias for compatibility with services that expect Description
    public string Description => GetDescription();

    // Domain methods
    /// <summary>
    /// Gets a human-readable description of the audit event.
    /// </summary>
    /// <returns>Formatted description of the audit event.</returns>
    public string GetDescription()
    {
        var status = IsSuccess ? "successfully" : "failed to";
        return $"{UserName} {status} {Action.ToLower()} {Resource}";
    }

    /// <summary>
    /// Determines the severity level of the audit event.
    /// </summary>
    /// <returns>Severity level as string.</returns>
    public string GetSeverity()
    {
        if (!IsSuccess)
            return "Error";

        return Action.ToUpper() switch
        {
            "DELETE" => "High",
            "CREATE" or "UPDATE" => "Medium",
            "LOGIN" or "LOGOUT" => "Low",
            _ => "Info"
        };
    }

    /// <summary>
    /// Checks if this audit event should trigger security alerts.
    /// </summary>
    /// <returns>True if the event is security-sensitive.</returns>
    public bool IsSecuritySensitive()
    {
        var sensitiveActions = new[] { "DELETE", "LOGIN_FAILED", "PASSWORD_CHANGE", "ROLE_CHANGE" };
        return sensitiveActions.Contains(Action.ToUpper()) || !IsSuccess;
    }
}
