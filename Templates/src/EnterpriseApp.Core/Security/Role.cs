<!--#if (enableAuth)-->
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseApp.Core.Security;

/// <summary>
/// Role entity for authorization
/// </summary>
[Table("Roles", Schema = "auth")]
public class Role
{
    /// <summary>
    /// Unique identifier for the role
    /// </summary>
    [Key]
    [StringLength(50)]
    public string RoleId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the role
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the role
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is a system role (cannot be deleted)
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// Indicates if the role is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the role was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the role was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created the role
    /// </summary>
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Who last modified the role
    /// </summary>
    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    // Navigation properties
    /// <summary>
    /// Users assigned to this role
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Permissions assigned to this role
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    // Business logic methods
    /// <summary>
    /// Checks if the role can be deleted
    /// </summary>
    public bool CanDelete()
    {
        return !IsSystemRole && UserRoles.Count == 0;
    }

    /// <summary>
    /// Gets all permission names for this role
    /// </summary>
    public List<string> GetPermissionNames()
    {
        return RolePermissions.Select(rp => rp.Permission.Name).ToList();
    }

    /// <summary>
    /// Checks if the role has a specific permission
    /// </summary>
    public bool HasPermission(string permissionName)
    {
        return RolePermissions.Any(rp => rp.Permission.Name.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Junction table for User-Role many-to-many relationship
/// </summary>
[Table("UserRoles", Schema = "auth")]
public class UserRole
{
    /// <summary>
    /// User ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Role ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// When the role was assigned
    /// </summary>
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who assigned the role
    /// </summary>
    [StringLength(100)]
    public string? AssignedBy { get; set; }

    // Navigation properties
    /// <summary>
    /// User reference
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Role reference
    /// </summary>
    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;
}

/// <summary>
/// Permission entity
/// </summary>
[Table("Permissions", Schema = "auth")]
public class Permission
{
    /// <summary>
    /// Unique identifier for the permission
    /// </summary>
    [Key]
    [StringLength(50)]
    public string PermissionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the permission
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the permission
    /// </summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Resource this permission applies to
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Action this permission allows
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is a system permission
    /// </summary>
    public bool IsSystemPermission { get; set; } = false;

    /// <summary>
    /// Indicates if the permission is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the permission was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the permission was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// Roles that have this permission
    /// </summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Junction table for Role-Permission many-to-many relationship
/// </summary>
[Table("RolePermissions", Schema = "auth")]
public class RolePermission
{
    /// <summary>
    /// Role ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string RoleId { get; set; } = string.Empty;

    /// <summary>
    /// Permission ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string PermissionId { get; set; } = string.Empty;

    /// <summary>
    /// When the permission was assigned
    /// </summary>
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who assigned the permission
    /// </summary>
    [StringLength(100)]
    public string? AssignedBy { get; set; }

    // Navigation properties
    /// <summary>
    /// Role reference
    /// </summary>
    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;

    /// <summary>
    /// Permission reference
    /// </summary>
    [ForeignKey(nameof(PermissionId))]
    public virtual Permission Permission { get; set; } = null!;
}
<!--#endif-->
