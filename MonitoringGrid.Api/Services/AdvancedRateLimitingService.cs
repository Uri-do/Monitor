using Microsoft.Extensions.Caching.Memory;
using MonitoringGrid.Api.Middleware;
using MonitoringGrid.Api.Security;
using MonitoringGrid.Api.Common;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Advanced rate limiting service with intelligent throttling and user-based limits
/// </summary>
public interface IAdvancedRateLimitingService
{
    /// <summary>
    /// Checks if request is allowed based on rate limits
    /// </summary>
    Task<RateLimitResult> CheckRateLimitAsync(RateLimitRequest request);

    /// <summary>
    /// Records a request for rate limiting tracking
    /// </summary>
    Task RecordRequestAsync(RateLimitRequest request);

    /// <summary>
    /// Gets rate limiting statistics
    /// </summary>
    RateLimitStatistics GetStatistics();

    /// <summary>
    /// Temporarily blocks an IP address
    /// </summary>
    Task BlockIpAddressAsync(string ipAddress, TimeSpan duration, string reason);

    /// <summary>
    /// Checks if an IP address is blocked
    /// </summary>
    Task<bool> IsIpBlockedAsync(string ipAddress);
}

/// <summary>
/// Implementation of advanced rate limiting service
/// </summary>
public class AdvancedRateLimitingService : IAdvancedRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdvancedRateLimitingService> _logger;
    private readonly ISecurityEventService _securityEventService;
    private readonly RateLimitStatistics _statistics;

    // Rate limiting stores
    private readonly ConcurrentDictionary<string, RateLimitBucket> _ipBuckets = new();
    private readonly ConcurrentDictionary<string, RateLimitBucket> _userBuckets = new();
    private readonly ConcurrentDictionary<string, RateLimitBucket> _endpointBuckets = new();
    private readonly ConcurrentDictionary<string, DateTime> _blockedIps = new();

    // Rate limiting configuration
    private readonly RateLimitConfiguration _config;

    public AdvancedRateLimitingService(
        IMemoryCache cache,
        ILogger<AdvancedRateLimitingService> logger,
        ISecurityEventService securityEventService,
        IConfiguration configuration)
    {
        _cache = cache;
        _logger = logger;
        _securityEventService = securityEventService;
        _statistics = new RateLimitStatistics();

        _config = new RateLimitConfiguration();
        configuration.GetSection("Security:RateLimit").Bind(_config);

        // Start cleanup task
        _ = Task.Run(CleanupExpiredBucketsAsync);
    }

    /// <summary>
    /// Checks if request is allowed based on comprehensive rate limits
    /// </summary>
    public async Task<RateLimitResult> CheckRateLimitAsync(RateLimitRequest request)
    {
        var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString();

        try
        {
            // Check if IP is blocked
            if (await IsIpBlockedAsync(request.IpAddress))
            {
                _statistics.RecordBlocked();
                return new RateLimitResult
                {
                    IsAllowed = false,
                    Reason = "IP address is temporarily blocked",
                    RetryAfter = TimeSpan.FromMinutes(15)
                };
            }

            // Check IP-based rate limit
            var ipResult = CheckBucketLimit(request.IpAddress, _ipBuckets, _config.IpLimits, "IP");
            if (!ipResult.IsAllowed)
            {
                await HandleRateLimitExceededAsync(request, "IP", correlationId);
                return ipResult;
            }

            // Check user-based rate limit (if authenticated)
            if (!string.IsNullOrEmpty(request.UserId))
            {
                var userResult = CheckBucketLimit(request.UserId, _userBuckets, _config.UserLimits, "User");
                if (!userResult.IsAllowed)
                {
                    await HandleRateLimitExceededAsync(request, "User", correlationId);
                    return userResult;
                }
            }

            // Check endpoint-based rate limit
            var endpointKey = $"{request.Method}:{request.Endpoint}";
            var endpointResult = CheckBucketLimit(endpointKey, _endpointBuckets, _config.EndpointLimits, "Endpoint");
            if (!endpointResult.IsAllowed)
            {
                await HandleRateLimitExceededAsync(request, "Endpoint", correlationId);
                return endpointResult;
            }

            _statistics.RecordAllowed();
            return new RateLimitResult { IsAllowed = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit [{CorrelationId}]", correlationId);
            // Fail open - allow request if rate limiting fails
            return new RateLimitResult { IsAllowed = true };
        }
    }

    /// <summary>
    /// Records a request for rate limiting tracking
    /// </summary>
    public Task RecordRequestAsync(RateLimitRequest request)
    {
        var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString();

        try
        {
            // Record in IP bucket
            RecordInBucket(request.IpAddress, _ipBuckets, _config.IpLimits);

            // Record in user bucket (if authenticated)
            if (!string.IsNullOrEmpty(request.UserId))
            {
                RecordInBucket(request.UserId, _userBuckets, _config.UserLimits);
            }

            // Record in endpoint bucket
            var endpointKey = $"{request.Method}:{request.Endpoint}";
            RecordInBucket(endpointKey, _endpointBuckets, _config.EndpointLimits);

            _statistics.RecordRequest();

            _logger.LogDebug("Request recorded for rate limiting: {IpAddress} | {UserId} | {Endpoint} [{CorrelationId}]",
                request.IpAddress, request.UserId ?? "Anonymous", request.Endpoint, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording request for rate limiting [{CorrelationId}]", correlationId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets comprehensive rate limiting statistics
    /// </summary>
    public RateLimitStatistics GetStatistics()
    {
        var stats = _statistics.Clone();
        stats.ActiveIpBuckets = _ipBuckets.Count;
        stats.ActiveUserBuckets = _userBuckets.Count;
        stats.ActiveEndpointBuckets = _endpointBuckets.Count;
        stats.BlockedIps = _blockedIps.Count;
        return stats;
    }

    /// <summary>
    /// Temporarily blocks an IP address
    /// </summary>
    public async Task BlockIpAddressAsync(string ipAddress, TimeSpan duration, string reason)
    {
        var correlationId = Guid.NewGuid().ToString();
        var unblockTime = DateTime.UtcNow.Add(duration);

        _blockedIps.AddOrUpdate(ipAddress, unblockTime, (key, existing) => unblockTime);

        await _securityEventService.LogSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.SuspiciousActivity,
            IpAddress = ipAddress,
            CorrelationId = correlationId,
            AdditionalData = new Dictionary<string, object>
            {
                ["Action"] = "IPBlocked",
                ["Duration"] = duration.ToString(),
                ["Reason"] = reason,
                ["UnblockTime"] = unblockTime
            }
        });

        _logger.LogWarning("IP address {IpAddress} blocked for {Duration} - Reason: {Reason} [{CorrelationId}]",
            ipAddress, duration, reason, correlationId);
    }

    /// <summary>
    /// Checks if an IP address is currently blocked
    /// </summary>
    public Task<bool> IsIpBlockedAsync(string ipAddress)
    {
        if (_blockedIps.TryGetValue(ipAddress, out var unblockTime))
        {
            if (DateTime.UtcNow < unblockTime)
            {
                return Task.FromResult(true);
            }
            else
            {
                // Remove expired block
                _blockedIps.TryRemove(ipAddress, out _);
            }
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Checks bucket-based rate limit
    /// </summary>
    private RateLimitResult CheckBucketLimit(
        string key, 
        ConcurrentDictionary<string, RateLimitBucket> buckets, 
        RateLimitPolicy policy, 
        string limitType)
    {
        var bucket = buckets.GetOrAdd(key, _ => new RateLimitBucket(policy));
        
        if (!bucket.TryConsume())
        {
            var retryAfter = bucket.GetRetryAfter();
            return new RateLimitResult
            {
                IsAllowed = false,
                Reason = $"{limitType} rate limit exceeded",
                RetryAfter = retryAfter,
                RemainingRequests = 0,
                ResetTime = DateTime.UtcNow.Add(retryAfter)
            };
        }

        return new RateLimitResult
        {
            IsAllowed = true,
            RemainingRequests = bucket.RemainingTokens,
            ResetTime = bucket.NextRefillTime
        };
    }

    /// <summary>
    /// Records request in bucket
    /// </summary>
    private void RecordInBucket(
        string key, 
        ConcurrentDictionary<string, RateLimitBucket> buckets, 
        RateLimitPolicy policy)
    {
        var bucket = buckets.GetOrAdd(key, _ => new RateLimitBucket(policy));
        bucket.TryConsume(); // This will update the bucket state
    }

    /// <summary>
    /// Handles rate limit exceeded scenarios
    /// </summary>
    private async Task HandleRateLimitExceededAsync(RateLimitRequest request, string limitType, string correlationId)
    {
        _statistics.RecordBlocked();

        await _securityEventService.LogSecurityEventAsync(new SecurityEvent
        {
            EventType = SecurityEventType.RateLimitExceeded,
            UserId = request.UserId,
            IpAddress = request.IpAddress,
            CorrelationId = correlationId,
            AdditionalData = new Dictionary<string, object>
            {
                ["LimitType"] = limitType,
                ["Endpoint"] = request.Endpoint,
                ["Method"] = request.Method,
                ["UserAgent"] = request.UserAgent ?? "Unknown"
            }
        });

        // Auto-block IP if too many rate limit violations
        if (limitType == "IP")
        {
            var recentViolations = await CountRecentViolationsAsync(request.IpAddress);
            if (recentViolations > 5)
            {
                await BlockIpAddressAsync(request.IpAddress, TimeSpan.FromMinutes(15), "Excessive rate limit violations");
            }
        }

        _logger.LogWarning("Rate limit exceeded: {LimitType} for {IpAddress} | {UserId} | {Endpoint} [{CorrelationId}]",
            limitType, request.IpAddress, request.UserId ?? "Anonymous", request.Endpoint, correlationId);
    }

    /// <summary>
    /// Counts recent rate limit violations for an IP
    /// </summary>
    private Task<int> CountRecentViolationsAsync(string ipAddress)
    {
        // This would typically query the security events
        // For now, return a simplified count
        return Task.FromResult(0);
    }

    /// <summary>
    /// Cleanup expired buckets and blocked IPs
    /// </summary>
    private async Task CleanupExpiredBucketsAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1));

                var now = DateTime.UtcNow;

                // Cleanup expired IP blocks
                var expiredBlocks = _blockedIps.Where(kvp => now > kvp.Value).ToList();
                foreach (var block in expiredBlocks)
                {
                    _blockedIps.TryRemove(block.Key, out _);
                }

                // Cleanup inactive buckets
                CleanupInactiveBuckets(_ipBuckets, now);
                CleanupInactiveBuckets(_userBuckets, now);
                CleanupInactiveBuckets(_endpointBuckets, now);

                _logger.LogDebug("Rate limiting cleanup completed - IP buckets: {IpCount}, User buckets: {UserCount}, Endpoint buckets: {EndpointCount}, Blocked IPs: {BlockedCount}",
                    _ipBuckets.Count, _userBuckets.Count, _endpointBuckets.Count, _blockedIps.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during rate limiting cleanup");
            }
        }
    }

    /// <summary>
    /// Cleanup inactive buckets
    /// </summary>
    private void CleanupInactiveBuckets(ConcurrentDictionary<string, RateLimitBucket> buckets, DateTime now)
    {
        var inactiveBuckets = buckets.Where(kvp => now - kvp.Value.LastAccessTime > TimeSpan.FromMinutes(10)).ToList();
        foreach (var bucket in inactiveBuckets)
        {
            buckets.TryRemove(bucket.Key, out _);
        }
    }
}

/// <summary>
/// Rate limit request information
/// </summary>
public class RateLimitRequest
{
    public string IpAddress { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Rate limit check result
/// </summary>
public class RateLimitResult
{
    public bool IsAllowed { get; set; }
    public string? Reason { get; set; }
    public TimeSpan? RetryAfter { get; set; }
    public int RemainingRequests { get; set; }
    public DateTime? ResetTime { get; set; }
}

/// <summary>
/// Rate limiting statistics
/// </summary>
public class RateLimitStatistics
{
    private long _totalRequests;
    private long _allowedRequests;
    private long _blockedRequests;

    public long TotalRequests => _totalRequests;
    public long AllowedRequests => _allowedRequests;
    public long BlockedRequests => _blockedRequests;
    public double BlockedPercentage => _totalRequests > 0 ? (double)_blockedRequests / _totalRequests * 100 : 0;
    
    public int ActiveIpBuckets { get; set; }
    public int ActiveUserBuckets { get; set; }
    public int ActiveEndpointBuckets { get; set; }
    public int BlockedIps { get; set; }

    public void RecordRequest() => Interlocked.Increment(ref _totalRequests);
    public void RecordAllowed() => Interlocked.Increment(ref _allowedRequests);
    public void RecordBlocked() => Interlocked.Increment(ref _blockedRequests);

    public RateLimitStatistics Clone()
    {
        return new RateLimitStatistics
        {
            _totalRequests = _totalRequests,
            _allowedRequests = _allowedRequests,
            _blockedRequests = _blockedRequests,
            ActiveIpBuckets = ActiveIpBuckets,
            ActiveUserBuckets = ActiveUserBuckets,
            ActiveEndpointBuckets = ActiveEndpointBuckets,
            BlockedIps = BlockedIps
        };
    }
}

/// <summary>
/// Rate limiting configuration
/// </summary>
public class RateLimitConfiguration
{
    public RateLimitPolicy IpLimits { get; set; } = new() { RequestsPerMinute = 60, BurstSize = 10 };
    public RateLimitPolicy UserLimits { get; set; } = new() { RequestsPerMinute = 120, BurstSize = 20 };
    public RateLimitPolicy EndpointLimits { get; set; } = new() { RequestsPerMinute = 1000, BurstSize = 100 };
}

/// <summary>
/// Rate limiting policy
/// </summary>
public class RateLimitPolicy
{
    public int RequestsPerMinute { get; set; } = 60;
    public int BurstSize { get; set; } = 10;
}

/// <summary>
/// Token bucket implementation for rate limiting
/// </summary>
public class RateLimitBucket
{
    private readonly RateLimitPolicy _policy;
    private readonly object _lock = new();
    private int _tokens;
    private DateTime _lastRefill;

    public DateTime LastAccessTime { get; private set; }
    public DateTime NextRefillTime => _lastRefill.AddMinutes(1);
    public int RemainingTokens => _tokens;

    public RateLimitBucket(RateLimitPolicy policy)
    {
        _policy = policy;
        _tokens = policy.BurstSize;
        _lastRefill = DateTime.UtcNow;
        LastAccessTime = DateTime.UtcNow;
    }

    public bool TryConsume()
    {
        lock (_lock)
        {
            LastAccessTime = DateTime.UtcNow;
            RefillTokens();

            if (_tokens > 0)
            {
                _tokens--;
                return true;
            }

            return false;
        }
    }

    public TimeSpan GetRetryAfter()
    {
        lock (_lock)
        {
            RefillTokens();
            if (_tokens > 0) return TimeSpan.Zero;

            var nextRefill = _lastRefill.AddMinutes(1);
            var retryAfter = nextRefill - DateTime.UtcNow;
            return retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero;
        }
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var timeSinceRefill = now - _lastRefill;

        if (timeSinceRefill >= TimeSpan.FromMinutes(1))
        {
            var tokensToAdd = (int)(timeSinceRefill.TotalMinutes * _policy.RequestsPerMinute);
            _tokens = Math.Min(_policy.BurstSize, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }
}
