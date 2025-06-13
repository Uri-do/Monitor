<!--#if (enableAuth)-->
using EnterpriseApp.Core.Security;
using EnterpriseApp.Core.Models;

namespace EnterpriseApp.Core.Interfaces;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(LoginRequest request, string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token
    /// </summary>
    Task<AuthenticationResult> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT token
    /// </summary>
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user from JWT token
    /// </summary>
    Task<User?> GetUserFromTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token
    /// </summary>
    Task<bool> RevokeTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes user password
    /// </summary>
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets user password
    /// </summary>
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out a user
    /// </summary>
    Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Authorization service interface
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Checks if a user has a specific permission
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific role
    /// </summary>
    Task<bool> HasRoleAsync(string userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a user
    /// </summary>
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles for a user
    /// </summary>
    Task<IEnumerable<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user
    /// </summary>
    Task<bool> AssignRoleAsync(string userId, string roleId, string assignedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    Task<bool> RemoveRoleAsync(string userId, string roleId, string removedBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// User management service interface
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Creates a new user
    /// </summary>
    Task<User> CreateUserAsync(CreateUserRequest request, string createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    Task<User> UpdateUserAsync(string userId, UpdateUserRequest request, string modifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user (soft delete)
    /// </summary>
    Task<bool> DeleteUserAsync(string userId, string deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username
    /// </summary>
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email
    /// </summary>
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with pagination
    /// </summary>
    Task<PagedResult<User>> GetUsersAsync(int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a user account
    /// </summary>
    Task<bool> ActivateUserAsync(string userId, string activatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a user account
    /// </summary>
    Task<bool> DeactivateUserAsync(string userId, string deactivatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks a user account
    /// </summary>
    Task<bool> LockUserAsync(string userId, TimeSpan lockoutDuration, string lockedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks a user account
    /// </summary>
    Task<bool> UnlockUserAsync(string userId, string unlockedBy, CancellationToken cancellationToken = default);
}

/// <summary>
/// Role management service interface
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Creates a new role
    /// </summary>
    Task<Role> CreateRoleAsync(CreateRoleRequest request, string createdBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role
    /// </summary>
    Task<Role> UpdateRoleAsync(string roleId, UpdateRoleRequest request, string modifiedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role
    /// </summary>
    Task<bool> DeleteRoleAsync(string roleId, string deletedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by ID
    /// </summary>
    Task<Role?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by name
    /// </summary>
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles
    /// </summary>
    Task<IEnumerable<Role>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a permission to a role
    /// </summary>
    Task<bool> AssignPermissionAsync(string roleId, string permissionId, string assignedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a permission from a role
    /// </summary>
    Task<bool> RemovePermissionAsync(string roleId, string permissionId, string removedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a role
    /// </summary>
    Task<IEnumerable<Permission>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Security event service interface
/// </summary>
public interface ISecurityEventService
{
    /// <summary>
    /// Logs a security event
    /// </summary>
    Task LogSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security events with filtering
    /// </summary>
    Task<IEnumerable<SecurityEvent>> GetSecurityEventsAsync(SecurityEventFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects suspicious activity
    /// </summary>
    Task<IEnumerable<SuspiciousActivity>> DetectSuspiciousActivityAsync(string userId, TimeSpan timeWindow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed login attempts
    /// </summary>
    Task<IEnumerable<FailedLoginAttempt>> GetFailedLoginAttemptsAsync(string? userId = null, string? ipAddress = null, TimeSpan? timeWindow = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Password service interface
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a password
    /// </summary>
    string HashPassword(string password, string? salt = null);

    /// <summary>
    /// Verifies a password against a hash
    /// </summary>
    bool VerifyPassword(string password, string hash, string? salt = null);

    /// <summary>
    /// Generates a random salt
    /// </summary>
    string GenerateSalt();

    /// <summary>
    /// Generates a secure random password
    /// </summary>
    string GeneratePassword(int length = 12, bool includeSpecialCharacters = true);

    /// <summary>
    /// Validates password strength
    /// </summary>
    PasswordValidationResult ValidatePassword(string password);

    /// <summary>
    /// Checks if password has been used before
    /// </summary>
    Task<bool> IsPasswordReusedAsync(string userId, string password, CancellationToken cancellationToken = default);
}
<!--#endif-->
