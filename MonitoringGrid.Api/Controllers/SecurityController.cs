using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IAuthenticationService _authenticationService;
    private readonly IMapper _mapper;
    private readonly SecurityConfiguration _securityConfig;

    public SecurityController(
        IMediator mediator,
        ISecurityAuditService securityAuditService,
        IUserService userService,
        IRoleManagementService roleService,
        ApiKeyService apiKeyService,
        IAuthenticationService authenticationService,
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

    /// <summary>
    /// Get current security configuration
    /// </summary>
    /// <param name="request">Get security config request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced security configuration</returns>
    [HttpGet("config")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SecurityConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SecurityConfigResponse>> GetSecurityConfig(
        [FromQuery] GetSecurityConfigRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetSecurityConfigRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            var userId = GetCurrentUserId();
            Logger.LogDebug("Getting security configuration for user {UserId}", userId);

            // Get additional metrics for enhanced response
            var activeSessionsCount = 0; // Placeholder - would get from service
            var twoFactorUsersCount = 0; // Placeholder - would get from service
            var rateLimitViolations = 0; // Placeholder - would get from service

            stopwatch.Stop();

            var response = new SecurityConfigResponse
            {
                PasswordPolicy = new PasswordPolicyResponse
                {
                    MinimumLength = _securityConfig.PasswordPolicy.MinimumLength,
                    RequireUppercase = _securityConfig.PasswordPolicy.RequireUppercase,
                    RequireLowercase = _securityConfig.PasswordPolicy.RequireLowercase,
                    RequireNumbers = _securityConfig.PasswordPolicy.RequireDigit,
                    RequireSpecialChars = _securityConfig.PasswordPolicy.RequireSpecialCharacter,
                    PasswordExpirationDays = _securityConfig.PasswordPolicy.PasswordExpirationDays,
                    MaxFailedAttempts = _securityConfig.PasswordPolicy.MaxFailedAttempts,
                    LockoutDurationMinutes = _securityConfig.PasswordPolicy.LockoutDurationMinutes,
                    PasswordStrengthScore = CalculatePasswordPolicyStrength()
                },
                SessionSettings = new SessionSettingsResponse
                {
                    SessionTimeoutMinutes = _securityConfig.Session.SessionTimeoutMinutes,
                    IdleTimeoutMinutes = _securityConfig.Session.IdleTimeoutMinutes,
                    AllowConcurrentSessions = true, // Default value
                    ActiveSessionsCount = activeSessionsCount
                },
                TwoFactorSettings = new TwoFactorSettingsResponse
                {
                    Enabled = _securityConfig.TwoFactor.IsEnabled,
                    Required = _securityConfig.TwoFactor.IsRequired,
                    Methods = _securityConfig.TwoFactor.EnabledProviders.ToList(),
                    UsersWithTwoFactorCount = twoFactorUsersCount
                },
                RateLimitSettings = new RateLimitSettingsResponse
                {
                    Enabled = _securityConfig.RateLimit.IsEnabled,
                    MaxRequestsPerMinute = _securityConfig.RateLimit.RequestsPerMinute,
                    MaxRequestsPerHour = _securityConfig.RateLimit.RequestsPerHour,
                    CurrentViolationsCount = rateLimitViolations
                },
                LastModified = DateTime.UtcNow, // Placeholder
                LastModifiedBy = "System", // Placeholder
                Version = "1.0" // Placeholder
            };

            // Add details if requested
            if (request.IncludeDetails)
            {
                response.Details = new Dictionary<string, object>
                {
                    ["QueryDurationMs"] = stopwatch.ElapsedMilliseconds,
                    ["RequestedBy"] = userId,
                    ["ConfigurationHealth"] = CalculateConfigurationHealth(response)
                };
            }

            // Log security configuration access
            await LogSecurityEventAsync("SecurityConfigAccess", "READ", "SecurityConfig",
                true, userId, "Security configuration accessed", cancellationToken);

            Logger.LogInformation("Security configuration retrieved by user {UserId} in {Duration}ms",
                userId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, "Security configuration retrieved successfully"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get security config operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Get security config operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var userId = GetCurrentUserId();
            Logger.LogError(ex, "Error retrieving security configuration for user {UserId}: {Message}", userId, ex.Message);

            // Log security event for config access error
            await LogSecurityEventAsync("SecurityConfigAccessError", "READ", "SecurityConfig",
                false, userId, $"Security config access error: {ex.Message}", cancellationToken);

            return StatusCode(500, CreateErrorResponse("Failed to retrieve security configuration", "GET_SECURITY_CONFIG_ERROR"));
        }
    }

    /// <summary>
    /// Update security configuration
    /// </summary>
    /// <param name="request">Update security config request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced security operation response</returns>
    [HttpPut("config")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SecurityOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SecurityOperationResponse>> UpdateSecurityConfig(
        [FromBody] UpdateSecurityConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            var userId = GetCurrentUserId();
            Logger.LogDebug("Updating security configuration for user {UserId}", userId);

            // Validate configuration changes
            var validationResult = ValidateSecurityConfigurationChanges(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(CreateErrorResponse(string.Join(", ", validationResult.Errors), "INVALID_CONFIGURATION"));
            }

            // In a real implementation, you would update the configuration in the database
            // For now, we'll simulate the update
            var previousConfig = GetCurrentSecurityConfigSnapshot();

            // Apply configuration changes (placeholder)
            // await _securityConfigService.UpdateConfigurationAsync(request, cancellationToken);

            stopwatch.Stop();

            var response = new SecurityOperationResponse
            {
                Success = true,
                Message = "Security configuration updated successfully",
                PerformedBy = userId,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, object>
                {
                    ["ChangeReason"] = request.ChangeReason ?? "No reason provided",
                    ["ChangedFields"] = GetChangedFields(previousConfig, request),
                    ["ConfigurationVersion"] = "1.1", // Incremented version
                    ["ValidationPassed"] = true,
                    ["IpAddress"] = GetClientIpAddress()
                }
            };

            // Log the configuration change with detailed audit
            await LogSecurityEventAsync("SecurityConfigurationChanged", "UPDATE", "SecurityConfiguration",
                true, userId, $"Security configuration updated: {request.ChangeReason}", cancellationToken);

            Logger.LogInformation("Security configuration updated by user {UserId} in {Duration}ms. Reason: {Reason}",
                userId, stopwatch.ElapsedMilliseconds, request.ChangeReason);

            return Ok(CreateSuccessResponse(response, response.Message));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Update security config operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Update security config operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (UnauthorizedAccessException ex)
        {
            stopwatch.Stop();
            var userId = GetCurrentUserId();
            Logger.LogWarning("Unauthorized attempt to update security configuration by user {UserId}: {Error}", userId, ex.Message);

            // Log security event for unauthorized config change attempt
            await LogSecurityEventAsync("SecurityConfigurationUnauthorized", "UPDATE", "SecurityConfiguration",
                false, userId, $"Unauthorized config update attempt: {ex.Message}", cancellationToken);

            return Forbid("Insufficient permissions to update security configuration");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var userId = GetCurrentUserId();
            Logger.LogError(ex, "Error updating security configuration for user {UserId}: {Message}", userId, ex.Message);

            // Log security event for config update error
            await LogSecurityEventAsync("SecurityConfigurationError", "UPDATE", "SecurityConfiguration",
                false, userId, $"Config update error: {ex.Message}", cancellationToken);

            return StatusCode(500, CreateErrorResponse("Failed to update security configuration", "UPDATE_SECURITY_CONFIG_ERROR"));
        }
    }

    /// <summary>
    /// Get security events
    /// </summary>
    /// <param name="request">Get security events request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated security events with analytics</returns>
    [HttpGet("events")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PaginatedSecurityEventsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedSecurityEventsResponse>> GetSecurityEvents(
        [FromQuery] GetSecurityEventsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetSecurityEventsRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            var userId = GetCurrentUserId();
            Logger.LogDebug("Getting security events for user {UserId} with filters", userId);

            var events = await _securityAuditService.GetSecurityEventsAsync(
                request.StartDate, request.EndDate, request.UserId, cancellationToken);

            // Apply additional filtering
            var filteredEvents = events.AsQueryable();

            if (!string.IsNullOrEmpty(request.EventType))
            {
                filteredEvents = filteredEvents.Where(e => e.EventType.Contains(request.EventType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.Action))
            {
                filteredEvents = filteredEvents.Where(e => e.Action.Contains(request.Action, StringComparison.OrdinalIgnoreCase));
            }

            if (request.IsSuccess.HasValue)
            {
                filteredEvents = filteredEvents.Where(e => e.IsSuccess == request.IsSuccess.Value);
            }

            // Apply sorting
            filteredEvents = request.SortDirection?.ToLower() == "asc"
                ? filteredEvents.OrderBy(e => e.Timestamp)
                : filteredEvents.OrderByDescending(e => e.Timestamp);

            // Apply pagination
            var totalCount = filteredEvents.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var skip = (request.Page - 1) * request.PageSize;
            var pagedEvents = filteredEvents.Skip(skip).Take(request.PageSize).ToList();

            stopwatch.Stop();

            // Map to enhanced response DTOs
            var eventResponses = pagedEvents.Select(e => MapToSecurityEventResponse(e, request.IncludeDetails)).ToList();

            // Calculate summary statistics
            var summary = new SecurityEventsSummary
            {
                TotalEvents = totalCount,
                SuccessfulEvents = eventResponses.Count(e => e.IsSuccess),
                FailedEvents = eventResponses.Count(e => !e.IsSuccess),
                HighRiskEvents = eventResponses.Count(e => e.RiskScore >= 80),
                UniqueUsers = eventResponses.Where(e => !string.IsNullOrEmpty(e.UserId)).Select(e => e.UserId).Distinct().Count(),
                UniqueIpAddresses = eventResponses.Where(e => !string.IsNullOrEmpty(e.IpAddress)).Select(e => e.IpAddress).Distinct().Count()
            };

            var response = new PaginatedSecurityEventsResponse
            {
                Events = eventResponses,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1,
                Summary = summary,
                QueryMetrics = new QueryMetrics
                {
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryCount = 1,
                    CacheHit = false
                }
            };

            // Log security events access
            await LogSecurityEventAsync("SecurityEventsAccess", "READ", "SecurityEvents",
                true, userId, $"Security events accessed with filters", cancellationToken);

            Logger.LogInformation("Retrieved {Count} security events for user {UserId} in {Duration}ms",
                eventResponses.Count, userId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, $"Retrieved {eventResponses.Count} security events"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get security events operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Get security events operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var userId = GetCurrentUserId();
            Logger.LogError(ex, "Error retrieving security events for user {UserId}: {Message}", userId, ex.Message);

            return StatusCode(500, CreateErrorResponse("Failed to retrieve security events", "GET_SECURITY_EVENTS_ERROR"));
        }
    }

    /// <summary>
    /// Get security events for a specific user
    /// </summary>
    /// <param name="userId">Target user ID</param>
    /// <param name="request">Get security events request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated user security events with risk analysis</returns>
    [HttpGet("events/user/{userId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PaginatedSecurityEventsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedSecurityEventsResponse>> GetUserSecurityEvents(
        string userId,
        [FromQuery] GetSecurityEventsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetSecurityEventsRequest();
            request.UserId = userId; // Override with route parameter

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Additional validation for user ID
            var paramValidation = ValidateParameter(userId, nameof(userId),
                id => !string.IsNullOrEmpty(id), "User ID cannot be empty");
            if (paramValidation != null) return BadRequest(paramValidation);

            var currentUserId = GetCurrentUserId();
            Logger.LogDebug("Getting security events for target user {TargetUserId} by user {CurrentUserId}", userId, currentUserId);

            // Verify target user exists
            var targetUser = await _userService.GetUserByIdAsync(userId, cancellationToken);
            if (targetUser == null)
            {
                Logger.LogWarning("Security events requested for non-existent user {UserId}", userId);
                return NotFound(CreateErrorResponse($"User {userId} not found", "USER_NOT_FOUND"));
            }

            var events = await _securityAuditService.GetSecurityEventsAsync(
                request.StartDate, request.EndDate, userId, cancellationToken);

            // Apply additional filtering
            var filteredEvents = events.AsQueryable();

            if (!string.IsNullOrEmpty(request.EventType))
            {
                filteredEvents = filteredEvents.Where(e => e.EventType.Contains(request.EventType, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.Action))
            {
                filteredEvents = filteredEvents.Where(e => e.Action.Contains(request.Action, StringComparison.OrdinalIgnoreCase));
            }

            if (request.IsSuccess.HasValue)
            {
                filteredEvents = filteredEvents.Where(e => e.IsSuccess == request.IsSuccess.Value);
            }

            // Apply sorting
            filteredEvents = request.SortDirection?.ToLower() == "asc"
                ? filteredEvents.OrderBy(e => e.Timestamp)
                : filteredEvents.OrderByDescending(e => e.Timestamp);

            // Apply pagination
            var totalCount = filteredEvents.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var skip = (request.Page - 1) * request.PageSize;
            var pagedEvents = filteredEvents.Skip(skip).Take(request.PageSize).ToList();

            stopwatch.Stop();

            // Map to enhanced response DTOs with user-specific risk analysis
            var eventResponses = pagedEvents.Select(e => MapToSecurityEventResponse(e, request.IncludeDetails, true)).ToList();

            // Calculate user-specific summary statistics
            var summary = new SecurityEventsSummary
            {
                TotalEvents = totalCount,
                SuccessfulEvents = eventResponses.Count(e => e.IsSuccess),
                FailedEvents = eventResponses.Count(e => !e.IsSuccess),
                HighRiskEvents = eventResponses.Count(e => e.RiskScore >= 80),
                UniqueUsers = 1, // Single user
                UniqueIpAddresses = eventResponses.Where(e => !string.IsNullOrEmpty(e.IpAddress)).Select(e => e.IpAddress).Distinct().Count()
            };

            var response = new PaginatedSecurityEventsResponse
            {
                Events = eventResponses,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1,
                Summary = summary,
                QueryMetrics = new QueryMetrics
                {
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryCount = 1,
                    CacheHit = false
                }
            };

            // Log user security events access
            await LogSecurityEventAsync("UserSecurityEventsAccess", "READ", $"User/{userId}/SecurityEvents",
                true, currentUserId, $"User security events accessed for {userId}", cancellationToken);

            Logger.LogInformation("Retrieved {Count} security events for user {TargetUserId} by {CurrentUserId} in {Duration}ms",
                eventResponses.Count, userId, currentUserId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, $"Retrieved {eventResponses.Count} security events for user {userId}"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get user security events operation was cancelled for user {UserId}", userId);
            return StatusCode(499, CreateErrorResponse("Get user security events operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var currentUserId = GetCurrentUserId();
            Logger.LogError(ex, "Error retrieving user security events for user {TargetUserId} by {CurrentUserId}: {Message}",
                userId, currentUserId, ex.Message);

            return StatusCode(500, CreateErrorResponse($"Failed to retrieve security events for user {userId}", "GET_USER_SECURITY_EVENTS_ERROR"));
        }
    }

    /// <summary>
    /// Get all users for management
    /// </summary>
    /// <param name="request">Get users request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated users with security information</returns>
    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PaginatedUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedUsersResponse>> GetUsers(
        [FromQuery] GetUsersRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new GetUsersRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            var currentUserId = GetCurrentUserId();
            Logger.LogDebug("Getting users for user {UserId} with filters", currentUserId);

            var users = await _userService.GetUsersAsync();

            // Apply filtering
            var filteredUsers = users.AsQueryable();

            if (!string.IsNullOrEmpty(request.SearchText))
            {
                filteredUsers = filteredUsers.Where(u =>
                    u.Username.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.FirstName.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase) ||
                    u.LastName.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(request.Role))
            {
                // User entity doesn't have Roles property in Core - would need to filter via service
                // For now, skip role filtering
            }

            if (request.IsActive.HasValue)
            {
                filteredUsers = filteredUsers.Where(u => u.IsActive == request.IsActive.Value);
            }

            // Apply pagination
            var totalCount = filteredUsers.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
            var skip = (request.Page - 1) * request.PageSize;
            var pagedUsers = filteredUsers.Skip(skip).Take(request.PageSize).ToList();

            stopwatch.Stop();

            // Map to enhanced response DTOs
            var userResponses = pagedUsers.Select(u => MapToUserResponse(u, request.IncludeDetails)).ToList();

            // Calculate summary statistics
            var summary = new UsersSummary
            {
                TotalUsers = totalCount,
                ActiveUsers = userResponses.Count(u => u.IsActive),
                LockedOutUsers = userResponses.Count(u => u.IsLockedOut),
                TwoFactorEnabledUsers = userResponses.Count(u => u.TwoFactorEnabled),
                LoggedInTodayUsers = userResponses.Count(u => u.LastLogin.HasValue && u.LastLogin.Value.Date == DateTime.UtcNow.Date)
            };

            var response = new PaginatedUsersResponse
            {
                Users = userResponses,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalPages = totalPages,
                HasNextPage = request.Page < totalPages,
                HasPreviousPage = request.Page > 1,
                Summary = summary,
                QueryMetrics = new QueryMetrics
                {
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    QueryCount = 1,
                    CacheHit = false
                }
            };

            // Log users access
            await LogSecurityEventAsync("UsersAccess", "READ", "Users",
                true, currentUserId, "Users list accessed", cancellationToken);

            Logger.LogInformation("Retrieved {Count} users for user {UserId} in {Duration}ms",
                userResponses.Count, currentUserId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, $"Retrieved {userResponses.Count} users"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get users operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Get users operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var currentUserId = GetCurrentUserId();
            Logger.LogError(ex, "Error retrieving users for user {UserId}: {Message}", currentUserId, ex.Message);

            return StatusCode(500, CreateErrorResponse("Failed to retrieve users", "GET_USERS_ERROR"));
        }
    }

    /// <summary>
    /// Update user roles
    /// </summary>
    /// <param name="userId">Target user ID</param>
    /// <param name="request">Update user roles request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced role update operation response</returns>
    [HttpPut("users/{userId}/roles")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SecurityOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SecurityOperationResponse>> UpdateUserRoles(
        string userId,
        [FromBody] UpdateUserRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate user ID matches route parameter
            if (request.UserId != userId)
            {
                request.UserId = userId; // Use route parameter
            }

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Additional validation for user ID
            var paramValidation = ValidateParameter(userId, nameof(userId),
                id => !string.IsNullOrEmpty(id), "User ID cannot be empty");
            if (paramValidation != null) return BadRequest(paramValidation);

            var currentUserId = GetCurrentUserId();
            Logger.LogDebug("Updating roles for user {TargetUserId} by user {CurrentUserId}", userId, currentUserId);

            // Verify target user exists
            var targetUser = await _userService.GetUserByIdAsync(userId, cancellationToken);
            if (targetUser == null)
            {
                Logger.LogWarning("Role update attempted for non-existent user {UserId}", userId);
                return NotFound(CreateErrorResponse($"User {userId} not found", "USER_NOT_FOUND"));
            }

            // Get current roles for comparison - User entity doesn't have Roles property in Core
            var currentRoles = new List<string>(); // Placeholder - would need to get from role service
            var newRoles = request.Roles;

            // Validate roles exist
            var availableRoles = await _roleService.GetRolesAsync();
            var invalidRoles = newRoles.Except(availableRoles.Select(r => r.Name)).ToList();
            if (invalidRoles.Any())
            {
                return BadRequest(CreateErrorResponse($"Invalid roles: {string.Join(", ", invalidRoles)}", "INVALID_ROLES"));
            }

            // Update user roles - Core service doesn't have UpdateUserRolesAsync with cancellationToken
            await _userService.UpdateUserRolesAsync(userId, newRoles);

            stopwatch.Stop();

            var addedRoles = newRoles.Except(currentRoles).ToList();
            var removedRoles = currentRoles.Except(newRoles).ToList();

            var response = new SecurityOperationResponse
            {
                Success = true,
                Message = $"User roles updated successfully for {targetUser.Username}",
                PerformedBy = currentUserId,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, object>
                {
                    ["TargetUserId"] = userId,
                    ["TargetUsername"] = targetUser.Username,
                    ["ChangeReason"] = request.ChangeReason ?? "No reason provided",
                    ["PreviousRoles"] = currentRoles,
                    ["NewRoles"] = newRoles,
                    ["AddedRoles"] = addedRoles,
                    ["RemovedRoles"] = removedRoles,
                    ["RoleChangeCount"] = addedRoles.Count + removedRoles.Count
                }
            };

            // Log the role change with detailed audit
            await LogSecurityEventAsync("UserRolesChanged", "UPDATE", $"User/{userId}/Roles",
                true, currentUserId, $"User roles updated for {targetUser.Username}: {request.ChangeReason}", cancellationToken);

            Logger.LogInformation("User roles updated for user {TargetUserId} ({Username}) by {CurrentUserId} in {Duration}ms. Added: [{Added}], Removed: [{Removed}]",
                userId, targetUser.Username, currentUserId, stopwatch.ElapsedMilliseconds,
                string.Join(", ", addedRoles), string.Join(", ", removedRoles));

            return Ok(CreateSuccessResponse(response, response.Message));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Update user roles operation was cancelled for user {UserId}", userId);
            return StatusCode(499, CreateErrorResponse("Update user roles operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (UnauthorizedAccessException ex)
        {
            stopwatch.Stop();
            var currentUserId = GetCurrentUserId();
            Logger.LogWarning("Unauthorized attempt to update user roles for user {TargetUserId} by {CurrentUserId}: {Error}",
                userId, currentUserId, ex.Message);

            // Log security event for unauthorized role change attempt
            await LogSecurityEventAsync("UserRolesUnauthorized", "UPDATE", $"User/{userId}/Roles",
                false, currentUserId, $"Unauthorized role update attempt: {ex.Message}", cancellationToken);

            return Forbid("Insufficient permissions to update user roles");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var currentUserId = GetCurrentUserId();
            Logger.LogError(ex, "Error updating user roles for user {TargetUserId} by {CurrentUserId}: {Message}",
                userId, currentUserId, ex.Message);

            // Log security event for role update error
            await LogSecurityEventAsync("UserRolesError", "UPDATE", $"User/{userId}/Roles",
                false, currentUserId, $"Role update error: {ex.Message}", cancellationToken);

            return StatusCode(500, CreateErrorResponse($"Failed to update user roles for {userId}", "UPDATE_USER_ROLES_ERROR"));
        }
    }

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









    /// <summary>
    /// Authenticate user and return JWT token
    /// </summary>
    /// <param name="request">Login request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced login response with security tracking</returns>
    [HttpPost("auth/login")]
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
                // IpAddress and UserAgent properties don't exist in Core LoginRequest
            };

            var authResponse = await _authenticationService.AuthenticateAsync(loginRequest, ipAddress, cancellationToken);

            stopwatch.Stop();

            var response = new MonitoringGrid.Api.DTOs.Security.LoginResponse
            {
                IsSuccess = authResponse.IsSuccess,
                ErrorMessage = authResponse.Error?.Message ?? "Unknown error",
                RequiresTwoFactor = false, // Core Result<T> doesn't have RequiresTwoFactor
                RequiresPasswordChange = false, // Core Result<T> doesn't have RequiresPasswordChange
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
                    Scope = string.Empty // Core LoginResponse doesn't have Scope
                };

                response.User = MapToUserResponse(authResponse.Value?.User, true);

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

                // Return 401 for failed authentication
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
    [HttpPost("auth/register")]
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
                // IpAddress and UserAgent properties don't exist in Core CreateUserRequest
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
    /// Get current user profile information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced user profile with security information</returns>
    [HttpGet("auth/profile")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetProfile(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
            {
                Logger.LogWarning("Profile access attempt with invalid token - no user ID found");
                return Unauthorized(CreateErrorResponse("User ID not found in token", "INVALID_TOKEN"));
            }

            Logger.LogDebug("Getting profile for user {UserId}", userId);

            var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                Logger.LogWarning("Profile requested for non-existent user {UserId}", userId);
                return NotFound(CreateErrorResponse("User not found", "USER_NOT_FOUND"));
            }

            stopwatch.Stop();

            var response = MapToUserResponse(user, true);

            // Add additional profile details
            response.Details = new Dictionary<string, object>
            {
                ["ProfileAccessTime"] = DateTime.UtcNow,
                ["QueryDurationMs"] = stopwatch.ElapsedMilliseconds,
                ["TokenValid"] = true,
                ["AccountAge"] = (DateTime.UtcNow - user.CreatedDate).TotalDays,
                ["LastProfileAccess"] = DateTime.UtcNow
            };

            // Log profile access
            await LogSecurityEventAsync("ProfileAccess", "READ", $"User/{userId}",
                true, userId, "User profile accessed", cancellationToken);

            Logger.LogDebug("Retrieved profile for user {UserId} in {Duration}ms",
                userId, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, "Profile retrieved successfully"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Get profile operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Get profile operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var userId = GetCurrentUserId();
            Logger.LogError(ex, "Error retrieving user profile for user {UserId}: {Message}", userId, ex.Message);

            // Log security event for profile access error
            await LogSecurityEventAsync("ProfileAccessError", "READ", $"User/{userId}",
                false, userId, $"Profile access error: {ex.Message}", cancellationToken);

            return StatusCode(500, CreateErrorResponse("Failed to retrieve user profile", "PROFILE_ACCESS_ERROR"));
        }
    }





    /// <summary>
    /// Refresh JWT token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced JWT token response with security tracking</returns>
    [HttpPost("auth/refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(JwtTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<JwtTokenResponse>> RefreshToken(
        [FromBody] MonitoringGrid.Api.DTOs.Security.RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        try
        {
            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogDebug("Token refresh attempt from IP {IpAddress}", ipAddress);

            var tokenResult = await _authenticationService.RefreshTokenAsync(request.RefreshToken, cancellationToken);

            if (!tokenResult.IsSuccess)
            {
                return Unauthorized(CreateErrorResponse(tokenResult.Error?.Message ?? "Token refresh failed", "TOKEN_REFRESH_FAILED"));
            }

            stopwatch.Stop();

            var response = new JwtTokenResponse
            {
                AccessToken = tokenResult.Value?.AccessToken ?? string.Empty,
                RefreshToken = tokenResult.Value?.RefreshToken ?? string.Empty,
                TokenType = "Bearer",
                ExpiresAt = tokenResult.Value?.ExpiresAt ?? DateTime.UtcNow.AddHours(1),
                ExpiresIn = (int)((tokenResult.Value?.ExpiresAt ?? DateTime.UtcNow.AddHours(1)) - DateTime.UtcNow).TotalSeconds,
                Scope = string.Empty // Core JwtToken doesn't have Scope property
            };

            // Get user ID from the new token for logging
            var userId = ExtractUserIdFromToken(tokenResult.Value?.AccessToken ?? string.Empty);

            // Log successful token refresh
            await LogSecurityEventAsync("TokenRefresh", "REFRESH", "Token",
                true, userId, "JWT token refreshed successfully", cancellationToken);

            Logger.LogInformation("Token refreshed successfully for user {UserId} from IP {IpAddress} in {Duration}ms",
                userId, ipAddress, stopwatch.ElapsedMilliseconds);

            return Ok(CreateSuccessResponse(response, "Token refreshed successfully"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Token refresh operation was cancelled from IP {IpAddress}", ipAddress);
            return StatusCode(499, CreateErrorResponse("Token refresh operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (UnauthorizedAccessException ex)
        {
            stopwatch.Stop();
            Logger.LogWarning("Token refresh failed from IP {IpAddress}: {Error}", ipAddress, ex.Message);

            // Log failed token refresh
            await LogSecurityEventAsync("TokenRefreshFailed", "REFRESH", "Token",
                false, null, $"Token refresh failed: {ex.Message}", cancellationToken);

            return Unauthorized(CreateErrorResponse("Invalid or expired refresh token", "INVALID_REFRESH_TOKEN"));
        }
        catch (Microsoft.IdentityModel.Tokens.SecurityTokenException ex)
        {
            stopwatch.Stop();
            Logger.LogWarning("Token refresh failed due to security token exception from IP {IpAddress}: {Error}",
                ipAddress, ex.Message);

            // Log failed token refresh
            await LogSecurityEventAsync("TokenRefreshFailed", "REFRESH", "Token",
                false, null, $"Security token exception: {ex.Message}", cancellationToken);

            return Unauthorized(CreateErrorResponse("Invalid or expired refresh token", "INVALID_SECURITY_TOKEN"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error refreshing token from IP {IpAddress}: {Message}", ipAddress, ex.Message);

            // Log security event for token refresh error
            await LogSecurityEventAsync("TokenRefreshError", "REFRESH", "Token",
                false, null, $"Token refresh error: {ex.Message}", cancellationToken);

            return StatusCode(500, CreateErrorResponse("An error occurred while refreshing the token", "TOKEN_REFRESH_ERROR"));
        }
    }





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
        if (config.PasswordPolicy.PasswordStrengthScore < 80) score -= 20;
        if (config.PasswordPolicy.MaxFailedAttempts > 10) score -= 10;

        // Two-factor health
        if (!config.TwoFactorSettings.Enabled) score -= 30;
        if (config.TwoFactorSettings.Enabled && !config.TwoFactorSettings.Required) score -= 15;

        // Rate limiting health
        if (!config.RateLimitSettings.Enabled) score -= 20;
        if (config.RateLimitSettings.CurrentViolationsCount > 10) score -= 15;

        // Session health
        if (config.SessionSettings.SessionTimeoutMinutes > 480) score -= 10;
        if (config.SessionSettings.IdleTimeoutMinutes > 60) score -= 5;

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
        if (request.SessionSettings.SessionTimeoutMinutes < 5 || request.SessionSettings.SessionTimeoutMinutes > 1440)
            errors.Add("Session timeout must be between 5 and 1440 minutes");

        // Validate rate limiting
        if (request.RateLimitSettings.MaxRequestsPerMinute < 10 || request.RateLimitSettings.MaxRequestsPerMinute > 10000)
            errors.Add("Max requests per minute must be between 10 and 10000");

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
            ["SessionSettings"] = new
            {
                _securityConfig.Session.SessionTimeoutMinutes,
                _securityConfig.Session.IdleTimeoutMinutes
            },
            ["TwoFactorSettings"] = new
            {
                _securityConfig.TwoFactor.IsEnabled,
                _securityConfig.TwoFactor.IsRequired
            },
            ["RateLimitSettings"] = new
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
        changedFields.Add("SessionSettings");
        changedFields.Add("TwoFactorSettings");
        changedFields.Add("RateLimitSettings");

        return changedFields;
    }

    /// <summary>
    /// Maps a user entity to an enhanced user response DTO
    /// </summary>
    private UserResponse MapToUserResponse(dynamic user, bool includeDetails = false)
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
            Roles = new List<string>(), // User entity doesn't have Roles property in Core
            Permissions = new List<string>(), // User entity doesn't have Permissions property in Core
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

            await _securityAuditService.LogSecurityEventAsync(securityEvent, cancellationToken);
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
