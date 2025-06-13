using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using EnterpriseApp.Core.Interfaces;

namespace EnterpriseApp.Infrastructure.Services;

/// <summary>
/// Memory cache service implementation
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;

    /// <summary>
    /// Initializes a new instance of the MemoryCacheService class
    /// </summary>
    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = _memoryCache.Get<T>(key);
            _logger.LogDebug("Cache {Operation} for key: {Key}", value != null ? "HIT" : "MISS", key);
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache value for key: {Key}", key);
            return Task.FromResult<T?>(default);
        }
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                options.SlidingExpiration = TimeSpan.FromMinutes(30); // Default sliding expiration
            }

            _memoryCache.Set(key, value, options);
            _logger.LogDebug("Cache SET for key: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cache value for key: {Key}", key);
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("Cache REMOVE for key: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache value for key: {Key}", key);
            return Task.CompletedTask;
        }
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = _memoryCache.TryGetValue(key, out _);
            return Task.FromResult(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check cache existence for key: {Key}", key);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        var value = await getItem();
        await SetAsync(key, value, expiration, cancellationToken);
        return value;
    }

    /// <inheritdoc />
    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Memory cache doesn't support pattern-based removal
        // This would require maintaining a separate index of keys
        _logger.LogWarning("Pattern-based cache removal is not supported by MemoryCache");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Redis cache service implementation
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the RedisCacheService class
    /// </summary>
    public RedisCacheService(IDistributedCache distributedCache, ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            
            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache MISS for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache HIT for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache value for key: {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                options.SlidingExpiration = TimeSpan.FromMinutes(30); // Default sliding expiration
            }

            await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
            _logger.LogDebug("Cache SET for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set cache value for key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache REMOVE for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache value for key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _distributedCache.GetStringAsync(key, cancellationToken);
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check cache existence for key: {Key}", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        var value = await getItem();
        await SetAsync(key, value, expiration, cancellationToken);
        return value;
    }

    /// <inheritdoc />
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a simplified implementation
            // In a real Redis implementation, you would use Redis SCAN command with pattern matching
            _logger.LogWarning("Pattern-based cache removal requires Redis-specific implementation");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache values by pattern: {Pattern}", pattern);
        }
    }
}

/// <summary>
/// Cache key builder utility
/// </summary>
public static class CacheKeys
{
    private const string Separator = ":";

    /// <summary>
    /// Builds a cache key for DomainEntity
    /// </summary>
    public static string DomainEntity(int id) => $"domainentity{Separator}{id}";

    /// <summary>
    /// Builds a cache key for DomainEntity list
    /// </summary>
    public static string DomainEntityList(string? category = null, bool? isActive = null)
    {
        var key = "domainentity" + Separator + "list";
        
        if (!string.IsNullOrEmpty(category))
            key += Separator + "category" + Separator + category;
            
        if (isActive.HasValue)
            key += Separator + "active" + Separator + isActive.Value;
            
        return key;
    }

    /// <summary>
    /// Builds a cache key for user data
    /// </summary>
    public static string User(string userId) => $"user{Separator}{userId}";

    /// <summary>
    /// Builds a cache key for user permissions
    /// </summary>
    public static string UserPermissions(string userId) => $"user{Separator}{userId}{Separator}permissions";

    /// <summary>
    /// Builds a cache key for statistics
    /// </summary>
    public static string Statistics(string type, DateTime? date = null)
    {
        var key = $"statistics{Separator}{type}";
        
        if (date.HasValue)
            key += Separator + date.Value.ToString("yyyy-MM-dd");
            
        return key;
    }

    /// <summary>
    /// Builds a cache key with custom segments
    /// </summary>
    public static string Build(params string[] segments) => string.Join(Separator, segments);
}

/// <summary>
/// Cache configuration options
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Default cache expiration time
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Short cache expiration time
    /// </summary>
    public TimeSpan ShortExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Long cache expiration time
    /// </summary>
    public TimeSpan LongExpiration { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// Very long cache expiration time
    /// </summary>
    public TimeSpan VeryLongExpiration { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Cache key prefix
    /// </summary>
    public string KeyPrefix { get; set; } = "EnterpriseApp";
}
