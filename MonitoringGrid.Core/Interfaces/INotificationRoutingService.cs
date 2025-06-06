using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Enums;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Interface for notification routing service
/// </summary>
public interface INotificationRoutingService
{
    Task<NotificationResult> RouteNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default);
    Task<List<NotificationChannel>> GetAvailableChannelsAsync(CancellationToken cancellationToken = default);
    Task<Entities.NotificationPreferences> GetUserPreferencesAsync(string userId, CancellationToken cancellationToken = default);
    Task UpdateUserPreferencesAsync(string userId, Entities.NotificationPreferences preferences, CancellationToken cancellationToken = default);
}
