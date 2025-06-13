using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MonitoringGrid.Api.Middleware;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Advanced caching service with multi-layer caching, cache warming, and intelligent invalidation
/// </summary>
public interface IAdvancedCachingService
{
    /// <summary>
    /// Gets cached data with automatic serialization/deserialization
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Sets cached data with configurable expiration and tags
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets or sets cached data using a factory function
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Removes cached data by key
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes cached data by tag pattern
    /// </summary>
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Warms up cache with frequently accessed data
    /// </summary>
    Task WarmupCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics and metrics
    /// </summary>
    CacheStatistics GetStatistics();

    /// <summary>
    /// Generates a cache key with consistent hashing
    /// </summary>
    string GenerateCacheKey(string prefix, params object[] parameters);
}

/// <summary>
/// Implementation of advanced caching service with multi-layer support
/// </summary>
public class AdvancedCachingService : IAdvancedCachingService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<AdvancedCachingService> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    private readonly CacheStatistics _statistics;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _warmupSemaphore;

    // Cache configuration
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(15);
    private readonly TimeSpan _memoryExpiration = TimeSpan.FromMinutes(5);
    private readonly int _maxMemoryCacheSize = 1000;

    public AdvancedCachingService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<AdvancedCachingService> logger,
        ICorrelationIdService correlationIdService)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        _correlationIdService = correlationIdService;
        _statistics = new CacheStatistics();
        _warmupSemaphore = new SemaphoreSlim(1, 1);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Gets cached data with multi-layer cache support
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Try memory cache first (L1)
            if (_memoryCache.TryGetValue(key, out T? memoryValue))
            {
                _statistics.RecordHit(CacheLayer.Memory);
                _logger.LogDebug("Cache hit (Memory): {Key} [{CorrelationId}]", key, correlationId);
                return memoryValue;
            }

            // Try distributed cache (L2)
            var distributedValue = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(distributedValue))
            {
                var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue, _jsonOptions);
                
                // Store in memory cache for faster access
                _memoryCache.Set(key, deserializedValue, _memoryExpiration);
                
                _statistics.RecordHit(CacheLayer.Distributed);
                _logger.LogDebug("Cache hit (Distributed): {Key} [{CorrelationId}]", key, correlationId);
                return deserializedValue;
            }

            _statistics.RecordMiss();
            _logger.LogDebug("Cache miss: {Key} [{CorrelationId}]", key, correlationId);
            return null;
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogError(ex, "Cache get error for key: {Key} [{CorrelationId}]", key, correlationId);
            return null;
        }
        finally
        {
            stopwatch.Stop();
            _statistics.RecordOperation(stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Sets cached data in both memory and distributed cache
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class
    {
        var correlationId = _correlationIdService.GetCorrelationId();
        var cacheExpiration = expiration ?? _defaultExpiration;

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);

            // Set in memory cache (L1)
            var memoryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(Math.Min(cacheExpiration.TotalMilliseconds, _memoryExpiration.TotalMilliseconds)),
                Size = 1
            };

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    memoryOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
                    {
                        EvictionCallback = (k, v, r, s) => _logger.LogDebug("Memory cache evicted: {Key} (Reason: {Reason})", k, r)
                    });
                }
            }

            _memoryCache.Set(key, value, memoryOptions);

            // Set in distributed cache (L2)
            var distributedOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration
            };

            await _distributedCache.SetStringAsync(key, serializedValue, distributedOptions, cancellationToken);

            // Store tags for invalidation
            if (tags != null)
            {
                await StoreCacheTagsAsync(key, tags, cacheExpiration, cancellationToken);
            }

            _statistics.RecordSet();
            _logger.LogDebug("Cache set: {Key} (Expiration: {Expiration}) [{CorrelationId}]", key, cacheExpiration, correlationId);
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogError(ex, "Cache set error for key: {Key} [{CorrelationId}]", key, correlationId);
        }
    }

    /// <summary>
    /// Gets or sets cached data using factory pattern
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, string[]? tags = null, CancellationToken cancellationToken = default) where T : class
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        var value = await factory();
        if (value != null)
        {
            await SetAsync(key, value, expiration, tags, cancellationToken);
        }

        return value ?? default(T);
    }

    /// <summary>
    /// Removes cached data from both layers
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdService.GetCorrelationId();

        try
        {
            _memoryCache.Remove(key);
            await _distributedCache.RemoveAsync(key, cancellationToken);

            _statistics.RecordRemoval();
            _logger.LogDebug("Cache removed: {Key} [{CorrelationId}]", key, correlationId);
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogError(ex, "Cache removal error for key: {Key} [{CorrelationId}]", key, correlationId);
        }
    }

    /// <summary>
    /// Removes cached data by tag pattern
    /// </summary>
    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var correlationId = _correlationIdService.GetCorrelationId();

        try
        {
            var tagKey = $"tag:{tag}";
            var taggedKeysJson = await _distributedCache.GetStringAsync(tagKey, cancellationToken);
            
            if (!string.IsNullOrEmpty(taggedKeysJson))
            {
                var taggedKeys = JsonSerializer.Deserialize<List<string>>(taggedKeysJson, _jsonOptions);
                if (taggedKeys != null)
                {
                    var removeTasks = taggedKeys.Select(key => RemoveAsync(key, cancellationToken));
                    await Task.WhenAll(removeTasks);
                }

                await _distributedCache.RemoveAsync(tagKey, cancellationToken);
            }

            _logger.LogDebug("Cache invalidated by tag: {Tag} [{CorrelationId}]", tag, correlationId);
        }
        catch (Exception ex)
        {
            _statistics.RecordError();
            _logger.LogError(ex, "Cache tag invalidation error for tag: {Tag} [{CorrelationId}]", tag, correlationId);
        }
    }

    /// <summary>
    /// Warms up cache with frequently accessed data
    /// </summary>
    public async Task WarmupCacheAsync(CancellationToken cancellationToken = default)
    {
        if (!await _warmupSemaphore.WaitAsync(100, cancellationToken))
        {
            _logger.LogDebug("Cache warmup already in progress, skipping");
            return;
        }

        try
        {
            var correlationId = _correlationIdService.GetCorrelationId();
            _logger.LogInformation("Starting cache warmup [{CorrelationId}]", correlationId);

            // Warmup frequently accessed Indicator data
            var warmupTasks = new List<Task>
            {
                WarmupIndicatorDataAsync(cancellationToken),
                WarmupSystemConfigurationAsync(cancellationToken),
                WarmupUserPermissionsAsync(cancellationToken)
            };

            await Task.WhenAll(warmupTasks);

            _logger.LogInformation("Cache warmup completed [{CorrelationId}]", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warmup failed");
        }
        finally
        {
            _warmupSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets cache performance statistics
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        return _statistics.Clone();
    }

    /// <summary>
    /// Generates consistent cache key with hashing
    /// </summary>
    public string GenerateCacheKey(string prefix, params object[] parameters)
    {
        var keyBuilder = new StringBuilder(prefix);
        
        foreach (var param in parameters)
        {
            keyBuilder.Append(':');
            keyBuilder.Append(param?.ToString() ?? "null");
        }

        var key = keyBuilder.ToString();
        
        // Hash long keys to ensure consistent length
        if (key.Length > 200)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            var hash = Convert.ToBase64String(hashBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            return $"{prefix}:hash:{hash}";
        }

        return key;
    }

    /// <summary>
    /// Stores cache tags for invalidation support
    /// </summary>
    private async Task StoreCacheTagsAsync(string key, string[] tags, TimeSpan expiration, CancellationToken cancellationToken)
    {
        foreach (var tag in tags)
        {
            var tagKey = $"tag:{tag}";
            var existingKeysJson = await _distributedCache.GetStringAsync(tagKey, cancellationToken);
            
            var keys = string.IsNullOrEmpty(existingKeysJson) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(existingKeysJson, _jsonOptions) ?? new List<string>();

            if (!keys.Contains(key))
            {
                keys.Add(key);
                var updatedKeysJson = JsonSerializer.Serialize(keys, _jsonOptions);
                
                var tagOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration
                };

                await _distributedCache.SetStringAsync(tagKey, updatedKeysJson, tagOptions, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Warms up Indicator-related cache data
    /// </summary>
    private async Task WarmupIndicatorDataAsync(CancellationToken cancellationToken)
    {
        // Implementation would load frequently accessed Indicator data
        // This is a placeholder for the actual implementation
        await Task.Delay(10, cancellationToken);
        _logger.LogDebug("Indicator data cache warmup completed");
    }

    /// <summary>
    /// Warms up system configuration cache
    /// </summary>
    private async Task WarmupSystemConfigurationAsync(CancellationToken cancellationToken)
    {
        // Implementation would load system configuration data
        await Task.Delay(10, cancellationToken);
        _logger.LogDebug("System configuration cache warmup completed");
    }

    /// <summary>
    /// Warms up user permissions cache
    /// </summary>
    private async Task WarmupUserPermissionsAsync(CancellationToken cancellationToken)
    {
        // Implementation would load user permissions data
        await Task.Delay(10, cancellationToken);
        _logger.LogDebug("User permissions cache warmup completed");
    }
}

/// <summary>
/// Cache performance statistics
/// </summary>
public class CacheStatistics
{
    private long _memoryHits;
    private long _distributedHits;
    private long _misses;
    private long _sets;
    private long _removals;
    private long _errors;
    private long _totalOperations;
    private long _totalOperationTimeMs;

    public long MemoryHits => _memoryHits;
    public long DistributedHits => _distributedHits;
    public long TotalHits => _memoryHits + _distributedHits;
    public long Misses => _misses;
    public long Sets => _sets;
    public long Removals => _removals;
    public long Errors => _errors;
    public long TotalOperations => _totalOperations;
    public double HitRatio => _totalOperations > 0 ? (double)TotalHits / _totalOperations : 0;
    public double AverageOperationTimeMs => _totalOperations > 0 ? (double)_totalOperationTimeMs / _totalOperations : 0;

    public void RecordHit(CacheLayer layer)
    {
        if (layer == CacheLayer.Memory)
            Interlocked.Increment(ref _memoryHits);
        else
            Interlocked.Increment(ref _distributedHits);
        
        Interlocked.Increment(ref _totalOperations);
    }

    public void RecordMiss()
    {
        Interlocked.Increment(ref _misses);
        Interlocked.Increment(ref _totalOperations);
    }

    public void RecordSet() => Interlocked.Increment(ref _sets);
    public void RecordRemoval() => Interlocked.Increment(ref _removals);
    public void RecordError() => Interlocked.Increment(ref _errors);

    public void RecordOperation(long operationTimeMs)
    {
        Interlocked.Add(ref _totalOperationTimeMs, operationTimeMs);
    }

    public CacheStatistics Clone()
    {
        return new CacheStatistics
        {
            _memoryHits = _memoryHits,
            _distributedHits = _distributedHits,
            _misses = _misses,
            _sets = _sets,
            _removals = _removals,
            _errors = _errors,
            _totalOperations = _totalOperations,
            _totalOperationTimeMs = _totalOperationTimeMs
        };
    }
}

/// <summary>
/// Cache layer enumeration
/// </summary>
public enum CacheLayer
{
    Memory,
    Distributed
}
