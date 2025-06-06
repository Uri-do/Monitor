using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;
using System.Security.Claims;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Authentication controller for login, registration, and token management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authenticationService,
        IUserService userService,
        IMapper mapper,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _userService = userService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    [HttpPost("login")]
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

            if (!response.IsSuccess)
            {
                _logger.LogWarning("Failed login attempt for user {Username} from {IpAddress}: {Error}", 
                    request.Username, ipAddress, response.ErrorMessage);
                return Unauthorized(loginResponse);
            }

            _logger.LogInformation("User {Username} logged in successfully from {IpAddress}", 
                request.Username, ipAddress);

            return Ok(loginResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}: {Message}", request.Username, ex.Message);
            return StatusCode(500, new LoginResponseDto
            {
                IsSuccess = false,
                ErrorMessage = "An error occurred during authentication. Please try again."
            });
        }
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponseDto<UserDto>>> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponseDto<UserDto>
                {
                    IsSuccess = false,
                    Message = "Invalid request data",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });

            // Check if username is available
            if (!await _userService.IsUsernameAvailableAsync(request.Username))
            {
                return BadRequest(new ApiResponseDto<UserDto>
                {
                    IsSuccess = false,
                    Message = "Username is already taken",
                    Errors = new List<string> { $"Username '{request.Username}' is already registered" }
                });
            }

            // Check if email is available
            if (!await _userService.IsEmailAvailableAsync(request.Email))
            {
                return BadRequest(new ApiResponseDto<UserDto>
                {
                    IsSuccess = false,
                    Message = "Email is already registered",
                    Errors = new List<string> { $"Email '{request.Email}' is already registered" }
                });
            }

            // Create user
            var createUserRequest = new CreateUserRequest
            {
                Username = request.Username,
                Email = request.Email,
                DisplayName = request.DisplayName,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Department = request.Department,
                Title = request.Title,
                Password = request.Password,
                RoleIds = new List<string> { "role-viewer" }, // Default role
                IsActive = true,
                EmailConfirmed = false,
                CreatedBy = "SYSTEM"
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
    /// Refresh JWT token using refresh token
    /// </summary>
    [HttpPost("refresh")]
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
            _logger.LogError(ex, "Error during token refresh: {Message}", ex.Message);
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Logout user and revoke token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var token = GetTokenFromHeader();
            if (!string.IsNullOrEmpty(token))
            {
                await _authenticationService.RevokeTokenAsync(token);
            }

            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} logged out", userId);

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout: {Message}", ex.Message);
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var changePasswordRequest = new ChangePasswordRequest
            {
                CurrentPassword = request.CurrentPassword,
                NewPassword = request.NewPassword,
                ConfirmPassword = request.ConfirmPassword
            };

            var success = await _authenticationService.ChangePasswordAsync(userId, changePasswordRequest);

            if (!success)
            {
                return BadRequest(new { message = "Failed to change password. Please check your current password." });
            }

            _logger.LogInformation("Password changed for user {UserId}", userId);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {Message}", ex.Message);
            return StatusCode(500, new { message = "An error occurred while changing password" });
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            var userDto = _mapper.Map<UserDto>(user);
            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile: {Message}", ex.Message);
            return StatusCode(500, new { message = "An error occurred while retrieving profile" });
        }
    }

    /// <summary>
    /// Validate token
    /// </summary>
    [HttpPost("validate")]
    [AllowAnonymous]
    public async Task<ActionResult> ValidateToken([FromBody] string token)
    {
        try
        {
            var isValid = await _authenticationService.ValidateTokenAsync(token);
            return Ok(new { isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token: {Message}", ex.Message);
            return Ok(new { isValid = false });
        }
    }

    #region Helper Methods

    private string GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        // Check for forwarded IP (when behind proxy/load balancer)
        if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }
        else if (HttpContext.Request.Headers.ContainsKey("X-Real-IP"))
        {
            ipAddress = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        }

        return ipAddress ?? "Unknown";
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string? GetTokenFromHeader()
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }
        return null;
    }

    #endregion
}
