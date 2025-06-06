using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service for managing roles and permissions
/// </summary>
public class RoleManagementService : IRoleManagementService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<RoleManagementService> _logger;

    public RoleManagementService(
        MonitoringContext context,
        ILogger<RoleManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Role> CreateRoleAsync(string name, string description, List<string> permissions, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = new Role
            {
                RoleId = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                IsSystemRole = false,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role {RoleName} created successfully with ID {RoleId}", name, role.RoleId);
            return role;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create role {RoleName}: {Message}", name, ex.Message);
            throw;
        }
    }

    public async Task<Role?> GetRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get role {RoleId}: {Message}", roleId, ex.Message);
            throw;
        }
    }

    public async Task<List<Role>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get roles: {Message}", ex.Message);
            throw;
        }
    }

    public async Task<bool> UpdateRoleAsync(string roleId, string name, string description, List<string> permissions, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);

            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found for update", roleId);
                return false;
            }

            role.Name = name;
            role.Description = description;
            role.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role {RoleId} updated successfully", roleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update role {RoleId}: {Message}", roleId, ex.Message);
            throw;
        }
    }

    public async Task<bool> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);

            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found for deletion", roleId);
                return false;
            }

            if (role.IsSystemRole)
            {
                _logger.LogWarning("Cannot delete system role {RoleId}", roleId);
                return false;
            }

            // Soft delete by setting IsActive to false
            role.IsActive = false;
            role.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role {RoleId} deleted successfully", roleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete role {RoleId}: {Message}", roleId, ex.Message);
            throw;
        }
    }

    public async Task<bool> AssignPermissionAsync(string roleId, string permissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingAssignment = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

            if (existingAssignment != null)
            {
                _logger.LogInformation("Permission {PermissionId} already assigned to role {RoleId}", permissionId, roleId);
                return true;
            }

            var rolePermission = new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                AssignedDate = DateTime.UtcNow
            };

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Permission {PermissionId} assigned to role {RoleId}", permissionId, roleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign permission {PermissionId} to role {RoleId}: {Message}", permissionId, roleId, ex.Message);
            throw;
        }
    }

    public async Task<bool> RemovePermissionAsync(string roleId, string permissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId, cancellationToken);

            if (rolePermission == null)
            {
                _logger.LogWarning("Permission {PermissionId} not found for role {RoleId}", permissionId, roleId);
                return false;
            }

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Permission {PermissionId} removed from role {RoleId}", permissionId, roleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove permission {PermissionId} from role {RoleId}: {Message}", permissionId, roleId, ex.Message);
            throw;
        }
    }

    public async Task<List<Permission>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions for role {RoleId}: {Message}", roleId, ex.Message);
            throw;
        }
    }

    public async Task<List<User>> GetRoleUsersAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserRoles
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.User)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get users for role {RoleId}: {Message}", roleId, ex.Message);
            throw;
        }
    }

    public async Task<bool> RoleExistsAsync(string name, string? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Roles.Where(r => r.Name == name && r.IsActive);

            if (!string.IsNullOrEmpty(excludeRoleId))
            {
                query = query.Where(r => r.RoleId != excludeRoleId);
            }

            return await query.AnyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if role exists {RoleName}: {Message}", name, ex.Message);
            throw;
        }
    }

    public async Task<List<Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Resource)
                .ThenBy(p => p.Action)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions: {Message}", ex.Message);
            throw;
        }
    }
}
