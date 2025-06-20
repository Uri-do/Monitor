using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.DTOs.Hubs;

namespace MonitoringGrid.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time monitoring notifications
/// </summary>
[Authorize] // Authentication enabled for production security
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

    // Legacy KPI methods DELETED - Use Indicator methods only

    /// <summary>
    /// Join a specific Indicator monitoring group
    /// </summary>
    public async Task JoinIndicatorGroup(int indicatorId)
    {
        var groupName = $"Indicator_{indicatorId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug("Client {ConnectionId} joined Indicator group {GroupName}", Context.ConnectionId, groupName);

        await Clients.Caller.SendAsync("JoinedGroup", new
        {
            GroupName = groupName,
            Message = $"Joined Indicator {indicatorId} monitoring group"
        });
    }

    /// <summary>
    /// Leave a specific Indicator monitoring group
    /// </summary>
    public async Task LeaveIndicatorGroup(int indicatorId)
    {
        var groupName = $"Indicator_{indicatorId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug("Client {ConnectionId} left Indicator group {GroupName}", Context.ConnectionId, groupName);

        await Clients.Caller.SendAsync("LeftGroup", new
        {
            GroupName = groupName,
            Message = $"Left Indicator {indicatorId} monitoring group"
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
    /// Client-callable method for worker to broadcast indicator execution started
    /// </summary>
    public async Task SendIndicatorExecutionStartedAsync(object indicatorExecution)
    {
        try
        {
            _logger.LogDebug("Worker broadcasting indicator execution started: {Data}", indicatorExecution);

            // Broadcast to dashboard subscribers
            await Clients.Group("Dashboard")
                .SendAsync("IndicatorExecutionStarted", indicatorExecution);

            // Broadcast to specific indicator group if available
            if (indicatorExecution is IDictionary<string, object> dict && dict.ContainsKey("IndicatorID"))
            {
                var indicatorId = dict["IndicatorID"];
                await Clients.Group($"Indicator_{indicatorId}")
                    .SendAsync("IndicatorExecutionStarted", indicatorExecution);
            }

            _logger.LogDebug("Worker indicator execution started broadcast completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast worker indicator execution started");
        }
    }

    /// <summary>
    /// Client-callable method for worker to broadcast indicator execution completed
    /// </summary>
    public async Task SendIndicatorExecutionCompletedAsync(object indicatorCompletion)
    {
        try
        {
            _logger.LogDebug("Worker broadcasting indicator execution completed: {Data}", indicatorCompletion);

            // Broadcast to dashboard subscribers
            await Clients.Group("Dashboard")
                .SendAsync("IndicatorExecutionCompleted", indicatorCompletion);

            // Broadcast to specific indicator group if available
            if (indicatorCompletion is IDictionary<string, object> dict && dict.ContainsKey("IndicatorId"))
            {
                var indicatorId = dict["IndicatorId"];
                await Clients.Group($"Indicator_{indicatorId}")
                    .SendAsync("IndicatorExecutionCompleted", indicatorCompletion);
            }

            _logger.LogDebug("Worker indicator execution completed broadcast completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast worker indicator execution completed");
        }
    }

    /// <summary>
    /// Client-callable method for worker to broadcast indicator execution progress
    /// </summary>
    public async Task SendIndicatorExecutionProgressAsync(object indicatorProgress)
    {
        try
        {
            _logger.LogDebug("Worker broadcasting indicator execution progress: {Data}", indicatorProgress);

            // Broadcast to dashboard subscribers
            await Clients.Group("Dashboard")
                .SendAsync("IndicatorExecutionProgress", indicatorProgress);

            // Broadcast to specific indicator group if available
            if (indicatorProgress is IDictionary<string, object> dict && dict.ContainsKey("IndicatorId"))
            {
                var indicatorId = dict["IndicatorId"];
                await Clients.Group($"Indicator_{indicatorId}")
                    .SendAsync("IndicatorExecutionProgress", indicatorProgress);
            }

            _logger.LogDebug("Worker indicator execution progress broadcast completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast worker indicator execution progress");
        }
    }

    /// <summary>
    /// Client-callable method for worker to broadcast countdown updates
    /// </summary>
    public async Task SendIndicatorCountdownUpdateAsync(object countdownUpdate)
    {
        try
        {
            _logger.LogTrace("Worker broadcasting countdown update: {Data}", countdownUpdate);

            // Broadcast to dashboard subscribers
            await Clients.Group("Dashboard")
                .SendAsync("IndicatorCountdownUpdate", countdownUpdate);

            _logger.LogTrace("Worker countdown update broadcast completed");
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Failed to broadcast worker countdown update");
        }
    }

    /// <summary>
    /// Client-callable method for worker to broadcast worker status updates
    /// </summary>
    public async Task SendWorkerStatusUpdateAsync(object workerStatus)
    {
        try
        {
            _logger.LogTrace("Worker broadcasting status update: {Data}", workerStatus);

            // Broadcast to dashboard subscribers
            await Clients.Group("Dashboard")
                .SendAsync("WorkerStatusUpdate", workerStatus);

            _logger.LogTrace("Worker status update broadcast completed");
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "Failed to broadcast worker status update");
        }
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

    /// <summary>
    /// Join a specific group (generic method for frontend compatibility)
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug("Client {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);

        await Clients.Caller.SendAsync("JoinedGroup", new
        {
            GroupName = groupName,
            Message = $"Joined group {groupName}"
        });
    }

    /// <summary>
    /// Leave a specific group (generic method for frontend compatibility)
    /// </summary>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug("Client {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);

        await Clients.Caller.SendAsync("LeftGroup", new
        {
            GroupName = groupName,
            Message = $"Left group {groupName}"
        });
    }

    /// <summary>
    /// Test connection method for debugging SignalR connectivity
    /// </summary>
    public async Task TestConnection()
    {
        _logger.LogInformation("Test connection called by client {ConnectionId}", Context.ConnectionId);
        await Clients.Caller.SendAsync("TestMessage", new {
            message = "Test connection successful",
            connectionId = Context.ConnectionId,
            timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// Service for sending real-time notifications through SignalR
/// </summary>
public interface IRealtimeNotificationService
{
    Task SendAlertNotificationAsync(AlertNotificationDto alert);
    Task SendIndicatorUpdateAsync(IndicatorStatusUpdateDto indicatorUpdate);
    Task SendDashboardUpdateAsync(DashboardUpdateDto dashboardUpdate);
    Task SendSystemStatusAsync(SystemStatusDto systemStatus);

    // Enhanced real-time events
    Task SendWorkerStatusUpdateAsync(WorkerStatusUpdateDto workerStatus);

    // Legacy KPI events DELETED - Use Indicator events only
    Task SendCountdownUpdateAsync(CountdownUpdateDto countdown);

    // Indicator real-time events
    Task SendIndicatorExecutionStartedAsync(IndicatorExecutionStartedDto indicatorExecution);
    Task SendIndicatorExecutionProgressAsync(IndicatorExecutionProgressDto indicatorProgress);
    Task SendIndicatorExecutionCompletedAsync(IndicatorExecutionCompletedDto indicatorCompletion);
    Task SendIndicatorCountdownUpdateAsync(IndicatorCountdownUpdateDto countdown);

    Task SendIndicatorAlertAsync(IndicatorAlertDto indicatorAlert);
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
            _logger.LogDebug("Sending real-time alert notification for Indicator {IndicatorId}", alert.IndicatorId);

            // Send to all monitoring users
            await _hubContext.Clients.Group("MonitoringUsers")
                .SendAsync("AlertTriggered", alert);

            // Send to specific KPI group if anyone is monitoring it
            await _hubContext.Clients.Group($"Indicator_{alert.IndicatorId}")
                .SendAsync("IndicatorAlert", alert);

            // Send to dashboard subscribers
            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("DashboardAlert", alert);

            _logger.LogInformation("Real-time alert notification sent for Indicator {IndicatorId}", alert.IndicatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time alert notification for Indicator {IndicatorId}", alert.IndicatorId);
        }
    }

    public async Task SendIndicatorUpdateAsync(IndicatorStatusUpdateDto indicatorUpdate)
    {
        try
        {
            _logger.LogDebug("Sending real-time Indicator update for Indicator {IndicatorId}", indicatorUpdate.IndicatorId);

            // Send to specific Indicator group
            await _hubContext.Clients.Group($"Indicator_{indicatorUpdate.IndicatorId}")
                .SendAsync("IndicatorStatusUpdate", indicatorUpdate);

            // Send to dashboard if it's a significant update
            if (indicatorUpdate.IsSignificantChange)
            {
                await _hubContext.Clients.Group("Dashboard")
                    .SendAsync("DashboardIndicatorUpdate", indicatorUpdate);
            }

            _logger.LogDebug("Real-time Indicator update sent for Indicator {IndicatorId}", indicatorUpdate.IndicatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time Indicator update for Indicator {IndicatorId}", indicatorUpdate.IndicatorId);
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

    public async Task SendWorkerStatusUpdateAsync(WorkerStatusUpdateDto workerStatus)
    {
        try
        {
            _logger.LogDebug("Sending real-time worker status update");

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("WorkerStatusUpdate", workerStatus);

            _logger.LogDebug("Real-time worker status update sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time worker status update");
        }
    }

    // Legacy KPI methods DELETED - Use Indicator methods only

    // Broken duplicate method DELETED - Use correct method below

    // Obsolete KPI method DELETED - Use Indicator methods only

    public async Task SendCountdownUpdateAsync(CountdownUpdateDto countdown)
    {
        try
        {
            var nextIndicator = countdown.NextIndicators.FirstOrDefault();
            _logger.LogDebug("Sending countdown update for next Indicator {IndicatorId}: {Seconds} seconds",
                nextIndicator?.IndicatorId ?? 0, nextIndicator?.SecondsUntilDue ?? 0);

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("CountdownUpdate", countdown);

            _logger.LogDebug("Countdown update sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send countdown update");
        }
    }

    // Obsolete KPI schedule methods DELETED - Use Indicator methods only

    public async Task SendIndicatorExecutionStartedAsync(IndicatorExecutionStartedDto indicatorExecution)
    {
        try
        {
            _logger.LogDebug("Sending Indicator execution started notification for Indicator {IndicatorId}", indicatorExecution.IndicatorID);

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("IndicatorExecutionStarted", indicatorExecution);

            await _hubContext.Clients.Group($"Indicator_{indicatorExecution.IndicatorID}")
                .SendAsync("IndicatorExecutionStarted", indicatorExecution);

            _logger.LogDebug("Indicator execution started notification sent for Indicator {IndicatorId}", indicatorExecution.IndicatorID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Indicator execution started notification for Indicator {IndicatorId}", indicatorExecution.IndicatorID);
        }
    }

    public async Task SendIndicatorExecutionCompletedAsync(IndicatorExecutionCompletedDto indicatorCompletion)
    {
        try
        {
            _logger.LogDebug("Sending Indicator execution completed notification for Indicator {IndicatorId}", indicatorCompletion.IndicatorId);

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("IndicatorExecutionCompleted", indicatorCompletion);

            await _hubContext.Clients.Group($"Indicator_{indicatorCompletion.IndicatorId}")
                .SendAsync("IndicatorExecutionCompleted", indicatorCompletion);

            _logger.LogDebug("Indicator execution completed notification sent for Indicator {IndicatorId}", indicatorCompletion.IndicatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Indicator execution completed notification for Indicator {IndicatorId}", indicatorCompletion.IndicatorId);
        }
    }

    public async Task SendIndicatorExecutionProgressAsync(IndicatorExecutionProgressDto indicatorProgress)
    {
        try
        {
            _logger.LogDebug("Sending Indicator execution progress for Indicator {IndicatorId}: {Progress}%", indicatorProgress.IndicatorId, indicatorProgress.Progress);

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("IndicatorExecutionProgress", indicatorProgress);

            await _hubContext.Clients.Group($"Indicator_{indicatorProgress.IndicatorId}")
                .SendAsync("IndicatorExecutionProgress", indicatorProgress);

            _logger.LogDebug("Indicator execution progress sent for Indicator {IndicatorId}", indicatorProgress.IndicatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Indicator execution progress for Indicator {IndicatorId}", indicatorProgress.IndicatorId);
        }
    }

    public async Task SendIndicatorCountdownUpdateAsync(IndicatorCountdownUpdateDto countdown)
    {
        try
        {
            _logger.LogDebug("Sending Indicator countdown update for next Indicator {IndicatorId}: {Seconds} seconds", countdown.NextIndicatorID, countdown.SecondsUntilDue);

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("IndicatorCountdownUpdate", countdown);

            _logger.LogDebug("Indicator countdown update sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Indicator countdown update");
        }
    }



    public async Task SendIndicatorAlertAsync(IndicatorAlertDto indicatorAlert)
    {
        try
        {
            _logger.LogDebug("Sending real-time Indicator alert for Indicator {IndicatorId}", indicatorAlert.IndicatorID);

            // Send to all monitoring users
            await _hubContext.Clients.Group("MonitoringUsers")
                .SendAsync("IndicatorAlertTriggered", indicatorAlert);

            // Send to specific Indicator group if anyone is monitoring it
            await _hubContext.Clients.Group($"Indicator_{indicatorAlert.IndicatorID}")
                .SendAsync("IndicatorAlert", indicatorAlert);

            // Send to dashboard subscribers
            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("DashboardIndicatorAlert", indicatorAlert);

            _logger.LogInformation("Real-time Indicator alert sent for Indicator {IndicatorId}", indicatorAlert.IndicatorID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send real-time Indicator alert for Indicator {IndicatorId}", indicatorAlert.IndicatorID);
        }
    }
}
