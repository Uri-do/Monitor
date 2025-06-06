using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Interfaces;
using System.Security.Claims;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// User management controller for CRUD operations on users
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        IMapper mapper,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with optional filtering
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireUserReadPermission")]
    public async Task<ActionResult<List<UserDto>>> GetUsers([FromQuery] bool? isActive = null)
    {
        try
        {
            var users = await _userService.GetUsersAsync(isActive);
            var userDtos = _mapper.Map<List<UserDto>>(users);

            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users: {Message}", ex.Message);
            return StatusCode(500, new { message = "An error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "RequireUserReadPermission")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound($"User with ID {id} not found");

            var userDto = _mapper.Map<UserDto>(user);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "An error occurred while retrieving user" });
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireUserWritePermission")]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = GetCurrentUserId();
            var createUserRequest = _mapper.Map<CreateUserRequest>(request);
            createUserRequest.CreatedBy = currentUserId;

            var user = await _userService.CreateUserAsync(createUserRequest);
            var userDto = _mapper.Map<UserDto>(user);

            _logger.LogInformation("User {Username} created by {CreatedBy}", request.Username, currentUserId);

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userDto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Username}: {Message}", request.Username, ex.Message);
            return StatusCode(500, new { message = "An error occurred while creating user" });
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireUserWritePermission")]
    public async Task<ActionResult<UserDto>> UpdateUser(string id, [FromBody] UpdateUserRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = GetCurrentUserId();
            var updateUserRequest = _mapper.Map<UpdateUserRequest>(request);
            updateUserRequest.ModifiedBy = currentUserId;

            var user = await _userService.UpdateUserAsync(id, updateUserRequest);
            var userDto = _mapper.Map<UserDto>(user);

            _logger.LogInformation("User {UserId} updated by {ModifiedBy}", id, currentUserId);

            return Ok(userDto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "An error occurred while updating user" });
        }
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireUserDeletePermission")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            // Prevent users from deleting themselves
            if (id == currentUserId)
            {
                return BadRequest(new { message = "You cannot delete your own account" });
            }

            var success = await _userService.DeleteUserAsync(id);
            if (!success)
                return NotFound($"User with ID {id} not found");

            _logger.LogInformation("User {UserId} deleted by {DeletedBy}", id, currentUserId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "An error occurred while deleting user" });
        }
    }

    /// <summary>
    /// Activate a user
    /// </summary>
    [HttpPost("{id}/activate")]
    [Authorize(Policy = "RequireUserWritePermission")]
    public async Task<ActionResult> ActivateUser(string id)
    {
        try
        {
            var success = await _userService.ActivateUserAsync(id);
            if (!success)
                return NotFound($"User with ID {id} not found");

            var currentUserId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} activated by {ActivatedBy}", id, currentUserId);

            return Ok(new { message = "User activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "An error occurred while activating user" });
        }
    }

    /// <summary>
    /// Deactivate a user
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Policy = "RequireUserWritePermission")]
    public async Task<ActionResult> DeactivateUser(string id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            
            // Prevent users from deactivating themselves
            if (id == currentUserId)
            {
                return BadRequest(new { message = "You cannot deactivate your own account" });
            }

            var success = await _userService.DeactivateUserAsync(id);
            if (!success)
                return NotFound($"User with ID {id} not found");

            _logger.LogInformation("User {UserId} deactivated by {DeactivatedBy}", id, currentUserId);

            return Ok(new { message = "User deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "An error occurred while deactivating user" });
        }
    }

    /// <summary>
    /// Assign roles to a user
    /// </summary>
    [HttpPost("{id}/roles")]
    [Authorize(Policy = "RequireRoleWritePermission")]
    public async Task<ActionResult> AssignRoles(string id, [FromBody] UserRoleAssignmentRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != request.UserId)
                return BadRequest(new { message = "User ID in URL does not match request body" });

            var currentUserId = GetCurrentUserId();
            var success = await _userService.UpdateUserRolesAsync(id, request.RoleIds, currentUserId);

            if (!success)
                return NotFound($"User with ID {id} not found");

            _logger.LogInformation("Roles updated for user {UserId} by {UpdatedBy}", id, currentUserId);

            return Ok(new { message = "User roles updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating roles for user {UserId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "An error occurred while updating user roles" });
        }
    }

    /// <summary>
    /// Get user roles
    /// </summary>
    [HttpGet("{id}/roles")]
    [Authorize(Policy = "RequireUserReadPermission")]
    public async Task<ActionResult<List<RoleDto>>> GetUserRoles(string id)
    {
        try
        {
            var roles = await _userService.GetUserRolesAsync(id);
            var roleDtos = _mapper.Map<List<RoleDto>>(roles);

            return Ok(roleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for user {UserId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "An error occurred while retrieving user roles" });
        }
    }

    /// <summary>
    /// Get user permissions
    /// </summary>
    [HttpGet("{id}/permissions")]
    [Authorize(Policy = "RequireUserReadPermission")]
    public async Task<ActionResult<List<PermissionDto>>> GetUserPermissions(string id)
    {
        try
        {
            var permissions = await _userService.GetUserPermissionsAsync(id);
            var permissionDtos = _mapper.Map<List<PermissionDto>>(permissions);

            return Ok(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for user {UserId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "An error occurred while retrieving user permissions" });
        }
    }

    /// <summary>
    /// Bulk operations on users
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Policy = "RequireUserWritePermission")]
    public async Task<ActionResult> BulkOperation([FromBody] BulkUserOperationRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentUserId = GetCurrentUserId();
            bool success = false;

            switch (request.Operation.ToLower())
            {
                case "activate":
                    success = await _userService.BulkActivateUsersAsync(request.UserIds);
                    break;
                case "deactivate":
                    // Prevent users from deactivating themselves
                    if (!string.IsNullOrEmpty(currentUserId) && request.UserIds.Contains(currentUserId))
                    {
                        return BadRequest(new { message = "You cannot deactivate your own account" });
                    }
                    success = await _userService.BulkDeactivateUsersAsync(request.UserIds);
                    break;
                case "delete":
                    // Prevent users from deleting themselves
                    if (!string.IsNullOrEmpty(currentUserId) && request.UserIds.Contains(currentUserId))
                    {
                        return BadRequest(new { message = "You cannot delete your own account" });
                    }
                    success = await _userService.BulkDeleteUsersAsync(request.UserIds);
                    break;
                case "assign-role":
                    if (string.IsNullOrEmpty(request.RoleId))
                        return BadRequest(new { message = "RoleId is required for assign-role operation" });
                    success = await _userService.BulkAssignRoleAsync(request.UserIds, request.RoleId, currentUserId);
                    break;
                case "remove-role":
                    if (string.IsNullOrEmpty(request.RoleId))
                        return BadRequest(new { message = "RoleId is required for remove-role operation" });
                    success = await _userService.BulkRemoveRoleAsync(request.UserIds, request.RoleId);
                    break;
                default:
                    return BadRequest(new { message = "Invalid operation. Supported operations: activate, deactivate, delete, assign-role, remove-role" });
            }

            if (!success)
                return BadRequest(new { message = $"Failed to perform {request.Operation} operation" });

            _logger.LogInformation("Bulk operation {Operation} performed on {UserCount} users by {PerformedBy}", 
                request.Operation, request.UserIds.Count, currentUserId);

            return Ok(new { message = $"Bulk {request.Operation} operation completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation {Operation}: {Message}", request.Operation, ex.Message);
            return StatusCode(500, new { message = "An error occurred while performing bulk operation" });
        }
    }

    /// <summary>
    /// Check username availability
    /// </summary>
    [HttpGet("check-username/{username}")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> CheckUsernameAvailability(string username)
    {
        try
        {
            var isAvailable = await _userService.IsUsernameAvailableAsync(username);
            return Ok(new { isAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking username availability for {Username}: {Message}", username, ex.Message);
            return StatusCode(500, new { message = "An error occurred while checking username availability" });
        }
    }

    /// <summary>
    /// Check email availability
    /// </summary>
    [HttpGet("check-email/{email}")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> CheckEmailAvailability(string email)
    {
        try
        {
            var isAvailable = await _userService.IsEmailAvailableAsync(email);
            return Ok(new { isAvailable });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email availability for {Email}: {Message}", email, ex.Message);
            return StatusCode(500, new { message = "An error occurred while checking email availability" });
        }
    }

    #region Helper Methods

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    #endregion
}
