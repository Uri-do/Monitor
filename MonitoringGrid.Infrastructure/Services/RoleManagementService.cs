using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Common;
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

    public async Task<Result<Role>> CreateRoleAsync(string roleName, string description, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = new Role
            {
                RoleId = Guid.NewGuid().ToString(),
                Name = roleName,
                Description = description,
                IsSystemRole = false,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role {RoleName} created successfully with ID {RoleId}", roleName, role.RoleId);
            return Result<Role>.Success(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create role {RoleName}: {Message}", roleName, ex.Message);
            return Result.Failure<Role>(Error.Failure("Role.CreateFailed", $"Failed to create role: {ex.Message}"));
        }
    }

    public async Task<Role?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default)
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

    public async Task<IEnumerable<Role>> GetRolesAsync(CancellationToken cancellationToken = default)
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

    public async Task<Result<Role>> UpdateRoleAsync(string roleId, string roleName, string description, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);

            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found for update", roleId);
                return Result.Failure<Role>(Error.NotFound("Role", "Role not found"));
            }

            role.Name = roleName;
            role.Description = description;
            role.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role {RoleId} updated successfully", roleId);
            return Result<Role>.Success(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update role {RoleId}: {Message}", roleId, ex.Message);
            return Result.Failure<Role>(Error.Failure("Role.UpdateFailed", $"Failed to update role: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);

            if (role == null)
            {
                _logger.LogWarning("Role {RoleId} not found for deletion", roleId);
                return Result.Failure<bool>(Error.NotFound("Role", "Role not found"));
            }

            if (role.IsSystemRole)
            {
                _logger.LogWarning("Cannot delete system role {RoleId}", roleId);
                return Result.Failure<bool>(Error.Validation("Role.SystemRole", "Cannot delete system role"));
            }

            // Soft delete by setting IsActive to false
            role.IsActive = false;
            role.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role {RoleId} deleted successfully", roleId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete role {RoleId}: {Message}", roleId, ex.Message);
            return Result.Failure<bool>(Error.Failure("Role.DeleteFailed", $"Failed to delete role: {ex.Message}"));
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

    public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default)
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

    public async Task<IEnumerable<User>> GetRoleUsersAsync(string roleId, CancellationToken cancellationToken = default)
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

    public async Task<IEnumerable<Permission>> GetPermissionsAsync(CancellationToken cancellationToken = default)
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

    public async Task<Result<bool>> AssignRoleToUserAsync(string userId, string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var existingAssignment = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

            if (existingAssignment != null)
            {
                _logger.LogInformation("Role {RoleId} already assigned to user {UserId}", roleId, userId);
                return Result<bool>.Success(true);
            }

            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedDate = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role {RoleId} assigned to user {UserId}", roleId, userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign role {RoleId} to user {UserId}: {Message}", roleId, userId, ex.Message);
            return Result.Failure<bool>(Error.Failure("Role.AssignFailed", $"Failed to assign role: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> RemoveRoleFromUserAsync(string userId, string roleId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

            if (userRole == null)
            {
                _logger.LogWarning("Role {RoleId} not found for user {UserId}", roleId, userId);
                return Result.Failure<bool>(Error.NotFound("UserRole", "User role assignment not found"));
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role {RoleId} removed from user {UserId}", roleId, userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove role {RoleId} from user {UserId}: {Message}", roleId, userId, ex.Message);
            return Result.Failure<bool>(Error.Failure("Role.RemoveFailed", $"Failed to remove role: {ex.Message}"));
        }
    }

    public async Task<IEnumerable<Role>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get roles for user {UserId}: {Message}", userId, ex.Message);
            return new List<Role>();
        }
    }

    public async Task<bool> UserHasRoleAsync(string userId, string roleName, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user {UserId} has role {RoleName}: {Message}", userId, roleName, ex.Message);
            return false;
        }
    }
}
