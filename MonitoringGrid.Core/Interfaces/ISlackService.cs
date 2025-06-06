using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for Slack integration service
/// </summary>
public interface ISlackService
{
    Task<bool> SendMessageAsync(string channel, string message, CancellationToken cancellationToken = default);
    Task<bool> SendAlertAsync(string channel, AlertNotificationDto alert, CancellationToken cancellationToken = default);
    Task<bool> SendRichMessageAsync(string channel, SlackMessage message, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}
