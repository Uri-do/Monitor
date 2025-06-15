using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using System.Text.Json;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// High-performance caching service with memory and distributed cache support
/// </summary>
public class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<CacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly HashSet<string> _trackedKeys = new(); // For testing purposes

    public CacheService(
        IMemoryCache memoryCache,
        IDistributedCache? distributedCache,
        ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Gets a cached value with fallback to distributed cache
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            // Try memory cache first (fastest)
            if (_memoryCache.TryGetValue(key, out T? memoryValue))
            {
                _logger.LogDebug("Cache HIT (Memory) for key: {Key}", key);
                return memoryValue;
            }

            // Try distributed cache if available
            if (_distributedCache != null)
            {
                var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
                if (!string.IsNullOrEmpty(distributedValue))
                {
                    var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue, _jsonOptions);
                    
                    // Store in memory cache for faster subsequent access
                    _memoryCache.Set(key, deserializedValue, TimeSpan.FromMinutes(5));
                    
                    _logger.LogDebug("Cache HIT (Distributed) for key: {Key}", key);
                    return deserializedValue;
                }
            }

            _logger.LogDebug("Cache MISS for key: {Key}", key);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache value for key: {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// Sets a value in both memory and distributed cache
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cacheExpiration = expiration ?? TimeSpan.FromMinutes(30);

            // Set in memory cache
            var memoryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration,
                Priority = CacheItemPriority.Normal
            };
            _memoryCache.Set(key, value, memoryOptions);

            // Set in distributed cache if available
            if (_distributedCache != null)
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                var distributedOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheExpiration
                };
                await _distributedCache.SetStringAsync(key, serializedValue, distributedOptions, cancellationToken);
            }

            // Track the key for testing purposes
            _trackedKeys.Add(key);

            _logger.LogDebug("Cache SET for key: {Key}, expiration: {Expiration}", key, cacheExpiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
        }
    }

    /// <summary>
    /// Removes a value from both caches
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);

            if (_distributedCache != null)
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
            }

            // Remove from tracked keys
            _trackedKeys.Remove(key);

            _logger.LogDebug("Cache REMOVE for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
        }
    }

    /// <summary>
    /// Gets or sets a cached value with a factory function
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        var value = await factory();
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value!;
    }

    /// <summary>
    /// Removes all cached values with a specific prefix
    /// </summary>
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Removing cache entries with prefix: {Prefix}", prefix);

            // Find keys that match the prefix
            var keysToRemove = _trackedKeys.Where(key => key.StartsWith(prefix)).ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);

                if (_distributedCache != null)
                {
                    await _distributedCache.RemoveAsync(key, cancellationToken);
                }

                _trackedKeys.Remove(key);
            }

            _logger.LogDebug("Removed {Count} cache entries with prefix: {Prefix}", keysToRemove.Count, prefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries with prefix: {Prefix}", prefix);
        }
    }

    /// <summary>
    /// Clears all cached values
    /// </summary>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Clearing all cache entries");

            // For testing purposes, we'll track keys and remove them
            // In production, this would depend on the cache implementation
            if (_trackedKeys.Any())
            {
                var keysToRemove = _trackedKeys.ToList();
                foreach (var key in keysToRemove)
                {
                    _memoryCache?.Remove(key);
                    if (_distributedCache != null)
                    {
                        await _distributedCache.RemoveAsync(key, cancellationToken);
                    }
                }
                _trackedKeys.Clear();
                _logger.LogDebug("Cleared {Count} cache entries", keysToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }
    }
}

/// <summary>
/// Cache key builder for consistent cache key generation
/// </summary>
public static class CacheKeys
{
    private const string Separator = ":";
    
    // Indicator cache keys
    public static string AllIndicators => "indicators:all";
    public static string ActiveIndicators => "indicators:active";
    public static string IndicatorsByOwner(int ownerId) => $"indicators:owner:{ownerId}";
    public static string IndicatorsByPriority(string priority) => $"indicators:priority:{priority}";
    public static string DueIndicators => "indicators:due";
    public static string IndicatorById(long id) => $"indicator:{id}";
    public static string IndicatorDashboard => "indicators:dashboard";
    
    // Contact cache keys
    public static string AllContacts => "contacts:all";
    public static string ActiveContacts => "contacts:active";
    public static string ContactById(int id) => $"contact:{id}";
    
    // Statistics cache keys
    public static string CollectorStatistics(long collectorId, DateTime fromDate, DateTime toDate) => 
        $"statistics:collector:{collectorId}:{fromDate:yyyyMMdd}:{toDate:yyyyMMdd}";
    public static string LatestStatistics(long collectorId, int hours) => 
        $"statistics:latest:{collectorId}:{hours}";
    public static string ActiveCollectors => "collectors:active";
    
    // Configuration cache keys
    public static string SystemConfiguration => "config:system";
    public static string ConfigurationValue(string key) => $"config:value:{key}";
    
    // Alert cache keys
    public static string RecentAlerts(int hours) => $"alerts:recent:{hours}";
    public static string AlertsByIndicator(long indicatorId) => $"alerts:indicator:{indicatorId}";
    
    // System status cache keys
    public static string SystemStatus => "system:status";
    public static string HealthChecks => "system:health";
    
    // Scheduler cache keys
    public static string AllSchedulers => "schedulers:all";
    public static string ActiveSchedulers => "schedulers:active";
    public static string SchedulerById(int id) => $"scheduler:{id}";
}

/// <summary>
/// Cache expiration times for different data types
/// </summary>
public static class CacheExpirations
{
    public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Medium = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Long = TimeSpan.FromHours(2);
    public static readonly TimeSpan VeryLong = TimeSpan.FromHours(24);
    
    // Specific expirations for different data types
    public static readonly TimeSpan Indicators = Medium;
    public static readonly TimeSpan Contacts = Long;
    public static readonly TimeSpan Statistics = Short;
    public static readonly TimeSpan Configuration = VeryLong;
    public static readonly TimeSpan SystemStatus = Short;
    public static readonly TimeSpan Schedulers = Long;
}
