using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// User-Role junction table
/// </summary>
[Table("UserRoles", Schema = "auth")]
public class UserRole
{
    [Required]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string RoleId { get; set; } = string.Empty;

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? AssignedBy { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;
}
