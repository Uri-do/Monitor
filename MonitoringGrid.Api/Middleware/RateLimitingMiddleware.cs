using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Api.Configuration;
using System.Net;
using System.Text.Json;

namespace MonitoringGrid.Api.Middleware;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware for rate limiting requests
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitingOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptions<RateLimitingOptions> options,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var endpoint = GetEndpointKey(context);
        
        // Check if endpoint has custom rate limit
        var rateLimit = GetRateLimitForEndpoint(endpoint);
        
        var key = $"rate_limit:{clientId}:{endpoint}";
        var requestCount = await GetRequestCountAsync(key);
        
        // Check if rate limit exceeded
        if (requestCount >= rateLimit.MaxRequests)
        {
            await HandleRateLimitExceeded(context, rateLimit);
            return;
        }

        // Increment request count
        await IncrementRequestCountAsync(key, rateLimit.WindowSeconds);
        
        // Add rate limit headers
        AddRateLimitHeaders(context, requestCount + 1, rateLimit);
        
        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get user ID from claims
        var userId = context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        return $"ip:{ipAddress ?? "unknown"}";
    }

    private string GetEndpointKey(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var method = context.Request.Method.ToUpperInvariant();
        return $"{method}:{path}";
    }

    private EndpointRateLimit GetRateLimitForEndpoint(string endpoint)
    {
        if (_options.EndpointLimits.TryGetValue(endpoint, out var customLimit))
        {
            return customLimit;
        }

        // Return default rate limit
        return new EndpointRateLimit
        {
            MaxRequests = _options.MaxRequests,
            WindowSeconds = _options.WindowSeconds
        };
    }

    private async Task<int> GetRequestCountAsync(string key)
    {
        return await Task.FromResult(_cache.Get<int>(key));
    }

    private async Task IncrementRequestCountAsync(string key, int windowSeconds)
    {
        var currentCount = _cache.Get<int>(key);
        var newCount = currentCount + 1;
        
        var expiry = TimeSpan.FromSeconds(windowSeconds);
        _cache.Set(key, newCount, expiry);
        
        await Task.CompletedTask;
    }

    private void AddRateLimitHeaders(HttpContext context, int currentCount, EndpointRateLimit rateLimit)
    {
        context.Response.Headers["X-RateLimit-Limit"] = rateLimit.MaxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, rateLimit.MaxRequests - currentCount).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(rateLimit.WindowSeconds).ToUnixTimeSeconds().ToString();
    }

    private async Task HandleRateLimitExceeded(HttpContext context, EndpointRateLimit rateLimit)
    {
        _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}", 
            GetClientIdentifier(context), GetEndpointKey(context));

        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        AddRateLimitHeaders(context, rateLimit.MaxRequests, rateLimit);
        context.Response.Headers["Retry-After"] = rateLimit.WindowSeconds.ToString();

        var response = new
        {
            error = "Rate limit exceeded",
            message = $"Too many requests. Limit: {rateLimit.MaxRequests} per {rateLimit.WindowSeconds} seconds",
            retryAfter = rateLimit.WindowSeconds
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
