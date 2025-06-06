using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

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
