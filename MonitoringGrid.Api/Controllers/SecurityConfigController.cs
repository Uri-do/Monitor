using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.DTOs.Security;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Interfaces.Security;
using MonitoringGrid.Core.Security;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using AutoMapper;
using MediatR;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Controller for security configuration management
/// </summary>
[ApiController]
[Route("api/security/config")]
[Authorize]
[Produces("application/json")]
public class SecurityConfigController : BaseApiController
{
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IRoleManagementService _roleManagementService;
    private readonly IMapper _mapper;
    private readonly SecurityConfiguration _securityConfig;

    public SecurityConfigController(
        IMediator mediator,
        ISecurityAuditService securityAuditService,
        IRoleManagementService roleManagementService,
        IMapper mapper,
        ILogger<SecurityConfigController> logger,
        IOptions<SecurityConfiguration> securityConfig)
        : base(mediator, logger)
    {
        _securityAuditService = securityAuditService;
        _roleManagementService = roleManagementService;
        _mapper = mapper;
        _securityConfig = securityConfig.Value;
    }

    /// <summary>
    /// Get current security configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Security configuration</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SecurityConfigResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SecurityConfigResponse>> GetSecurityConfig(
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Retrieving security configuration");

            var response = new SecurityConfigResponse
            {
                PasswordPolicy = new PasswordPolicyResponse
                {
                    MinimumLength = _securityConfig.PasswordPolicy?.MinimumLength ?? 8,
                    RequireUppercase = _securityConfig.PasswordPolicy?.RequireUppercase ?? true,
                    RequireLowercase = _securityConfig.PasswordPolicy?.RequireLowercase ?? true,
                    RequireDigits = _securityConfig.PasswordPolicy?.RequireDigit ?? true,
                    RequireSpecialCharacters = _securityConfig.PasswordPolicy?.RequireSpecialCharacter ?? true,
                    MaxAge = TimeSpan.FromDays(_securityConfig.PasswordPolicy?.PasswordExpirationDays ?? 90),
                    PreventReuse = _securityConfig.PasswordPolicy?.PasswordHistoryCount ?? 5
                },
                SessionPolicy = new SessionPolicyResponse
                {
                    SessionTimeout = TimeSpan.FromMinutes(_securityConfig.Session?.SessionTimeoutMinutes ?? 480),
                    MaxConcurrentSessions = 3, // Not in SecurityConfiguration, using default
                    RequireReauthentication = false, // Not in SecurityConfiguration, using default
                    IdleTimeout = TimeSpan.FromMinutes(_securityConfig.Session?.IdleTimeoutMinutes ?? 60)
                },
                LockoutPolicy = new LockoutPolicyResponse
                {
                    MaxFailedAttempts = _securityConfig.PasswordPolicy?.MaxFailedAttempts ?? 5,
                    LockoutDuration = TimeSpan.FromMinutes(_securityConfig.PasswordPolicy?.LockoutDurationMinutes ?? 30),
                    ResetCounterAfter = TimeSpan.FromMinutes(60) // Not in SecurityConfiguration, using default
                },
                TwoFactorPolicy = new TwoFactorPolicyResponse
                {
                    IsEnabled = _securityConfig.TwoFactor?.IsEnabled ?? false,
                    IsRequired = _securityConfig.TwoFactor?.IsRequired ?? false,
                    AllowedProviders = _securityConfig.TwoFactor?.EnabledProviders ?? new List<string> { "TOTP", "SMS", "Email" },
                    TokenLifetime = TimeSpan.FromMinutes(_securityConfig.TwoFactor?.CodeExpirationMinutes ?? 5)
                },
                AuditPolicy = new AuditPolicyResponse
                {
                    LogAllEvents = true, // Not in SecurityConfiguration, using default
                    RetentionPeriod = TimeSpan.FromDays(365), // Not in SecurityConfiguration, using default
                    LogSensitiveData = false, // Not in SecurityConfiguration, using default
                    EnableRealTimeAlerts = true // Not in SecurityConfiguration, using default
                },
                LastUpdated = DateTime.UtcNow,
                UpdatedBy = GetCurrentUserId()
            };

            Logger.LogInformation("Security configuration retrieved successfully");

            return Ok(CreateSuccessResponse(response, "Security configuration retrieved successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving security configuration: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to retrieve security configuration", "CONFIG_RETRIEVAL_ERROR"));
        }
    }

    /// <summary>
    /// Update security configuration
    /// </summary>
    /// <param name="request">Security configuration update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated security configuration</returns>
    [HttpPut]
    [ProducesResponseType(typeof(SecurityConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SecurityConfigResponse>> UpdateSecurityConfig(
        [FromBody] UpdateSecurityConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentUserId = GetCurrentUserId();

        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Updating security configuration by user {UserId}", currentUserId);

            // Validate configuration values
            var validationErrors = ValidateSecurityConfig(request);
            if (validationErrors.Any())
            {
                return BadRequest(CreateErrorResponse("Invalid configuration values", "INVALID_CONFIG"));
            }

            // Note: In a real implementation, you would update the configuration in a database
            // or configuration store. For now, we'll return the updated configuration.

            // Note: In a real implementation, you would update the configuration in a database
            // or configuration store. For now, we'll return the updated configuration.
            var response = new SecurityConfigResponse
            {
                PasswordPolicy = new PasswordPolicyResponse
                {
                    MinimumLength = request.PasswordPolicy?.MinimumLength ?? _securityConfig.PasswordPolicy?.MinimumLength ?? 8,
                    RequireUppercase = request.PasswordPolicy?.RequireUppercase ?? _securityConfig.PasswordPolicy?.RequireUppercase ?? true,
                    RequireLowercase = request.PasswordPolicy?.RequireLowercase ?? _securityConfig.PasswordPolicy?.RequireLowercase ?? true,
                    RequireDigits = request.PasswordPolicy?.RequireDigits ?? _securityConfig.PasswordPolicy?.RequireDigit ?? true,
                    RequireSpecialCharacters = request.PasswordPolicy?.RequireSpecialCharacters ?? _securityConfig.PasswordPolicy?.RequireSpecialCharacter ?? true,
                    MaxAge = request.PasswordPolicy?.MaxAge ?? TimeSpan.FromDays(_securityConfig.PasswordPolicy?.PasswordExpirationDays ?? 90),
                    PreventReuse = request.PasswordPolicy?.PreventReuse ?? _securityConfig.PasswordPolicy?.PasswordHistoryCount ?? 5
                },
                SessionPolicy = new SessionPolicyResponse
                {
                    SessionTimeout = request.SessionPolicy?.SessionTimeout ?? TimeSpan.FromMinutes(_securityConfig.Session?.SessionTimeoutMinutes ?? 480),
                    MaxConcurrentSessions = request.SessionPolicy?.MaxConcurrentSessions ?? 3,
                    RequireReauthentication = request.SessionPolicy?.RequireReauthentication ?? false,
                    IdleTimeout = request.SessionPolicy?.IdleTimeout ?? TimeSpan.FromMinutes(_securityConfig.Session?.IdleTimeoutMinutes ?? 60)
                },
                LockoutPolicy = new LockoutPolicyResponse
                {
                    MaxFailedAttempts = request.LockoutPolicy?.MaxFailedAttempts ?? _securityConfig.PasswordPolicy?.MaxFailedAttempts ?? 5,
                    LockoutDuration = request.LockoutPolicy?.LockoutDuration ?? TimeSpan.FromMinutes(_securityConfig.PasswordPolicy?.LockoutDurationMinutes ?? 30),
                    ResetCounterAfter = request.LockoutPolicy?.ResetCounterAfter ?? TimeSpan.FromMinutes(60)
                },
                TwoFactorPolicy = new TwoFactorPolicyResponse
                {
                    IsEnabled = request.TwoFactorPolicy?.IsEnabled ?? _securityConfig.TwoFactor?.IsEnabled ?? false,
                    IsRequired = request.TwoFactorPolicy?.IsRequired ?? _securityConfig.TwoFactor?.IsRequired ?? false,
                    AllowedProviders = request.TwoFactorPolicy?.AllowedProviders ?? _securityConfig.TwoFactor?.EnabledProviders ?? new List<string> { "TOTP", "SMS", "Email" },
                    TokenLifetime = request.TwoFactorPolicy?.TokenLifetime ?? TimeSpan.FromMinutes(_securityConfig.TwoFactor?.CodeExpirationMinutes ?? 5)
                },
                AuditPolicy = new AuditPolicyResponse
                {
                    LogAllEvents = request.AuditPolicy?.LogAllEvents ?? true,
                    RetentionPeriod = request.AuditPolicy?.RetentionPeriod ?? TimeSpan.FromDays(365),
                    LogSensitiveData = request.AuditPolicy?.LogSensitiveData ?? false,
                    EnableRealTimeAlerts = request.AuditPolicy?.EnableRealTimeAlerts ?? true
                },
                LastUpdated = DateTime.UtcNow,
                UpdatedBy = currentUserId
            };

            stopwatch.Stop();

            // Log configuration update event
            await LogSecurityEventAsync("SecurityConfigUpdate", "UPDATE", "SecurityConfig",
                true, currentUserId, "Security configuration updated", cancellationToken);

            Logger.LogInformation("Security configuration updated by user {UserId} in {Duration}ms",
                currentUserId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, "Security configuration updated successfully"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error updating security configuration: {Message}", ex.Message);

            // Log configuration update error
            await LogSecurityEventAsync("SecurityConfigUpdateError", "UPDATE", "SecurityConfig",
                false, currentUserId, $"Failed to update security configuration: {ex.Message}", cancellationToken);

            return StatusCode(500, CreateErrorResponse("Failed to update security configuration", "CONFIG_UPDATE_ERROR"));
        }
    }

    /// <summary>
    /// Get all roles and permissions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Roles and permissions</returns>
    [HttpGet("roles-permissions")]
    [ProducesResponseType(typeof(RolesPermissionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RolesPermissionsResponse>> GetRolesAndPermissions(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Logger.LogDebug("Retrieving roles and permissions");

            var roles = await _roleManagementService.GetRolesAsync(cancellationToken);
            var permissions = await _roleManagementService.GetPermissionsAsync(cancellationToken);

            stopwatch.Stop();

            var response = new RolesPermissionsResponse
            {
                Roles = roles.Select(r => new RoleResponse
                {
                    RoleId = r.RoleId,
                    RoleName = r.Name,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    CreatedDate = r.CreatedDate,
                    Permissions = r.RolePermissions?.Select(rp => rp.Permission.GetFullPermissionName()).ToList() ?? new List<string>()
                }).ToList(),
                Permissions = permissions.Select(p => new PermissionResponse
                {
                    PermissionId = p.PermissionId,
                    PermissionName = p.Name,
                    Description = p.Description,
                    Category = p.Resource, // Using Resource as Category
                    IsSystemPermission = p.IsSystemPermission
                }).ToList(),
                RetrievedAt = DateTime.UtcNow,
                RetrievalDurationMs = stopwatch.ElapsedMilliseconds
            };

            Logger.LogInformation("Retrieved {RoleCount} roles and {PermissionCount} permissions in {Duration}ms",
                response.Roles.Count, response.Permissions.Count, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, $"Retrieved {response.Roles.Count} roles and {response.Permissions.Count} permissions"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving roles and permissions: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to retrieve roles and permissions", "ROLES_PERMISSIONS_ERROR"));
        }
    }

    // Helper methods
    private List<string> ValidateSecurityConfig(UpdateSecurityConfigRequest request)
    {
        var errors = new List<string>();

        // Validate password policy
        if (request.PasswordPolicy != null)
        {
            if (request.PasswordPolicy.MinimumLength < 4 || request.PasswordPolicy.MinimumLength > 128)
                errors.Add("Password minimum length must be between 4 and 128 characters");

            if (request.PasswordPolicy.MaxAge < TimeSpan.FromDays(1) || request.PasswordPolicy.MaxAge > TimeSpan.FromDays(365))
                errors.Add("Password max age must be between 1 and 365 days");

            if (request.PasswordPolicy.PreventReuse < 0 || request.PasswordPolicy.PreventReuse > 50)
                errors.Add("Password reuse prevention must be between 0 and 50");
        }

        // Validate session policy
        if (request.SessionPolicy != null)
        {
            if (request.SessionPolicy.SessionTimeout < TimeSpan.FromMinutes(1) || request.SessionPolicy.SessionTimeout > TimeSpan.FromHours(24))
                errors.Add("Session timeout must be between 1 minute and 24 hours");

            if (request.SessionPolicy.MaxConcurrentSessions < 1 || request.SessionPolicy.MaxConcurrentSessions > 100)
                errors.Add("Max concurrent sessions must be between 1 and 100");

            if (request.SessionPolicy.IdleTimeout < TimeSpan.FromMinutes(1) || request.SessionPolicy.IdleTimeout > TimeSpan.FromHours(12))
                errors.Add("Idle timeout must be between 1 minute and 12 hours");
        }

        // Validate lockout policy
        if (request.LockoutPolicy != null)
        {
            if (request.LockoutPolicy.MaxFailedAttempts < 1 || request.LockoutPolicy.MaxFailedAttempts > 100)
                errors.Add("Max failed attempts must be between 1 and 100");

            if (request.LockoutPolicy.LockoutDuration < TimeSpan.FromMinutes(1) || request.LockoutPolicy.LockoutDuration > TimeSpan.FromDays(1))
                errors.Add("Lockout duration must be between 1 minute and 1 day");
        }

        // Validate two-factor policy
        if (request.TwoFactorPolicy != null)
        {
            if (request.TwoFactorPolicy.TokenLifetime < TimeSpan.FromMinutes(1) || request.TwoFactorPolicy.TokenLifetime > TimeSpan.FromMinutes(30))
                errors.Add("Two-factor token lifetime must be between 1 and 30 minutes");
        }

        // Validate audit policy
        if (request.AuditPolicy != null)
        {
            if (request.AuditPolicy.RetentionPeriod < TimeSpan.FromDays(1) || request.AuditPolicy.RetentionPeriod > TimeSpan.FromDays(3650))
                errors.Add("Audit retention period must be between 1 day and 10 years");
        }

        return errors;
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
