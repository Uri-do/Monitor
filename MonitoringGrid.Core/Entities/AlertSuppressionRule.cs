using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Alert suppression rule entity
/// </summary>
[Table("AlertSuppressionRules")]
public class AlertSuppressionRule
{
    [Key]
    public int SuppressionRuleId { get; set; }
    
    public int? KpiId { get; set; } // null for global rules
    
    [StringLength(100)]
    public string? Owner { get; set; } // null for all owners
    
    public DateTime StartTime { get; set; }
    
    public DateTime EndTime { get; set; }
    
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    [ForeignKey("KpiId")]
    public virtual KPI? Kpi { get; set; }
}
