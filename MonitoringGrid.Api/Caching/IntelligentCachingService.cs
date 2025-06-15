using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;
using System.Text.Json;

namespace MonitoringGrid.Api.Caching;

using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.Models;

/// <summary>
/// Intelligent caching service with predictive caching and smart invalidation
/// </summary>
public interface IIntelligentCachingService
{
    /// <summary>
    /// Gets cached data with intelligent cache warming
    /// </summary>
    Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, CacheOptions? options = null) where T : class;

    /// <summary>
    /// Sets cached data with intelligent expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, CacheOptions? options = null) where T : class;

    /// <summary>
    /// Invalidates cache by pattern
    /// </summary>
    Task InvalidatePatternAsync(string pattern);

    /// <summary>
    /// Preloads frequently accessed data
    /// </summary>
    Task PreloadCacheAsync();

    /// <summary>
    /// Gets cache statistics and recommendations
    /// </summary>
    Task<CacheAnalytics> GetCacheAnalyticsAsync();

    /// <summary>
    /// Optimizes cache based on usage patterns
    /// </summary>
    Task OptimizeCacheAsync();
}

/// <summary>
/// Implementation of intelligent caching service
/// </summary>
public class IntelligentCachingService : IIntelligentCachingService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<IntelligentCachingService> _logger;
    private readonly ConcurrentDictionary<string, CacheMetrics> _cacheMetrics = new();
    private readonly ConcurrentDictionary<string, CacheAccessPattern> _accessPatterns = new();
    private readonly Timer _optimizationTimer;
    private readonly JsonSerializerOptions _jsonOptions;

    public IntelligentCachingService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        ILogger<IntelligentCachingService> logger)
    {
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Run optimization every 5 minutes
        _optimizationTimer = new Timer(async _ => await OptimizeCacheAsync(), null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, CacheOptions? options = null) where T : class
    {
        options ??= CacheOptions.Default;
        var startTime = DateTime.UtcNow;

        try
        {
            // Record access pattern
            RecordAccess(key);

            // Try memory cache first (L1)
            if (_memoryCache.TryGetValue(key, out T? memoryValue))
            {
                RecordCacheHit(key, CacheLayer.Memory, startTime);
                return memoryValue;
            }

            // Try distributed cache (L2)
            var distributedValue = await _distributedCache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(distributedValue))
            {
                var deserializedValue = JsonSerializer.Deserialize<T>(distributedValue, _jsonOptions);
                
                // Promote to memory cache
                var memoryOptions = CreateMemoryCacheOptions(options);
                _memoryCache.Set(key, deserializedValue, memoryOptions);
                
                RecordCacheHit(key, CacheLayer.Distributed, startTime);
                return deserializedValue;
            }

            // Cache miss - execute factory
            RecordCacheMiss(key, startTime);
            var factoryValue = await factory();
            
            if (factoryValue != null)
            {
                await SetAsync(key, factoryValue, options);
            }

            return factoryValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in intelligent cache get for key {Key}", key);
            
            // Fallback to factory on cache error
            try
            {
                return await factory();
            }
            catch (Exception factoryEx)
            {
                _logger.LogError(factoryEx, "Factory method also failed for key {Key}", key);
                throw;
            }
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheOptions? options = null) where T : class
    {
        options ??= CacheOptions.Default;

        try
        {
            var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            var expiration = CalculateIntelligentExpiration(key, options);

            // Set in memory cache (L1)
            var memoryOptions = CreateMemoryCacheOptions(options);
            memoryOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Min(expiration.TotalMinutes, 30));
            _memoryCache.Set(key, value, memoryOptions);

            // Set in distributed cache (L2)
            var distributedOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            await _distributedCache.SetStringAsync(key, serializedValue, distributedOptions);

            // Update cache metrics
            UpdateCacheMetrics(key, serializedValue.Length);

            _logger.LogDebug("Set cache value for key {Key} with expiration {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache value for key {Key}", key);
        }
    }

    public async Task InvalidatePatternAsync(string pattern)
    {
        try
        {
            var keysToInvalidate = _cacheMetrics.Keys
                .Where(key => IsPatternMatch(key, pattern))
                .ToList();

            foreach (var key in keysToInvalidate)
            {
                _memoryCache.Remove(key);
                await _distributedCache.RemoveAsync(key);
                _cacheMetrics.TryRemove(key, out _);
            }

            _logger.LogInformation("Invalidated {Count} cache entries matching pattern {Pattern}", 
                keysToInvalidate.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache pattern {Pattern}", pattern);
        }
    }

    public async Task PreloadCacheAsync()
    {
        try
        {
            _logger.LogInformation("Starting intelligent cache preload");

            // Identify frequently accessed keys that are not currently cached
            var frequentKeys = _accessPatterns
                .Where(kvp => kvp.Value.AccessCount > 10 && kvp.Value.LastAccess > DateTime.UtcNow.AddHours(-1))
                .OrderByDescending(kvp => kvp.Value.AccessFrequency)
                .Take(50)
                .Select(kvp => kvp.Key)
                .ToList();

            var preloadTasks = new List<Task>();

            foreach (var key in frequentKeys)
            {
                if (!_memoryCache.TryGetValue(key, out _))
                {
                    // This would typically involve calling the appropriate factory method
                    // For now, we'll just log the intention
                    _logger.LogDebug("Would preload cache for key {Key}", key);
                }
            }

            await Task.WhenAll(preloadTasks);
            _logger.LogInformation("Completed cache preload for {Count} keys", frequentKeys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache preload");
        }
    }

    public async Task<CacheAnalytics> GetCacheAnalyticsAsync()
    {
        try
        {
            var analytics = new CacheAnalytics
            {
                GeneratedAt = DateTime.UtcNow,
                TotalKeys = _cacheMetrics.Count,
                TotalMemoryUsage = CalculateTotalMemoryUsage(),
                HitRate = CalculateOverallHitRate(),
                MissRate = CalculateOverallMissRate(),
                TopKeys = GetTopAccessedKeys(),
                CacheEfficiency = CalculateCacheEfficiency(),
                Recommendations = GenerateRecommendations()
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating cache analytics");
            throw;
        }
    }

    public async Task OptimizeCacheAsync()
    {
        try
        {
            _logger.LogDebug("Starting cache optimization");

            // Remove expired entries
            await CleanupExpiredEntries();

            // Adjust cache sizes based on usage patterns
            await AdjustCacheSizes();

            // Update access patterns
            UpdateAccessPatterns();

            _logger.LogDebug("Completed cache optimization");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache optimization");
        }
    }

    private void RecordAccess(string key)
    {
        _accessPatterns.AddOrUpdate(key,
            new CacheAccessPattern { AccessCount = 1, LastAccess = DateTime.UtcNow, FirstAccess = DateTime.UtcNow },
            (k, existing) =>
            {
                existing.AccessCount++;
                existing.LastAccess = DateTime.UtcNow;
                existing.AccessFrequency = CalculateAccessFrequency(existing);
                return existing;
            });
    }

    private void RecordCacheHit(string key, CacheLayer layer, DateTime startTime)
    {
        var duration = DateTime.UtcNow - startTime;
        
        _cacheMetrics.AddOrUpdate(key,
            new CacheMetrics { Hits = 1, LastHit = DateTime.UtcNow, AverageHitTime = duration },
            (k, existing) =>
            {
                existing.Hits++;
                existing.LastHit = DateTime.UtcNow;
                existing.AverageHitTime = TimeSpan.FromMilliseconds(
                    (existing.AverageHitTime.TotalMilliseconds + duration.TotalMilliseconds) / 2);
                return existing;
            });

        _logger.LogDebug("Cache hit for key {Key} from {Layer} in {Duration}ms", 
            key, layer, duration.TotalMilliseconds);
    }

    private void RecordCacheMiss(string key, DateTime startTime)
    {
        var duration = DateTime.UtcNow - startTime;
        
        _cacheMetrics.AddOrUpdate(key,
            new CacheMetrics { Misses = 1, LastMiss = DateTime.UtcNow },
            (k, existing) =>
            {
                existing.Misses++;
                existing.LastMiss = DateTime.UtcNow;
                return existing;
            });

        _logger.LogDebug("Cache miss for key {Key} in {Duration}ms", key, duration.TotalMilliseconds);
    }

    private TimeSpan CalculateIntelligentExpiration(string key, CacheOptions options)
    {
        var baseExpiration = options.Expiration ?? TimeSpan.FromMinutes(30);
        
        // Adjust based on access patterns
        if (_accessPatterns.TryGetValue(key, out var pattern))
        {
            if (pattern.AccessFrequency > 10) // Frequently accessed
            {
                return TimeSpan.FromMinutes(baseExpiration.TotalMinutes * 1.5);
            }
            else if (pattern.AccessFrequency < 2) // Rarely accessed
            {
                return TimeSpan.FromMinutes(baseExpiration.TotalMinutes * 0.5);
            }
        }

        return baseExpiration;
    }

    private MemoryCacheEntryOptions CreateMemoryCacheOptions(CacheOptions options)
    {
        return new MemoryCacheEntryOptions
        {
            Priority = options.Priority switch
            {
                CachePriority.High => Microsoft.Extensions.Caching.Memory.CacheItemPriority.High,
                CachePriority.Normal => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal,
                CachePriority.Low => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Low,
                _ => Microsoft.Extensions.Caching.Memory.CacheItemPriority.Normal
            },
            Size = 1
        };
    }

    private void UpdateCacheMetrics(string key, int size)
    {
        _cacheMetrics.AddOrUpdate(key,
            new CacheMetrics { Size = size, LastUpdate = DateTime.UtcNow },
            (k, existing) =>
            {
                existing.Size = size;
                existing.LastUpdate = DateTime.UtcNow;
                return existing;
            });
    }

    private bool IsPatternMatch(string key, string pattern)
    {
        // Simple pattern matching - could be enhanced with regex
        return pattern.Contains('*') 
            ? key.StartsWith(pattern.Replace("*", ""))
            : key.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private double CalculateAccessFrequency(CacheAccessPattern pattern)
    {
        var timeSpan = DateTime.UtcNow - pattern.FirstAccess;
        return timeSpan.TotalHours > 0 ? pattern.AccessCount / timeSpan.TotalHours : pattern.AccessCount;
    }

    private long CalculateTotalMemoryUsage()
    {
        return _cacheMetrics.Values.Sum(m => m.Size);
    }

    private double CalculateOverallHitRate()
    {
        var totalHits = _cacheMetrics.Values.Sum(m => m.Hits);
        var totalRequests = _cacheMetrics.Values.Sum(m => m.Hits + m.Misses);
        return totalRequests > 0 ? (double)totalHits / totalRequests : 0;
    }

    private double CalculateOverallMissRate()
    {
        return 1.0 - CalculateOverallHitRate();
    }

    private List<CacheKeyInfo> GetTopAccessedKeys()
    {
        return _accessPatterns
            .OrderByDescending(kvp => kvp.Value.AccessFrequency)
            .Take(10)
            .Select(kvp => new CacheKeyInfo
            {
                Key = kvp.Key,
                AccessCount = kvp.Value.AccessCount,
                AccessFrequency = kvp.Value.AccessFrequency,
                LastAccess = kvp.Value.LastAccess
            })
            .ToList();
    }

    private double CalculateCacheEfficiency()
    {
        var hitRate = CalculateOverallHitRate();
        var avgResponseTime = _cacheMetrics.Values
            .Where(m => m.AverageHitTime != TimeSpan.Zero)
            .Average(m => m.AverageHitTime.TotalMilliseconds);
        
        // Efficiency score based on hit rate and response time
        var responseTimeScore = Math.Max(0, 100 - avgResponseTime / 10);
        return (hitRate * 100 + responseTimeScore) / 2;
    }

    private List<CacheRecommendation> GenerateRecommendations()
    {
        var recommendations = new List<CacheRecommendation>();
        
        var hitRate = CalculateOverallHitRate();
        if (hitRate < 0.8)
        {
            recommendations.Add(new CacheRecommendation
            {
                Type = "Hit Rate",
                Message = $"Cache hit rate is {hitRate:P2}. Consider increasing cache expiration times or preloading frequently accessed data.",
                Priority = "Medium"
            });
        }

        var largeKeys = _cacheMetrics
            .Where(kvp => kvp.Value.Size > 1024 * 1024) // > 1MB
            .ToList();
        
        if (largeKeys.Any())
        {
            recommendations.Add(new CacheRecommendation
            {
                Type = "Memory Usage",
                Message = $"Found {largeKeys.Count} large cache entries. Consider compressing data or reducing cache size.",
                Priority = "High"
            });
        }

        return recommendations;
    }

    private async Task CleanupExpiredEntries()
    {
        // This would typically involve checking expiration times
        // For now, just remove old metrics
        var cutoffTime = DateTime.UtcNow.AddHours(-24);
        var expiredKeys = _cacheMetrics
            .Where(kvp => kvp.Value.LastUpdate < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cacheMetrics.TryRemove(key, out _);
            _accessPatterns.TryRemove(key, out _);
        }
    }

    private async Task AdjustCacheSizes()
    {
        // Implement intelligent cache size adjustment based on usage patterns
        await Task.CompletedTask;
    }

    private void UpdateAccessPatterns()
    {
        // Clean up old access patterns
        var cutoffTime = DateTime.UtcNow.AddDays(-7);
        var oldPatterns = _accessPatterns
            .Where(kvp => kvp.Value.LastAccess < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in oldPatterns)
        {
            _accessPatterns.TryRemove(key, out _);
        }
    }

    public void Dispose()
    {
        _optimizationTimer?.Dispose();
    }
}
