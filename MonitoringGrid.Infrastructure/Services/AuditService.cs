using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;
using System.Text.Json;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Audit service for logging user actions and system events
/// </summary>
public class AuditService : IAuditService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(MonitoringContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogActionAsync(string userId, string action, string resource, object? details = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEntry = new AuditLogEntry
            {
                UserId = userId,
                Action = action,
                Resource = resource,
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                Timestamp = DateTime.UtcNow,
                IsSuccess = true
            };

            _context.Set<AuditLogEntry>().Add(auditEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Audit log created: User {UserId} performed {Action} on {Resource}", userId, action, resource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit action for user {UserId}: {Action} on {Resource}", userId, action, resource);
        }
    }

    public async Task LogLoginAsync(string userId, string ipAddress, bool success, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEntry = new AuditLogEntry
            {
                UserId = userId,
                Action = success ? "LOGIN_SUCCESS" : "LOGIN_FAILED",
                Resource = "Authentication",
                Details = JsonSerializer.Serialize(new { IpAddress = ipAddress, Reason = reason }),
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow,
                IsSuccess = success,
                ErrorMessage = success ? null : reason
            };

            _context.Set<AuditLogEntry>().Add(auditEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Login audit logged: User {UserId} from {IpAddress} - {Result}", 
                userId, ipAddress, success ? "SUCCESS" : "FAILED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log login audit for user {UserId}", userId);
        }
    }

    public async Task LogConfigurationChangeAsync(string userId, string configType, object oldValue, object newValue, CancellationToken cancellationToken = default)
    {
        try
        {
            var changeDetails = new
            {
                ConfigurationType = configType,
                OldValue = oldValue,
                NewValue = newValue,
                ChangeTime = DateTime.UtcNow
            };

            var auditEntry = new AuditLogEntry
            {
                UserId = userId,
                Action = "CONFIGURATION_CHANGE",
                Resource = configType,
                Details = JsonSerializer.Serialize(changeDetails),
                Timestamp = DateTime.UtcNow,
                IsSuccess = true
            };

            _context.Set<AuditLogEntry>().Add(auditEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Configuration change logged: User {UserId} modified {ConfigType}", userId, configType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log configuration change for user {UserId}: {ConfigType}", userId, configType);
        }
    }

    public async Task LogAlertActionAsync(string userId, int alertId, string action, string? notes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var actionDetails = new
            {
                AlertId = alertId,
                Action = action,
                Notes = notes,
                ActionTime = DateTime.UtcNow
            };

            var auditEntry = new AuditLogEntry
            {
                UserId = userId,
                Action = $"ALERT_{action.ToUpper()}",
                Resource = $"Alert/{alertId}",
                Details = JsonSerializer.Serialize(actionDetails),
                Timestamp = DateTime.UtcNow,
                IsSuccess = true
            };

            _context.Set<AuditLogEntry>().Add(auditEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Alert action logged: User {UserId} performed {Action} on Alert {AlertId}", userId, action, alertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log alert action for user {UserId}: {Action} on Alert {AlertId}", userId, action, alertId);
        }
    }

    public async Task<List<AuditLogEntry>> GetAuditLogsAsync(AuditLogFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<AuditLogEntry>().AsQueryable();

            // Apply filters
            if (filter.StartDate.HasValue)
            {
                query = query.Where(log => log.Timestamp >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(log => log.Timestamp <= filter.EndDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.UserId))
            {
                query = query.Where(log => log.UserId == filter.UserId);
            }

            if (!string.IsNullOrWhiteSpace(filter.Action))
            {
                query = query.Where(log => log.Action.Contains(filter.Action));
            }

            if (!string.IsNullOrWhiteSpace(filter.Resource))
            {
                query = query.Where(log => log.Resource.Contains(filter.Resource));
            }

            if (filter.IsSuccess.HasValue)
            {
                query = query.Where(log => log.IsSuccess == filter.IsSuccess.Value);
            }

            // Apply pagination
            var totalCount = await query.CountAsync(cancellationToken);
            var logs = await query
                .OrderByDescending(log => log.Timestamp)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} audit logs out of {Total} total", logs.Count, totalCount);
            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve audit logs");
            return new List<AuditLogEntry>();
        }
    }

    /// <summary>
    /// Log system events (for internal system actions)
    /// </summary>
    public async Task LogSystemEventAsync(string eventType, string description, object? details = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var auditEntry = new AuditLogEntry
            {
                UserId = "SYSTEM",
                UserName = "System",
                Action = eventType,
                Resource = "System",
                Details = details != null ? JsonSerializer.Serialize(details) : description,
                Timestamp = DateTime.UtcNow,
                IsSuccess = true
            };

            _context.Set<AuditLogEntry>().Add(auditEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("System event logged: {EventType} - {Description}", eventType, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log system event: {EventType}", eventType);
        }
    }

    /// <summary>
    /// Log data access events for compliance
    /// </summary>
    public async Task LogDataAccessAsync(string userId, string dataType, string operation, string? recordId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var accessDetails = new
            {
                DataType = dataType,
                Operation = operation,
                RecordId = recordId,
                AccessTime = DateTime.UtcNow
            };

            var auditEntry = new AuditLogEntry
            {
                UserId = userId,
                Action = $"DATA_{operation.ToUpper()}",
                Resource = $"{dataType}/{recordId ?? "COLLECTION"}",
                Details = JsonSerializer.Serialize(accessDetails),
                Timestamp = DateTime.UtcNow,
                IsSuccess = true
            };

            _context.Set<AuditLogEntry>().Add(auditEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Data access logged: User {UserId} performed {Operation} on {DataType}", userId, operation, dataType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log data access for user {UserId}: {Operation} on {DataType}", userId, operation, dataType);
        }
    }

    /// <summary>
    /// Generate compliance report
    /// </summary>
    public async Task<ComplianceReport> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = await _context.Set<AuditLogEntry>()
                .Where(log => log.Timestamp >= startDate && log.Timestamp <= endDate)
                .ToListAsync(cancellationToken);

            var report = new ComplianceReport
            {
                ReportPeriod = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                GeneratedAt = DateTime.UtcNow,
                TotalEvents = logs.Count,
                SuccessfulEvents = logs.Count(l => l.IsSuccess),
                FailedEvents = logs.Count(l => !l.IsSuccess),
                UniqueUsers = logs.Select(l => l.UserId).Distinct().Count(),
                EventsByType = logs.GroupBy(l => l.Action)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByUser = logs.GroupBy(l => l.UserId)
                    .ToDictionary(g => g.Key, g => g.Count()),
                FailedLoginAttempts = logs.Count(l => l.Action == "LOGIN_FAILED"),
                ConfigurationChanges = logs.Count(l => l.Action == "CONFIGURATION_CHANGE"),
                DataAccessEvents = logs.Count(l => l.Action.StartsWith("DATA_"))
            };

            _logger.LogInformation("Compliance report generated for period {StartDate} to {EndDate}: {TotalEvents} events", 
                startDate, endDate, report.TotalEvents);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate compliance report for period {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    /// <summary>
    /// Archive old audit logs for performance
    /// </summary>
    public async Task ArchiveOldLogsAsync(int retentionDays = 365, CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
            
            var oldLogs = await _context.Set<AuditLogEntry>()
                .Where(log => log.Timestamp < cutoffDate)
                .ToListAsync(cancellationToken);

            if (oldLogs.Any())
            {
                // In a real implementation, you might want to export these to a separate archive storage
                // before deleting them from the main database
                _context.Set<AuditLogEntry>().RemoveRange(oldLogs);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Archived {Count} audit logs older than {CutoffDate}", oldLogs.Count, cutoffDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive old audit logs");
            throw;
        }
    }
}

/// <summary>
/// Compliance report model
/// </summary>
public class ComplianceReport
{
    public string ReportPeriod { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int TotalEvents { get; set; }
    public int SuccessfulEvents { get; set; }
    public int FailedEvents { get; set; }
    public int UniqueUsers { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<string, int> EventsByUser { get; set; } = new();
    public int FailedLoginAttempts { get; set; }
    public int ConfigurationChanges { get; set; }
    public int DataAccessEvents { get; set; }
}
