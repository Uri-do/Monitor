using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

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
