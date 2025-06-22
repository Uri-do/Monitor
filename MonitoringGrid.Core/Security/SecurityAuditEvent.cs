using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Security;

/// <summary>
/// Security audit event entity for database storage
/// </summary>
public class SecurityAuditEvent
{
    /// <summary>
    /// Unique identifier for the audit event
    /// </summary>
    [Key]
    [Required]
    [StringLength(50)]
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// Type of security event
    /// </summary>
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// User ID associated with the event (if applicable)
    /// </summary>
    [StringLength(50)]
    public string? UserId { get; set; }

    /// <summary>
    /// Username associated with the event (if applicable)
    /// </summary>
    [StringLength(100)]
    public string? Username { get; set; }

    /// <summary>
    /// IP address where the event originated
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string from the request
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Resource that was accessed or attempted to be accessed
    /// </summary>
    [StringLength(200)]
    public string? Resource { get; set; }

    /// <summary>
    /// Action that was performed or attempted
    /// </summary>
    [StringLength(100)]
    public string? Action { get; set; }

    /// <summary>
    /// Whether the action was successful
    /// </summary>
    [Required]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if the action failed
    /// </summary>
    [StringLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional data related to the event (JSON format)
    /// </summary>
    [Required]
    public string AdditionalData { get; set; } = "{}";

    /// <summary>
    /// When the event occurred
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Severity level of the event
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Severity { get; set; } = "Information";

    /// <summary>
    /// Additional description for compatibility
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Helper property to get additional data as dictionary
    /// </summary>
    public Dictionary<string, object> GetAdditionalDataAsDictionary()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(AdditionalData) ?? new();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Helper method to set additional data from dictionary
    /// </summary>
    public void SetAdditionalDataFromDictionary(Dictionary<string, object> data)
    {
        AdditionalData = System.Text.Json.JsonSerializer.Serialize(data);
    }
}
