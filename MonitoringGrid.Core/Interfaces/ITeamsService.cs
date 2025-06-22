

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for Microsoft Teams integration services
/// </summary>
public interface ITeamsService
{
    /// <summary>
    /// Sends a simple message to a Teams channel
    /// </summary>
    Task<bool> SendMessageAsync(string webhookUrl, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a rich message with cards to a Teams channel
    /// </summary>
    Task<bool> SendRichMessageAsync(string webhookUrl, object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert notification to a Teams channel
    /// </summary>
    Task<bool> SendAlertAsync(string webhookUrl, MonitoringGrid.Core.Models.AlertNotificationDto alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the Teams webhook configuration
    /// </summary>
    Task<bool> ValidateWebhookAsync(string webhookUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the Teams configuration
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}
