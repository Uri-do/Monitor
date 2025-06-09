using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MonitoringGrid.Api.Middleware;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MonitoringGrid.Api.Security;

/// <summary>
/// Enhanced JWT middleware with advanced security features
/// </summary>
public class EnhancedJwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<EnhancedJwtMiddleware> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly ISecurityEventService _securityEventService;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;

    public EnhancedJwtMiddleware(
        RequestDelegate next,
        ILogger<EnhancedJwtMiddleware> logger,
        ICorrelationIdService correlationIdService,
        ISecurityEventService securityEventService,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _correlationIdService = correlationIdService;
        _securityEventService = securityEventService;
        _tokenHandler = new JwtSecurityTokenHandler();

        var jwtSettings = configuration.GetSection("Security:Jwt");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true
        };
    }

    /// <summary>
    /// Process JWT token with enhanced security validation
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        var token = ExtractToken(context);

        if (!string.IsNullOrEmpty(token))
        {
            var validationResult = await ValidateTokenAsync(token, context, correlationId);
            
            if (validationResult.IsValid)
            {
                // Set user context
                context.User = validationResult.Principal!;
                
                // Log successful authentication
                await _securityEventService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.AuthenticationSuccess,
                    UserId = validationResult.UserId,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    CorrelationId = correlationId,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["TokenType"] = "JWT",
                        ["TokenExpiry"] = validationResult.ExpiryTime
                    }
                });
            }
            else
            {
                // Log authentication failure
                await _securityEventService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.AuthenticationFailure,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    CorrelationId = correlationId,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["TokenType"] = "JWT",
                        ["FailureReason"] = validationResult.FailureReason ?? "Unknown"
                    }
                });

                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized: Invalid token");
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Extracts JWT token from request headers
    /// </summary>
    private string? ExtractToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        
        if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        // Check query string for SignalR connections
        var accessToken = context.Request.Query["access_token"].FirstOrDefault();
        if (!string.IsNullOrEmpty(accessToken) && context.Request.Path.StartsWithSegments("/monitoring-hub"))
        {
            return accessToken;
        }

        return null;
    }

    /// <summary>
    /// Validates JWT token with enhanced security checks
    /// </summary>
    private async Task<TokenValidationResult> ValidateTokenAsync(string token, HttpContext context, string correlationId)
    {
        try
        {
            // Basic token validation
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);
            
            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                return new TokenValidationResult { FailureReason = "Invalid token format" };
            }

            // Enhanced security checks
            var securityChecks = await PerformSecurityChecksAsync(jwtToken, context, correlationId);
            if (!securityChecks.IsValid)
            {
                return new TokenValidationResult { FailureReason = securityChecks.FailureReason };
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var expiryTime = jwtToken.ValidTo;

            return new TokenValidationResult
            {
                IsValid = true,
                Principal = principal,
                UserId = userId,
                ExpiryTime = expiryTime
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult { FailureReason = "Token expired" };
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return new TokenValidationResult { FailureReason = "Invalid token signature" };
        }
        catch (SecurityTokenValidationException ex)
        {
            return new TokenValidationResult { FailureReason = $"Token validation failed: {ex.Message}" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation [{CorrelationId}]", correlationId);
            return new TokenValidationResult { FailureReason = "Token validation error" };
        }
    }

    /// <summary>
    /// Performs additional security checks on the token
    /// </summary>
    private async Task<SecurityCheckResult> PerformSecurityChecksAsync(JwtSecurityToken token, HttpContext context, string correlationId)
    {
        // Check for token replay attacks
        var jti = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        if (!string.IsNullOrEmpty(jti))
        {
            var isTokenUsed = await _securityEventService.IsTokenUsedAsync(jti);
            if (isTokenUsed)
            {
                await _securityEventService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.TokenReplayAttempt,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    CorrelationId = correlationId,
                    AdditionalData = new Dictionary<string, object> { ["TokenId"] = jti }
                });

                return new SecurityCheckResult { FailureReason = "Token replay detected" };
            }

            // Mark token as used
            await _securityEventService.MarkTokenAsUsedAsync(jti, token.ValidTo);
        }

        // Check for suspicious activity patterns
        var userId = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(ipAddress))
        {
            var isSuspicious = await _securityEventService.IsSuspiciousActivityAsync(userId, ipAddress);
            if (isSuspicious)
            {
                await _securityEventService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.SuspiciousActivity,
                    UserId = userId,
                    IpAddress = ipAddress,
                    CorrelationId = correlationId,
                    AdditionalData = new Dictionary<string, object> { ["Reason"] = "Unusual access pattern" }
                });

                // Don't block but log for monitoring
                _logger.LogWarning("Suspicious activity detected for user {UserId} from IP {IpAddress} [{CorrelationId}]", 
                    userId, ipAddress, correlationId);
            }
        }

        return new SecurityCheckResult { IsValid = true };
    }
}

/// <summary>
/// Token validation result
/// </summary>
public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
    public string? UserId { get; set; }
    public DateTime ExpiryTime { get; set; }
    public string? FailureReason { get; set; }
}

/// <summary>
/// Security check result
/// </summary>
public class SecurityCheckResult
{
    public bool IsValid { get; set; } = false;
    public string? FailureReason { get; set; }
}

/// <summary>
/// Security event service interface
/// </summary>
public interface ISecurityEventService
{
    /// <summary>
    /// Logs a security event
    /// </summary>
    Task LogSecurityEventAsync(SecurityEvent securityEvent);

    /// <summary>
    /// Checks if a token has been used (replay protection)
    /// </summary>
    Task<bool> IsTokenUsedAsync(string tokenId);

    /// <summary>
    /// Marks a token as used
    /// </summary>
    Task MarkTokenAsUsedAsync(string tokenId, DateTime expiry);

    /// <summary>
    /// Checks for suspicious activity patterns
    /// </summary>
    Task<bool> IsSuspiciousActivityAsync(string userId, string ipAddress);

    /// <summary>
    /// Gets security events for analysis
    /// </summary>
    Task<List<SecurityEvent>> GetSecurityEventsAsync(SecurityEventFilter filter);
}

/// <summary>
/// Security event model
/// </summary>
public class SecurityEvent
{
    public int Id { get; set; }
    public SecurityEventType EventType { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

/// <summary>
/// Security event types
/// </summary>
public enum SecurityEventType
{
    AuthenticationSuccess,
    AuthenticationFailure,
    AuthorizationFailure,
    TokenReplayAttempt,
    SuspiciousActivity,
    RateLimitExceeded,
    InvalidApiKey,
    PasswordChange,
    AccountLockout,
    PrivilegeEscalation
}

/// <summary>
/// Security event filter
/// </summary>
public class SecurityEventFilter
{
    public SecurityEventType? EventType { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageSize { get; set; } = 50;
    public int PageNumber { get; set; } = 1;
}

/// <summary>
/// Extension methods for enhanced JWT middleware
/// </summary>
public static class EnhancedJwtMiddlewareExtensions
{
    /// <summary>
    /// Adds enhanced JWT middleware to the pipeline
    /// </summary>
    public static IApplicationBuilder UseEnhancedJwt(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<EnhancedJwtMiddleware>();
    }
}
