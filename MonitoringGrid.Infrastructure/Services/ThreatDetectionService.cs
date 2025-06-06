using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Threat detection service for identifying and managing security threats
/// </summary>
public class ThreatDetectionService : IThreatDetectionService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<ThreatDetectionService> _logger;

    public ThreatDetectionService(
        MonitoringContext context,
        ILogger<ThreatDetectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SecurityThreat>> DetectThreatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Starting threat detection scan");

            var threats = new List<SecurityThreat>();

            // Detect brute force attacks
            var bruteForceThreats = await DetectBruteForceAttacksAsync(cancellationToken);
            threats.AddRange(bruteForceThreats);

            // Detect suspicious IP addresses
            var suspiciousIpThreats = await DetectSuspiciousIpAddressesAsync(cancellationToken);
            threats.AddRange(suspiciousIpThreats);

            // Detect unusual user behavior
            var behaviorThreats = await DetectUnusualUserBehaviorAsync(cancellationToken);
            threats.AddRange(behaviorThreats);

            _logger.LogInformation("Threat detection completed. Found {ThreatCount} threats", threats.Count);

            return threats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during threat detection");
            throw;
        }
    }

    public async Task<bool> IsIpAddressSuspiciousAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if IP is in known threat list
            var knownThreat = await _context.Set<SecurityThreat>()
                .AnyAsync(st => st.IpAddress == ipAddress && 
                               st.ThreatType == "SUSPICIOUS_IP" && 
                               !st.IsResolved, cancellationToken);

            if (knownThreat)
                return true;

            // Check for multiple failed login attempts from this IP
            var recentFailures = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.IpAddress == ipAddress && 
                             sae.EventType == "LOGIN_FAILED" && 
                             sae.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .CountAsync(cancellationToken);

            if (recentFailures >= 10) // Threshold for suspicious activity
            {
                await ReportSuspiciousIpAsync(ipAddress, $"Multiple failed login attempts: {recentFailures}", cancellationToken);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if IP address {IpAddress} is suspicious", ipAddress);
            return false;
        }
    }

    public async Task<bool> IsUserBehaviorSuspiciousAsync(string userId, string action, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for unusual activity patterns
            var recentActions = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.UserId == userId && 
                             sae.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .CountAsync(cancellationToken);

            // Threshold for suspicious activity (more than 100 actions per hour)
            if (recentActions > 100)
            {
                await ReportSuspiciousUserBehaviorAsync(userId, action, $"Excessive activity: {recentActions} actions in 1 hour", cancellationToken);
                return true;
            }

            // Check for failed permission attempts
            var failedPermissions = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.UserId == userId && 
                             sae.EventType == "PERMISSION_DENIED" && 
                             sae.Timestamp >= DateTime.UtcNow.AddMinutes(-30))
                .CountAsync(cancellationToken);

            if (failedPermissions >= 5)
            {
                await ReportSuspiciousUserBehaviorAsync(userId, action, $"Multiple permission denials: {failedPermissions}", cancellationToken);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking user behavior for {UserId}", userId);
            return false;
        }
    }

    public async Task ReportThreatAsync(SecurityThreat threat, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Reporting security threat: {ThreatType} - {Description}", threat.ThreatType, threat.Description);

            _context.Set<SecurityThreat>().Add(threat);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Security threat {ThreatId} reported successfully", threat.ThreatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reporting security threat {ThreatId}", threat.ThreatId);
            throw;
        }
    }

    public async Task<List<SecurityThreat>> GetActiveThreatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Set<SecurityThreat>()
                .Where(st => !st.IsResolved)
                .OrderByDescending(st => st.DetectedAt)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active threats");
            throw;
        }
    }

    public async Task ResolveThreatAsync(string threatId, string resolution, CancellationToken cancellationToken = default)
    {
        try
        {
            var threat = await _context.Set<SecurityThreat>()
                .FirstOrDefaultAsync(st => st.ThreatId == threatId, cancellationToken);

            if (threat == null)
            {
                _logger.LogWarning("Threat {ThreatId} not found", threatId);
                return;
            }

            threat.IsResolved = true;
            threat.ResolvedAt = DateTime.UtcNow;
            threat.Resolution = resolution;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Threat {ThreatId} resolved: {Resolution}", threatId, resolution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving threat {ThreatId}", threatId);
            throw;
        }
    }

    private async Task<List<SecurityThreat>> DetectBruteForceAttacksAsync(CancellationToken cancellationToken)
    {
        var threats = new List<SecurityThreat>();

        try
        {
            // Group failed login attempts by IP address in the last hour
            var suspiciousIps = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.EventType == "LOGIN_FAILED" && 
                             sae.Timestamp >= DateTime.UtcNow.AddHours(-1))
                .GroupBy(sae => sae.IpAddress)
                .Where(g => g.Count() >= 20) // 20+ failed attempts in 1 hour
                .Select(g => new { IpAddress = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            foreach (var suspiciousIp in suspiciousIps)
            {
                if (string.IsNullOrEmpty(suspiciousIp.IpAddress)) continue;

                var threat = new SecurityThreat
                {
                    ThreatId = Guid.NewGuid().ToString(),
                    ThreatType = "BRUTE_FORCE_ATTACK",
                    Severity = "High",
                    Description = $"Brute force attack detected from IP {suspiciousIp.IpAddress} with {suspiciousIp.Count} failed login attempts",
                    IpAddress = suspiciousIp.IpAddress,
                    DetectedAt = DateTime.UtcNow,
                    IsResolved = false,
                    ThreatData = new Dictionary<string, object>
                    {
                        ["FailedAttempts"] = suspiciousIp.Count,
                        ["TimeWindow"] = "1 hour"
                    }
                };

                threats.Add(threat);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting brute force attacks");
        }

        return threats;
    }

    private async Task<List<SecurityThreat>> DetectSuspiciousIpAddressesAsync(CancellationToken cancellationToken)
    {
        var threats = new List<SecurityThreat>();

        try
        {
            // Detect IPs with multiple different usernames
            var suspiciousIps = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.EventType == "LOGIN_FAILED" && 
                             sae.Timestamp >= DateTime.UtcNow.AddHours(-24))
                .GroupBy(sae => sae.IpAddress)
                .Where(g => g.Select(x => x.Username).Distinct().Count() >= 10) // 10+ different usernames
                .Select(g => new { IpAddress = g.Key, UserCount = g.Select(x => x.Username).Distinct().Count() })
                .ToListAsync(cancellationToken);

            foreach (var suspiciousIp in suspiciousIps)
            {
                if (string.IsNullOrEmpty(suspiciousIp.IpAddress)) continue;

                var threat = new SecurityThreat
                {
                    ThreatId = Guid.NewGuid().ToString(),
                    ThreatType = "SUSPICIOUS_IP",
                    Severity = "Medium",
                    Description = $"Suspicious IP {suspiciousIp.IpAddress} attempted login with {suspiciousIp.UserCount} different usernames",
                    IpAddress = suspiciousIp.IpAddress,
                    DetectedAt = DateTime.UtcNow,
                    IsResolved = false,
                    ThreatData = new Dictionary<string, object>
                    {
                        ["UniqueUsernames"] = suspiciousIp.UserCount,
                        ["TimeWindow"] = "24 hours"
                    }
                };

                threats.Add(threat);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting suspicious IP addresses");
        }

        return threats;
    }

    private async Task<List<SecurityThreat>> DetectUnusualUserBehaviorAsync(CancellationToken cancellationToken)
    {
        var threats = new List<SecurityThreat>();

        try
        {
            // Detect users with excessive activity
            var suspiciousUsers = await _context.Set<SecurityAuditEvent>()
                .Where(sae => sae.Timestamp >= DateTime.UtcNow.AddHours(-1) && 
                             !string.IsNullOrEmpty(sae.UserId))
                .GroupBy(sae => sae.UserId)
                .Where(g => g.Count() >= 200) // 200+ actions in 1 hour
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            foreach (var suspiciousUser in suspiciousUsers)
            {
                if (string.IsNullOrEmpty(suspiciousUser.UserId)) continue;

                var threat = new SecurityThreat
                {
                    ThreatId = Guid.NewGuid().ToString(),
                    ThreatType = "UNUSUAL_USER_BEHAVIOR",
                    Severity = "Medium",
                    Description = $"User {suspiciousUser.UserId} performed {suspiciousUser.Count} actions in 1 hour",
                    UserId = suspiciousUser.UserId,
                    DetectedAt = DateTime.UtcNow,
                    IsResolved = false,
                    ThreatData = new Dictionary<string, object>
                    {
                        ["ActionCount"] = suspiciousUser.Count,
                        ["TimeWindow"] = "1 hour"
                    }
                };

                threats.Add(threat);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting unusual user behavior");
        }

        return threats;
    }

    private async Task ReportSuspiciousIpAsync(string ipAddress, string description, CancellationToken cancellationToken)
    {
        var threat = new SecurityThreat
        {
            ThreatId = Guid.NewGuid().ToString(),
            ThreatType = "SUSPICIOUS_IP",
            Severity = "High",
            Description = description,
            IpAddress = ipAddress,
            DetectedAt = DateTime.UtcNow,
            IsResolved = false
        };

        await ReportThreatAsync(threat, cancellationToken);
    }

    private async Task ReportSuspiciousUserBehaviorAsync(string userId, string action, string description, CancellationToken cancellationToken)
    {
        var threat = new SecurityThreat
        {
            ThreatId = Guid.NewGuid().ToString(),
            ThreatType = "SUSPICIOUS_USER_BEHAVIOR",
            Severity = "Medium",
            Description = description,
            UserId = userId,
            DetectedAt = DateTime.UtcNow,
            IsResolved = false,
            ThreatData = new Dictionary<string, object>
            {
                ["Action"] = action
            }
        };

        await ReportThreatAsync(threat, cancellationToken);
    }
}
