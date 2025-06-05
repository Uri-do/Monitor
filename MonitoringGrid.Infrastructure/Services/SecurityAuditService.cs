using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Infrastructure.Data;
using System.Text.Json;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Security audit service for comprehensive security event logging
/// </summary>
public class SecurityAuditService : ISecurityAuditService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<SecurityAuditService> _logger;
    private readonly IThreatDetectionService _threatDetectionService;

    public SecurityAuditService(
        MonitoringContext context,
        ILogger<SecurityAuditService> logger,
        IThreatDetectionService threatDetectionService)
    {
        _context = context;
        _logger = logger;
        _threatDetectionService = threatDetectionService;
    }

    public async Task LogSecurityEventAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            auditEvent.EventId = Guid.NewGuid().ToString();
            auditEvent.Timestamp = DateTime.UtcNow;

            // Store in database
            _context.Set<SecurityAuditEvent>().Add(auditEvent);
            await _context.SaveChangesAsync(cancellationToken);

            // Log to application logger
            var logLevel = auditEvent.Severity switch
            {
                "Critical" => LogLevel.Critical,
                "Error" => LogLevel.Error,
                "Warning" => LogLevel.Warning,
                "Information" => LogLevel.Information,
                _ => LogLevel.Debug
            };

            _logger.Log(logLevel, "Security Event: {EventType} - {Action} on {Resource} by {Username} from {IpAddress}. Success: {IsSuccess}",
                auditEvent.EventType, auditEvent.Action, auditEvent.Resource, auditEvent.Username, auditEvent.IpAddress, auditEvent.IsSuccess);

            // Check for suspicious activity
            if (!auditEvent.IsSuccess || IsSuspiciousEvent(auditEvent))
            {
                await _threatDetectionService.IsUserBehaviorSuspiciousAsync(auditEvent.UserId ?? "unknown", auditEvent.Action ?? "unknown", cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event: {EventType}", auditEvent.EventType);
        }
    }

    public async Task LogLoginAttemptAsync(string username, string ipAddress, bool success, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED",
                Username = username,
                IpAddress = ipAddress,
                Action = "LOGIN",
                Resource = "Authentication",
                IsSuccess = success,
                ErrorMessage = success ? null : reason,
                Severity = success ? "Information" : "Warning",
                AdditionalData = new Dictionary<string, object>
                {
                    ["LoginAttempt"] = true,
                    ["Reason"] = reason ?? "N/A"
                }
            };

            await LogSecurityEventAsync(auditEvent, cancellationToken);

            // Additional logging for failed attempts
            if (!success)
            {
                await LogFailedLoginAttemptAsync(username, ipAddress, reason, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log login attempt for user {Username}", username);
        }
    }

    public async Task LogPasswordChangeAsync(string userId, bool success, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = "PASSWORD_CHANGE",
                UserId = userId,
                Action = "CHANGE_PASSWORD",
                Resource = "UserAccount",
                IsSuccess = success,
                Severity = success ? "Information" : "Warning",
                AdditionalData = new Dictionary<string, object>
                {
                    ["PasswordChange"] = true,
                    ["Timestamp"] = DateTime.UtcNow
                }
            };

            await LogSecurityEventAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log password change for user {UserId}", userId);
        }
    }

    public async Task LogPermissionChangeAsync(string userId, string action, string resource, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = "PERMISSION_CHANGE",
                UserId = userId,
                Action = action,
                Resource = resource,
                IsSuccess = true,
                Severity = "Information",
                AdditionalData = new Dictionary<string, object>
                {
                    ["PermissionChange"] = true,
                    ["Action"] = action,
                    ["Resource"] = resource
                }
            };

            await LogSecurityEventAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log permission change for user {UserId}", userId);
        }
    }

    public async Task LogSuspiciousActivityAsync(string? userId, string activityType, string description, string ipAddress, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = "SUSPICIOUS_ACTIVITY",
                UserId = userId,
                IpAddress = ipAddress,
                Action = activityType,
                Resource = "System",
                IsSuccess = false,
                ErrorMessage = description,
                Severity = "Critical",
                AdditionalData = new Dictionary<string, object>
                {
                    ["SuspiciousActivity"] = true,
                    ["ActivityType"] = activityType,
                    ["Description"] = description,
                    ["RequiresInvestigation"] = true
                }
            };

            await LogSecurityEventAsync(auditEvent, cancellationToken);

            // Create security threat
            var threat = new SecurityThreat
            {
                ThreatId = Guid.NewGuid().ToString(),
                ThreatType = activityType,
                Severity = "High",
                Description = description,
                UserId = userId,
                IpAddress = ipAddress,
                DetectedAt = DateTime.UtcNow,
                IsResolved = false,
                ThreatData = new Dictionary<string, object>
                {
                    ["SourceEvent"] = auditEvent.EventId,
                    ["AutoDetected"] = true
                }
            };

            await _threatDetectionService.ReportThreatAsync(threat, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log suspicious activity: {ActivityType}", activityType);
        }
    }

    public async Task<List<SecurityAuditEvent>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null, string? userId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<SecurityAuditEvent>().AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(e => e.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.Timestamp <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(e => e.UserId == userId);
            }

            return await query
                .OrderByDescending(e => e.Timestamp)
                .Take(1000) // Limit for performance
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve security events");
            return new List<SecurityAuditEvent>();
        }
    }

    /// <summary>
    /// Log data access events for GDPR compliance
    /// </summary>
    public async Task LogDataAccessAsync(string userId, string dataType, string operation, string? recordId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = "DATA_ACCESS",
                UserId = userId,
                Action = operation,
                Resource = $"{dataType}/{recordId ?? "COLLECTION"}",
                IsSuccess = true,
                Severity = "Information",
                AdditionalData = new Dictionary<string, object>
                {
                    ["DataAccess"] = true,
                    ["DataType"] = dataType,
                    ["Operation"] = operation,
                    ["RecordId"] = recordId ?? "N/A",
                    ["GdprRelevant"] = IsGdprRelevantData(dataType)
                }
            };

            await LogSecurityEventAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log data access for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Log API access events
    /// </summary>
    public async Task LogApiAccessAsync(string? userId, string endpoint, string method, string ipAddress, int statusCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEvent = new SecurityAuditEvent
            {
                EventType = "API_ACCESS",
                UserId = userId,
                IpAddress = ipAddress,
                Action = method,
                Resource = endpoint,
                IsSuccess = statusCode < 400,
                Severity = statusCode >= 400 ? "Warning" : "Information",
                AdditionalData = new Dictionary<string, object>
                {
                    ["ApiAccess"] = true,
                    ["Endpoint"] = endpoint,
                    ["Method"] = method,
                    ["StatusCode"] = statusCode,
                    ["IsAuthenticated"] = !string.IsNullOrWhiteSpace(userId)
                }
            };

            await LogSecurityEventAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log API access: {Method} {Endpoint}", method, endpoint);
        }
    }

    /// <summary>
    /// Generate security compliance report
    /// </summary>
    public async Task<SecurityComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var events = await _context.Set<SecurityAuditEvent>()
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .ToListAsync(cancellationToken);

            var report = new SecurityComplianceReport
            {
                ReportPeriod = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                GeneratedAt = DateTime.UtcNow,
                TotalEvents = events.Count,
                SuccessfulEvents = events.Count(e => e.IsSuccess),
                FailedEvents = events.Count(e => !e.IsSuccess),
                UniqueUsers = events.Where(e => !string.IsNullOrWhiteSpace(e.UserId)).Select(e => e.UserId).Distinct().Count(),
                UniqueIpAddresses = events.Where(e => !string.IsNullOrWhiteSpace(e.IpAddress)).Select(e => e.IpAddress).Distinct().Count(),
                LoginAttempts = events.Count(e => e.EventType.Contains("LOGIN")),
                FailedLogins = events.Count(e => e.EventType == "LOGIN_FAILED"),
                PasswordChanges = events.Count(e => e.EventType == "PASSWORD_CHANGE"),
                PermissionChanges = events.Count(e => e.EventType == "PERMISSION_CHANGE"),
                SuspiciousActivities = events.Count(e => e.EventType == "SUSPICIOUS_ACTIVITY"),
                DataAccessEvents = events.Count(e => e.EventType == "DATA_ACCESS"),
                ApiAccessEvents = events.Count(e => e.EventType == "API_ACCESS"),
                EventsByType = events.GroupBy(e => e.EventType).ToDictionary(g => g.Key, g => g.Count()),
                EventsBySeverity = events.GroupBy(e => e.Severity).ToDictionary(g => g.Key, g => g.Count()),
                TopUsers = events.Where(e => !string.IsNullOrWhiteSpace(e.UserId))
                    .GroupBy(e => e.UserId)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key!, g => g.Count()),
                TopIpAddresses = events.Where(e => !string.IsNullOrWhiteSpace(e.IpAddress))
                    .GroupBy(e => e.IpAddress)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key!, g => g.Count())
            };

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate security compliance report");
            throw;
        }
    }

    /// <summary>
    /// Archive old security events
    /// </summary>
    public async Task ArchiveOldEventsAsync(int retentionDays = 2555, CancellationToken cancellationToken = default) // 7 years default
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            
            var oldEvents = await _context.Set<SecurityAuditEvent>()
                .Where(e => e.Timestamp < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldEvents.Any())
            {
                // In production, export to long-term storage before deletion
                _context.Set<SecurityAuditEvent>().RemoveRange(oldEvents);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Archived {Count} security events older than {CutoffDate}", oldEvents.Count, cutoffDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive old security events");
            throw;
        }
    }

    private async Task LogFailedLoginAttemptAsync(string username, string ipAddress, string? reason, CancellationToken cancellationToken)
    {
        // Check for brute force patterns
        var recentFailures = await _context.Set<SecurityAuditEvent>()
            .Where(e => e.Username == username && 
                       e.EventType == "LOGIN_FAILED" && 
                       e.Timestamp >= DateTime.UtcNow.AddMinutes(-15))
            .CountAsync(cancellationToken);

        if (recentFailures >= 5)
        {
            await LogSuspiciousActivityAsync(null, "BRUTE_FORCE_ATTACK", 
                $"Multiple failed login attempts for user {username} from {ipAddress}", 
                ipAddress, cancellationToken);
        }
    }

    private bool IsSuspiciousEvent(SecurityAuditEvent auditEvent)
    {
        // Define suspicious patterns
        var suspiciousPatterns = new[]
        {
            "MULTIPLE_FAILED_LOGINS",
            "UNUSUAL_ACCESS_PATTERN",
            "PRIVILEGE_ESCALATION",
            "DATA_EXFILTRATION",
            "UNAUTHORIZED_ACCESS"
        };

        return suspiciousPatterns.Contains(auditEvent.EventType) ||
               auditEvent.Severity == "Critical" ||
               (auditEvent.AdditionalData?.ContainsKey("SuspiciousActivity") == true);
    }

    private bool IsGdprRelevantData(string dataType)
    {
        var gdprRelevantTypes = new[]
        {
            "PersonalData",
            "ContactInformation",
            "UserProfile",
            "FinancialData",
            "HealthData",
            "BiometricData"
        };

        return gdprRelevantTypes.Contains(dataType);
    }
}

/// <summary>
/// Security compliance report model
/// </summary>
public class SecurityComplianceReport
{
    public string ReportPeriod { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int TotalEvents { get; set; }
    public int SuccessfulEvents { get; set; }
    public int FailedEvents { get; set; }
    public int UniqueUsers { get; set; }
    public int UniqueIpAddresses { get; set; }
    public int LoginAttempts { get; set; }
    public int FailedLogins { get; set; }
    public int PasswordChanges { get; set; }
    public int PermissionChanges { get; set; }
    public int SuspiciousActivities { get; set; }
    public int DataAccessEvents { get; set; }
    public int ApiAccessEvents { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<string, int> EventsBySeverity { get; set; } = new();
    public Dictionary<string, int> TopUsers { get; set; } = new();
    public Dictionary<string, int> TopIpAddresses { get; set; } = new();
}
