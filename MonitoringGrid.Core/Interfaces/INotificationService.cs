using MonitoringGrid.Core.Enums;
using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Consolidated notification service interface supporting multiple channels
/// Replaces individual IEmailService, ISmsService, ISlackService, ITeamsService, IWebhookService
/// </summary>
public interface INotificationService
{
    #region Email Notifications
    
    /// <summary>
    /// Sends an email to a single recipient
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email to multiple recipients
    /// </summary>
    Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);

    #endregion

    #region SMS Notifications
    
    /// <summary>
    /// Sends SMS to a single phone number
    /// </summary>
    Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends SMS to multiple phone numbers
    /// </summary>
    Task<bool> SendSmsAsync(IEnumerable<string> phoneNumbers, string message, CancellationToken cancellationToken = default);

    #endregion

    #region Slack Notifications
    
    /// <summary>
    /// Sends a message to a Slack channel
    /// </summary>
    Task<bool> SendSlackMessageAsync(string channel, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert notification to Slack
    /// </summary>
    Task<bool> SendSlackAlertAsync(string channel, AlertNotificationDto alert, CancellationToken cancellationToken = default);

    #endregion

    #region Teams Notifications
    
    /// <summary>
    /// Sends a message to Microsoft Teams
    /// </summary>
    Task<bool> SendTeamsMessageAsync(string webhookUrl, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert notification to Teams
    /// </summary>
    Task<bool> SendTeamsAlertAsync(string webhookUrl, AlertNotificationDto alert, CancellationToken cancellationToken = default);

    #endregion

    #region Webhook Notifications
    
    /// <summary>
    /// Sends a webhook notification
    /// </summary>
    Task<bool> SendWebhookAsync(string url, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert via webhook
    /// </summary>
    Task<bool> SendWebhookAlertAsync(string url, AlertNotificationDto alert, CancellationToken cancellationToken = default);

    #endregion

    #region Multi-Channel Notifications
    
    /// <summary>
    /// Sends a notification through multiple channels based on configuration
    /// </summary>
    Task<NotificationResult> SendMultiChannelNotificationAsync(
        NotificationRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert through all configured channels for a contact
    /// </summary>
    Task<NotificationResult> SendAlertNotificationAsync(
        AlertNotificationDto alert,
        List<NotificationChannel> channels,
        CancellationToken cancellationToken = default);

    #endregion

    #region Configuration and Testing
    
    /// <summary>
    /// Validates configuration for a specific notification channel
    /// </summary>
    Task<bool> ValidateChannelConfigurationAsync(NotificationChannel channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a test notification through a specific channel
    /// </summary>
    Task<bool> SendTestNotificationAsync(NotificationChannel channel, string recipient, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available notification channels
    /// </summary>
    Task<List<NotificationChannel>> GetAvailableChannelsAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Notification request model for multi-channel notifications
/// </summary>
public class NotificationRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<NotificationChannel> Channels { get; set; } = new();
    public List<string> Recipients { get; set; } = new();
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result of a notification operation
/// </summary>
public class NotificationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public Dictionary<NotificationChannel, bool> ChannelResults { get; set; } = new();
    public int TotalSent { get; set; }
    public int TotalFailed { get; set; }
    
    public string GetSummary()
    {
        if (IsSuccess)
            return $"Notification sent successfully to {TotalSent} recipient(s)";
        
        return $"Notification partially failed: {TotalSent} sent, {TotalFailed} failed";
    }
}
