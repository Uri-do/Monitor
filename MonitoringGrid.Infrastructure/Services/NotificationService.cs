using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitoringGrid.Core.Enums;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Infrastructure.Configuration;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Consolidated notification service supporting multiple channels
/// Replaces individual EmailService, SmsService, SlackService, TeamsService, WebhookService
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;
    private readonly ISlackService _slackService;
    private readonly ITeamsService _teamsService;
    private readonly IWebhookService _webhookService;
    private readonly MonitoringOptions _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        ISmsService smsService,
        ISlackService slackService,
        ITeamsService teamsService,
        IWebhookService webhookService,
        IOptions<MonitoringOptions> config,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _smsService = smsService;
        _slackService = slackService;
        _teamsService = teamsService;
        _webhookService = webhookService;
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

    #region Slack Notifications

    public async Task<bool> SendSlackMessageAsync(string channel, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _slackService.SendMessageAsync(channel, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack message to {Channel}", channel);
            return false;
        }
    }

    public async Task<bool> SendSlackAlertAsync(string channel, AlertNotificationDto alert, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _slackService.SendAlertAsync(channel, alert, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Slack alert to {Channel}", channel);
            return false;
        }
    }

    #endregion

    #region Teams Notifications

    public async Task<bool> SendTeamsMessageAsync(string webhookUrl, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _teamsService.SendMessageAsync(webhookUrl, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams message to {WebhookUrl}", webhookUrl);
            return false;
        }
    }

    public async Task<bool> SendTeamsAlertAsync(string webhookUrl, AlertNotificationDto alert, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _teamsService.SendAlertAsync(webhookUrl, alert, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Teams alert to {WebhookUrl}", webhookUrl);
            return false;
        }
    }

    #endregion

    #region Webhook Notifications

    public async Task<bool> SendWebhookAsync(string url, object payload, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a temporary webhook configuration for the URL
            var webhookConfig = new WebhookConfiguration
            {
                Url = url,
                IsActive = true
            };

            return await _webhookService.SendWebhookAsync(url, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook to {Url}", url);
            return false;
        }
    }

    public async Task<bool> SendWebhookAlertAsync(string url, AlertNotificationDto alert, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a temporary webhook configuration for the URL
            var webhookConfig = new WebhookConfiguration
            {
                Url = url,
                IsActive = true
            };

            return await _webhookService.SendAlertWebhookAsync(webhookConfig, alert, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook alert to {Url}", url);
            return false;
        }
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
                        // For Slack, use the first recipient as channel name
                        if (request.Recipients.Any())
                        {
                            channelSuccess = await SendSlackMessageAsync(request.Recipients.First(), request.Message, cancellationToken);
                        }
                        break;

                    case NotificationChannel.Teams:
                        // For Teams, use the first recipient as webhook URL
                        if (request.Recipients.Any())
                        {
                            channelSuccess = await SendTeamsMessageAsync(request.Recipients.First(), request.Message, cancellationToken);
                        }
                        break;

                    case NotificationChannel.Webhook:
                        // For Webhook, use the first recipient as URL
                        if (request.Recipients.Any())
                        {
                            var payload = new { subject = request.Subject, message = request.Message, metadata = request.Metadata };
                            channelSuccess = await SendWebhookAsync(request.Recipients.First(), payload, cancellationToken);
                        }
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
                NotificationChannel.Slack => await _slackService.ValidateConfigurationAsync(cancellationToken),
                NotificationChannel.Teams => await _teamsService.ValidateConfigurationAsync(cancellationToken),
                NotificationChannel.Webhook => true, // Webhook validation would require a specific URL
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
                NotificationChannel.Slack => await SendSlackMessageAsync(recipient, testMessage, cancellationToken),
                NotificationChannel.Teams => await SendTeamsMessageAsync(recipient, testMessage, cancellationToken),
                NotificationChannel.Webhook => await SendWebhookAsync(recipient, new { message = testMessage, test = true }, cancellationToken),
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

        // Slack and Teams availability would depend on their specific configurations
        if (await ValidateChannelConfigurationAsync(NotificationChannel.Slack, cancellationToken))
        {
            availableChannels.Add(NotificationChannel.Slack);
        }

        if (await ValidateChannelConfigurationAsync(NotificationChannel.Teams, cancellationToken))
        {
            availableChannels.Add(NotificationChannel.Teams);
        }

        // Webhook is always available as it doesn't require global configuration
        availableChannels.Add(NotificationChannel.Webhook);

        _logger.LogDebug("Available notification channels: {Channels}", string.Join(", ", availableChannels));

        return availableChannels;
    }

    #endregion
}
