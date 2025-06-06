using Microsoft.Extensions.Caching.Memory;
using MonitoringGrid.Api.Observability;
using System.Net;

namespace MonitoringGrid.Api.Middleware;

/// <summary>
/// Advanced rate limiting middleware with different limits for different endpoints
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitOptions _options;
    private readonly MetricsService _metricsService;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitOptions options,
        MetricsService metricsService)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _options = options;
        _metricsService = metricsService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks and internal endpoints
        if (ShouldSkipRateLimit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var clientKey = GenerateClientKey(context);
        var endpoint = GetEndpointCategory(context.Request.Path);
        var limit = GetRateLimitForEndpoint(endpoint);

        var requestCount = await GetRequestCountAsync(clientKey, endpoint);

        if (requestCount >= limit.RequestsPerWindow)
        {
            // Record rate limit exceeded metric
            var clientType = clientKey.StartsWith("user:") ? "authenticated" : "anonymous";
            _metricsService.RecordRateLimitExceeded(endpoint.ToString(), clientType);

            await HandleRateLimitExceeded(context, clientKey, endpoint, requestCount, limit);
            return;
        }

        // Increment request count
        await IncrementRequestCountAsync(clientKey, endpoint, limit.WindowSizeMinutes);

        // Add rate limit headers
        AddRateLimitHeaders(context, requestCount + 1, limit);

        await _next(context);
    }

    private static bool ShouldSkipRateLimit(PathString path)
    {
        var pathValue = path.Value?.ToLower() ?? "";
        return pathValue.StartsWith("/health") ||
               pathValue.StartsWith("/api/info") ||
               pathValue.StartsWith("/swagger") ||
               pathValue.StartsWith("/monitoring-hub");
    }

    private string GenerateClientKey(HttpContext context)
    {
        // Use multiple factors to identify clients
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var remoteIp = forwarded ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // If user is authenticated, use user ID
        var userId = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // For anonymous users, use IP + User Agent hash
        var userAgentHash = userAgent.GetHashCode().ToString();
        return $"ip:{remoteIp}:ua:{userAgentHash}";
    }

    private static EndpointCategory GetEndpointCategory(PathString path)
    {
        var pathValue = path.Value?.ToLower() ?? "";

        if (pathValue.Contains("/auth/"))
            return EndpointCategory.Authentication;
        
        if (pathValue.Contains("/kpi/") && pathValue.Contains("/execute"))
            return EndpointCategory.KpiExecution;
        
        if (pathValue.Contains("/kpi/"))
            return EndpointCategory.KpiManagement;
        
        if (pathValue.Contains("/alert/"))
            return EndpointCategory.AlertManagement;
        
        if (pathValue.Contains("/analytics/"))
            return EndpointCategory.Analytics;

        return EndpointCategory.General;
    }

    private RateLimit GetRateLimitForEndpoint(EndpointCategory category)
    {
        return category switch
        {
            EndpointCategory.Authentication => _options.AuthenticationLimit,
            EndpointCategory.KpiExecution => _options.KpiExecutionLimit,
            EndpointCategory.KpiManagement => _options.KpiManagementLimit,
            EndpointCategory.AlertManagement => _options.AlertManagementLimit,
            EndpointCategory.Analytics => _options.AnalyticsLimit,
            EndpointCategory.General => _options.GeneralLimit,
            _ => _options.GeneralLimit
        };
    }

    private Task<int> GetRequestCountAsync(string clientKey, EndpointCategory endpoint)
    {
        var cacheKey = $"rate_limit:{clientKey}:{endpoint}";

        if (_cache.TryGetValue(cacheKey, out int count))
        {
            return Task.FromResult(count);
        }

        return Task.FromResult(0);
    }

    private Task IncrementRequestCountAsync(string clientKey, EndpointCategory endpoint, int windowMinutes)
    {
        var cacheKey = $"rate_limit:{clientKey}:{endpoint}";

        var count = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(windowMinutes);
            return 0;
        });

        _cache.Set(cacheKey, count + 1, TimeSpan.FromMinutes(windowMinutes));
        return Task.CompletedTask;
    }

    private async Task HandleRateLimitExceeded(
        HttpContext context, 
        string clientKey, 
        EndpointCategory endpoint, 
        int currentCount, 
        RateLimit limit)
    {
        _logger.LogWarning("Rate limit exceeded for client {ClientKey} on endpoint {Endpoint}. " +
                          "Current count: {CurrentCount}, Limit: {Limit}",
            clientKey, endpoint, currentCount, limit.RequestsPerWindow);

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        // Add retry-after header
        var retryAfter = TimeSpan.FromMinutes(limit.WindowSizeMinutes).TotalSeconds;
        context.Response.Headers["Retry-After"] = retryAfter.ToString();

        AddRateLimitHeaders(context, currentCount, limit);

        var response = new
        {
            error = "Rate limit exceeded",
            message = $"Too many requests for {endpoint} endpoint. Try again later.",
            retryAfter = retryAfter
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }

    private static void AddRateLimitHeaders(HttpContext context, int currentCount, RateLimit limit)
    {
        context.Response.Headers["X-RateLimit-Limit"] = limit.RequestsPerWindow.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit.RequestsPerWindow - currentCount).ToString();
        context.Response.Headers["X-RateLimit-Window"] = $"{limit.WindowSizeMinutes}m";
    }
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitOptions
{
    public RateLimit GeneralLimit { get; set; } = new() { RequestsPerWindow = 100, WindowSizeMinutes = 1 };
    public RateLimit AuthenticationLimit { get; set; } = new() { RequestsPerWindow = 10, WindowSizeMinutes = 1 };
    public RateLimit KpiExecutionLimit { get; set; } = new() { RequestsPerWindow = 20, WindowSizeMinutes = 1 };
    public RateLimit KpiManagementLimit { get; set; } = new() { RequestsPerWindow = 50, WindowSizeMinutes = 1 };
    public RateLimit AlertManagementLimit { get; set; } = new() { RequestsPerWindow = 30, WindowSizeMinutes = 1 };
    public RateLimit AnalyticsLimit { get; set; } = new() { RequestsPerWindow = 25, WindowSizeMinutes = 1 };
}

/// <summary>
/// Rate limit configuration for a specific endpoint category
/// </summary>
public class RateLimit
{
    public int RequestsPerWindow { get; set; }
    public int WindowSizeMinutes { get; set; }
}

/// <summary>
/// Categories of endpoints for different rate limiting rules
/// </summary>
public enum EndpointCategory
{
    General,
    Authentication,
    KpiExecution,
    KpiManagement,
    AlertManagement,
    Analytics
}
