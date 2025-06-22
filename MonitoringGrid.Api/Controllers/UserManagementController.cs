using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.DTOs.Security;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Interfaces.Security;
using MonitoringGrid.Core.Security;
using System.Diagnostics;
using System.Security.Claims;
using AutoMapper;
using MediatR;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Controller for user management operations (CRUD, profile management)
/// </summary>
[ApiController]
[Route("api/security/users")]
[Authorize]
[Produces("application/json")]
public class UserManagementController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IRoleManagementService _roleManagementService;
    private readonly ISecurityService _securityService;
    private readonly IMapper _mapper;

    public UserManagementController(
        IMediator mediator,
        IUserService userService,
        ISecurityAuditService securityAuditService,
        IRoleManagementService roleManagementService,
        ISecurityService securityService,
        IMapper mapper,
        ILogger<UserManagementController> logger)
        : base(mediator, logger)
    {
        _userService = userService;
        _securityAuditService = securityAuditService;
        _roleManagementService = roleManagementService;
        _securityService = securityService;
        _mapper = mapper;
    }

    /// <summary>
    /// Get all users with filtering and pagination
    /// </summary>
    /// <param name="searchTerm">Search term for username, email, or name</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetUsers(
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogDebug("Retrieving users with filters: SearchTerm={SearchTerm}, IsActive={IsActive}, Page={Page}, PageSize={PageSize}",
                searchTerm, isActive, page, pageSize);

            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 1000) pageSize = 50;

            var users = await _userService.GetUsersAsync(isActive, cancellationToken);

            // Apply filtering
            if (!string.IsNullOrEmpty(searchTerm))
            {
                users = users.Where(u => 
                    u.Username.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    $"{u.FirstName} {u.LastName}".Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            if (isActive.HasValue)
            {
                users = users.Where(u => u.IsActive == isActive.Value).ToList();
            }

            // Apply pagination
            var totalCount = users.Count();
            var pagedUsers = users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var userResponses = pagedUsers.Select(u => MapToUserResponse(u, false)).ToList();

            stopwatch.Stop();

            var response = new PagedResponse<UserResponse>
            {
                Data = userResponses,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = page * pageSize < totalCount,
                HasPreviousPage = page > 1
            };

            Logger.LogInformation("Retrieved {Count} users (page {Page}/{TotalPages}) in {Duration}ms",
                userResponses.Count, page, response.TotalPages, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, $"Retrieved {userResponses.Count} users"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving users: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to retrieve users", "USERS_RETRIEVAL_ERROR"));
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="includeDetails">Include detailed information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetUser(
        string id,
        [FromQuery] bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Retrieving user {UserId} with details={IncludeDetails}", id, includeDetails);

            var user = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(CreateErrorResponse("User not found", "USER_NOT_FOUND"));
            }

            var userResponse = MapToUserResponse(user, includeDetails);

            Logger.LogInformation("Retrieved user {UserId} ({Username})", id, user.Username);

            return Ok(CreateSuccessResponse(userResponse, "User retrieved successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving user {UserId}: {Message}", id, ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to retrieve user", "USER_RETRIEVAL_ERROR"));
        }
    }

    /// <summary>
    /// Update user information
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Update user request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated user information</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> UpdateUser(
        string id,
        [FromBody] MonitoringGrid.Api.DTOs.Security.UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = GetCurrentUserId();

        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Updating user {UserId} by user {CurrentUserId}", id, currentUserId);

            var existingUser = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (existingUser == null)
            {
                return NotFound(CreateErrorResponse("User not found", "USER_NOT_FOUND"));
            }

            var updateRequest = new MonitoringGrid.Core.Interfaces.UpdateUserRequest
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = request.IsActive
            };

            var updatedUser = await _userService.UpdateUserAsync(id, updateRequest, cancellationToken);

            stopwatch.Stop();

            var userResponse = MapToUserResponse(updatedUser, true);

            // Log user update event
            await LogSecurityEventAsync("UserUpdate", "UPDATE", $"User/{id}",
                true, currentUserId, $"User {id} updated by {currentUserId}", cancellationToken);

            Logger.LogInformation("User {UserId} updated by {CurrentUserId} in {Duration}ms",
                id, currentUserId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(userResponse, "User updated successfully"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error updating user {UserId}: {Message}", id, ex.Message);

            // Log user update error
            await LogSecurityEventAsync("UserUpdateError", "UPDATE", $"User/{id}",
                false, currentUserId, $"Failed to update user {id}: {ex.Message}", cancellationToken);

            return StatusCode(500, CreateErrorResponse("Failed to update user", "USER_UPDATE_ERROR"));
        }
    }

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deletion confirmation</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteUser(
        string id,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = GetCurrentUserId();

        try
        {
            Logger.LogDebug("Deleting user {UserId} by user {CurrentUserId}", id, currentUserId);

            // Check if user exists
            var existingUser = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (existingUser == null)
            {
                return NotFound(CreateErrorResponse("User not found", "USER_NOT_FOUND"));
            }

            // Prevent self-deletion
            if (id == currentUserId)
            {
                return BadRequest(CreateErrorResponse("Cannot delete your own account", "SELF_DELETE_NOT_ALLOWED"));
            }

            await _userService.DeleteUserAsync(id, cancellationToken);

            stopwatch.Stop();

            // Log user deletion event
            await LogSecurityEventAsync("UserDelete", "DELETE", $"User/{id}",
                true, currentUserId, $"User {id} deleted by {currentUserId}", cancellationToken);

            Logger.LogInformation("User {UserId} deleted by {CurrentUserId} in {Duration}ms",
                id, currentUserId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(new { message = "User deleted successfully" }, "User deleted successfully"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error deleting user {UserId}: {Message}", id, ex.Message);

            // Log user deletion error
            await LogSecurityEventAsync("UserDeleteError", "DELETE", $"User/{id}",
                false, currentUserId, $"Failed to delete user {id}: {ex.Message}", cancellationToken);

            return StatusCode(500, CreateErrorResponse("Failed to delete user", "USER_DELETE_ERROR"));
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Change password request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password change confirmation</returns>
    [HttpPost("{id}/change-password")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ChangePassword(
        string id,
        [FromBody] MonitoringGrid.Api.DTOs.Security.ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = GetCurrentUserId();

        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Changing password for user {UserId} by user {CurrentUserId}", id, currentUserId);

            // Check if user exists
            var existingUser = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (existingUser == null)
            {
                return NotFound(CreateErrorResponse("User not found", "USER_NOT_FOUND"));
            }

            // Validate new password confirmation
            if (request.NewPassword != request.ConfirmNewPassword)
            {
                return BadRequest(CreateErrorResponse("New password and confirmation do not match", "PASSWORD_MISMATCH"));
            }

            var changePasswordRequest = new MonitoringGrid.Core.Security.ChangePasswordRequest
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword,
                ConfirmPassword = request.ConfirmNewPassword
            };

            var result = await _securityService.ChangePasswordAsync(id, changePasswordRequest, cancellationToken);
            if (!result.IsSuccess)
            {
                return BadRequest(CreateErrorResponse(result.Error?.Message ?? "Failed to change password", "PASSWORD_CHANGE_FAILED"));
            }

            stopwatch.Stop();

            // Log password change event
            await LogSecurityEventAsync("PasswordChange", "UPDATE", $"User/{id}",
                true, currentUserId, $"Password changed for user {id}", cancellationToken);

            Logger.LogInformation("Password changed for user {UserId} by {CurrentUserId} in {Duration}ms",
                id, currentUserId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(new { message = "Password changed successfully" }, "Password changed successfully"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error changing password for user {UserId}: {Message}", id, ex.Message);

            // Log password change error
            await LogSecurityEventAsync("PasswordChangeError", "UPDATE", $"User/{id}",
                false, currentUserId, $"Failed to change password for user {id}: {ex.Message}", cancellationToken);

            return StatusCode(500, CreateErrorResponse("Failed to change password", "PASSWORD_CHANGE_ERROR"));
        }
    }

    // Helper methods
    private UserResponse MapToUserResponse(dynamic user, bool includeDetails = false)
    {
        return new UserResponse
        {
            UserId = user?.UserId?.ToString() ?? string.Empty,
            Username = user?.Username ?? string.Empty,
            Email = user?.Email ?? string.Empty,
            FirstName = user?.FirstName ?? string.Empty,
            LastName = user?.LastName ?? string.Empty,
            IsActive = user?.IsActive ?? false,
            CreatedDate = user?.CreatedDate ?? DateTime.UtcNow,
            LastLogin = user?.LastLogin,
            Roles = new List<string>(), // Will be populated from user roles
            Permissions = new List<string>() // Will be populated from user permissions
        };
    }

    private async Task LogSecurityEventAsync(string eventType, string action, string resource,
        bool isSuccess, string? userId, string description, CancellationToken cancellationToken = default)
    {
        try
        {
            await _securityAuditService.LogSecurityEventAsync(eventType, description, 
                string.IsNullOrEmpty(userId) ? null : int.TryParse(userId, out var id) ? id : null, 
                GetClientIpAddress(), cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               User.FindFirst("sub")?.Value ??
               User.FindFirst("user_id")?.Value ??
               string.Empty;
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
