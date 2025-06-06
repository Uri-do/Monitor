using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

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
