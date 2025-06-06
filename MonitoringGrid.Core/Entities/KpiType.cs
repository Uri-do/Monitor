using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents a KPI type definition with metadata and requirements
/// </summary>
public class KpiType
{
    [Key]
    [MaxLength(50)]
    public string KpiTypeId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of required field names for this KPI type
    /// </summary>
    [Required]
    public string RequiredFields { get; set; } = string.Empty;

    /// <summary>
    /// Default stored procedure for this KPI type
    /// </summary>
    [MaxLength(255)]
    public string? DefaultStoredProcedure { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<KPI> KPIs { get; set; } = new List<KPI>();
}
