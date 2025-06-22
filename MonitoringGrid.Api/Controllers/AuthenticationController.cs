using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.DTOs.Security;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Interfaces.Security;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using AutoMapper;
using MediatR;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Controller for authentication operations (login, register, profile, token management)
/// </summary>
[ApiController]
[Route("api/security/auth")]
[Produces("application/json")]
public class AuthenticationController : BaseApiController
{
    private readonly ISecurityService _securityService;
    private readonly IUserService _userService;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IMapper _mapper;
    private readonly SecurityConfiguration _securityConfig;

    public AuthenticationController(
        IMediator mediator,
        ISecurityService securityService,
        IUserService userService,
        ISecurityAuditService securityAuditService,
        IMapper mapper,
        ILogger<AuthenticationController> logger,
        IOptions<SecurityConfiguration> securityConfig)
        : base(mediator, logger)
    {
        _securityService = securityService;
        _userService = userService;
        _securityAuditService = securityAuditService;
        _mapper = mapper;
        _securityConfig = securityConfig.Value;
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    /// <param name="request">Login request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced login response with security tracking</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MonitoringGrid.Api.DTOs.Security.LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MonitoringGrid.Api.DTOs.Security.LoginResponse>> Login(
        [FromBody] MonitoringGrid.Api.DTOs.Security.LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Login attempt for user {Username} from IP {IpAddress}", request.Username, ipAddress);

            // Create enhanced login request
            var loginRequest = new Core.Security.LoginRequest
            {
                Username = request.Username,
                Password = request.Password,
                RememberMe = request.RememberMe,
                TwoFactorCode = request.TwoFactorCode
            };

            var authResponse = await _securityService.AuthenticateAsync(loginRequest, ipAddress, cancellationToken);

            stopwatch.Stop();

            var response = new MonitoringGrid.Api.DTOs.Security.LoginResponse
            {
                IsSuccess = authResponse.IsSuccess,
                ErrorMessage = authResponse.Error?.Message ?? "Unknown error",
                RequiresTwoFactor = false,
                RequiresPasswordChange = false,
                LoginAttemptTime = DateTime.UtcNow,
                LoginDurationMs = stopwatch.ElapsedMilliseconds,
                IpAddress = ipAddress,
                Details = new Dictionary<string, object>
                {
                    ["UserAgent"] = userAgent,
                    ["RememberMe"] = request.RememberMe,
                    ["TwoFactorProvided"] = !string.IsNullOrEmpty(request.TwoFactorCode)
                }
            };

            if (authResponse.IsSuccess && authResponse.Value != null)
            {
                response.Token = new JwtTokenResponse
                {
                    AccessToken = authResponse.Value?.Token?.AccessToken ?? string.Empty,
                    RefreshToken = authResponse.Value?.Token?.RefreshToken ?? string.Empty,
                    TokenType = "Bearer",
                    ExpiresAt = authResponse.Value?.Token?.ExpiresAt ?? DateTime.UtcNow.AddHours(1),
                    ExpiresIn = (int)((authResponse.Value?.Token?.ExpiresAt ?? DateTime.UtcNow.AddHours(1)) - DateTime.UtcNow).TotalSeconds,
                    Scope = string.Empty
                };

                // Extract roles from the authenticated user
                var userRoles = authResponse.Value?.User?.UserRoles?.Select(ur => ur.Role).ToList() ?? new List<MonitoringGrid.Core.Entities.Role>();
                var userPermissions = userRoles.SelectMany(r => r.RolePermissions?.Select(rp => rp.Permission) ?? new List<MonitoringGrid.Core.Entities.Permission>()).Distinct().ToList();

                response.User = MapToUserResponse(authResponse.Value?.User, true, userRoles, userPermissions);

                // Log successful login
                await LogSecurityEventAsync("UserLogin", "LOGIN", $"User/{authResponse.Value?.User?.UserId}",
                    true, authResponse.Value?.User?.UserId, "Successful user login", cancellationToken);

                Logger.LogInformation("Successful login for user {Username} from IP {IpAddress} in {Duration}ms",
                    request.Username, ipAddress, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                // Log failed login attempt
                await LogSecurityEventAsync("UserLoginFailed", "LOGIN", $"User/{request.Username}",
                    false, null, $"Failed login attempt: {authResponse.Error?.Message ?? "Unknown error"}", cancellationToken);

                Logger.LogWarning("Failed login attempt for user {Username} from IP {IpAddress}: {Error}",
                    request.Username, ipAddress, authResponse.Error?.Message ?? "Unknown error");

                if (!authResponse.IsSuccess)
                {
                    return Unauthorized(CreateErrorResponse(authResponse.Error?.Message ?? "Authentication failed", "AUTHENTICATION_FAILED"));
                }
            }

            return Ok(CreateSuccessResponse(response, authResponse.IsSuccess ? "Login successful" : "Additional authentication required"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Login operation was cancelled for user {Username}", request.Username);
            return StatusCode(499, CreateErrorResponse("Login operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error during authentication for user {Username} from IP {IpAddress}: {Message}",
                request.Username, ipAddress, ex.Message);

            // Log security event for login error
            await LogSecurityEventAsync("UserLoginError", "LOGIN", $"User/{request.Username}",
                false, null, $"Login error: {ex.Message}", cancellationToken);

            return StatusCode(500, CreateErrorResponse("An error occurred during authentication. Please try again.", "AUTHENTICATION_ERROR"));
        }
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced registration response with security tracking</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Registration attempt for user {Username} ({Email}) from IP {IpAddress}",
                request.Username, request.Email, ipAddress);

            // Additional validation
            if (request.Password != request.ConfirmPassword)
            {
                return BadRequest(CreateErrorResponse("Password and confirm password do not match", "PASSWORD_MISMATCH"));
            }

            var createUserRequest = new CreateUserRequest
            {
                Username = request.Username,
                Email = request.Email,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var user = await _userService.CreateUserAsync(createUserRequest, cancellationToken);

            stopwatch.Stop();

            var userResponse = MapToUserResponse(user, true);

            var response = new RegisterResponse
            {
                IsSuccess = true,
                Message = "User registered successfully",
                User = userResponse,
                RegistrationTime = DateTime.UtcNow,
                RegistrationDurationMs = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, object>
                {
                    ["IpAddress"] = ipAddress,
                    ["UserAgent"] = userAgent,
                    ["PasswordStrength"] = CalculatePasswordStrength(request.Password),
                    ["AccountCreated"] = user.CreatedDate
                }
            };

            // Log successful registration
            await LogSecurityEventAsync("UserRegistration", "CREATE", $"User/{user.UserId}",
                true, user.UserId, "New user registration", cancellationToken);

            Logger.LogInformation("New user registered: {Username} ({Email}) from IP {IpAddress} in {Duration}ms",
                request.Username, request.Email, ipAddress, stopwatch.ElapsedMilliseconds);

            return CreatedAtAction(nameof(GetProfile), null, CreateSuccessResponse(response, response.Message));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Registration operation was cancelled for user {Username}", request.Username);
            return StatusCode(499, CreateErrorResponse("Registration operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            stopwatch.Stop();
            Logger.LogWarning("Registration failed for user {Username}: {Error}", request.Username, ex.Message);

            // Log failed registration attempt
            await LogSecurityEventAsync("UserRegistrationFailed", "CREATE", $"User/{request.Username}",
                false, null, $"Registration failed: {ex.Message}", cancellationToken);

            var response = new RegisterResponse
            {
                IsSuccess = false,
                Message = "Registration failed",
                RegistrationTime = DateTime.UtcNow,
                RegistrationDurationMs = stopwatch.ElapsedMilliseconds,
                Errors = new List<string> { ex.Message }
            };

            return BadRequest(CreateErrorResponse("User already exists", "USER_ALREADY_EXISTS"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error during registration for user {Username} from IP {IpAddress}: {Message}",
                request.Username, ipAddress, ex.Message);

            // Log security event for registration error
            await LogSecurityEventAsync("UserRegistrationError", "CREATE", $"User/{request.Username}",
                false, null, $"Registration error: {ex.Message}", cancellationToken);

            var response = new RegisterResponse
            {
                IsSuccess = false,
                Message = "An error occurred during registration. Please try again.",
                RegistrationTime = DateTime.UtcNow,
                RegistrationDurationMs = stopwatch.ElapsedMilliseconds,
                Errors = new List<string> { ex.Message }
            };

            return StatusCode(500, CreateErrorResponse("Registration failed", "REGISTRATION_ERROR"));
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User profile information</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetProfile(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(CreateErrorResponse("User not authenticated", "NOT_AUTHENTICATED"));
            }

            var user = await _userService.GetUserByIdAsync(currentUserId, cancellationToken);
            if (user == null)
            {
                return NotFound(CreateErrorResponse("User not found", "USER_NOT_FOUND"));
            }

            var userResponse = MapToUserResponse(user, true);
            return Ok(CreateSuccessResponse(userResponse, "Profile retrieved successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error retrieving profile for user {UserId}: {Message}", GetCurrentUserId(), ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to retrieve profile", "PROFILE_ERROR"));
        }
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    /// <param name="request">Token refresh request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New JWT token</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JwtTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<JwtTokenResponse>> RefreshToken(
        [FromBody] MonitoringGrid.Api.DTOs.Security.RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            var result = await _securityService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                return Unauthorized(CreateErrorResponse("Invalid refresh token", "INVALID_REFRESH_TOKEN"));
            }

            var response = new JwtTokenResponse
            {
                AccessToken = result.Value.AccessToken,
                RefreshToken = result.Value.RefreshToken,
                TokenType = "Bearer",
                ExpiresAt = result.Value.ExpiresAt,
                ExpiresIn = (int)(result.Value.ExpiresAt - DateTime.UtcNow).TotalSeconds,
                Scope = string.Empty
            };

            return Ok(CreateSuccessResponse(response, "Token refreshed successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing token: {Message}", ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to refresh token", "TOKEN_REFRESH_ERROR"));
        }
    }

    /// <summary>
    /// Logout user and invalidate token
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout confirmation</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // Log logout event
            await LogSecurityEventAsync("UserLogout", "LOGOUT", $"User/{currentUserId}",
                true, currentUserId, "User logout", cancellationToken);

            Logger.LogInformation("User {UserId} logged out from IP {IpAddress}", currentUserId, GetClientIpAddress());

            return Ok(CreateSuccessResponse(new { message = "Logged out successfully" }, "Logout successful"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during logout for user {UserId}: {Message}", GetCurrentUserId(), ex.Message);
            return StatusCode(500, CreateErrorResponse("Failed to logout", "LOGOUT_ERROR"));
        }
    }

    // Helper methods
    private UserResponse MapToUserResponse(dynamic user, bool includeDetails = false, IEnumerable<MonitoringGrid.Core.Entities.Role>? userRoles = null, IEnumerable<MonitoringGrid.Core.Entities.Permission>? userPermissions = null)
    {
        // Placeholder implementation - will be moved from SecurityController
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
            Roles = userRoles?.Select(r => r.Name).ToList() ?? new List<string>(),
            Permissions = userPermissions?.Select(p => p.Name).ToList() ?? new List<string>()
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
