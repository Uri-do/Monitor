using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Security;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for user management service
/// </summary>
public interface IUserService
{
    // User CRUD operations
    Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<User> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<User> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ActivateUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> DeactivateUserAsync(string userId, CancellationToken cancellationToken = default);

    // User validation
    Task<bool> IsUsernameAvailableAsync(string username, string? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null, CancellationToken cancellationToken = default);
    Task<bool> ValidateUserAsync(string userId, CancellationToken cancellationToken = default);

    // Role management
    Task<IEnumerable<MonitoringGrid.Core.Entities.Role>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> AssignRoleAsync(string userId, string roleId, string? assignedBy = null, CancellationToken cancellationToken = default);
    Task<bool> RemoveRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserRolesAsync(string userId, IEnumerable<string> roleIds, string? assignedBy = null, CancellationToken cancellationToken = default);

    // Permission management
    Task<IEnumerable<MonitoringGrid.Core.Entities.Permission>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(string userId, string resource, string action, CancellationToken cancellationToken = default);
    Task<bool> HasAnyPermissionAsync(string userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    // Password management
    Task<bool> SetPasswordAsync(string userId, string password, string? setBy = null, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(string userId, string newPassword, string? resetBy = null, CancellationToken cancellationToken = default);
    Task<bool> IsPasswordExpiredAsync(string userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MonitoringGrid.Core.Entities.UserPassword>> GetPasswordHistoryAsync(string userId, int count = 10, CancellationToken cancellationToken = default);

    // Security operations
    Task<bool> LockUserAsync(string userId, int lockoutDurationMinutes, string? reason = null, CancellationToken cancellationToken = default);
    Task<bool> UnlockUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserLockedAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> GetFailedLoginAttemptsAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IncrementFailedLoginAttemptsAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> ResetFailedLoginAttemptsAsync(string userId, CancellationToken cancellationToken = default);

    // Bulk operations
    Task<bool> BulkActivateUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    Task<bool> BulkDeactivateUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    Task<bool> BulkDeleteUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
    Task<bool> BulkAssignRoleAsync(IEnumerable<string> userIds, string roleId, string? assignedBy = null, CancellationToken cancellationToken = default);
    Task<bool> BulkRemoveRoleAsync(IEnumerable<string> userIds, string roleId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Create user request model
/// </summary>
public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public string Password { get; set; } = string.Empty;
    public IEnumerable<string> RoleIds { get; set; } = new List<string>();
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; } = false;
    public string? CreatedBy { get; set; }
}

/// <summary>
/// Update user request model
/// </summary>
public class UpdateUserRequest
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Department { get; set; }
    public string? Title { get; set; }
    public IEnumerable<string>? RoleIds { get; set; }
    public bool? IsActive { get; set; }
    public bool? EmailConfirmed { get; set; }
    public string? ModifiedBy { get; set; }
}
