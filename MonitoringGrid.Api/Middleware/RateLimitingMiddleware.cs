using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.Services;
using System.Security.Claims;
using System.Text.Json;

namespace MonitoringGrid.Api.Middleware;

/// <summary>
/// Enhanced rate limiting middleware with intelligent throttling and security integration
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Process HTTP request with enhanced rate limiting checks
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items.TryGetValue("CorrelationId", out var corrId)
            ? corrId?.ToString() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();

        // Skip rate limiting for certain paths
        if (ShouldSkipRateLimit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            // Resolve the rate limiting service from the request scope
            var rateLimitingService = context.RequestServices.GetRequiredService<IAdvancedRateLimitingService>();

            var rateLimitRequest = CreateRateLimitRequest(context);
            var result = await rateLimitingService.CheckRateLimitAsync(rateLimitRequest);

            if (!result.IsAllowed)
            {
                await HandleRateLimitExceededAsync(context, result, correlationId);
                return;
            }

            // Record the request for tracking
            await rateLimitingService.RecordRequestAsync(rateLimitRequest);

            // Add rate limit headers to response
            AddRateLimitHeaders(context.Response, result);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limiting middleware [{CorrelationId}]", correlationId);
            // Fail open - continue processing if rate limiting fails
            await _next(context);
        }
    }

    /// <summary>
    /// Determines if rate limiting should be skipped for certain paths
    /// </summary>
    private bool ShouldSkipRateLimit(PathString path)
    {
        var skipPaths = new[]
        {
            "/health",
            "/metrics",
            "/swagger",
            "/favicon.ico"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates rate limit request from HTTP context
    /// </summary>
    private RateLimitRequest CreateRateLimitRequest(HttpContext context)
    {
        var ipAddress = GetClientIpAddress(context);
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var endpoint = GetNormalizedEndpoint(context.Request.Path);
        var method = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var correlationId = context.Items.TryGetValue("CorrelationId", out var corrId)
            ? corrId?.ToString() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();

        return new RateLimitRequest
        {
            IpAddress = ipAddress,
            UserId = userId,
            Endpoint = endpoint,
            Method = method,
            UserAgent = userAgent,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Gets client IP address with proxy support
    /// </summary>
    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        // Check for real IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fall back to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Normalizes endpoint path for rate limiting
    /// </summary>
    private string GetNormalizedEndpoint(PathString path)
    {
        var pathValue = path.Value ?? "/";

        // Normalize API versioning paths
        if (pathValue.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase))
        {
            var segments = pathValue.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 3)
            {
                // Return /api/v{version}/{controller} format
                return $"/{segments[0]}/{segments[1]}/{segments[2]}";
            }
        }

        // Normalize paths with IDs (replace with placeholder)
        var normalizedPath = System.Text.RegularExpressions.Regex.Replace(
            pathValue,
            @"/\d+(/|$)",
            "/{id}$1",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return normalizedPath;
    }

    /// <summary>
    /// Handles rate limit exceeded scenarios
    /// </summary>
    private async Task HandleRateLimitExceededAsync(HttpContext context, RateLimitResult result, string correlationId)
    {
        context.Response.StatusCode = 429; // Too Many Requests
        context.Response.ContentType = "application/json";

        // Add rate limit headers
        if (result.RetryAfter.HasValue)
        {
            context.Response.Headers["Retry-After"] = ((int)result.RetryAfter.Value.TotalSeconds).ToString();
        }

        if (result.ResetTime.HasValue)
        {
            var resetTimeUnix = ((DateTimeOffset)result.ResetTime.Value).ToUnixTimeSeconds();
            context.Response.Headers["X-RateLimit-Reset"] = resetTimeUnix.ToString();
        }

        context.Response.Headers["X-RateLimit-Remaining"] = "0";

        // Create error response
        var errorResponse = ApiResponse.Failure(
            result.Reason ?? "Rate limit exceeded",
            new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId,
                ["RetryAfter"] = result.RetryAfter?.TotalSeconds ?? 0,
                ["ResetTime"] = result.ResetTime?.ToString("O") ?? "",
                ["RemainingRequests"] = result.RemainingRequests
            });

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);

        _logger.LogWarning("Rate limit exceeded: {Reason} [{CorrelationId}]", result.Reason, correlationId);
    }

    /// <summary>
    /// Adds rate limit headers to response
    /// </summary>
    private void AddRateLimitHeaders(HttpResponse response, RateLimitResult result)
    {
        if (result.IsAllowed)
        {
            response.Headers["X-RateLimit-Remaining"] = result.RemainingRequests.ToString();

            if (result.ResetTime.HasValue)
            {
                var resetTimeUnix = ((DateTimeOffset)result.ResetTime.Value).ToUnixTimeSeconds();
                response.Headers["X-RateLimit-Reset"] = resetTimeUnix.ToString();
            }
        }
    }
}

/// <summary>
/// Extension methods for rate limiting middleware registration
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    /// Adds enhanced rate limiting middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseAdvancedRateLimit(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
