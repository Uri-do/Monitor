using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Hubs;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Controller for testing SignalR functionality
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class SignalRTestController : ControllerBase
{
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly ILogger<SignalRTestController> _logger;

    public SignalRTestController(
        IHubContext<MonitoringHub> hubContext,
        IRealtimeNotificationService realtimeNotificationService,
        ILogger<SignalRTestController> logger)
    {
        _hubContext = hubContext;
        _realtimeNotificationService = realtimeNotificationService;
        _logger = logger;
    }

    /// <summary>
    /// Test SignalR connection by sending a test message
    /// </summary>
    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("TestMessage", new
            {
                Message = "SignalR connection test successful!",
                Timestamp = DateTime.UtcNow,
                Server = Environment.MachineName
            });

            _logger.LogInformation("SignalR test message sent successfully");
            return Ok(new { success = true, message = "Test message sent to all connected clients" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR test message");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Test worker status update
    /// </summary>
    [HttpPost("test-worker-status")]
    public async Task<IActionResult> TestWorkerStatus()
    {
        try
        {
            var workerStatus = new WorkerStatusUpdateDto
            {
                IsRunning = true,
                Mode = "Test",
                ProcessId = Environment.ProcessId,
                Services = new List<WorkerServiceDto>
                {
                    new() { Name = "TestService1", Status = "Running", LastActivity = DateTime.UtcNow },
                    new() { Name = "TestService2", Status = "Running", LastActivity = DateTime.UtcNow }
                },
                LastHeartbeat = DateTime.UtcNow.ToString("O"),
                Uptime = "00:05:30"
            };

            await _realtimeNotificationService.SendWorkerStatusUpdateAsync(workerStatus);

            _logger.LogInformation("Test worker status update sent successfully");
            return Ok(new { success = true, message = "Test worker status sent", data = workerStatus });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test worker status");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Test countdown update
    /// </summary>
    [HttpPost("test-countdown")]
    public async Task<IActionResult> TestCountdown()
    {
        try
        {
            var countdown = new CountdownUpdateDto
            {
                NextKpiId = 1,
                Indicator = "Test KPI",
                Owner = "Test Owner",
                SecondsUntilDue = 300, // 5 minutes
                ScheduledTime = DateTime.UtcNow.AddMinutes(5).ToString("O")
            };

            await _realtimeNotificationService.SendCountdownUpdateAsync(countdown);

            _logger.LogInformation("Test countdown update sent successfully");
            return Ok(new { success = true, message = "Test countdown sent", data = countdown });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test countdown");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Test running KPIs update
    /// </summary>
    [HttpPost("test-running-kpis")]
    public async Task<IActionResult> TestRunningKpis()
    {
        try
        {
            var runningKpis = new RunningKpisUpdateDto
            {
                RunningKpis = new List<RunningKpiDto>
                {
                    new()
                    {
                        KpiId = 1,
                        Indicator = "Test KPI 1",
                        Owner = "Test Owner 1",
                        StartTime = DateTime.UtcNow.AddMinutes(-2).ToString("O"),
                        Progress = 45,
                        EstimatedCompletion = DateTime.UtcNow.AddMinutes(3).ToString("O")
                    },
                    new()
                    {
                        KpiId = 2,
                        Indicator = "Test KPI 2",
                        Owner = "Test Owner 2",
                        StartTime = DateTime.UtcNow.AddMinutes(-1).ToString("O"),
                        Progress = 75,
                        EstimatedCompletion = DateTime.UtcNow.AddMinutes(1).ToString("O")
                    }
                }
            };

            await _realtimeNotificationService.SendRunningKpisUpdateAsync(runningKpis);

            _logger.LogInformation("Test running KPIs update sent successfully");
            return Ok(new { success = true, message = "Test running KPIs sent", data = runningKpis });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test running KPIs");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get SignalR connection info
    /// </summary>
    [HttpGet("connection-info")]
    public IActionResult GetConnectionInfo()
    {
        return Ok(new
        {
            hubPath = "/monitoring-hub",
            serverTime = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            machineName = Environment.MachineName,
            processId = Environment.ProcessId
        });
    }

    /// <summary>
    /// Send test message to specific group
    /// </summary>
    [HttpPost("test-group/{groupName}")]
    public async Task<IActionResult> TestGroup(string groupName)
    {
        try
        {
            await _hubContext.Clients.Group(groupName).SendAsync("GroupTestMessage", new
            {
                Group = groupName,
                Message = $"Test message for group: {groupName}",
                Timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Test message sent to group {GroupName}", groupName);
            return Ok(new { success = true, message = $"Test message sent to group: {groupName}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test message to group {GroupName}", groupName);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Check if RealtimeUpdateService is running and get KPI status
    /// </summary>
    [HttpGet("service-status")]
    public async Task<IActionResult> GetServiceStatus([FromServices] IKpiService kpiService)
    {
        try
        {
            var kpis = await kpiService.GetAllKpisAsync();
            var activeKpis = kpis.Where(k => k.IsActive).ToList();

            var nextKpiData = activeKpis
                .Select(k => new { Kpi = k, NextRun = k.GetNextRunTime() })
                .Where(x => x.NextRun.HasValue)
                .OrderBy(x => x.NextRun)
                .FirstOrDefault();

            return Ok(new
            {
                success = true,
                totalKpis = kpis.Count(),
                activeKpis = activeKpis.Count,
                nextKpi = nextKpiData != null ? new
                {
                    kpiId = nextKpiData.Kpi.KpiId,
                    indicator = nextKpiData.Kpi.Indicator,
                    owner = nextKpiData.Kpi.Owner,
                    nextRun = nextKpiData.NextRun?.ToString("O"),
                    secondsUntilDue = nextKpiData.NextRun.HasValue
                        ? (int)(nextKpiData.NextRun.Value - DateTime.UtcNow).TotalSeconds
                        : 0
                } : null,
                serverTime = DateTime.UtcNow.ToString("O"),
                realtimeServiceRunning = true // If this endpoint responds, the service is running
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service status");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
