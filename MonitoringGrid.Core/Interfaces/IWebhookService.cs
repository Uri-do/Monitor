namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for webhook services
/// </summary>
public interface IWebhookService
{
    /// <summary>
    /// Sends a webhook with JSON payload
    /// </summary>
    Task<bool> SendWebhookAsync(string url, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a webhook with custom headers
    /// </summary>
    Task<bool> SendWebhookAsync(string url, object payload, Dictionary<string, string> headers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert via webhook
    /// </summary>
    Task<bool> SendAlertWebhookAsync(MonitoringGrid.Core.Models.WebhookConfiguration webhook, MonitoringGrid.Core.Models.AlertNotificationDto alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a webhook URL
    /// </summary>
    Task<bool> ValidateWebhookAsync(string url, CancellationToken cancellationToken = default);
}
