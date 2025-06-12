using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MonitoringGrid.Api.DTOs;

namespace MonitoringGrid.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time monitoring notifications
/// </summary>
[AllowAnonymous] // Temporary: Allow anonymous access for testing real-time features
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
    Task SendKpiExecutionStartedAsync(KpiExecutionStartedDto kpiExecution);
    Task SendKpiExecutionProgressAsync(KpiExecutionProgressDto kpiProgress);
    Task SendKpiExecutionCompletedAsync(KpiExecutionCompletedDto kpiCompletion);
    Task SendCountdownUpdateAsync(CountdownUpdateDto countdown);
    Task SendNextKpiScheduleUpdateAsync(NextKpiScheduleUpdateDto schedule);
    Task SendRunningKpisUpdateAsync(RunningKpisUpdateDto runningKpis);

    // Indicator real-time events
    Task SendIndicatorExecutionStartedAsync(IndicatorExecutionStartedDto indicatorExecution);
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

    public async Task SendKpiExecutionStartedAsync(KpiExecutionStartedDto kpiExecution)
    {
        try
        {
            _logger.LogDebug("Sending KPI execution started notification for KPI {KpiId}", kpiExecution.KpiId);

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("KpiExecutionStarted", kpiExecution);

            await _hubContext.Clients.Group($"KPI_{kpiExecution.KpiId}")
                .SendAsync("KpiExecutionStarted", kpiExecution);

            _logger.LogDebug("KPI execution started notification sent for KPI {KpiId}", kpiExecution.KpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send KPI execution started notification for KPI {KpiId}", kpiExecution.KpiId);
        }
    }

    public async Task SendKpiExecutionProgressAsync(KpiExecutionProgressDto kpiProgress)
    {
        try
        {
            _logger.LogDebug("Sending KPI execution progress for KPI {KpiId}: {Progress}%", kpiProgress.KpiId, kpiProgress.Progress);

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("KpiExecutionProgress", kpiProgress);

            await _hubContext.Clients.Group($"KPI_{kpiProgress.KpiId}")
                .SendAsync("KpiExecutionProgress", kpiProgress);

            _logger.LogDebug("KPI execution progress sent for KPI {KpiId}", kpiProgress.KpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send KPI execution progress for KPI {KpiId}", kpiProgress.KpiId);
        }
    }

    public async Task SendKpiExecutionCompletedAsync(KpiExecutionCompletedDto kpiCompletion)
    {
        try
        {
            _logger.LogDebug("Sending KPI execution completed notification for KPI {KpiId}", kpiCompletion.KpiId);

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("KpiExecutionCompleted", kpiCompletion);

            await _hubContext.Clients.Group($"KPI_{kpiCompletion.KpiId}")
                .SendAsync("KpiExecutionCompleted", kpiCompletion);

            _logger.LogDebug("KPI execution completed notification sent for KPI {KpiId}", kpiCompletion.KpiId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send KPI execution completed notification for KPI {KpiId}", kpiCompletion.KpiId);
        }
    }

    public async Task SendCountdownUpdateAsync(CountdownUpdateDto countdown)
    {
        try
        {
            _logger.LogDebug("Sending countdown update for next KPI {KpiId}: {Seconds} seconds", countdown.NextKpiId, countdown.SecondsUntilDue);

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("CountdownUpdate", countdown);

            _logger.LogDebug("Countdown update sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send countdown update");
        }
    }

    public async Task SendNextKpiScheduleUpdateAsync(NextKpiScheduleUpdateDto schedule)
    {
        try
        {
            _logger.LogDebug("Sending next KPI schedule update with {Count} upcoming KPIs", schedule.NextKpis.Count);

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("NextKpiScheduleUpdate", schedule);

            _logger.LogDebug("Next KPI schedule update sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send next KPI schedule update");
        }
    }

    public async Task SendRunningKpisUpdateAsync(RunningKpisUpdateDto runningKpis)
    {
        try
        {
            _logger.LogDebug("Sending running KPIs update with {Count} running KPIs", runningKpis.RunningKpis.Count);

            await _hubContext.Clients.Group("Dashboard")
                .SendAsync("RunningKpisUpdate", runningKpis);

            _logger.LogDebug("Running KPIs update sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send running KPIs update");
        }
    }

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
