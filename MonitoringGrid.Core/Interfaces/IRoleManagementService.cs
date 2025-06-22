using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Common;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for role and permission management services
/// </summary>
public interface IRoleManagementService
{
    /// <summary>
    /// Gets all roles
    /// </summary>
    Task<IEnumerable<Role>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by ID
    /// </summary>
    Task<Role?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new role
    /// </summary>
    Task<Result<Role>> CreateRoleAsync(string roleName, string description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role
    /// </summary>
    Task<Result<Role>> UpdateRoleAsync(string roleId, string roleName, string description, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role
    /// </summary>
    Task<Result<bool>> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user
    /// </summary>
    Task<Result<bool>> AssignRoleToUserAsync(string userId, string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    Task<Result<bool>> RemoveRoleFromUserAsync(string userId, string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roles for a user
    /// </summary>
    Task<IEnumerable<Role>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions
    /// </summary>
    Task<IEnumerable<Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default);
}
