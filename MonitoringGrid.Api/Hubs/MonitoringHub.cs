using Microsoft.AspNetCore.SignalR;
using MonitoringGrid.Api.DTOs;

namespace MonitoringGrid.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time monitoring notifications
/// </summary>
public class MonitoringHub : Hub
{
    private readonly ILogger<MonitoringHub> _logger;

    public MonitoringHub(ILogger<MonitoringHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        
        // Add to general monitoring group
        await Groups.AddToGroupAsync(Context.ConnectionId, "MonitoringUsers");
        
        // Send current system status to the new client
        await Clients.Caller.SendAsync("SystemStatus", new
        {
            Status = "Connected",
            Timestamp = DateTime.UtcNow,
            Message = "Connected to monitoring system"
        });

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a specific KPI monitoring group
    /// </summary>
    public async Task JoinKpiGroup(int kpiId)
    {
        var groupName = $"KPI_{kpiId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogDebug("Client {ConnectionId} joined KPI group {GroupName}", Context.ConnectionId, groupName);
        
        await Clients.Caller.SendAsync("JoinedGroup", new
        {
            GroupName = groupName,
            Message = $"Joined KPI {kpiId} monitoring group"
        });
    }

    /// <summary>
    /// Leave a specific KPI monitoring group
    /// </summary>
    public async Task LeaveKpiGroup(int kpiId)
    {
        var groupName = $"KPI_{kpiId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        
        _logger.LogDebug("Client {ConnectionId} left KPI group {GroupName}", Context.ConnectionId, groupName);
        
        await Clients.Caller.SendAsync("LeftGroup", new
        {
            GroupName = groupName,
            Message = $"Left KPI {kpiId} monitoring group"
        });
    }

    /// <summary>
    /// Subscribe to dashboard updates
    /// </summary>
    public async Task SubscribeToDashboard()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Dashboard");
        
        _logger.LogDebug("Client {ConnectionId} subscribed to dashboard updates", Context.ConnectionId);
        
        await Clients.Caller.SendAsync("SubscribedToDashboard", new
        {
            Message = "Subscribed to dashboard updates"
        });
    }

    /// <summary>
    /// Unsubscribe from dashboard updates
    /// </summary>
    public async Task UnsubscribeFromDashboard()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Dashboard");
        
        _logger.LogDebug("Client {ConnectionId} unsubscribed from dashboard updates", Context.ConnectionId);
        
        await Clients.Caller.SendAsync("UnsubscribedFromDashboard", new
        {
            Message = "Unsubscribed from dashboard updates"
        });
    }
}

/// <summary>
/// Service for sending real-time notifications through SignalR
/// </summary>
public interface IRealtimeNotificationService
{
    Task SendAlertNotificationAsync(AlertNotificationDto alert);
    Task SendKpiUpdateAsync(KpiStatusUpdateDto kpiUpdate);
    Task SendDashboardUpdateAsync(DashboardUpdateDto dashboardUpdate);
    Task SendSystemStatusAsync(SystemStatusDto systemStatus);
}

/// <summary>
/// Implementation of real-time notification service
/// </summary>
public class RealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly ILogger<RealtimeNotificationService> _logger;

    public RealtimeNotificationService(
        IHubContext<MonitoringHub> hubContext,
        ILogger<RealtimeNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendAlertNotificationAsync(AlertNotificationDto alert)
    {
        try
        {
            _logger.LogDebug("Sending real-time alert notification for KPI {KpiId}", alert.KpiId);

            // Send to all monitoring users
            await _hubContext.Clients.Group("MonitoringUsers")
                .SendAsync("AlertTriggered", alert);

            // Send to specific KPI group if anyone is monitoring it
            await _hubContext.Clients.Group($"KPI_{alert.KpiId}")
                .SendAsync("KpiAlert", alert);

            // Send to dashboard subscribers
            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("DashboardAlert", alert);

            _logger.LogInformation("Real-time alert notification sent for KPI {KpiId}", alert.KpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time alert notification for KPI {KpiId}", alert.KpiId);
        }
    }

    public async Task SendKpiUpdateAsync(KpiStatusUpdateDto kpiUpdate)
    {
        try
        {
            _logger.LogDebug("Sending real-time KPI update for KPI {KpiId}", kpiUpdate.KpiId);

            // Send to specific KPI group
            await _hubContext.Clients.Group($"KPI_{kpiUpdate.KpiId}")
                .SendAsync("KpiStatusUpdate", kpiUpdate);

            // Send to dashboard if it's a significant update
            if (kpiUpdate.IsSignificantChange)
            {
                await _hubContext.Clients.Group("Dashboard")
                    .SendAsync("DashboardKpiUpdate", kpiUpdate);
            }

            _logger.LogDebug("Real-time KPI update sent for KPI {KpiId}", kpiUpdate.KpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time KPI update for KPI {KpiId}", kpiUpdate.KpiId);
        }
    }

    public async Task SendDashboardUpdateAsync(DashboardUpdateDto dashboardUpdate)
    {
        try
        {
            _logger.LogDebug("Sending real-time dashboard update");

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("DashboardUpdate", dashboardUpdate);

            _logger.LogDebug("Real-time dashboard update sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time dashboard update");
        }
    }

    public async Task SendSystemStatusAsync(SystemStatusDto systemStatus)
    {
        try
        {
            _logger.LogDebug("Sending real-time system status update");

            await _hubContext.Clients.Group("MonitoringUsers")
                .SendAsync("SystemStatusUpdate", systemStatus);

            _logger.LogDebug("Real-time system status update sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time system status update");
        }
    }
}
