using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MediatR;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.DTOs.Common;
using MonitoringGrid.Api.DTOs.Security;
// API Key authentication removed - was using test implementation
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Api.Observability;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Interfaces.Security;
using MonitoringGrid.Core.Security;
using System.Security.Claims;
using System.Diagnostics;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Basic security management controller for authentication and authorization
/// </summary>
[ApiController]
[Route("api/security")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
public class SecurityController : BaseApiController
{
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IUserService _userService;
    private readonly IRoleManagementService _roleService;
    private readonly ISecurityService _authenticationService;
    private readonly IMapper _mapper;
    private readonly SecurityConfiguration _securityConfig;

    public SecurityController(
        IMediator mediator,
        ISecurityAuditService securityAuditService,
        IUserService userService,
        IRoleManagementService roleService,
        ISecurityService authenticationService,
        IMapper mapper,
        ILogger<SecurityController> logger,
        IOptions<SecurityConfiguration> securityConfig)
        : base(mediator, logger)
    {
        _securityAuditService = securityAuditService;
        _userService = userService;
        _roleService = roleService;
        _authenticationService = authenticationService;
        _mapper = mapper;
        _securityConfig = securityConfig.Value;
    }

    // GetSecurityConfig method moved to SecurityConfigController

    // UpdateSecurityConfig method moved to SecurityConfigController

    // GetSecurityEvents method moved to SecurityAuditController

    // GetUserSecurityEvents method moved to SecurityAuditController

    // GetUsers method moved to UserManagementController

    // UpdateUserRoles method moved to UserManagementController

    /// <summary>
    /// Get all roles
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced roles with permissions and user counts</returns>
    [HttpGet("roles")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<RoleResponse>>> GetRoles(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var currentUserId = GetCurrentUserId();
            Logger.LogDebug("Getting roles for user {UserId}", currentUserId);

            var roles = await _roleService.GetRolesAsync(cancellationToken);

            stopwatch.Stop();

            // Map to enhanced response DTOs
            var roleResponses = new List<RoleResponse>();
            foreach (var role in roles)
            {
                var userCount = 0; // Core service doesn't have GetUserCountByRoleAsync method
                var permissions = new List<Permission>(); // Core service doesn't have GetRolePermissionsAsync method

                roleResponses.Add(new RoleResponse
                {
                    RoleId = role.RoleId, // Use RoleId property instead of Id
                    RoleName = role.Name,
                    Description = role.Description,
                    Permissions = permissions.Select(p => p.Name).ToList(),
                    UserCount = userCount,
                    IsSystemRole = role.IsSystemRole,
                    CreatedDate = role.CreatedDate
                });
            }

            // Log roles access
            await LogSecurityEventAsync("RolesAccess", "READ", "Roles",
                true, currentUserId, "Roles list accessed", cancellationToken);

            Logger.LogInformation("Retrieved {Count} roles for user {UserId} in {Duration}ms",
                roleResponses.Count, currentUserId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(roleResponses, $"Retrieved {roleResponses.Count} roles"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get roles operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Get roles operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var currentUserId = GetCurrentUserId();
            Logger.LogError(ex, "Error retrieving roles for user {UserId}: {Message}", currentUserId, ex.Message);

            return StatusCode(500, CreateErrorResponse("Failed to retrieve roles", "GET_ROLES_ERROR"));
        }
    }

    /// <summary>
    /// Get all permissions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced permissions with categories and descriptions</returns>
    [HttpGet("permissions")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<PermissionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<PermissionResponse>>> GetPermissions(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var currentUserId = GetCurrentUserId();
            Logger.LogDebug("Getting permissions for user {UserId}", currentUserId);

            var permissions = await _roleService.GetPermissionsAsync(cancellationToken);

            stopwatch.Stop();

            // Map to enhanced response DTOs
            var permissionResponses = permissions.Select(permission => new PermissionResponse
            {
                PermissionId = permission.PermissionId, // Use PermissionId property instead of Id
                PermissionName = permission.Name,
                Description = permission.Description,
                Category = "General", // Core Permission doesn't have Category property
                IsSystemPermission = permission.IsSystemPermission
            }).ToList();

            // Group by category for better organization
            var groupedPermissions = permissionResponses
                .GroupBy(p => p.Category)
                .OrderBy(g => g.Key)
                .SelectMany(g => g.OrderBy(p => p.PermissionName))
                .ToList();

            // Log permissions access
            await LogSecurityEventAsync("PermissionsAccess", "READ", "Permissions",
                true, currentUserId, "Permissions list accessed", cancellationToken);

            Logger.LogInformation("Retrieved {Count} permissions for user {UserId} in {Duration}ms",
                groupedPermissions.Count, currentUserId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(groupedPermissions, $"Retrieved {groupedPermissions.Count} permissions"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get permissions operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Get permissions operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var currentUserId = GetCurrentUserId();
            Logger.LogError(ex, "Error retrieving permissions for user {UserId}: {Message}", currentUserId, ex.Message);

            return StatusCode(500, CreateErrorResponse("Failed to retrieve permissions", "GET_PERMISSIONS_ERROR"));
        }
    }









    // Login method moved to AuthenticationController

    // Register method moved to AuthenticationController



    // GetProfile method moved to AuthenticationController






    // RefreshToken method moved to AuthenticationController





















    /// <summary>
    /// Maps a user entity to a basic user response DTO
    /// </summary>
    private UserResponse MapToUserResponse(dynamic user, bool includeDetails = false,
        IEnumerable<MonitoringGrid.Core.Entities.Role>? userRoles = null,
        IEnumerable<MonitoringGrid.Core.Entities.Permission>? userPermissions = null)
    {
        var response = new UserResponse
        {
            UserId = user.UserId ?? user.Id ?? string.Empty,
            Username = user.Username ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            DisplayName = user.DisplayName ?? string.Empty,
            IsActive = user.IsActive,
            IsLockedOut = user.IsLockedOut(),
            Roles = userRoles?.Select(r => r.Name).ToList() ?? new List<string>(),
            Permissions = userPermissions?.Select(p => p.Name).ToList() ?? new List<string>(),
            LastLogin = user.LastLogin,
            CreatedDate = user.CreatedDate,
            FailedLoginAttempts = user.FailedLoginAttempts,
            TwoFactorEnabled = user.TwoFactorEnabled
        };

        if (includeDetails)
        {
            response.Details = new Dictionary<string, object>
            {
                ["AccountAge"] = (DateTime.UtcNow - user.CreatedDate).TotalDays,
                ["DaysSinceLastLogin"] = user.LastLogin != null ? (DateTime.UtcNow - (DateTime)user.LastLogin).TotalDays : null,
                ["RoleCount"] = response.Roles.Count
            };
        }

        return response;
    }

    /// <summary>
    /// Logs a security event asynchronously
    /// </summary>
    private async Task LogSecurityEventAsync(string eventType, string action, string resource,
        bool isSuccess, string? userId, string description, CancellationToken cancellationToken = default)
    {
        try
        {
            var securityEvent = new SecurityAuditEvent
            {
                EventId = Guid.NewGuid().ToString(),
                UserId = userId,
                EventType = eventType,
                Action = action,
                Resource = resource,
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                IsSuccess = isSuccess,
                Timestamp = DateTime.UtcNow,
                AdditionalData = new Dictionary<string, object>
                {
                    ["Description"] = description,
                    ["ControllerAction"] = $"{ControllerContext.ActionDescriptor.ControllerName}.{ControllerContext.ActionDescriptor.ActionName}"
                }
            };

            await _securityAuditService.LogSecurityEventAsync(eventType, description,
                string.IsNullOrEmpty(userId) ? null : int.TryParse(userId, out var id) ? id : null,
                GetClientIpAddress(), cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
        }
    }







    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               User.FindFirst("sub")?.Value ??
               User.FindFirst("user_id")?.Value ??
               string.Empty;
    }

    /// <summary>
    /// Gets the client IP address
    /// </summary>
    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
