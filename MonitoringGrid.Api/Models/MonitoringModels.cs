using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Models;



/// <summary>
/// Represents a contact for notifications
/// </summary>
[Table("Contacts", Schema = "monitoring")]
public class Contact
{
    [Key]
    public int ContactId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
}






/// <summary>
/// System configuration settings
/// </summary>
[Table("Config", Schema = "monitoring")]
public class Config
{
    [Key]
    [MaxLength(50)]
    public string ConfigKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string ConfigValue { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// System status tracking
/// </summary>
[Table("SystemStatus", Schema = "monitoring")]
public class SystemStatus
{
    [Key]
    public int StatusId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;

    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public int ProcessedKpis { get; set; } = 0;

    public int AlertsSent { get; set; } = 0;
}



/// <summary>
/// Result of alert sending operation
/// </summary>
public class AlertResult
{
    public int EmailsSent { get; set; }
    public int SmsSent { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public bool Success => Errors.Count == 0;
}
