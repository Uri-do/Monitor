using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;

namespace MonitoringGrid.Api.Middleware;

/// <summary>
/// Advanced rate limiting middleware with multiple strategies and user-based limits
/// </summary>
public class AdvancedRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdvancedRateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;
    private readonly ConcurrentDictionary<string, RateLimitCounter> _counters = new();

    public AdvancedRateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<AdvancedRateLimitingMiddleware> logger,
        RateLimitingOptions options)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var rateLimitRule = GetRateLimitRule(context);
        if (rateLimitRule == null)
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context, rateLimitRule);
        var key = $"rate_limit_{rateLimitRule.Name}_{clientId}";

        var counter = _counters.GetOrAdd(key, _ => new RateLimitCounter());

        bool isLimitExceeded;
        lock (counter)
        {
            var now = DateTime.UtcNow;

            // Clean old entries
            counter.Requests.RemoveAll(r => now - r > rateLimitRule.Window);

            // Check if limit exceeded
            isLimitExceeded = counter.Requests.Count >= rateLimitRule.Limit;

            if (!isLimitExceeded)
            {
                // Add current request
                counter.Requests.Add(now);
            }
        }

        if (isLimitExceeded)
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId} on rule {RuleName}. {RequestCount}/{Limit} requests in {Window}",
                clientId, rateLimitRule.Name, counter.Requests.Count, rateLimitRule.Limit, rateLimitRule.Window);

            await HandleRateLimitExceeded(context, rateLimitRule, counter);
            return;
        }

        // Add rate limit headers
        AddRateLimitHeaders(context, rateLimitRule, counter);

        await _next(context);
    }

    private RateLimitRule? GetRateLimitRule(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();
        var method = context.Request.Method;
        var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
        var userRole = GetUserRole(context);

        // Check for specific endpoint rules first
        var endpointRule = _options.Rules.FirstOrDefault(r => 
            r.PathPattern != null && 
            path?.Contains(r.PathPattern) == true &&
            (r.HttpMethods == null || r.HttpMethods.Contains(method)));

        if (endpointRule != null)
            return endpointRule;

        // Check for user-based rules
        if (isAuthenticated)
        {
            var userRule = _options.Rules.FirstOrDefault(r => 
                r.UserRoles != null && 
                r.UserRoles.Contains(userRole));
            
            if (userRule != null)
                return userRule;
        }

        // Return default rule
        return _options.Rules.FirstOrDefault(r => r.IsDefault);
    }

    private string GetClientIdentifier(HttpContext context, RateLimitRule rule)
    {
        return rule.IdentifierType switch
        {
            RateLimitIdentifierType.IpAddress => GetClientIpAddress(context),
            RateLimitIdentifierType.User => context.User.Identity?.Name ?? "anonymous",
            RateLimitIdentifierType.ApiKey => GetApiKey(context) ?? "no-api-key",
            RateLimitIdentifierType.Combined => $"{GetClientIpAddress(context)}_{context.User.Identity?.Name ?? "anonymous"}",
            _ => GetClientIpAddress(context)
        };
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string? GetApiKey(HttpContext context)
    {
        return context.Request.Headers["X-API-Key"].FirstOrDefault() ??
               context.Request.Query["api_key"].FirstOrDefault();
    }

    private string GetUserRole(HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
    }

    private async Task HandleRateLimitExceeded(HttpContext context, RateLimitRule rule, RateLimitCounter counter)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        var retryAfter = rule.Window.TotalSeconds;
        context.Response.Headers["Retry-After"] = retryAfter.ToString();

        var response = new
        {
            error = "Rate limit exceeded",
            message = $"Too many requests. Limit: {rule.Limit} requests per {rule.Window.TotalMinutes} minutes",
            retryAfter = retryAfter,
            limit = rule.Limit,
            window = rule.Window.TotalSeconds,
            current = counter.Requests.Count
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }

    private void AddRateLimitHeaders(HttpContext context, RateLimitRule rule, RateLimitCounter counter)
    {
        var remaining = Math.Max(0, rule.Limit - counter.Requests.Count);
        var resetTime = counter.Requests.Any() 
            ? counter.Requests.Min().Add(rule.Window)
            : DateTime.UtcNow.Add(rule.Window);

        context.Response.Headers["X-RateLimit-Limit"] = rule.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)resetTime).ToUnixTimeSeconds().ToString();
        context.Response.Headers["X-RateLimit-Window"] = rule.Window.TotalSeconds.ToString();
    }
}

/// <summary>
/// Rate limiting configuration options
/// </summary>
public class RateLimitingOptions
{
    public List<RateLimitRule> Rules { get; set; } = new();
    public bool EnableLogging { get; set; } = true;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Rate limiting rule configuration
/// </summary>
public class RateLimitRule
{
    public string Name { get; set; } = string.Empty;
    public int Limit { get; set; }
    public TimeSpan Window { get; set; }
    public RateLimitIdentifierType IdentifierType { get; set; } = RateLimitIdentifierType.IpAddress;
    public string? PathPattern { get; set; }
    public string[]? HttpMethods { get; set; }
    public string[]? UserRoles { get; set; }
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// Rate limit identifier types
/// </summary>
public enum RateLimitIdentifierType
{
    IpAddress,
    User,
    ApiKey,
    Combined
}

/// <summary>
/// Rate limit counter for tracking requests
/// </summary>
public class RateLimitCounter
{
    public List<DateTime> Requests { get; set; } = new();
}

/// <summary>
/// Rate limiting service for programmatic access
/// </summary>
public interface IRateLimitingService
{
    Task<bool> IsAllowedAsync(string clientId, string ruleName);
    Task<RateLimitStatus> GetStatusAsync(string clientId, string ruleName);
    Task ResetAsync(string clientId, string ruleName);
    Task<Dictionary<string, RateLimitStatus>> GetAllStatusAsync();
}

/// <summary>
/// Rate limit status information
/// </summary>
public class RateLimitStatus
{
    public string ClientId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public int Limit { get; set; }
    public int Current { get; set; }
    public int Remaining => Math.Max(0, Limit - Current);
    public DateTime? ResetTime { get; set; }
    public TimeSpan Window { get; set; }
    public bool IsExceeded => Current >= Limit;
}

/// <summary>
/// Implementation of rate limiting service
/// </summary>
public class RateLimitingService : IRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly RateLimitingOptions _options;
    private readonly ILogger<RateLimitingService> _logger;
    private readonly ConcurrentDictionary<string, RateLimitCounter> _counters = new();

    public RateLimitingService(
        IMemoryCache cache,
        RateLimitingOptions options,
        ILogger<RateLimitingService> logger)
    {
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    public Task<bool> IsAllowedAsync(string clientId, string ruleName)
    {
        var rule = _options.Rules.FirstOrDefault(r => r.Name == ruleName);
        if (rule == null)
            return Task.FromResult(true);

        var key = $"rate_limit_{ruleName}_{clientId}";
        var counter = _counters.GetOrAdd(key, _ => new RateLimitCounter());

        lock (counter)
        {
            var now = DateTime.UtcNow;
            counter.Requests.RemoveAll(r => now - r > rule.Window);
            return Task.FromResult(counter.Requests.Count < rule.Limit);
        }
    }

    public Task<RateLimitStatus> GetStatusAsync(string clientId, string ruleName)
    {
        var rule = _options.Rules.FirstOrDefault(r => r.Name == ruleName);
        if (rule == null)
        {
            return Task.FromResult(new RateLimitStatus
            {
                ClientId = clientId,
                RuleName = ruleName,
                Limit = int.MaxValue,
                Current = 0
            });
        }

        var key = $"rate_limit_{ruleName}_{clientId}";
        var counter = _counters.GetOrAdd(key, _ => new RateLimitCounter());

        lock (counter)
        {
            var now = DateTime.UtcNow;
            counter.Requests.RemoveAll(r => now - r > rule.Window);

            var status = new RateLimitStatus
            {
                ClientId = clientId,
                RuleName = ruleName,
                Limit = rule.Limit,
                Current = counter.Requests.Count,
                Window = rule.Window,
                ResetTime = counter.Requests.Any() ? counter.Requests.Min().Add(rule.Window) : now.Add(rule.Window)
            };

            return Task.FromResult(status);
        }
    }

    public Task ResetAsync(string clientId, string ruleName)
    {
        var key = $"rate_limit_{ruleName}_{clientId}";
        if (_counters.TryRemove(key, out var counter))
        {
            _logger.LogInformation("Reset rate limit for client {ClientId} on rule {RuleName}", clientId, ruleName);
        }
        return Task.CompletedTask;
    }

    public async Task<Dictionary<string, RateLimitStatus>> GetAllStatusAsync()
    {
        var result = new Dictionary<string, RateLimitStatus>();

        foreach (var kvp in _counters)
        {
            var parts = kvp.Key.Split('_');
            if (parts.Length >= 4)
            {
                var ruleName = parts[2];
                var clientId = string.Join("_", parts.Skip(3));
                var status = await GetStatusAsync(clientId, ruleName);
                result[kvp.Key] = status;
            }
        }

        return result;
    }
}
