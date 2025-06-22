using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MonitoringGrid.Api.Hubs;

/// <summary>
/// SignalR Hub for worker integration test real-time updates
/// </summary>
[AllowAnonymous] // Allow anonymous access for testing
public class WorkerIntegrationTestHub : Hub
{
    private readonly ILogger<WorkerIntegrationTestHub> _logger;

    public WorkerIntegrationTestHub(ILogger<WorkerIntegrationTestHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client {ConnectionId} connected to WorkerIntegrationTestHub", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected from WorkerIntegrationTestHub. Exception: {Exception}", 
            Context.ConnectionId, exception?.Message);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to worker test updates
    /// </summary>
    public async Task SubscribeToWorkerTests()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "WorkerTests");
        _logger.LogInformation("Client {ConnectionId} subscribed to worker test updates", Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from worker test updates
    /// </summary>
    public async Task UnsubscribeFromWorkerTests()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "WorkerTests");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from worker test updates", Context.ConnectionId);
    }

    /// <summary>
    /// Join a specific worker test group for real-time updates
    /// </summary>
    public async Task JoinWorkerTestGroup(string testId)
    {
        var groupName = $"WorkerTest_{testId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined worker test group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a specific worker test group
    /// </summary>
    public async Task LeaveWorkerTestGroup(string testId)
    {
        var groupName = $"WorkerTest_{testId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left worker test group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Join a generic group (for testing purposes)
    /// </summary>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a generic group (for testing purposes)
    /// </summary>
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Client {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Send a test message to verify connectivity
    /// </summary>
    public async Task SendTestMessage(string message)
    {
        _logger.LogInformation("Received test message from {ConnectionId}: {Message}", Context.ConnectionId, message);
        await Clients.Caller.SendAsync("TestMessageReceived", new
        {
            Message = $"Echo: {message}",
            Timestamp = DateTime.UtcNow,
            ConnectionId = Context.ConnectionId
        });
    }

    /// <summary>
    /// Send system health update
    /// </summary>
    public async Task SendSystemHealthUpdate(object healthData)
    {
        await Clients.All.SendAsync("SystemHealthUpdate", healthData);
        _logger.LogDebug("System health update sent to all clients");
    }

    /// <summary>
    /// Send worker process lifecycle event
    /// </summary>
    public async Task SendWorkerProcessEvent(string eventType, object eventData)
    {
        await Clients.All.SendAsync("WorkerProcessEvent", new
        {
            EventType = eventType,
            Data = eventData,
            Timestamp = DateTime.UtcNow
        });
        _logger.LogDebug("Worker process event sent: {EventType}", eventType);
    }

    /// <summary>
    /// Send detailed error information
    /// </summary>
    public async Task SendErrorEvent(string errorType, string message, object? details = null)
    {
        await Clients.All.SendAsync("ErrorEvent", new
        {
            ErrorType = errorType,
            Message = message,
            Details = details,
            Timestamp = DateTime.UtcNow
        });
        _logger.LogWarning("Error event sent: {ErrorType} - {Message}", errorType, message);
    }
}
