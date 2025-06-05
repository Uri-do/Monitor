using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Enhanced alert service with escalation, suppression, and advanced features
/// </summary>
public class EnhancedAlertService : IAlertService
{
    private readonly MonitoringContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly MonitoringConfiguration _config;
    private readonly AdvancedAlertConfiguration _advancedConfig;
    private readonly ILogger<EnhancedAlertService> _logger;

    public EnhancedAlertService(
        MonitoringContext context,
        IEmailService emailService,
        ISmsService smsService,
        IOptions<MonitoringConfiguration> config,
        IOptions<AdvancedAlertConfiguration> advancedConfig,
        ILogger<EnhancedAlertService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _config = config.Value;
        _advancedConfig = advancedConfig.Value;
        _logger = logger;
    }

    public async Task<AlertResult> SendAlertsAsync(KPI kpi, KpiExecutionResult executionResult, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing enhanced alerts for KPI {Indicator}", kpi.Indicator);

            // Check if alerts are suppressed
            if (await IsAlertSuppressedAsync(kpi, cancellationToken))
            {
                _logger.LogDebug("Alerts suppressed for KPI {Indicator}", kpi.Indicator);
                return new AlertResult
                {
                    Message = "Alerts are currently suppressed",
                    Errors = new List<string> { "Alert suppression active" }
                };
            }

            // Check business hours if enabled
            if (_advancedConfig.EnableBusinessHoursOnly && !IsBusinessHours())
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
                _logger.LogDebug("KPI {Indicator} is in cooldown period", kpi.Indicator);
                return new AlertResult
                {
                    Message = "KPI is in cooldown period",
                    Errors = new List<string> { "Cooldown period active" }
                };
            }

            // Determine alert severity
            var severity = DetermineAlertSeverity(kpi, executionResult);

            // Get contacts for this KPI
            var contacts = await GetKpiContactsAsync(kpi.KpiId, cancellationToken);
            if (!contacts.Any())
            {
                _logger.LogWarning("No active contacts found for KPI {Indicator}", kpi.Indicator);
                return new AlertResult
                {
                    Message = "No contacts configured",
                    Errors = new List<string> { "No active contacts found" }
                };
            }

            var result = new AlertResult();
            var subject = BuildMessageFromTemplate(kpi.SubjectTemplate, kpi, executionResult);
            var body = BuildMessageFromTemplate(kpi.DescriptionTemplate, kpi, executionResult);

            // Create alert log entry
            var alertLog = await CreateAlertLogAsync(kpi, executionResult, severity, subject, body, cancellationToken);

            // Send immediate notifications
            await SendImmediateNotificationsAsync(kpi, contacts, subject, body, severity, result, cancellationToken);

            // Schedule escalations if enabled
            if (_advancedConfig.EnableEscalation)
            {
                await ScheduleEscalationsAsync(alertLog.AlertLogId, kpi, severity, cancellationToken);
            }

            // Schedule auto-resolution if enabled
            if (_advancedConfig.EnableAutoResolution)
            {
                await ScheduleAutoResolutionAsync(alertLog.AlertLogId, cancellationToken);
            }

            result.Message = result.GetSummary();
            _logger.LogInformation("Enhanced alerts processed for KPI {Indicator}: {Summary}", kpi.Indicator, result.Message);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process enhanced alerts for KPI {Indicator}: {Message}", kpi.Indicator, ex.Message);
            return new AlertResult
            {
                Message = "Enhanced alert processing failed",
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

    private async Task<bool> IsAlertSuppressedAsync(KPI kpi, CancellationToken cancellationToken)
    {
        if (!_advancedConfig.EnableAlertSuppression)
            return false;

        var now = DateTime.UtcNow;
        
        // Check for active suppression rules
        var suppressionRules = await _context.Set<AlertSuppressionRule>()
            .Where(r => r.IsActive && 
                       r.StartTime <= now && 
                       r.EndTime >= now &&
                       (r.KpiId == null || r.KpiId == kpi.KpiId) &&
                       (r.Owner == null || r.Owner == kpi.Owner))
            .AnyAsync(cancellationToken);

        return suppressionRules;
    }

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

    private AlertSeverity DetermineAlertSeverity(KPI kpi, KpiExecutionResult executionResult)
    {
        var deviation = Math.Abs(executionResult.DeviationPercentage);
        
        return deviation switch
        {
            >= 50 => AlertSeverity.Emergency,
            >= 30 => AlertSeverity.Critical,
            >= 20 => AlertSeverity.High,
            >= 10 => AlertSeverity.Medium,
            _ => AlertSeverity.Low
        };
    }

    private async Task<List<Contact>> GetKpiContactsAsync(int kpiId, CancellationToken cancellationToken)
    {
        return await _context.KpiContacts
            .Include(kc => kc.Contact)
            .Where(kc => kc.KpiId == kpiId && kc.Contact.IsActive)
            .Select(kc => kc.Contact)
            .ToListAsync(cancellationToken);
    }

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
            DeviationPercentage = executionResult.DeviationPercentage,
            Subject = subject,
            Description = body,
            SentVia = 0, // Will be updated as notifications are sent
            IsResolved = false
        };

        _context.AlertLogs.Add(alertLog);
        await _context.SaveChangesAsync(cancellationToken);

        return alertLog;
    }

    private async Task SendImmediateNotificationsAsync(
        KPI kpi,
        List<Contact> contacts,
        string subject,
        string body,
        AlertSeverity severity,
        AlertResult result,
        CancellationToken cancellationToken)
    {
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

        // Send SMS for high severity alerts
        if (_config.EnableSms && (severity >= AlertSeverity.High || kpi.Priority == 1))
        {
            var smsContacts = contacts.Where(c => c.CanReceiveSms()).ToList();
            if (smsContacts.Any())
            {
                var phoneNumbers = smsContacts.Select(c => c.Phone!).ToList();
                var smsSent = await _smsService.SendSmsAsync(phoneNumbers, subject, cancellationToken);
                
                if (smsSent)
                {
                    result.SmsSent = phoneNumbers.Count;
                    _logger.LogInformation("SMS alerts sent to {Count} recipients for KPI {Indicator}", 
                        phoneNumbers.Count, kpi.Indicator);
                }
                else
                {
                    result.Errors.Add("Failed to send SMS alerts");
                }
            }
        }
    }

    private async Task ScheduleEscalationsAsync(int alertId, KPI kpi, AlertSeverity severity, CancellationToken cancellationToken)
    {
        // Implementation for scheduling escalations
        // This would create escalation records in the database
        _logger.LogDebug("Scheduling escalations for alert {AlertId}", alertId);
    }

    private async Task ScheduleAutoResolutionAsync(int alertId, CancellationToken cancellationToken)
    {
        // Implementation for scheduling auto-resolution
        _logger.LogDebug("Scheduling auto-resolution for alert {AlertId}", alertId);
    }

    private string BuildMessageFromTemplate(string template, KPI kpi, KpiExecutionResult result)
    {
        return template
            .Replace("{{indicator}}", kpi.Indicator)
            .Replace("{{owner}}", kpi.Owner)
            .Replace("{{current}}", result.CurrentValue.ToString("N2"))
            .Replace("{{historical}}", result.HistoricalValue.ToString("N2"))
            .Replace("{{deviation}}", result.DeviationPercentage.ToString("N2"))
            .Replace("{{timestamp}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
    }
}
