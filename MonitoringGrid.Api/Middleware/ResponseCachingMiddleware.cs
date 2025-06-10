using Microsoft.Extensions.Caching.Memory;
using System.Collections;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MonitoringGrid.Api.Middleware;

/// <summary>
/// Advanced response caching middleware with intelligent cache invalidation
/// </summary>
public class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ResponseCachingMiddleware> _logger;
    private readonly ResponseCachingOptions _options;

    public ResponseCachingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<ResponseCachingMiddleware> logger,
        ResponseCachingOptions options)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only cache GET requests
        if (context.Request.Method != HttpMethods.Get)
        {
            await _next(context);
            return;
        }

        // Check if caching is enabled for this endpoint
        var cacheConfig = GetCacheConfiguration(context);
        if (cacheConfig == null || !cacheConfig.Enabled)
        {
            await _next(context);
            return;
        }

        var cacheKey = GenerateCacheKey(context, cacheConfig);
        
        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out CachedResponse? cachedResponse))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            await WriteCachedResponse(context, cachedResponse!);
            return;
        }

        // Cache miss - execute request and cache response
        _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);
        
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        // Only cache successful responses
        if (context.Response.StatusCode == 200)
        {
            var response = await CacheResponse(context, responseBody, cacheKey, cacheConfig);
            await WriteResponse(context, response, originalBodyStream);
        }
        else
        {
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private CacheConfiguration? GetCacheConfiguration(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();
        
        return path switch
        {
            var p when p?.Contains("/api/kpi/dashboard") == true => new CacheConfiguration
            {
                Enabled = true,
                Duration = TimeSpan.FromMinutes(2),
                VaryByQuery = true,
                Tags = new[] { "dashboard", "kpi" }
            },
            var p when p?.Contains("/api/kpi") == true && !p.Contains("/execute") => new CacheConfiguration
            {
                Enabled = true,
                Duration = TimeSpan.FromMinutes(5),
                VaryByQuery = true,
                Tags = new[] { "kpi" }
            },
            var p when p?.Contains("/api/kpi/contacts") == true => new CacheConfiguration
            {
                Enabled = true,
                Duration = TimeSpan.FromMinutes(10),
                VaryByQuery = false,
                Tags = new[] { "contacts" }
            },
            var p when p?.Contains("/api/kpi/alerts/statistics") == true => new CacheConfiguration
            {
                Enabled = true,
                Duration = TimeSpan.FromMinutes(3),
                VaryByQuery = true,
                Tags = new[] { "alerts", "statistics" }
            },
            _ => null
        };
    }

    private string GenerateCacheKey(HttpContext context, CacheConfiguration config)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append(context.Request.Path);

        if (config.VaryByQuery && context.Request.QueryString.HasValue)
        {
            keyBuilder.Append(context.Request.QueryString);
        }

        if (config.VaryByUser && context.User.Identity?.IsAuthenticated == true)
        {
            keyBuilder.Append($"_user_{context.User.Identity.Name}");
        }

        var key = keyBuilder.ToString();
        
        // Hash long keys to avoid memory issues
        if (key.Length > 250)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            key = Convert.ToBase64String(hash);
        }

        return $"response_cache_{key}";
    }

    private async Task<CachedResponse> CacheResponse(
        HttpContext context, 
        MemoryStream responseBody, 
        string cacheKey, 
        CacheConfiguration config)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        var content = await new StreamReader(responseBody).ReadToEndAsync();
        
        var cachedResponse = new CachedResponse
        {
            Content = content,
            ContentType = context.Response.ContentType ?? "application/json",
            StatusCode = context.Response.StatusCode,
            Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            CachedAt = DateTime.UtcNow
        };

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = config.Duration,
            Priority = CacheItemPriority.Normal
        };

        if (config.Tags?.Any() == true)
        {
            foreach (var tag in config.Tags)
            {
                cacheOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (key, value, reason, state) =>
                    {
                        _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", key, reason);
                    }
                });
            }
        }

        // Set cache entry size for memory management
        cacheOptions.Size = cachedResponse.Content.Length;

        _cache.Set(cacheKey, cachedResponse, cacheOptions);
        _logger.LogDebug("Response cached with key: {CacheKey}, Duration: {Duration}, Size: {Size} bytes",
            cacheKey, config.Duration, cachedResponse.Content.Length);

        return cachedResponse;
    }

    private async Task WriteCachedResponse(HttpContext context, CachedResponse cachedResponse)
    {
        context.Response.StatusCode = cachedResponse.StatusCode;
        context.Response.ContentType = cachedResponse.ContentType;
        
        foreach (var header in cachedResponse.Headers)
        {
            if (!context.Response.Headers.ContainsKey(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value;
            }
        }

        // Add cache headers
        context.Response.Headers["X-Cache"] = "HIT";
        context.Response.Headers["X-Cache-Date"] = cachedResponse.CachedAt.ToString("R");

        await context.Response.WriteAsync(cachedResponse.Content);
    }

    private async Task WriteResponse(HttpContext context, CachedResponse response, Stream originalBodyStream)
    {
        context.Response.Body = originalBodyStream;
        
        // Add cache headers
        context.Response.Headers["X-Cache"] = "MISS";
        context.Response.Headers["X-Cache-Date"] = DateTime.UtcNow.ToString("R");

        await context.Response.WriteAsync(response.Content);
    }
}

/// <summary>
/// Cache configuration for different endpoints
/// </summary>
public class CacheConfiguration
{
    public bool Enabled { get; set; } = true;
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(5);
    public bool VaryByQuery { get; set; } = true;
    public bool VaryByUser { get; set; } = false;
    public string[]? Tags { get; set; }
}

/// <summary>
/// Cached response data
/// </summary>
public class CachedResponse
{
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/json";
    public int StatusCode { get; set; } = 200;
    public Dictionary<string, string> Headers { get; set; } = new();
    public DateTime CachedAt { get; set; }
}

/// <summary>
/// Response caching options
/// </summary>
public class ResponseCachingOptions
{
    public bool Enabled { get; set; } = true;
    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxCacheSize { get; set; } = 100; // MB
    public bool EnableCompression { get; set; } = true;
}

/// <summary>
/// Cache invalidation service
/// </summary>
public interface ICacheInvalidationService
{
    Task InvalidateByTagAsync(string tag);
    Task InvalidateByPatternAsync(string pattern);
    Task InvalidateAllAsync();
    Task InvalidateByKeyAsync(string key);
}

/// <summary>
/// Implementation of cache invalidation service
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheInvalidationService> _logger;
    private readonly ConcurrentDictionary<string, List<string>> _tagToKeys = new();
    private readonly ConcurrentDictionary<string, List<string>> _keyToTags = new();

    public CacheInvalidationService(IMemoryCache cache, ILogger<CacheInvalidationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task InvalidateByTagAsync(string tag)
    {
        if (_tagToKeys.TryGetValue(tag, out var keys))
        {
            foreach (var key in keys)
            {
                _cache.Remove(key);
                _logger.LogDebug("Invalidated cache key: {Key} for tag: {Tag}", key, tag);
            }
            _tagToKeys.TryRemove(tag, out _);
        }

        return Task.CompletedTask;
    }

    public Task InvalidateByPatternAsync(string pattern)
    {
        // This would require a more sophisticated implementation
        // For now, we'll invalidate all keys that contain the pattern
        _logger.LogWarning("Pattern-based cache invalidation not fully implemented: {Pattern}", pattern);
        return Task.CompletedTask;
    }

    public Task InvalidateAllAsync()
    {
        if (_cache is MemoryCache memoryCache)
        {
            var field = typeof(MemoryCache).GetField("_coherentState", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(memoryCache) is object coherentState)
            {
                var entriesCollection = coherentState.GetType()
                    .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (entriesCollection?.GetValue(coherentState) is IDictionary entries)
                {
                    var keysToRemove = entries.Keys.Cast<object>().ToList();
                    foreach (var key in keysToRemove)
                    {
                        _cache.Remove(key);
                    }
                }
            }
        }

        _tagToKeys.Clear();
        _keyToTags.Clear();
        _logger.LogInformation("All cache entries invalidated");
        
        return Task.CompletedTask;
    }

    public Task InvalidateByKeyAsync(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Invalidated cache key: {Key}", key);
        return Task.CompletedTask;
    }
}
