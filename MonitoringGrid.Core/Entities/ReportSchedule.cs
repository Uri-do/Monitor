using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

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
