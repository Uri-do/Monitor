using MonitoringGrid.Core.Models;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for Microsoft Teams integration service
/// </summary>
public interface ITeamsService
{
    Task<bool> SendMessageAsync(string webhookUrl, string message, CancellationToken cancellationToken = default);
    Task<bool> SendAlertAsync(string webhookUrl, AlertNotificationDto alert, CancellationToken cancellationToken = default);
    Task<bool> SendAdaptiveCardAsync(string webhookUrl, TeamsAdaptiveCard card, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);
}
