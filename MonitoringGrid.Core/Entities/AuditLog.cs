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
