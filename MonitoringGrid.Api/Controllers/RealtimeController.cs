using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Hubs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Services;
using MonitoringGrid.Core.Specifications;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Real-time monitoring and webhook API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class RealtimeController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly IKpiExecutionService _kpiExecutionService;
    private readonly IAlertService _alertService;
    private readonly ILogger<RealtimeController> _logger;

    public RealtimeController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHubContext<MonitoringHub> hubContext,
        IKpiExecutionService kpiExecutionService,
        IAlertService alertService,
        ILogger<RealtimeController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _hubContext = hubContext;
        _kpiExecutionService = kpiExecutionService;
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Get real-time system status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<RealtimeStatusDto>> GetRealtimeStatus()
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var alertRepository = _unitOfWork.Repository<AlertLog>();

            var now = DateTime.UtcNow;
            var last5Minutes = now.AddMinutes(-5);

            var allKpis = await kpiRepository.GetAllAsync();
            var activeKpis = allKpis.Where(k => k.IsActive);

            var allAlerts = await alertRepository.GetAllAsync();
            var recentAlerts = allAlerts.Where(a => a.TriggerTime >= last5Minutes);

            var dueKpisSpec = new KpisDueForExecutionSpecification();
            var dueKpis = await kpiRepository.GetAsync(dueKpisSpec);

            var status = new RealtimeStatusDto
            {
                Timestamp = now,
                ActiveKpis = activeKpis.Count(),
                DueKpis = dueKpis.Count(),
                RecentAlerts = recentAlerts.Count(),
                UnresolvedAlerts = recentAlerts.Count(a => !a.IsResolved),
                SystemLoad = CalculateSystemLoad(activeKpis.ToList(), dueKpis.ToList()),
                LastUpdate = now
            };

            // Broadcast to connected clients
            await _hubContext.Clients.All.SendAsync("StatusUpdate", status);

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving real-time status");
            return StatusCode(500, "An error occurred while retrieving real-time status");
        }
    }

    /// <summary>
    /// Execute KPI and broadcast results in real-time
    /// </summary>
    [HttpPost("execute/{id}")]
    public async Task<ActionResult<KpiExecutionResultDto>> ExecuteKpiRealtime(int id)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdAsync(id);
            
            if (kpi == null)
                return NotFound($"KPI with ID {id} not found");

            // Execute KPI
            var result = await _kpiExecutionService.ExecuteKpiAsync(kpi);

            // Convert to DTO
            var resultDto = _mapper.Map<KpiExecutionResultDto>(result);
            resultDto.KpiId = id;
            resultDto.Indicator = kpi.Indicator;

            // Broadcast execution result to connected clients
            await _hubContext.Clients.All.SendAsync("KpiExecuted", new
            {
                KpiId = id,
                Indicator = kpi.Indicator,
                Result = resultDto,
                Timestamp = DateTime.UtcNow
            });

            // If alert was triggered, broadcast alert notification
            if (result.ShouldAlert)
            {
                var alertNotification = new AlertNotificationDto
                {
                    KpiId = id,
                    Indicator = kpi.Indicator,
                    Owner = kpi.Owner,
                    Priority = kpi.Priority,
                    CurrentValue = result.CurrentValue,
                    HistoricalValue = result.HistoricalValue,
                    Deviation = result.DeviationPercent,
                    Subject = kpi.SubjectTemplate,
                    Description = kpi.DescriptionTemplate,
                    TriggerTime = DateTime.UtcNow,
                    Severity = CalculateSeverity(result.DeviationPercent),
                    NotifiedContacts = new List<string>() // Would be populated from actual notification service
                };

                await _hubContext.Clients.All.SendAsync("AlertTriggered", alertNotification);
            }

            _logger.LogInformation("Real-time KPI execution completed for {Indicator}", kpi.Indicator);

            return Ok(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing KPI {KpiId} in real-time", id);
            return StatusCode(500, "An error occurred while executing the KPI");
        }
    }

    /// <summary>
    /// Get live dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<LiveDashboardDto>> GetLiveDashboard()
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var alertRepository = _unitOfWork.Repository<AlertLog>();
            var historicalRepository = _unitOfWork.Repository<HistoricalData>();

            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);
            var lastHour = now.AddHours(-1);

            var kpis = (await kpiRepository.GetAllAsync()).ToList();
            var recentAlerts = (await alertRepository.GetAllAsync())
                .Where(a => a.TriggerTime >= last24Hours)
                .ToList();
            var recentExecutions = (await historicalRepository.GetAllAsync())
                .Where(h => h.Timestamp >= lastHour)
                .ToList();

            var dueKpisSpec = new KpisDueForExecutionSpecification();
            var dueKpis = await kpiRepository.GetAsync(dueKpisSpec);

            var dashboard = new LiveDashboardDto
            {
                Timestamp = now,
                TotalKpis = kpis.Count,
                ActiveKpis = kpis.Count(k => k.IsActive),
                DueKpis = dueKpis.Count(),
                ExecutionsLastHour = recentExecutions.Count,
                AlertsLast24Hours = recentAlerts.Count,
                UnresolvedAlerts = recentAlerts.Count(a => !a.IsResolved),
                CriticalAlerts = recentAlerts.Count(a => a.DeviationPercent.HasValue && 
                    Math.Abs(a.DeviationPercent.Value) >= 50),
                SystemHealth = CalculateSystemHealth(kpis, recentAlerts),
                RecentExecutions = recentExecutions
                    .OrderByDescending(h => h.Timestamp)
                    .Take(10)
                    .Select(h => new RecentExecutionDto
                    {
                        KpiId = h.KpiId,
                        Indicator = kpis.FirstOrDefault(k => k.KpiId == h.KpiId)?.Indicator ?? "Unknown",
                        Timestamp = h.Timestamp,
                        Value = h.Value,
                        DeviationPercent = h.DeviationPercent ?? 0,
                        IsSuccessful = h.IsSuccessful,
                        ExecutionTimeMs = h.ExecutionTimeMs ?? 0
                    })
                    .ToList(),
                RecentAlerts = recentAlerts
                    .OrderByDescending(a => a.TriggerTime)
                    .Take(5)
                    .Select(a => new RecentAlertDto
                    {
                        KpiId = a.KpiId,
                        Indicator = kpis.FirstOrDefault(k => k.KpiId == a.KpiId)?.Indicator ?? "Unknown",
                        Owner = kpis.FirstOrDefault(k => k.KpiId == a.KpiId)?.Owner ?? "Unknown",
                        TriggerTime = a.TriggerTime,
                        Deviation = a.DeviationPercent ?? 0,
                        Severity = CalculateSeverity(a.DeviationPercent ?? 0)
                    })
                    .ToList()
            };

            // Broadcast dashboard update to connected clients
            await _hubContext.Clients.All.SendAsync("DashboardUpdate", dashboard);

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving live dashboard");
            return StatusCode(500, "An error occurred while retrieving live dashboard");
        }
    }

    /// <summary>
    /// Webhook endpoint for external integrations
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous] // Consider adding webhook authentication
    public async Task<IActionResult> Webhook([FromBody] WebhookPayloadDto payload)
    {
        try
        {
            _logger.LogInformation("Webhook received: {Type} from {Source}", payload.Type, payload.Source);

            switch (payload.Type.ToLower())
            {
                case "kpi_execution":
                    await HandleKpiExecutionWebhook(payload);
                    break;
                case "alert_triggered":
                    await HandleAlertWebhook(payload);
                    break;
                case "system_status":
                    await HandleSystemStatusWebhook(payload);
                    break;
                default:
                    _logger.LogWarning("Unknown webhook type: {Type}", payload.Type);
                    return BadRequest($"Unknown webhook type: {payload.Type}");
            }

            return Ok(new { Message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook: {@Payload}", payload);
            return StatusCode(500, "An error occurred while processing the webhook");
        }
    }

    /// <summary>
    /// Get connection info for SignalR
    /// </summary>
    [HttpGet("connection-info")]
    public ActionResult<ConnectionInfoDto> GetConnectionInfo()
    {
        return Ok(new ConnectionInfoDto
        {
            HubUrl = "/monitoring-hub",
            AccessToken = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", ""),
            ConnectionId = Guid.NewGuid().ToString(), // This would be generated by SignalR
            SupportedEvents = new List<string>
            {
                "StatusUpdate",
                "KpiExecuted", 
                "AlertTriggered",
                "DashboardUpdate",
                "SystemHealthUpdate"
            }
        });
    }

    #region Helper Methods

    private double CalculateSystemLoad(List<KPI> activeKpis, List<KPI> dueKpis)
    {
        if (!activeKpis.Any()) return 0;
        return (double)dueKpis.Count / activeKpis.Count * 100;
    }

    private string CalculateSeverity(decimal deviationPercent)
    {
        var absDeviation = Math.Abs(deviationPercent);
        return absDeviation switch
        {
            >= 50 => "Critical",
            >= 25 => "High",
            >= 10 => "Medium",
            _ => "Low"
        };
    }

    private string CalculateSystemHealth(List<KPI> kpis, List<AlertLog> recentAlerts)
    {
        var activeKpis = kpis.Where(k => k.IsActive).ToList();
        if (!activeKpis.Any()) return "Unknown";

        var kpisWithAlerts = recentAlerts.Select(a => a.KpiId).Distinct().Count();
        var healthPercentage = (double)(activeKpis.Count - kpisWithAlerts) / activeKpis.Count * 100;

        return healthPercentage switch
        {
            >= 90 => "Excellent",
            >= 75 => "Good",
            >= 50 => "Fair",
            >= 25 => "Poor",
            _ => "Critical"
        };
    }

    private async Task HandleKpiExecutionWebhook(WebhookPayloadDto payload)
    {
        // Broadcast KPI execution update to connected clients
        await _hubContext.Clients.All.SendAsync("KpiExecutionWebhook", new
        {
            Source = payload.Source,
            Data = payload.Data,
            Timestamp = payload.Timestamp
        });
    }

    private async Task HandleAlertWebhook(WebhookPayloadDto payload)
    {
        // Broadcast alert webhook to connected clients
        await _hubContext.Clients.All.SendAsync("AlertWebhook", new
        {
            Source = payload.Source,
            Data = payload.Data,
            Timestamp = payload.Timestamp
        });
    }

    private async Task HandleSystemStatusWebhook(WebhookPayloadDto payload)
    {
        // Broadcast system status update to connected clients
        await _hubContext.Clients.All.SendAsync("SystemStatusWebhook", new
        {
            Source = payload.Source,
            Data = payload.Data,
            Timestamp = payload.Timestamp
        });
    }

    #endregion
}

#region Realtime DTOs

/// <summary>
/// Real-time status DTO
/// </summary>
public class RealtimeStatusDto
{
    public DateTime Timestamp { get; set; }
    public int ActiveKpis { get; set; }
    public int DueKpis { get; set; }
    public int RecentAlerts { get; set; }
    public int UnresolvedAlerts { get; set; }
    public double SystemLoad { get; set; }
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// Live dashboard DTO
/// </summary>
public class LiveDashboardDto
{
    public DateTime Timestamp { get; set; }
    public int TotalKpis { get; set; }
    public int ActiveKpis { get; set; }
    public int DueKpis { get; set; }
    public int ExecutionsLastHour { get; set; }
    public int AlertsLast24Hours { get; set; }
    public int UnresolvedAlerts { get; set; }
    public int CriticalAlerts { get; set; }
    public string SystemHealth { get; set; } = string.Empty;
    public List<RecentExecutionDto> RecentExecutions { get; set; } = new();
    public List<RecentAlertDto> RecentAlerts { get; set; } = new();
}

/// <summary>
/// Recent execution DTO
/// </summary>
public class RecentExecutionDto
{
    public int KpiId { get; set; }
    public string Indicator { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public decimal DeviationPercent { get; set; }
    public bool IsSuccessful { get; set; }
    public int ExecutionTimeMs { get; set; }
}

/// <summary>
/// Webhook payload DTO
/// </summary>
public class WebhookPayloadDto
{
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Connection info DTO
/// </summary>
public class ConnectionInfoDto
{
    public string HubUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public List<string> SupportedEvents { get; set; } = new();
}

#endregion
