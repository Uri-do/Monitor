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
using ApiKeyService = MonitoringGrid.Api.Authentication.IApiKeyService;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Api.Observability;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Interfaces.Security;
using MonitoringGrid.Core.Security;
using System.Security.Claims;
using System.Diagnostics;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Comprehensive security management controller for authentication, authorization, API keys, and security monitoring
/// </summary>
[ApiController]
[Route("api/security")]
[Authorize]
[Produces("application/json")]
[PerformanceMonitor(slowThresholdMs: 2000)]
[ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
public class SecurityController : BaseApiController
{
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IUserService _userService;
    private readonly IRoleManagementService _roleService;
    private readonly ApiKeyService _apiKeyService;
    private readonly ISecurityService _authenticationService;
    private readonly IMapper _mapper;
    private readonly SecurityConfiguration _securityConfig;

    public SecurityController(
        IMediator mediator,
        ISecurityAuditService securityAuditService,
        IUserService userService,
        IRoleManagementService roleService,
        ApiKeyService apiKeyService,
        ISecurityService authenticationService,
        IMapper mapper,
        ILogger<SecurityController> logger,
        IOptions<SecurityConfiguration> securityConfig)
        : base(mediator, logger)
    {
        _securityAuditService = securityAuditService;
        _userService = userService;
        _roleService = roleService;
        _apiKeyService = apiKeyService;
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

    /// <summary>
    /// Debug endpoint to check user roles directly from database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Debug information about user roles</returns>
    [HttpGet("debug/roles")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> DebugUserRoles(CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            Logger.LogDebug("Debug: Checking roles for user {UserId}", userId);

            using var scope = HttpContext.RequestServices.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Infrastructure.Data.MonitoringContext>();

            // Get user directly from database
            var user = await context.Users
                .Where(u => u.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                return Ok(new { UserId = userId, Message = "User not found in database" });
            }

            // Get user roles directly from database
            var userRoles = await context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .ToListAsync(cancellationToken);

            // Get all roles in system
            var allRoles = await context.Roles.ToListAsync(cancellationToken);

            return Ok(new
            {
                UserId = userId,
                UserEmail = user.Email,
                UserName = user.Username,
                UserRoles = userRoles.Select(ur => new
                {
                    RoleId = ur.RoleId,
                    RoleName = ur.Role.Name,
                    RoleDescription = ur.Role.Description
                }).ToList(),
                AllRolesInSystem = allRoles.Select(r => new
                {
                    RoleId = r.RoleId,
                    RoleName = r.Name,
                    RoleDescription = r.Description
                }).ToList(),
                HasAdminRole = userRoles.Any(ur => ur.Role.Name == "Admin"),
                ClaimsFromToken = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in debug roles endpoint");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    // GetProfile method moved to AuthenticationController






    // RefreshToken method moved to AuthenticationController





    /// <summary>
    /// Maps a security event to an enhanced security event response DTO
    /// </summary>
    private SecurityEventResponse MapToSecurityEventResponse(dynamic securityEvent, bool includeDetails = false, bool includeRiskAnalysis = false)
    {
        var now = DateTime.UtcNow;
        var timeSinceEvent = now - securityEvent.Timestamp;
        var riskScore = CalculateEventRiskScore(securityEvent, includeRiskAnalysis);

        var response = new SecurityEventResponse
        {
            EventId = securityEvent.EventId ?? Guid.NewGuid().ToString(),
            UserId = securityEvent.UserId,
            Username = securityEvent.Username ?? "Unknown",
            EventType = securityEvent.EventType ?? "Unknown",
            Action = securityEvent.Action ?? "Unknown",
            Resource = securityEvent.Resource,
            IpAddress = securityEvent.IpAddress,
            UserAgent = securityEvent.UserAgent,
            IsSuccess = securityEvent.IsSuccess,
            Timestamp = securityEvent.Timestamp,
            RiskScore = riskScore,
            TimeSinceEvent = timeSinceEvent
        };

        if (includeDetails)
        {
            response.AdditionalData = securityEvent.AdditionalData as Dictionary<string, object> ?? new Dictionary<string, object>();
            response.AdditionalData["EventAge"] = timeSinceEvent.TotalHours;
            response.AdditionalData["RiskLevel"] = GetRiskLevel(riskScore);
            response.AdditionalData["IsRecentEvent"] = timeSinceEvent.TotalHours < 24;
        }

        return response;
    }

    /// <summary>
    /// Calculates risk score for a security event
    /// </summary>
    private int CalculateEventRiskScore(dynamic securityEvent, bool includeAdvancedAnalysis = false)
    {
        var score = 0;

        // Base score for failed events
        if (!securityEvent.IsSuccess) score += 30;

        // Event type risk scoring
        var eventType = securityEvent.EventType?.ToString()?.ToLower() ?? "";
        score += eventType switch
        {
            var et when et.Contains("login") && !securityEvent.IsSuccess => 40,
            var et when et.Contains("unauthorized") => 50,
            var et when et.Contains("config") => 30,
            var et when et.Contains("role") => 25,
            var et when et.Contains("permission") => 20,
            _ => 10
        };

        // Time-based risk (recent events are higher risk)
        var timeSinceEvent = DateTime.UtcNow - securityEvent.Timestamp;
        if (timeSinceEvent.TotalHours < 1) score += 10;
        if (timeSinceEvent.TotalMinutes < 15) score += 15;

        // Advanced analysis if requested
        if (includeAdvancedAnalysis)
        {
            // IP-based risk (placeholder - would check against threat intelligence)
            if (!string.IsNullOrEmpty(securityEvent.IpAddress))
            {
                // Check for suspicious IP patterns
                if (securityEvent.IpAddress.StartsWith("10.") || securityEvent.IpAddress.StartsWith("192.168."))
                {
                    score -= 5; // Internal IP, lower risk
                }
                else
                {
                    score += 5; // External IP, higher risk
                }
            }
        }

        return Math.Min(score, 100);
    }

    /// <summary>
    /// Gets risk level description from risk score
    /// </summary>
    private string GetRiskLevel(int riskScore) => riskScore switch
    {
        >= 80 => "Critical",
        >= 60 => "High",
        >= 40 => "Medium",
        >= 20 => "Low",
        _ => "Minimal"
    };

    /// <summary>
    /// Calculates password policy strength score
    /// </summary>
    private int CalculatePasswordPolicyStrength()
    {
        var score = 0;
        var policy = _securityConfig.PasswordPolicy;

        if (policy.MinimumLength >= 8) score += 20;
        if (policy.MinimumLength >= 12) score += 10;
        if (policy.RequireUppercase) score += 15;
        if (policy.RequireLowercase) score += 15;
        if (policy.RequireDigit) score += 15;
        if (policy.RequireSpecialCharacter) score += 15;
        if (policy.PasswordExpirationDays > 0 && policy.PasswordExpirationDays <= 90) score += 10;

        return Math.Min(score, 100);
    }

    /// <summary>
    /// Calculates configuration health score
    /// </summary>
    private int CalculateConfigurationHealth(SecurityConfigResponse config)
    {
        var score = 100;

        // Password policy health
        // Note: PasswordStrengthScore property removed
        if (config.PasswordPolicy.MaxFailedAttempts > 10) score -= 10;

        // Two-factor health
        if (!config.TwoFactorPolicy.IsEnabled) score -= 30;
        if (config.TwoFactorPolicy.IsEnabled && !config.TwoFactorPolicy.IsRequired) score -= 15;

        // Rate limiting health - using AuditPolicy as replacement
        if (!config.AuditPolicy.LogAllEvents) score -= 20;

        // Session health
        if (config.SessionPolicy.SessionTimeout.TotalMinutes > 480) score -= 10;
        if (config.SessionPolicy.IdleTimeout.TotalMinutes > 60) score -= 5;

        return Math.Max(score, 0);
    }

    /// <summary>
    /// Validates security configuration changes
    /// </summary>
    private (bool IsValid, List<string> Errors) ValidateSecurityConfigurationChanges(UpdateSecurityConfigRequest request)
    {
        var errors = new List<string>();

        // Validate password policy
        if (request.PasswordPolicy.MinimumLength < 6 || request.PasswordPolicy.MinimumLength > 128)
            errors.Add("Password minimum length must be between 6 and 128 characters");

        if (request.PasswordPolicy.MaxFailedAttempts < 3 || request.PasswordPolicy.MaxFailedAttempts > 20)
            errors.Add("Max failed attempts must be between 3 and 20");

        // Validate session settings
        if (request.SessionPolicy?.SessionTimeout?.TotalMinutes < 5 || request.SessionPolicy?.SessionTimeout?.TotalMinutes > 1440)
            errors.Add("Session timeout must be between 5 and 1440 minutes");

        // Note: Rate limiting validation removed as RateLimitSettings no longer exists

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Gets current security configuration snapshot for change tracking
    /// </summary>
    private Dictionary<string, object> GetCurrentSecurityConfigSnapshot()
    {
        return new Dictionary<string, object>
        {
            ["PasswordPolicy"] = new
            {
                _securityConfig.PasswordPolicy.MinimumLength,
                _securityConfig.PasswordPolicy.RequireUppercase,
                _securityConfig.PasswordPolicy.RequireLowercase,
                _securityConfig.PasswordPolicy.RequireDigit,
                _securityConfig.PasswordPolicy.RequireSpecialCharacter
            },
            ["SessionPolicy"] = new
            {
                _securityConfig.Session.SessionTimeoutMinutes,
                _securityConfig.Session.IdleTimeoutMinutes
            },
            ["TwoFactorPolicy"] = new
            {
                _securityConfig.TwoFactor.IsEnabled,
                _securityConfig.TwoFactor.IsRequired
            },
            ["AuditPolicy"] = new
            {
                _securityConfig.RateLimit.IsEnabled,
                _securityConfig.RateLimit.RequestsPerMinute,
                _securityConfig.RateLimit.RequestsPerHour
            }
        };
    }

    /// <summary>
    /// Gets changed fields between previous and new configuration
    /// </summary>
    private List<string> GetChangedFields(Dictionary<string, object> previousConfig, UpdateSecurityConfigRequest newConfig)
    {
        var changedFields = new List<string>();

        // This is a simplified implementation
        // In a real scenario, you would compare each field individually
        changedFields.Add("PasswordPolicy");
        changedFields.Add("SessionPolicy");
        changedFields.Add("TwoFactorPolicy");
        changedFields.Add("AuditPolicy");

        return changedFields;
    }

    /// <summary>
    /// Maps a user entity to an enhanced user response DTO
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
                ["SecurityScore"] = CalculateUserSecurityScore(user),
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
    /// Calculates password strength score
    /// </summary>
    private int CalculatePasswordStrength(string password)
    {
        var score = 0;
        if (password.Length >= 8) score += 20;
        if (password.Length >= 12) score += 10;
        if (password.Any(char.IsUpper)) score += 20;
        if (password.Any(char.IsLower)) score += 20;
        if (password.Any(char.IsDigit)) score += 20;
        if (password.Any(c => !char.IsLetterOrDigit(c))) score += 10;
        return Math.Min(score, 100);
    }

    /// <summary>
    /// Calculates user security score based on various factors
    /// </summary>
    private int CalculateUserSecurityScore(dynamic user)
    {
        var score = 100;

        // Deduct for failed login attempts
        if (user.FailedLoginAttempts > 0)
            score -= Math.Min(user.FailedLoginAttempts * 5, 25);

        // Deduct if two-factor is not enabled
        if (!user.TwoFactorEnabled)
            score -= 20;

        // Deduct if account is locked
        if (user.IsLockedOut())
            score -= 30;

        // Deduct for old last login
        if (user.LastLogin != null)
        {
            var daysSinceLogin = (DateTime.UtcNow - (DateTime)user.LastLogin).TotalDays;
            if (daysSinceLogin > 30) score -= 10;
            if (daysSinceLogin > 90) score -= 20;
        }
        else
        {
            score -= 15; // Never logged in
        }

        return Math.Max(score, 0);
    }

    /// <summary>
    /// Extracts user ID from JWT token
    /// </summary>
    private string? ExtractUserIdFromToken(string token)
    {
        try
        {
            // This is a simplified implementation
            // In a real scenario, you would properly decode the JWT token
            return "extracted-user-id"; // Placeholder
        }
        catch
        {
            return null;
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
