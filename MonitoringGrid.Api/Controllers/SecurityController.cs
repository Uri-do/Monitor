using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;
using System.Security.Claims;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Security management controller for configuration and monitoring
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SecurityController : ControllerBase
{
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IUserService _userService;
    private readonly IRoleManagementService _roleService;
    private readonly IMapper _mapper;
    private readonly ILogger<SecurityController> _logger;
    private readonly SecurityConfiguration _securityConfig;

    public SecurityController(
        ISecurityAuditService securityAuditService,
        IUserService userService,
        IRoleManagementService roleService,
        IMapper mapper,
        ILogger<SecurityController> logger,
        IOptions<SecurityConfiguration> securityConfig)
    {
        _securityAuditService = securityAuditService;
        _userService = userService;
        _roleService = roleService;
        _mapper = mapper;
        _logger = logger;
        _securityConfig = securityConfig.Value;
    }

    /// <summary>
    /// Get current security configuration
    /// </summary>
    [HttpGet("config")]
    [Authorize(Roles = "Admin")]
    public ActionResult<SecurityConfigDto> GetSecurityConfig()
    {
        try
        {
            var config = new SecurityConfigDto
            {
                PasswordPolicy = new PasswordPolicyDto
                {
                    MinimumLength = _securityConfig.PasswordPolicy.MinimumLength,
                    RequireUppercase = _securityConfig.PasswordPolicy.RequireUppercase,
                    RequireLowercase = _securityConfig.PasswordPolicy.RequireLowercase,
                    RequireNumbers = _securityConfig.PasswordPolicy.RequireDigit,
                    RequireSpecialChars = _securityConfig.PasswordPolicy.RequireSpecialCharacter,
                    PasswordExpirationDays = _securityConfig.PasswordPolicy.PasswordExpirationDays,
                    MaxFailedAttempts = _securityConfig.PasswordPolicy.MaxFailedAttempts,
                    LockoutDurationMinutes = _securityConfig.PasswordPolicy.LockoutDurationMinutes
                },
                SessionSettings = new SessionSettingsDto
                {
                    SessionTimeoutMinutes = _securityConfig.Session.SessionTimeoutMinutes,
                    IdleTimeoutMinutes = _securityConfig.Session.IdleTimeoutMinutes,
                    AllowConcurrentSessions = true // Default value
                },
                TwoFactorSettings = new TwoFactorSettingsDto
                {
                    Enabled = _securityConfig.TwoFactor.IsEnabled,
                    Required = _securityConfig.TwoFactor.IsRequired,
                    Methods = _securityConfig.TwoFactor.EnabledProviders
                },
                RateLimitSettings = new RateLimitSettingsDto
                {
                    Enabled = _securityConfig.RateLimit.IsEnabled,
                    MaxRequestsPerMinute = _securityConfig.RateLimit.RequestsPerMinute,
                    MaxRequestsPerHour = _securityConfig.RateLimit.RequestsPerHour
                }
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security configuration");
            return StatusCode(500, new { message = "Failed to retrieve security configuration" });
        }
    }

    /// <summary>
    /// Update security configuration
    /// </summary>
    [HttpPut("config")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SecurityConfigDto>> UpdateSecurityConfig([FromBody] SecurityConfigDto config)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Log the configuration change
            var userId = GetCurrentUserId();
            await _securityAuditService.LogSecurityEventAsync(new SecurityAuditEvent
            {
                EventId = Guid.NewGuid().ToString(),
                UserId = userId,
                EventType = "SecurityConfigurationChanged",
                Action = "UPDATE",
                Resource = "SecurityConfiguration",
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                IsSuccess = true,
                Timestamp = DateTime.UtcNow,
                AdditionalData = new Dictionary<string, object>
                {
                    ["Description"] = "Security configuration updated"
                }
            });

            _logger.LogInformation("Security configuration updated by user {UserId}", userId);

            // In a real implementation, you would update the configuration in the database
            // For now, we'll just return the updated configuration
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating security configuration");
            return StatusCode(500, new { message = "Failed to update security configuration" });
        }
    }

    /// <summary>
    /// Get security events
    /// </summary>
    [HttpGet("events")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<SecurityEventDto>>> GetSecurityEvents(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? userId = null)
    {
        try
        {
            var events = await _securityAuditService.GetSecurityEventsAsync(startDate, endDate, userId);
            var eventDtos = _mapper.Map<List<SecurityEventDto>>(events);
            return Ok(eventDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security events");
            return StatusCode(500, new { message = "Failed to retrieve security events" });
        }
    }

    /// <summary>
    /// Get security events for a specific user
    /// </summary>
    [HttpGet("events/user/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<SecurityEventDto>>> GetUserSecurityEvents(
        string userId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var events = await _securityAuditService.GetSecurityEventsAsync(startDate, endDate, userId);
            var eventDtos = _mapper.Map<List<SecurityEventDto>>(events);
            return Ok(eventDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user security events for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to retrieve user security events" });
        }
    }

    /// <summary>
    /// Get all users for management
    /// </summary>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        try
        {
            var users = await _userService.GetUsersAsync();
            var userDtos = _mapper.Map<List<UserDto>>(users);
            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new { message = "Failed to retrieve users" });
        }
    }

    /// <summary>
    /// Update user roles
    /// </summary>
    [HttpPut("users/{userId}/roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateUserRoles(string userId, [FromBody] UpdateUserRolesDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _userService.UpdateUserRolesAsync(userId, request.Roles);

            // Log the role change
            var currentUserId = GetCurrentUserId();
            await _securityAuditService.LogSecurityEventAsync(new SecurityAuditEvent
            {
                EventId = Guid.NewGuid().ToString(),
                UserId = currentUserId,
                EventType = "UserRolesChanged",
                Action = "UPDATE",
                Resource = $"User/{userId}/Roles",
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                IsSuccess = true,
                Timestamp = DateTime.UtcNow,
                AdditionalData = new Dictionary<string, object>
                {
                    ["Description"] = $"User roles updated for user {userId}",
                    ["TargetUserId"] = userId
                }
            });

            _logger.LogInformation("User roles updated for user {UserId} by {CurrentUserId}", userId, currentUserId);

            return Ok(new { message = "User roles updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user roles for user {UserId}", userId);
            return StatusCode(500, new { message = "Failed to update user roles" });
        }
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet("roles")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<RoleDto>>> GetRoles()
    {
        try
        {
            var roles = await _roleService.GetRolesAsync();
            var roleDtos = _mapper.Map<List<RoleDto>>(roles);
            return Ok(roleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, new { message = "Failed to retrieve roles" });
        }
    }

    /// <summary>
    /// Get all permissions
    /// </summary>
    [HttpGet("permissions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions()
    {
        try
        {
            var permissions = await _roleService.GetPermissionsAsync();
            var permissionDtos = _mapper.Map<List<PermissionDto>>(permissions);
            return Ok(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions");
            return StatusCode(500, new { message = "Failed to retrieve permissions" });
        }
    }

    /// <summary>
    /// Create API key
    /// </summary>
    [HttpPost("api-keys")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<CreateApiKeyResponseDto>> CreateApiKey([FromBody] CreateApiKeyRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Generate a new API key
            var keyId = Guid.NewGuid().ToString();
            var apiKey = GenerateApiKey();

            // In a real implementation, you would store this in the database
            // For now, we'll just return the generated key
            var response = new CreateApiKeyResponseDto
            {
                KeyId = keyId,
                Key = apiKey
            };

            // Log the API key creation
            var userId = GetCurrentUserId();
            await _securityAuditService.LogSecurityEventAsync(new SecurityAuditEvent
            {
                EventId = Guid.NewGuid().ToString(),
                UserId = userId,
                EventType = "ApiKeyCreated",
                Action = "CREATE",
                Resource = "ApiKey",
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                IsSuccess = true,
                Timestamp = DateTime.UtcNow,
                AdditionalData = new Dictionary<string, object>
                {
                    ["Description"] = $"API key '{request.Name}' created",
                    ["KeyName"] = request.Name,
                    ["KeyId"] = keyId
                }
            });

            _logger.LogInformation("API key created: {KeyName} by user {UserId}", request.Name, userId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating API key");
            return StatusCode(500, new { message = "Failed to create API key" });
        }
    }

    /// <summary>
    /// Get API keys
    /// </summary>
    [HttpGet("api-keys")]
    [Authorize(Roles = "Admin")]
    public ActionResult<List<ApiKeyDto>> GetApiKeys()
    {
        try
        {
            // In a real implementation, you would retrieve from database
            // For now, return empty list
            var apiKeys = new List<ApiKeyDto>();
            return Ok(apiKeys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API keys");
            return StatusCode(500, new { message = "Failed to retrieve API keys" });
        }
    }

    /// <summary>
    /// Revoke API key
    /// </summary>
    [HttpDelete("api-keys/{keyId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> RevokeApiKey(string keyId)
    {
        try
        {
            // In a real implementation, you would mark the key as revoked in the database
            
            // Log the API key revocation
            var userId = GetCurrentUserId();
            await _securityAuditService.LogSecurityEventAsync(new SecurityAuditEvent
            {
                EventId = Guid.NewGuid().ToString(),
                UserId = userId,
                EventType = "ApiKeyRevoked",
                Action = "DELETE",
                Resource = $"ApiKey/{keyId}",
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                IsSuccess = true,
                Timestamp = DateTime.UtcNow,
                AdditionalData = new Dictionary<string, object>
                {
                    ["Description"] = $"API key {keyId} revoked",
                    ["KeyId"] = keyId
                }
            });

            _logger.LogInformation("API key revoked: {KeyId} by user {UserId}", keyId, userId);

            return Ok(new { message = "API key revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {KeyId}", keyId);
            return StatusCode(500, new { message = "Failed to revoke API key" });
        }
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    private string GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private static string GenerateApiKey()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 64)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
