using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Security;

/// <summary>
/// Security threat entity for database storage
/// </summary>
public class SecurityThreat
{
    /// <summary>
    /// Unique identifier for the threat
    /// </summary>
    [Key]
    [Required]
    [StringLength(50)]
    public string ThreatId { get; set; } = string.Empty;

    /// <summary>
    /// Type of security threat
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ThreatType { get; set; } = string.Empty;

    /// <summary>
    /// Severity level of the threat
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Description of the threat
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// User ID associated with the threat (if applicable)
    /// </summary>
    [StringLength(50)]
    public string? UserId { get; set; }

    /// <summary>
    /// IP address where the threat originated
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// When the threat was detected
    /// </summary>
    [Required]
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the threat has been resolved
    /// </summary>
    [Required]
    public bool IsResolved { get; set; } = false;

    /// <summary>
    /// When the threat was resolved (if applicable)
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Resolution details
    /// </summary>
    [StringLength(1000)]
    public string? Resolution { get; set; }

    /// <summary>
    /// Additional threat data (JSON format)
    /// </summary>
    [Required]
    public string ThreatData { get; set; } = "{}";

    /// <summary>
    /// Helper property to get threat data as dictionary
    /// </summary>
    public Dictionary<string, object> GetThreatDataAsDictionary()
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(ThreatData) ?? new();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Helper method to set threat data from dictionary
    /// </summary>
    public void SetThreatDataFromDictionary(Dictionary<string, object> data)
    {
        ThreatData = System.Text.Json.JsonSerializer.Serialize(data);
    }

    /// <summary>
    /// Mark the threat as resolved
    /// </summary>
    public void Resolve(string resolution)
    {
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        Resolution = resolution;
    }

    /// <summary>
    /// Check if the threat is critical based on severity
    /// </summary>
    public bool IsCritical()
    {
        return Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
               Severity.Equals("High", StringComparison.OrdinalIgnoreCase);
    }
}
