using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MonitoringGrid.Api.DTOs;
using ApiKeyService = MonitoringGrid.Api.Authentication.IApiKeyService;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Api.Observability;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;
using System.Security.Claims;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Comprehensive security management controller for authentication, authorization, API keys, and security monitoring
/// </summary>
[ApiController]
[Route("api/security")]
[Authorize]
[Produces("application/json")]
[PerformanceMonitor(slowThresholdMs: 2000)]
public class SecurityController : ControllerBase
{
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IUserService _userService;
    private readonly IRoleManagementService _roleService;
    private readonly ApiKeyService _apiKeyService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IMapper _mapper;
    private readonly ILogger<SecurityController> _logger;
    private readonly SecurityConfiguration _securityConfig;

    public SecurityController(
        ISecurityAuditService securityAuditService,
        IUserService userService,
        IRoleManagementService roleService,
        ApiKeyService apiKeyService,
        IAuthenticationService authenticationService,
        IMapper mapper,
        ILogger<SecurityController> logger,
        IOptions<SecurityConfiguration> securityConfig)
    {
        _securityAuditService = securityAuditService;
        _userService = userService;
        _roleService = roleService;
        _apiKeyService = apiKeyService;
        _authenticationService = authenticationService;
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
    /// Authenticate user and return JWT token
    /// </summary>
    [HttpPost("auth/login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ipAddress = GetClientIpAddress();
            var loginRequest = _mapper.Map<LoginRequest>(request);

            var response = await _authenticationService.AuthenticateAsync(loginRequest, ipAddress);

            var loginResponse = new LoginResponseDto
            {
                IsSuccess = response.IsSuccess,
                ErrorMessage = response.ErrorMessage,
                RequiresTwoFactor = response.RequiresTwoFactor,
                RequiresPasswordChange = response.RequiresPasswordChange
            };

            if (response.IsSuccess && response.Token != null)
            {
                loginResponse.Token = _mapper.Map<JwtTokenDto>(response.Token);
                loginResponse.User = _mapper.Map<UserDto>(response.User);
            }

            return Ok(loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user {Username}: {Message}", request.Username, ex.Message);
            return StatusCode(500, new LoginResponseDto
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred during authentication. Please try again."
            });
        }
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("auth/register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponseDto<UserDto>>> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createUserRequest = new CreateUserRequest
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var user = await _userService.CreateUserAsync(createUserRequest);
            var userDto = _mapper.Map<UserDto>(user);

            _logger.LogInformation("New user registered: {Username} ({Email})", request.Username, request.Email);

            return Ok(new ApiResponseDto<UserDto>
            {
                IsSuccess = true,
                Message = "User registered successfully",
                Data = userDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Username}: {Message}", request.Username, ex.Message);
            return StatusCode(500, new ApiResponseDto<UserDto>
            {
                IsSuccess = false,
                Message = "An error occurred during registration. Please try again.",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// Get current user profile information
    /// </summary>
    [HttpGet("auth/profile")]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            var user = await _userService.GetUserByIdAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var userDto = _mapper.Map<UserDto>(user);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Refresh JWT token using refresh token
    /// </summary>
    [HttpPost("auth/refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<JwtTokenDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var newToken = await _authenticationService.RefreshTokenAsync(request.RefreshToken);
            var tokenDto = _mapper.Map<JwtTokenDto>(newToken);

            return Ok(tokenDto);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new { message = "An error occurred while refreshing the token" });
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
}
