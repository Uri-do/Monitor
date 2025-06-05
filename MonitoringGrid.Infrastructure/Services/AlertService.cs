using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Service responsible for managing alerts and notifications
/// </summary>
public class AlertService : IAlertService
{
    private readonly MonitoringContext _context;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly MonitoringConfiguration _config;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        MonitoringContext context,
        IEmailService emailService,
        ISmsService smsService,
        IOptions<MonitoringConfiguration> config,
        ILogger<AlertService> logger)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<AlertResult> SendAlertsAsync(KPI kpi, KpiExecutionResult executionResult, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending alerts for KPI {Indicator}", kpi.Indicator);

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

            var result = new AlertResult();
            var subject = BuildMessageFromTemplate(kpi.SubjectTemplate, kpi, executionResult);
            var body = BuildMessageFromTemplate(kpi.DescriptionTemplate, kpi, executionResult);

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

            // Send SMS for priority 1 KPIs
            if (_config.EnableSms && kpi.Priority == 1)
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

            result.Message = result.GetSummary();

            // Log the alert
            await LogAlertAsync(kpi, result, executionResult, cancellationToken);

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
}
