using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Alert acknowledgment entity
/// </summary>
[Table("AlertAcknowledgments")]
public class AlertAcknowledgment
{
    [Key]
    public int AlertAcknowledgmentId { get; set; }
    
    public int AlertId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string AcknowledgedBy { get; set; } = string.Empty;
    
    public DateTime AcknowledgedAt { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public bool StopEscalation { get; set; } = true;
    
    // Navigation properties
    [ForeignKey("AlertId")]
    public virtual AlertLog Alert { get; set; } = null!;
}
