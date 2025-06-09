using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Enums;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Consolidated alert service with basic and enhanced features
/// Supports escalation, suppression, business hours, and advanced alerting
/// </summary>
public class AlertService : IAlertService
{
    private readonly MonitoringContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly MonitoringConfiguration _config;
    private readonly AdvancedAlertConfiguration _advancedConfig;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        MonitoringContext context,
        IEmailService emailService,
        ISmsService smsService,
        IOptions<MonitoringConfiguration> config,
        IOptions<AdvancedAlertConfiguration> advancedConfig,
        ILogger<AlertService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _config = config.Value;
        _advancedConfig = advancedConfig?.Value ?? new AdvancedAlertConfiguration();
        _logger = logger;
    }

    public async Task<AlertResult> SendAlertsAsync(KPI kpi, KpiExecutionResult executionResult, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing alerts for KPI {Indicator} (Enhanced: {Enhanced})",
                kpi.Indicator, _advancedConfig.EnableEnhancedFeatures);

            // Enhanced feature: Check if alerts are suppressed
            if (_advancedConfig.EnableEnhancedFeatures && await IsAlertSuppressedAsync(kpi, cancellationToken))
            {
                _logger.LogDebug("Alerts suppressed for KPI {Indicator}", kpi.Indicator);
                return new AlertResult
                {
                    Message = "Alerts are currently suppressed",
                    Errors = new List<string> { "Alert suppression active" }
                };
            }

            // Enhanced feature: Check business hours if enabled
            if (_advancedConfig.EnableEnhancedFeatures && _advancedConfig.EnableBusinessHoursOnly && !IsBusinessHours())
            {
                _logger.LogDebug("Outside business hours, skipping alerts for KPI {Indicator}", kpi.Indicator);
                return new AlertResult
                {
                    Message = "Outside business hours",
                    Errors = new List<string> { "Business hours restriction active" }
                };
            }

            // Check if KPI is in cooldown
            if (await IsInCooldownAsync(kpi, cancellationToken))
            {
                _logger.LogDebug("KPI {Indicator} is in cooldown period, skipping alerts", kpi.Indicator);
                return new AlertResult
                {
                    Message = "KPI is in cooldown period",
                    Errors = new List<string> { "Cooldown period active" }
                };
            }

            // Get contacts for this KPI
            var contacts = await _context.KpiContacts
                .Include(kc => kc.Contact)
                .Where(kc => kc.KpiId == kpi.KpiId && kc.Contact.IsActive)
                .Select(kc => kc.Contact)
                .ToListAsync(cancellationToken);

            if (!contacts.Any())
            {
                _logger.LogWarning("No active contacts found for KPI {Indicator}", kpi.Indicator);
                return new AlertResult
                {
                    Message = "No active contacts found",
                    Errors = new List<string> { "No contacts configured" }
                };
            }

            // Enhanced feature: Determine alert severity
            var severity = _advancedConfig.EnableEnhancedFeatures ?
                DetermineAlertSeverity(kpi, executionResult) : AlertSeverity.Medium;

            var result = new AlertResult();
            var subject = BuildMessageFromTemplate(kpi.SubjectTemplate, kpi, executionResult);
            var body = BuildMessageFromTemplate(kpi.DescriptionTemplate, kpi, executionResult);

            // Enhanced feature: Create alert log entry first if enhanced features are enabled
            AlertLog? alertLog = null;
            if (_advancedConfig.EnableEnhancedFeatures)
            {
                alertLog = await CreateAlertLogAsync(kpi, executionResult, severity, subject, body, cancellationToken);
            }

            // Send emails
            if (_config.EnableEmail)
            {
                var emailContacts = contacts.Where(c => c.CanReceiveEmail()).ToList();
                if (emailContacts.Any())
                {
                    var emailAddresses = emailContacts.Select(c => c.Email!).ToList();
                    var emailSent = await _emailService.SendEmailAsync(emailAddresses, subject, body, true, cancellationToken);

                    if (emailSent)
                    {
                        result.EmailsSent = emailAddresses.Count;
                        _logger.LogInformation("Email alerts sent to {Count} recipients for KPI {Indicator}",
                            emailAddresses.Count, kpi.Indicator);
                    }
                    else
                    {
                        result.Errors.Add("Failed to send email alerts");
                    }
                }
            }

            // Enhanced SMS logic: Send SMS for priority 1 KPIs OR high severity alerts
            var shouldSendSms = _config.EnableSms && (kpi.Priority == 1 ||
                (_advancedConfig.EnableEnhancedFeatures && severity >= AlertSeverity.High));

            if (shouldSendSms)
            {
                var smsContacts = contacts.Where(c => c.CanReceiveSms()).ToList();
                if (smsContacts.Any())
                {
                    var phoneNumbers = smsContacts.Select(c => c.Phone!).ToList();
                    var smsSent = await _smsService.SendSmsAsync(phoneNumbers, subject, cancellationToken);

                    if (smsSent)
                    {
                        result.SmsSent = phoneNumbers.Count;
                        _logger.LogInformation("SMS alerts sent to {Count} recipients for KPI {Indicator} (Severity: {Severity})",
                            phoneNumbers.Count, kpi.Indicator, severity);
                    }
                    else
                    {
                        result.Errors.Add("Failed to send SMS alerts");
                    }
                }
            }

            // Enhanced feature: Schedule escalations and auto-resolution
            if (_advancedConfig.EnableEnhancedFeatures && alertLog != null)
            {
                if (_advancedConfig.EnableEscalation)
                {
                    await ScheduleEscalationsAsync(alertLog.AlertLogId, kpi, severity, cancellationToken);
                }

                if (_advancedConfig.EnableAutoResolution)
                {
                    await ScheduleAutoResolutionAsync(alertLog.AlertLogId, cancellationToken);
                }
            }

            result.Message = result.GetSummary();

            // Log the alert (if not already logged by enhanced features)
            if (!_advancedConfig.EnableEnhancedFeatures)
            {
                await LogAlertAsync(kpi, result, executionResult, cancellationToken);
            }

            _logger.LogInformation("Alert processing completed for KPI {Indicator}: {Summary}",
                kpi.Indicator, result.Message);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alerts for KPI {Indicator}: {Message}", kpi.Indicator, ex.Message);
            return new AlertResult
            {
                Message = "Alert sending failed",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<bool> IsInCooldownAsync(KPI kpi, CancellationToken cancellationToken = default)
    {
        if (kpi.CooldownMinutes <= 0)
            return false;

        var cooldownStart = DateTime.UtcNow.AddMinutes(-kpi.CooldownMinutes);
        
        var recentAlert = await _context.AlertLogs
            .Where(a => a.KpiId == kpi.KpiId && a.TriggerTime >= cooldownStart)
            .OrderByDescending(a => a.TriggerTime)
            .FirstOrDefaultAsync(cancellationToken);

        return recentAlert != null;
    }

    public async Task LogAlertAsync(KPI kpi, AlertResult alertResult, KpiExecutionResult executionResult, CancellationToken cancellationToken = default)
    {
        try
        {
            var sentVia = GetSentViaValue(alertResult);
            var sentTo = GetSentToSummary(alertResult);

            var alertLog = new AlertLog
            {
                KpiId = kpi.KpiId,
                TriggerTime = DateTime.UtcNow,
                Message = alertResult.Message,
                Details = $"Execution Result: {executionResult.GetSummary()}",
                SentVia = sentVia,
                SentTo = sentTo,
                CurrentValue = executionResult.CurrentValue,
                HistoricalValue = executionResult.HistoricalValue,
                DeviationPercent = executionResult.DeviationPercent,
                IsResolved = false
            };

            _context.AlertLogs.Add(alertLog);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Alert logged for KPI {Indicator} with ID {AlertId}", kpi.Indicator, alertLog.AlertId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log alert for KPI {Indicator}: {Message}", kpi.Indicator, ex.Message);
        }
    }

    public string BuildMessageFromTemplate(string template, KPI kpi, KpiExecutionResult result)
    {
        return template
            .Replace("{Indicator}", kpi.Indicator)
            .Replace("{Owner}", kpi.Owner)
            .Replace("{CurrentValue}", result.CurrentValue.ToString("N2"))
            .Replace("{HistoricalValue}", result.HistoricalValue.ToString("N2"))
            .Replace("{DeviationPercent}", result.DeviationPercent.ToString("N2"))
            .Replace("{Threshold}", kpi.Deviation.ToString("N2"))
            .Replace("{Timestamp}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"))
            .Replace("{Severity}", result.GetSeverity())
            .Replace("{MinimumThreshold}", kpi.MinimumThreshold?.ToString("N2") ?? "N/A");
    }

    public async Task<bool> ResolveAlertAsync(long alertId, string resolvedBy, string? notes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = await _context.AlertLogs.FindAsync(new object[] { alertId }, cancellationToken);
            if (alert == null)
            {
                _logger.LogWarning("Alert {AlertId} not found for resolution", alertId);
                return false;
            }

            if (alert.IsResolved)
            {
                _logger.LogWarning("Alert {AlertId} is already resolved", alertId);
                return false;
            }

            alert.Resolve(resolvedBy, notes);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Alert {AlertId} resolved by {ResolvedBy}", alertId, resolvedBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve alert {AlertId}: {Message}", alertId, ex.Message);
            return false;
        }
    }

    public async Task<int> BulkResolveAlertsAsync(IEnumerable<long> alertIds, string resolvedBy, string? notes = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var alerts = await _context.AlertLogs
                .Where(a => alertIds.Contains(a.AlertId) && !a.IsResolved)
                .ToListAsync(cancellationToken);

            foreach (var alert in alerts)
            {
                alert.Resolve(resolvedBy, notes);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Bulk resolved {Count} alerts by {ResolvedBy}", alerts.Count, resolvedBy);
            return alerts.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk resolve alerts: {Message}", ex.Message);
            return 0;
        }
    }

    private static byte GetSentViaValue(AlertResult alertResult)
    {
        if (alertResult.EmailsSent > 0 && alertResult.SmsSent > 0)
            return 3; // Both
        if (alertResult.SmsSent > 0)
            return 1; // SMS
        if (alertResult.EmailsSent > 0)
            return 2; // Email
        return 2; // Default to Email
    }

    private static string GetSentToSummary(AlertResult alertResult)
    {
        var parts = new List<string>();
        if (alertResult.EmailsSent > 0)
            parts.Add($"{alertResult.EmailsSent} email recipient(s)");
        if (alertResult.SmsSent > 0)
            parts.Add($"{alertResult.SmsSent} SMS recipient(s)");

        return parts.Any() ? string.Join(", ", parts) : "No recipients";
    }

    #region Enhanced Alert Features

    /// <summary>
    /// Checks if alerts are suppressed for the given KPI
    /// </summary>
    private async Task<bool> IsAlertSuppressedAsync(KPI kpi, CancellationToken cancellationToken)
    {
        if (!_advancedConfig.EnableAlertSuppression)
            return false;

        var now = DateTime.UtcNow;

        // Check for active suppression rules
        var suppressionRules = await _context.Set<MonitoringGrid.Core.Entities.AlertSuppressionRule>()
            .Where(r => r.IsActive &&
                       r.StartTime <= now &&
                       r.EndTime >= now &&
                       (r.KpiId == null || r.KpiId == kpi.KpiId) &&
                       (r.Owner == null || r.Owner == kpi.Owner))
            .AnyAsync(cancellationToken);

        return suppressionRules;
    }

    /// <summary>
    /// Checks if current time is within business hours
    /// </summary>
    private bool IsBusinessHours()
    {
        var now = DateTime.Now;

        // Check if it's a business day
        if (!_advancedConfig.BusinessDays.Contains(now.DayOfWeek))
            return false;

        // Check if it's a holiday
        if (_advancedConfig.EnableHolidaySupport &&
            _advancedConfig.Holidays.Any(h => h.Date == now.Date))
            return false;

        // Check business hours
        var currentTime = now.TimeOfDay;
        return currentTime >= _advancedConfig.BusinessHoursStart &&
               currentTime <= _advancedConfig.BusinessHoursEnd;
    }

    /// <summary>
    /// Determines alert severity based on deviation percentage
    /// </summary>
    private AlertSeverity DetermineAlertSeverity(KPI kpi, KpiExecutionResult executionResult)
    {
        var deviation = Math.Abs(executionResult.DeviationPercent);

        return deviation switch
        {
            >= 50 => AlertSeverity.Emergency,
            >= 30 => AlertSeverity.Critical,
            >= 20 => AlertSeverity.High,
            >= 10 => AlertSeverity.Medium,
            _ => AlertSeverity.Low
        };
    }

    /// <summary>
    /// Creates an alert log entry for enhanced tracking
    /// </summary>
    private async Task<AlertLog> CreateAlertLogAsync(
        KPI kpi,
        KpiExecutionResult executionResult,
        AlertSeverity severity,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var alertLog = new AlertLog
        {
            KpiId = kpi.KpiId,
            TriggerTime = DateTime.UtcNow,
            CurrentValue = executionResult.CurrentValue,
            HistoricalValue = executionResult.HistoricalValue,
            DeviationPercent = executionResult.DeviationPercent,
            Message = subject,
            Details = body,
            Subject = subject,
            Description = body,
            SentVia = 0, // Will be updated as notifications are sent
            SentTo = string.Empty, // Will be updated as notifications are sent
            IsResolved = false
        };

        _context.AlertLogs.Add(alertLog);
        await _context.SaveChangesAsync(cancellationToken);

        return alertLog;
    }

    /// <summary>
    /// Schedules escalations for the alert (placeholder implementation)
    /// </summary>
    private Task ScheduleEscalationsAsync(long alertId, KPI kpi, AlertSeverity severity, CancellationToken cancellationToken)
    {
        // Implementation for scheduling escalations
        // This would create escalation records in the database
        _logger.LogDebug("Scheduling escalations for alert {AlertId} with severity {Severity}", alertId, severity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Schedules auto-resolution for the alert (placeholder implementation)
    /// </summary>
    private Task ScheduleAutoResolutionAsync(long alertId, CancellationToken cancellationToken)
    {
        // Implementation for scheduling auto-resolution
        _logger.LogDebug("Scheduling auto-resolution for alert {AlertId}", alertId);
        return Task.CompletedTask;
    }

    #endregion
}
