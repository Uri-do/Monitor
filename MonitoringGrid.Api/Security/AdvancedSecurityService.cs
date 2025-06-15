using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using System.Collections.Concurrent;

namespace MonitoringGrid.Api.Security;

/// <summary>
/// Advanced security service for comprehensive security operations
/// </summary>
public interface IAdvancedSecurityService
{
    /// <summary>
    /// Validates API key and returns associated claims
    /// </summary>
    Task<SecurityValidationResult> ValidateApiKeyAsync(string apiKey);

    /// <summary>
    /// Generates secure API key with metadata
    /// </summary>
    Task<ApiKeyInfo> GenerateApiKeyAsync(string userId, string description, TimeSpan? expiration = null);

    /// <summary>
    /// Revokes an API key
    /// </summary>
    Task<bool> RevokeApiKeyAsync(string apiKey);

    /// <summary>
    /// Encrypts sensitive data
    /// </summary>
    string EncryptSensitiveData(string data);

    /// <summary>
    /// Decrypts sensitive data
    /// </summary>
    string DecryptSensitiveData(string encryptedData);

    /// <summary>
    /// Validates request signature for webhook security
    /// </summary>
    bool ValidateRequestSignature(string payload, string signature, string secret);

    /// <summary>
    /// Generates secure random token
    /// </summary>
    string GenerateSecureToken(int length = 32);

    /// <summary>
    /// Checks if request is from trusted source
    /// </summary>
    Task<bool> IsTrustedSourceAsync(string ipAddress, string userAgent);

    /// <summary>
    /// Records security event
    /// </summary>
    Task RecordSecurityEventAsync(SecurityEvent securityEvent);

    /// <summary>
    /// Gets security metrics
    /// </summary>
    Task<SecurityMetrics> GetSecurityMetricsAsync(TimeSpan period);
}

/// <summary>
/// Implementation of advanced security service
/// </summary>
public class AdvancedSecurityService : IAdvancedSecurityService
{
    private readonly IDataProtector _dataProtector;
    private readonly ILogger<AdvancedSecurityService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<string, ApiKeyInfo> _apiKeys = new();
    private readonly ConcurrentDictionary<string, SecurityEvent> _securityEvents = new();
    private readonly ConcurrentDictionary<string, TrustedSource> _trustedSources = new();

    public AdvancedSecurityService(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<AdvancedSecurityService> logger,
        IConfiguration configuration)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("MonitoringGrid.Security");
        _logger = logger;
        _configuration = configuration;
        
        InitializeTrustedSources();
    }

    public async Task<SecurityValidationResult> ValidateApiKeyAsync(string apiKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return SecurityValidationResult.Failed("API key is required");
            }

            // Hash the API key for lookup
            var hashedKey = HashApiKey(apiKey);
            
            if (!_apiKeys.TryGetValue(hashedKey, out var keyInfo))
            {
                await RecordSecurityEventAsync(new SecurityEvent
                {
                    Type = SecurityEventType.InvalidApiKey,
                    Description = "Invalid API key used",
                    IpAddress = GetCurrentIpAddress(),
                    Timestamp = DateTime.UtcNow
                });
                
                return SecurityValidationResult.Failed("Invalid API key");
            }

            // Check if key is expired
            if (keyInfo.ExpiresAt.HasValue && keyInfo.ExpiresAt.Value < DateTime.UtcNow)
            {
                await RecordSecurityEventAsync(new SecurityEvent
                {
                    Type = SecurityEventType.ExpiredApiKey,
                    Description = "Expired API key used",
                    UserId = keyInfo.UserId,
                    IpAddress = GetCurrentIpAddress(),
                    Timestamp = DateTime.UtcNow
                });
                
                return SecurityValidationResult.Failed("API key has expired");
            }

            // Check if key is revoked
            if (keyInfo.IsRevoked)
            {
                await RecordSecurityEventAsync(new SecurityEvent
                {
                    Type = SecurityEventType.RevokedApiKey,
                    Description = "Revoked API key used",
                    UserId = keyInfo.UserId,
                    IpAddress = GetCurrentIpAddress(),
                    Timestamp = DateTime.UtcNow
                });
                
                return SecurityValidationResult.Failed("API key has been revoked");
            }

            // Update last used timestamp
            keyInfo.LastUsedAt = DateTime.UtcNow;
            keyInfo.UsageCount++;

            return SecurityValidationResult.Success(keyInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key");
            return SecurityValidationResult.Failed("Internal security error");
        }
    }

    public async Task<ApiKeyInfo> GenerateApiKeyAsync(string userId, string description, TimeSpan? expiration = null)
    {
        try
        {
            var keyId = Guid.NewGuid().ToString();
            var apiKey = GenerateSecureToken(64);
            var hashedKey = HashApiKey(apiKey);

            var keyInfo = new ApiKeyInfo
            {
                KeyId = keyId,
                HashedKey = hashedKey,
                UserId = userId,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null,
                IsRevoked = false,
                UsageCount = 0
            };

            _apiKeys[hashedKey] = keyInfo;

            await RecordSecurityEventAsync(new SecurityEvent
            {
                Type = SecurityEventType.ApiKeyGenerated,
                Description = $"API key generated: {description}",
                UserId = userId,
                Timestamp = DateTime.UtcNow
            });

            // Return the plain API key only once
            keyInfo.PlainKey = apiKey;
            return keyInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating API key for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> RevokeApiKeyAsync(string apiKey)
    {
        try
        {
            var hashedKey = HashApiKey(apiKey);
            
            if (_apiKeys.TryGetValue(hashedKey, out var keyInfo))
            {
                keyInfo.IsRevoked = true;
                keyInfo.RevokedAt = DateTime.UtcNow;

                await RecordSecurityEventAsync(new SecurityEvent
                {
                    Type = SecurityEventType.ApiKeyRevoked,
                    Description = $"API key revoked: {keyInfo.Description}",
                    UserId = keyInfo.UserId,
                    Timestamp = DateTime.UtcNow
                });

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key");
            return false;
        }
    }

    public string EncryptSensitiveData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        try
        {
            return _dataProtector.Protect(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting sensitive data");
            throw;
        }
    }

    public string DecryptSensitiveData(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
            return encryptedData;

        try
        {
            return _dataProtector.Unprotect(encryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting sensitive data");
            throw;
        }
    }

    public bool ValidateRequestSignature(string payload, string signature, string secret)
    {
        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = Convert.ToBase64String(computedHash);
            
            return string.Equals(signature, computedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating request signature");
            return false;
        }
    }

    public string GenerateSecureToken(int length = 32)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "")[..length];
    }

    public async Task<bool> IsTrustedSourceAsync(string ipAddress, string userAgent)
    {
        try
        {
            var sourceKey = $"{ipAddress}:{userAgent}";
            
            if (_trustedSources.TryGetValue(sourceKey, out var trustedSource))
            {
                return trustedSource.IsTrusted && trustedSource.ExpiresAt > DateTime.UtcNow;
            }

            // Check against configured trusted sources
            var trustedIps = _configuration.GetSection("Security:TrustedIpAddresses").Get<string[]>() ?? Array.Empty<string>();
            var isTrustedIp = trustedIps.Contains(ipAddress);

            if (isTrustedIp)
            {
                _trustedSources[sourceKey] = new TrustedSource
                {
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    IsTrusted = true,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking trusted source for IP {IpAddress}", ipAddress);
            return false;
        }
    }

    public async Task RecordSecurityEventAsync(SecurityEvent securityEvent)
    {
        try
        {
            var eventId = Guid.NewGuid().ToString();
            securityEvent.EventId = eventId;
            
            _securityEvents[eventId] = securityEvent;

            _logger.LogWarning("Security event recorded: {EventType} - {Description} (User: {UserId}, IP: {IpAddress})",
                securityEvent.Type, securityEvent.Description, securityEvent.UserId, securityEvent.IpAddress);

            // In a real implementation, you would persist this to a database
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording security event");
        }
    }

    public async Task<SecurityMetrics> GetSecurityMetricsAsync(TimeSpan period)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow - period;
            var recentEvents = _securityEvents.Values
                .Where(e => e.Timestamp >= cutoffTime)
                .ToList();

            var metrics = new SecurityMetrics
            {
                Period = period,
                TotalEvents = recentEvents.Count,
                EventsByType = recentEvents.GroupBy(e => e.Type)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                UniqueIpAddresses = recentEvents.Select(e => e.IpAddress).Distinct().Count(),
                UniqueUsers = recentEvents.Where(e => !string.IsNullOrEmpty(e.UserId))
                    .Select(e => e.UserId).Distinct().Count(),
                TopThreats = GetTopThreats(recentEvents),
                SecurityScore = CalculateSecurityScore(recentEvents)
            };

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security metrics");
            throw;
        }
    }

    private string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(hashedBytes);
    }

    private string GetCurrentIpAddress()
    {
        // This would typically get the IP from HttpContext
        return "127.0.0.1";
    }

    private void InitializeTrustedSources()
    {
        // Initialize with configured trusted sources
        var trustedIps = _configuration.GetSection("Security:TrustedIpAddresses").Get<string[]>() ?? Array.Empty<string>();
        
        foreach (var ip in trustedIps)
        {
            _trustedSources[$"{ip}:*"] = new TrustedSource
            {
                IpAddress = ip,
                UserAgent = "*",
                IsTrusted = true,
                ExpiresAt = DateTime.MaxValue
            };
        }
    }

    private List<ThreatInfo> GetTopThreats(List<SecurityEvent> events)
    {
        return events
            .Where(e => e.Type == SecurityEventType.InvalidApiKey || 
                       e.Type == SecurityEventType.SuspiciousActivity ||
                       e.Type == SecurityEventType.BruteForceAttempt)
            .GroupBy(e => e.IpAddress)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => new ThreatInfo
            {
                IpAddress = g.Key ?? "Unknown",
                EventCount = g.Count(),
                LastEventTime = g.Max(e => e.Timestamp),
                ThreatLevel = CalculateThreatLevel(g.Count())
            })
            .ToList();
    }

    private double CalculateSecurityScore(List<SecurityEvent> events)
    {
        if (!events.Any()) return 100.0;

        var baseScore = 100.0;
        var penaltyPerEvent = Math.Min(events.Count * 0.5, 50.0);
        
        return Math.Max(baseScore - penaltyPerEvent, 0.0);
    }

    private ThreatLevel CalculateThreatLevel(int eventCount)
    {
        return eventCount switch
        {
            >= 100 => ThreatLevel.Critical,
            >= 50 => ThreatLevel.High,
            >= 20 => ThreatLevel.Medium,
            >= 5 => ThreatLevel.Low,
            _ => ThreatLevel.Minimal
        };
    }
}
