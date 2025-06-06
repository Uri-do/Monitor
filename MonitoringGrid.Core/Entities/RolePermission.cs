using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Role-Permission junction table
/// </summary>
[Table("RolePermissions", Schema = "auth")]
public class RolePermission
{
    [Required]
    [StringLength(50)]
    public string RoleId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string PermissionId { get; set; } = string.Empty;

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? AssignedBy { get; set; }

    // Navigation properties
    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("PermissionId")]
    public virtual Permission Permission { get; set; } = null!;
}
