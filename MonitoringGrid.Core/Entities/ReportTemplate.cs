using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Report template entity
/// </summary>
[Table("ReportTemplates")]
public class ReportTemplate
{
    [Key]
    [StringLength(50)]
    public string TemplateId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string ReportType { get; set; } = string.Empty;
    
    [StringLength(4000)]
    public string Template { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? Parameters { get; set; } // JSON serialized parameters
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedDate { get; set; }
    
    public DateTime ModifiedDate { get; set; }
}
