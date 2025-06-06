using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for webhook integration service
/// </summary>
public interface IWebhookService
{
    Task<bool> SendWebhookAsync(WebhookConfiguration webhook, object payload, CancellationToken cancellationToken = default);
    Task<bool> SendAlertWebhookAsync(WebhookConfiguration webhook, AlertNotificationDto alert, CancellationToken cancellationToken = default);
    Task<WebhookTestResult> TestWebhookAsync(WebhookConfiguration webhook, CancellationToken cancellationToken = default);
    Task<List<Entities.WebhookDeliveryLog>> GetDeliveryLogsAsync(int webhookId, DateTime? startDate = null, CancellationToken cancellationToken = default);
}
