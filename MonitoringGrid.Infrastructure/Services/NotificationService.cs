using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Enums;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Configuration;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Consolidated notification service supporting multiple channels
/// Supports Email and SMS notifications (enterprise integrations removed during cleanup)
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly MonitoringOptions _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        ISmsService smsService,
        IOptions<MonitoringOptions> config,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _smsService = smsService;
        _config = config.Value;
        _logger = logger;
    }

    #region Email Notifications

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.EnableEmail)
            {
                _logger.LogDebug("Email notifications are disabled");
                return false;
            }

            return await _emailService.SendEmailAsync(to, subject, body, isHtml, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", to);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.EnableEmail)
            {
                _logger.LogDebug("Email notifications are disabled");
                return false;
            }

            return await _emailService.SendEmailAsync(to, subject, body, isHtml, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", to));
            return false;
        }
    }

    #endregion

    #region SMS Notifications

    public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.EnableSms)
            {
                _logger.LogDebug("SMS notifications are disabled");
                return false;
            }

            return await _smsService.SendSmsAsync(phoneNumber, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<bool> SendSmsAsync(IEnumerable<string> phoneNumbers, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_config.EnableSms)
            {
                _logger.LogDebug("SMS notifications are disabled");
                return false;
            }

            return await _smsService.SendSmsAsync(phoneNumbers, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumbers}", string.Join(", ", phoneNumbers));
            return false;
        }
    }

    #endregion

    #region Slack Notifications (Removed - Enterprise Feature)

    public async Task<bool> SendSlackMessageAsync(string channel, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Slack notifications removed during cleanup - enterprise feature not implemented");
        return false;
    }

    public async Task<bool> SendSlackAlertAsync(string channel, AlertNotificationDto alert, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Slack notifications removed during cleanup - enterprise feature not implemented");
        return false;
    }

    #endregion

    #region Teams Notifications (Removed - Enterprise Feature)

    public async Task<bool> SendTeamsMessageAsync(string webhookUrl, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Teams notifications removed during cleanup - enterprise feature not implemented");
        return false;
    }

    public async Task<bool> SendTeamsAlertAsync(string webhookUrl, AlertNotificationDto alert, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Teams notifications removed during cleanup - enterprise feature not implemented");
        return false;
    }

    #endregion

    #region Webhook Notifications (Removed - Enterprise Feature)

    public async Task<bool> SendWebhookAsync(string url, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Webhook notifications removed during cleanup - enterprise feature not implemented");
        return false;
    }

    public async Task<bool> SendWebhookAlertAsync(string url, AlertNotificationDto alert, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Webhook notifications removed during cleanup - enterprise feature not implemented");
        return false;
    }

    #endregion

    #region Multi-Channel Notifications

    public async Task<Core.Interfaces.NotificationResult> SendMultiChannelNotificationAsync(Core.Interfaces.NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var result = new Core.Interfaces.NotificationResult();
        var errors = new List<string>();

        _logger.LogInformation("Sending multi-channel notification: {Subject} to {ChannelCount} channels", 
            request.Subject, request.Channels.Count);

        foreach (var channel in request.Channels)
        {
            try
            {
                bool channelSuccess = false;

                switch (channel)
                {
                    case NotificationChannel.Email:
                        channelSuccess = await SendEmailAsync(request.Recipients, request.Subject, request.Message, true, cancellationToken);
                        break;

                    case NotificationChannel.Sms:
                        channelSuccess = await SendSmsAsync(request.Recipients, request.Message, cancellationToken);
                        break;

                    case NotificationChannel.Slack:
                        // Slack notifications removed during cleanup - enterprise feature
                        _logger.LogWarning("Slack notifications not available - enterprise feature removed");
                        channelSuccess = false;
                        break;

                    case NotificationChannel.Teams:
                        // Teams notifications removed during cleanup - enterprise feature
                        _logger.LogWarning("Teams notifications not available - enterprise feature removed");
                        channelSuccess = false;
                        break;

                    case NotificationChannel.Webhook:
                        // Webhook notifications removed during cleanup - enterprise feature
                        _logger.LogWarning("Webhook notifications not available - enterprise feature removed");
                        channelSuccess = false;
                        break;

                    default:
                        _logger.LogWarning("Unsupported notification channel: {Channel}", channel);
                        break;
                }

                result.ChannelResults[channel] = channelSuccess;

                if (channelSuccess)
                {
                    result.TotalSent++;
                }
                else
                {
                    result.TotalFailed++;
                    errors.Add($"Failed to send via {channel}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification via {Channel}", channel);
                result.ChannelResults[channel] = false;
                result.TotalFailed++;
                errors.Add($"Error in {channel}: {ex.Message}");
            }
        }

        result.IsSuccess = result.TotalSent > 0;
        result.Errors = errors;
        result.Message = result.GetSummary();

        _logger.LogInformation("Multi-channel notification completed: {TotalSent} sent, {TotalFailed} failed", 
            result.TotalSent, result.TotalFailed);

        return result;
    }

    public async Task<Core.Interfaces.NotificationResult> SendAlertNotificationAsync(AlertNotificationDto alert, List<NotificationChannel> channels, CancellationToken cancellationToken = default)
    {
        var request = new Core.Interfaces.NotificationRequest
        {
            Subject = alert.Subject ?? "Alert Notification",
            Message = alert.Message ?? "An alert has been triggered",
            Channels = channels,
            Recipients = new List<string>(), // Will be populated based on alert configuration
            Priority = NotificationPriority.High,
            Metadata = new Dictionary<string, object>
            {
                ["AlertId"] = alert.AlertId,
                ["KpiId"] = alert.KpiId,
                ["Severity"] = alert.Severity,
                ["TriggerTime"] = alert.TriggerTime
            }
        };

        return await SendMultiChannelNotificationAsync(request, cancellationToken);
    }

    #endregion

    #region Configuration and Testing

    public async Task<bool> ValidateChannelConfigurationAsync(NotificationChannel channel, CancellationToken cancellationToken = default)
    {
        try
        {
            return channel switch
            {
                NotificationChannel.Email => await _emailService.ValidateConfigurationAsync(cancellationToken),
                NotificationChannel.Sms => await _smsService.ValidateConfigurationAsync(cancellationToken),
                NotificationChannel.Slack => false, // Removed during cleanup
                NotificationChannel.Teams => false, // Removed during cleanup
                NotificationChannel.Webhook => false, // Removed during cleanup
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate configuration for {Channel}", channel);
            return false;
        }
    }

    public async Task<bool> SendTestNotificationAsync(NotificationChannel channel, string recipient, CancellationToken cancellationToken = default)
    {
        try
        {
            var testMessage = $"Test notification from Monitoring Grid - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            return channel switch
            {
                NotificationChannel.Email => await _emailService.SendTestEmailAsync(recipient, cancellationToken),
                NotificationChannel.Sms => await _smsService.SendTestSmsAsync(recipient, cancellationToken),
                NotificationChannel.Slack => false, // Removed during cleanup
                NotificationChannel.Teams => false, // Removed during cleanup
                NotificationChannel.Webhook => false, // Removed during cleanup
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test notification via {Channel} to {Recipient}", channel, recipient);
            return false;
        }
    }

    public async Task<List<NotificationChannel>> GetAvailableChannelsAsync(CancellationToken cancellationToken = default)
    {
        var availableChannels = new List<NotificationChannel>();

        // Check each channel's configuration
        if (_config.EnableEmail && await ValidateChannelConfigurationAsync(NotificationChannel.Email, cancellationToken))
        {
            availableChannels.Add(NotificationChannel.Email);
        }

        if (_config.EnableSms && await ValidateChannelConfigurationAsync(NotificationChannel.Sms, cancellationToken))
        {
            availableChannels.Add(NotificationChannel.Sms);
        }

        // Enterprise notification channels removed during cleanup
        // Slack, Teams, and Webhook notifications are not available

        _logger.LogDebug("Available notification channels: {Channels}", string.Join(", ", availableChannels));

        return availableChannels;
    }

    #endregion
}
