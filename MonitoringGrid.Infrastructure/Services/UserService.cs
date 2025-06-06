using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// User management service implementation
/// </summary>
public class UserService : IUserService
{
    private readonly MonitoringContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly SecurityConfiguration _securityConfig;
    private readonly ILogger<UserService> _logger;

    public UserService(
        MonitoringContext context,
        IEncryptionService encryptionService,
        IOptions<SecurityConfiguration> securityConfig,
        ILogger<UserService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _securityConfig = securityConfig.Value;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    public async Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetUsersAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        // Validate username and email uniqueness
        if (!await IsUsernameAvailableAsync(request.Username, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException($"Username '{request.Username}' is already taken");
        }

        if (!await IsEmailAvailableAsync(request.Email, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException($"Email '{request.Email}' is already registered");
        }

        try
        {
            // Create user entity
            var user = new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = request.Username,
                Email = request.Email,
                DisplayName = request.DisplayName,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Department = request.Department,
                Title = request.Title,
                PasswordHash = _encryptionService.Hash(request.Password),
                IsActive = request.IsActive,
                EmailConfirmed = request.EmailConfirmed,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                CreatedBy = request.CreatedBy,
                ModifiedBy = request.CreatedBy
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            // Create password history entry
            var passwordHistory = new MonitoringGrid.Core.Entities.UserPassword
            {
                UserId = user.UserId,
                PasswordHash = user.PasswordHash,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedBy = request.CreatedBy
            };

            _context.UserPasswords.Add(passwordHistory);

            // Assign roles
            if (request.RoleIds.Any())
            {
                await AssignRolesInternalAsync(user.UserId, request.RoleIds, request.CreatedBy, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {Username} created successfully with ID {UserId}", request.Username, user.UserId);

            // Return user with roles
            return await GetUserByIdAsync(user.UserId, cancellationToken) ?? user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user {Username}: {Message}", request.Username, ex.Message);
            throw;
        }
    }

    public async Task<User> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{userId}' not found");
        }

        // Validate email uniqueness if changed
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            if (!await IsEmailAvailableAsync(request.Email, userId, cancellationToken))
            {
                throw new InvalidOperationException($"Email '{request.Email}' is already registered");
            }
        }

        // Use execution strategy to handle the transaction properly
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Update user properties
                if (!string.IsNullOrEmpty(request.Email)) user.Email = request.Email;
                if (!string.IsNullOrEmpty(request.DisplayName)) user.DisplayName = request.DisplayName;
                if (request.FirstName != null) user.FirstName = request.FirstName;
                if (request.LastName != null) user.LastName = request.LastName;
                if (request.Department != null) user.Department = request.Department;
                if (request.Title != null) user.Title = request.Title;
                if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
                if (request.EmailConfirmed.HasValue) user.EmailConfirmed = request.EmailConfirmed.Value;

                user.ModifiedDate = DateTime.UtcNow;
                user.ModifiedBy = request.ModifiedBy;

                // Update roles if provided
                if (request.RoleIds != null)
                {
                    await UpdateUserRolesInternalAsync(userId, request.RoleIds, request.ModifiedBy, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("User {UserId} updated successfully", userId);

                return await GetUserByIdAsync(userId, cancellationToken) ?? user;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update user {UserId}: {Message}", userId, ex.Message);
                throw;
            }
        });
    }

    public async Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return false;
        }

        try
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} deleted successfully", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}: {Message}", userId, ex.Message);
            return false;
        }
    }

    public async Task<bool> ActivateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await UpdateUserStatusAsync(userId, true, cancellationToken);
    }

    public async Task<bool> DeactivateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await UpdateUserStatusAsync(userId, false, cancellationToken);
    }

    public async Task<bool> IsUsernameAvailableAsync(string username, string? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(u => u.Username == username);
        
        if (!string.IsNullOrEmpty(excludeUserId))
        {
            query = query.Where(u => u.UserId != excludeUserId);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.Where(u => u.Email == email);
        
        if (!string.IsNullOrEmpty(excludeUserId))
        {
            query = query.Where(u => u.UserId != excludeUserId);
        }

        return !await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ValidateUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        return user != null && user.IsActive && !user.IsLockedOut();
    }

    private async Task<bool> UpdateUserStatusAsync(string userId, bool isActive, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            return false;
        }

        try
        {
            user.IsActive = isActive;
            user.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} {Status}", userId, isActive ? "activated" : "deactivated");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId} status: {Message}", userId, ex.Message);
            return false;
        }
    }

    private async Task AssignRolesInternalAsync(string userId, IEnumerable<string> roleIds, string? assignedBy, CancellationToken cancellationToken)
    {
        foreach (var roleId in roleIds)
        {
            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedDate = DateTime.UtcNow,
                AssignedBy = assignedBy
            };

            _context.UserRoles.Add(userRole);
        }
    }

    private async Task UpdateUserRolesInternalAsync(string userId, IEnumerable<string> roleIds, string? assignedBy, CancellationToken cancellationToken)
    {
        // Remove existing roles
        var existingRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken);

        _context.UserRoles.RemoveRange(existingRoles);

        // Add new roles
        await AssignRolesInternalAsync(userId, roleIds, assignedBy, cancellationToken);
    }

    // Additional methods will be implemented in the next part...
    public Task<IEnumerable<MonitoringGrid.Core.Entities.Role>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> AssignRoleAsync(string userId, string roleId, string? assignedBy = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> RemoveRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateUserRolesAsync(string userId, IEnumerable<string> roleIds, string? assignedBy = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<MonitoringGrid.Core.Entities.Permission>> GetUserPermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasPermissionAsync(string userId, string resource, string action, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> HasAnyPermissionAsync(string userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> SetPasswordAsync(string userId, string password, string? setBy = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ResetPasswordAsync(string userId, string newPassword, string? resetBy = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsPasswordExpiredAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<MonitoringGrid.Core.Entities.UserPassword>> GetPasswordHistoryAsync(string userId, int count = 10, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> LockUserAsync(string userId, int lockoutDurationMinutes, string? reason = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UnlockUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsUserLockedAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetFailedLoginAttemptsAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IncrementFailedLoginAttemptsAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ResetFailedLoginAttemptsAsync(string userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> BulkActivateUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> BulkDeactivateUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> BulkDeleteUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> BulkAssignRoleAsync(IEnumerable<string> userIds, string roleId, string? assignedBy = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<bool> BulkRemoveRoleAsync(IEnumerable<string> userIds, string roleId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
