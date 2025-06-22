

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for Slack integration services
/// </summary>
public interface ISlackService
{
    /// <summary>
    /// Sends a simple message to a Slack channel
    /// </summary>
    Task<bool> SendMessageAsync(string channel, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a rich message with attachments to a Slack channel
    /// </summary>
    Task<bool> SendRichMessageAsync(string channel, object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an alert notification to a Slack channel
    /// </summary>
    Task<bool> SendAlertAsync(string channel, MonitoringGrid.Core.Models.AlertNotificationDto alert, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the Slack configuration
    /// </summary>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}
