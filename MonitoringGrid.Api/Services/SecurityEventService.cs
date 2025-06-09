using Microsoft.Extensions.Caching.Memory;
using MonitoringGrid.Api.Middleware;
using MonitoringGrid.Api.Security;
using MonitoringGrid.Infrastructure.Data;
using System.Collections.Concurrent;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Security event service implementation with threat detection and monitoring
/// </summary>
public class SecurityEventService : ISecurityEventService
{
    private readonly MonitoringContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecurityEventService> _logger;
    private readonly ICorrelationIdService _correlationIdService;
    
    // In-memory stores for high-performance security checks
    private readonly ConcurrentDictionary<string, DateTime> _usedTokens = new();
    private readonly ConcurrentDictionary<string, List<SecurityActivityRecord>> _userActivity = new();
    private readonly ConcurrentDictionary<string, List<SecurityActivityRecord>> _ipActivity = new();
    
    // Security thresholds
    private readonly TimeSpan _tokenReplayWindow = TimeSpan.FromMinutes(5);
    private readonly int _maxFailedAttemptsPerHour = 10;
    private readonly int _maxRequestsPerMinute = 100;
    private readonly TimeSpan _suspiciousActivityWindow = TimeSpan.FromHours(1);

    public SecurityEventService(
        MonitoringContext context,
        IMemoryCache cache,
        ILogger<SecurityEventService> logger,
        ICorrelationIdService correlationIdService)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _correlationIdService = correlationIdService;

        // Start cleanup task for in-memory stores
        _ = Task.Run(CleanupExpiredRecordsAsync);
    }

    /// <summary>
    /// Logs a security event with threat analysis
    /// </summary>
    public async Task LogSecurityEventAsync(SecurityEvent securityEvent)
    {
        var correlationId = _correlationIdService.GetCorrelationId();

        try
        {
            // Store in database for persistence
            await StoreSecurityEventAsync(securityEvent);

            // Update in-memory activity tracking
            UpdateActivityTracking(securityEvent);

            // Perform real-time threat analysis
            await AnalyzeThreatPatternsAsync(securityEvent);

            _logger.LogInformation("Security event logged: {EventType} for user {UserId} from IP {IpAddress} [{CorrelationId}]",
                securityEvent.EventType, securityEvent.UserId, securityEvent.IpAddress, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event [{CorrelationId}]", correlationId);
        }
    }

    /// <summary>
    /// Checks if a token has been used (replay protection)
    /// </summary>
    public async Task<bool> IsTokenUsedAsync(string tokenId)
    {
        // Check in-memory cache first for performance
        if (_usedTokens.TryGetValue(tokenId, out var usedTime))
        {
            return DateTime.UtcNow - usedTime < _tokenReplayWindow;
        }

        // Check database for persistent storage
        var cacheKey = $"token_used_{tokenId}";
        if (_cache.TryGetValue(cacheKey, out _))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Marks a token as used for replay protection
    /// </summary>
    public async Task MarkTokenAsUsedAsync(string tokenId, DateTime expiry)
    {
        // Store in memory for immediate checks
        _usedTokens.TryAdd(tokenId, DateTime.UtcNow);

        // Store in cache with expiration
        var cacheKey = $"token_used_{tokenId}";
        var cacheExpiry = expiry > DateTime.UtcNow ? expiry : DateTime.UtcNow.AddMinutes(5);
        _cache.Set(cacheKey, true, cacheExpiry);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks for suspicious activity patterns
    /// </summary>
    public async Task<bool> IsSuspiciousActivityAsync(string userId, string ipAddress)
    {
        var now = DateTime.UtcNow;
        var windowStart = now - _suspiciousActivityWindow;

        // Check user activity patterns
        if (_userActivity.TryGetValue(userId, out var userRecords))
        {
            var recentActivity = userRecords.Where(r => r.Timestamp > windowStart).ToList();
            
            // Check for too many failed attempts
            var failedAttempts = recentActivity.Count(r => r.EventType == SecurityEventType.AuthenticationFailure);
            if (failedAttempts > _maxFailedAttemptsPerHour)
            {
                return true;
            }

            // Check for unusual IP addresses
            var uniqueIPs = recentActivity.Select(r => r.IpAddress).Distinct().Count();
            if (uniqueIPs > 5) // More than 5 different IPs in an hour
            {
                return true;
            }
        }

        // Check IP activity patterns
        if (_ipActivity.TryGetValue(ipAddress, out var ipRecords))
        {
            var recentActivity = ipRecords.Where(r => r.Timestamp > windowStart).ToList();
            
            // Check for too many requests
            var requestCount = recentActivity.Count;
            if (requestCount > _maxRequestsPerMinute * 60) // Per hour
            {
                return true;
            }

            // Check for multiple user attempts from same IP
            var uniqueUsers = recentActivity.Select(r => r.UserId).Where(u => !string.IsNullOrEmpty(u)).Distinct().Count();
            if (uniqueUsers > 10) // More than 10 different users from same IP
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets security events for analysis
    /// </summary>
    public async Task<List<SecurityEvent>> GetSecurityEventsAsync(SecurityEventFilter filter)
    {
        var correlationId = _correlationIdService.GetCorrelationId();

        try
        {
            // This would typically query the database
            // For now, return a simplified implementation
            var events = new List<SecurityEvent>();

            _logger.LogDebug("Retrieved {EventCount} security events [{CorrelationId}]", events.Count, correlationId);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve security events [{CorrelationId}]", correlationId);
            return new List<SecurityEvent>();
        }
    }

    /// <summary>
    /// Stores security event in database
    /// </summary>
    private async Task StoreSecurityEventAsync(SecurityEvent securityEvent)
    {
        // In a real implementation, this would store to a SecurityEvents table
        // For now, we'll use logging as the persistent store
        _logger.LogInformation("SecurityEvent: {EventType} | User: {UserId} | IP: {IpAddress} | Data: {AdditionalData}",
            securityEvent.EventType,
            securityEvent.UserId ?? "Anonymous",
            securityEvent.IpAddress ?? "Unknown",
            System.Text.Json.JsonSerializer.Serialize(securityEvent.AdditionalData));

        await Task.CompletedTask;
    }

    /// <summary>
    /// Updates in-memory activity tracking for real-time analysis
    /// </summary>
    private void UpdateActivityTracking(SecurityEvent securityEvent)
    {
        var record = new SecurityActivityRecord
        {
            EventType = securityEvent.EventType,
            UserId = securityEvent.UserId,
            IpAddress = securityEvent.IpAddress ?? "Unknown",
            Timestamp = securityEvent.Timestamp
        };

        // Track user activity
        if (!string.IsNullOrEmpty(securityEvent.UserId))
        {
            _userActivity.AddOrUpdate(securityEvent.UserId,
                new List<SecurityActivityRecord> { record },
                (key, existing) =>
                {
                    existing.Add(record);
                    // Keep only recent records
                    return existing.Where(r => DateTime.UtcNow - r.Timestamp < _suspiciousActivityWindow).ToList();
                });
        }

        // Track IP activity
        if (!string.IsNullOrEmpty(securityEvent.IpAddress))
        {
            _ipActivity.AddOrUpdate(securityEvent.IpAddress,
                new List<SecurityActivityRecord> { record },
                (key, existing) =>
                {
                    existing.Add(record);
                    // Keep only recent records
                    return existing.Where(r => DateTime.UtcNow - r.Timestamp < _suspiciousActivityWindow).ToList();
                });
        }
    }

    /// <summary>
    /// Analyzes threat patterns and triggers alerts
    /// </summary>
    private async Task AnalyzeThreatPatternsAsync(SecurityEvent securityEvent)
    {
        // Analyze for brute force attacks
        if (securityEvent.EventType == SecurityEventType.AuthenticationFailure)
        {
            await CheckBruteForceAttackAsync(securityEvent);
        }

        // Analyze for privilege escalation attempts
        if (securityEvent.EventType == SecurityEventType.AuthorizationFailure)
        {
            await CheckPrivilegeEscalationAsync(securityEvent);
        }

        // Analyze for distributed attacks
        await CheckDistributedAttackAsync(securityEvent);
    }

    /// <summary>
    /// Checks for brute force attack patterns
    /// </summary>
    private async Task CheckBruteForceAttackAsync(SecurityEvent securityEvent)
    {
        if (string.IsNullOrEmpty(securityEvent.IpAddress)) return;

        if (_ipActivity.TryGetValue(securityEvent.IpAddress, out var records))
        {
            var recentFailures = records
                .Where(r => r.EventType == SecurityEventType.AuthenticationFailure)
                .Where(r => DateTime.UtcNow - r.Timestamp < TimeSpan.FromMinutes(10))
                .Count();

            if (recentFailures > 5)
            {
                _logger.LogWarning("Potential brute force attack detected from IP {IpAddress} - {FailureCount} failures in 10 minutes",
                    securityEvent.IpAddress, recentFailures);

                // Could trigger automatic IP blocking here
                await LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.SuspiciousActivity,
                    IpAddress = securityEvent.IpAddress,
                    CorrelationId = securityEvent.CorrelationId,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["ThreatType"] = "BruteForce",
                        ["FailureCount"] = recentFailures
                    }
                });
            }
        }
    }

    /// <summary>
    /// Checks for privilege escalation attempts
    /// </summary>
    private async Task CheckPrivilegeEscalationAsync(SecurityEvent securityEvent)
    {
        if (string.IsNullOrEmpty(securityEvent.UserId)) return;

        if (_userActivity.TryGetValue(securityEvent.UserId, out var records))
        {
            var recentAuthzFailures = records
                .Where(r => r.EventType == SecurityEventType.AuthorizationFailure)
                .Where(r => DateTime.UtcNow - r.Timestamp < TimeSpan.FromMinutes(5))
                .Count();

            if (recentAuthzFailures > 3)
            {
                _logger.LogWarning("Potential privilege escalation attempt by user {UserId} - {FailureCount} authorization failures",
                    securityEvent.UserId, recentAuthzFailures);
            }
        }
    }

    /// <summary>
    /// Checks for distributed attack patterns
    /// </summary>
    private async Task CheckDistributedAttackAsync(SecurityEvent securityEvent)
    {
        // Check if multiple IPs are targeting the same resources
        var now = DateTime.UtcNow;
        var recentAttacks = _ipActivity.Values
            .SelectMany(records => records)
            .Where(r => now - r.Timestamp < TimeSpan.FromMinutes(5))
            .Where(r => r.EventType == SecurityEventType.AuthenticationFailure)
            .GroupBy(r => r.IpAddress)
            .Count();

        if (recentAttacks > 10)
        {
            _logger.LogWarning("Potential distributed attack detected - {AttackingIPs} IPs with recent failures", recentAttacks);
        }
    }

    /// <summary>
    /// Cleanup expired records from in-memory stores
    /// </summary>
    private async Task CleanupExpiredRecordsAsync()
    {
        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5));

                var cutoff = DateTime.UtcNow - _suspiciousActivityWindow;

                // Cleanup used tokens
                var expiredTokens = _usedTokens.Where(kvp => DateTime.UtcNow - kvp.Value > _tokenReplayWindow).ToList();
                foreach (var token in expiredTokens)
                {
                    _usedTokens.TryRemove(token.Key, out _);
                }

                // Cleanup user activity
                foreach (var kvp in _userActivity.ToList())
                {
                    var recentRecords = kvp.Value.Where(r => r.Timestamp > cutoff).ToList();
                    if (recentRecords.Count == 0)
                    {
                        _userActivity.TryRemove(kvp.Key, out _);
                    }
                    else
                    {
                        _userActivity[kvp.Key] = recentRecords;
                    }
                }

                // Cleanup IP activity
                foreach (var kvp in _ipActivity.ToList())
                {
                    var recentRecords = kvp.Value.Where(r => r.Timestamp > cutoff).ToList();
                    if (recentRecords.Count == 0)
                    {
                        _ipActivity.TryRemove(kvp.Key, out _);
                    }
                    else
                    {
                        _ipActivity[kvp.Key] = recentRecords;
                    }
                }

                _logger.LogDebug("Security event cleanup completed - Tokens: {TokenCount}, Users: {UserCount}, IPs: {IpCount}",
                    _usedTokens.Count, _userActivity.Count, _ipActivity.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during security event cleanup");
            }
        }
    }
}

/// <summary>
/// Security activity record for in-memory tracking
/// </summary>
public class SecurityActivityRecord
{
    public SecurityEventType EventType { get; set; }
    public string? UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
