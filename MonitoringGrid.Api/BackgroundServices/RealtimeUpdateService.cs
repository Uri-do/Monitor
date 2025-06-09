using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Hubs;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Entities;
using System.Diagnostics;

namespace MonitoringGrid.Api.BackgroundServices;

/// <summary>
/// Background service that sends periodic real-time updates to connected clients
/// </summary>
public class RealtimeUpdateService : BackgroundService
{
    private readonly ILogger<RealtimeUpdateService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(5); // Update every 5 seconds
    private readonly TimeSpan _countdownInterval = TimeSpan.FromSeconds(1); // Countdown every second

    public RealtimeUpdateService(
        ILogger<RealtimeUpdateService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Real-time update service started");

        // Start countdown timer
        var countdownTask = RunCountdownUpdatesAsync(stoppingToken);
        
        // Start periodic updates
        var updatesTask = RunPeriodicUpdatesAsync(stoppingToken);

        try
        {
            await Task.WhenAll(countdownTask, updatesTask);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Real-time update service is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in real-time update service");
        }
        finally
        {
            _logger.LogInformation("Real-time update service stopped");
        }
    }

    private async Task RunCountdownUpdatesAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendCountdownUpdateAsync();
                await Task.Delay(_countdownInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending countdown update");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Wait before retrying
            }
        }
    }

    private async Task RunPeriodicUpdatesAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendPeriodicUpdatesAsync();
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending periodic updates");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait before retrying
            }
        }
    }

    private async Task SendPeriodicUpdatesAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        try
        {
            // Send worker status update
            await SendWorkerStatusUpdateAsync(scope);

            // Send running KPIs update
            await SendRunningKpisUpdateAsync(scope);

            // Send next KPI schedule update
            await SendNextKpiScheduleUpdateAsync(scope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in periodic updates");
        }
    }

    private async Task SendWorkerStatusUpdateAsync(IServiceScope scope)
    {
        try
        {
            var realtimeNotificationService = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
            var process = Process.GetCurrentProcess();
            var workerStatus = new WorkerStatusUpdateDto
            {
                IsRunning = true, // API is running if this service is running
                Mode = "Integrated", // Running as part of API
                ProcessId = process.Id,
                Services = new List<WorkerServiceDto>
                {
                    new() { Name = "KpiMonitoring", Status = "Running", LastActivity = DateTime.UtcNow },
                    new() { Name = "HealthCheck", Status = "Running", LastActivity = DateTime.UtcNow },
                    new() { Name = "RealtimeUpdates", Status = "Running", LastActivity = DateTime.UtcNow }
                },
                LastHeartbeat = DateTime.UtcNow.ToString("O"),
                Uptime = (DateTime.UtcNow - process.StartTime).ToString(@"hh\:mm\:ss")
            };

            await realtimeNotificationService.SendWorkerStatusUpdateAsync(workerStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending worker status update");
        }
    }

    private async Task SendRunningKpisUpdateAsync(IServiceScope scope)
    {
        try
        {
            var realtimeNotificationService = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
            var kpiRepository = scope.ServiceProvider.GetRequiredService<IRepository<KPI>>();
            var kpis = await kpiRepository.GetAllAsync();

            // For demo purposes, simulate some running KPIs
            // In a real implementation, this would come from actual execution tracking
            var runningKpis = kpis
                .Where(k => k.IsActive && k.IsCurrentlyRunning)
                .Select(k => new RunningKpiDto
                {
                    KpiId = k.KpiId,
                    Indicator = k.Indicator,
                    Owner = k.Owner,
                    StartTime = k.ExecutionStartTime?.ToString("O") ?? DateTime.UtcNow.ToString("O"),
                    Progress = Random.Shared.Next(10, 90), // Simulate progress
                    EstimatedCompletion = DateTime.UtcNow.AddMinutes(Random.Shared.Next(1, 5)).ToString("O")
                })
                .ToList();

            var runningKpisUpdate = new RunningKpisUpdateDto
            {
                RunningKpis = runningKpis
            };

            await realtimeNotificationService.SendRunningKpisUpdateAsync(runningKpisUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending running KPIs update");
        }
    }

    private async Task SendNextKpiScheduleUpdateAsync(IServiceScope scope)
    {
        try
        {
            var realtimeNotificationService = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
            var kpiRepository = scope.ServiceProvider.GetRequiredService<IRepository<KPI>>();
            var kpis = await kpiRepository.GetAllAsync();

            var nextKpis = kpis
                .Where(k => k.IsActive)
                .Select(k => new { Kpi = k, NextRun = k.GetNextRunTime() })
                .Where(x => x.NextRun.HasValue)
                .OrderBy(x => x.NextRun)
                .Take(5)
                .Select(x => new NextKpiDto
                {
                    KpiId = x.Kpi.KpiId,
                    Indicator = x.Kpi.Indicator,
                    Owner = x.Kpi.Owner,
                    ScheduledTime = x.NextRun!.Value.ToString("O"),
                    MinutesUntilDue = (int)(x.NextRun.Value - DateTime.UtcNow).TotalMinutes
                })
                .ToList();

            var scheduleUpdate = new NextKpiScheduleUpdateDto
            {
                NextKpis = nextKpis
            };

            await realtimeNotificationService.SendNextKpiScheduleUpdateAsync(scheduleUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending next KPI schedule update");
        }
    }

    private async Task SendCountdownUpdateAsync()
    {
        using var scope = _serviceProvider.CreateScope();

        try
        {
            var realtimeNotificationService = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
            var kpiRepository = scope.ServiceProvider.GetRequiredService<IRepository<KPI>>();
            var kpis = await kpiRepository.GetAllAsync();

            var nextKpiData = kpis
                .Where(k => k.IsActive)
                .Select(k => new { Kpi = k, NextRun = k.GetNextRunTime() })
                .Where(x => x.NextRun.HasValue)
                .OrderBy(x => x.NextRun)
                .FirstOrDefault();

            if (nextKpiData != null)
            {
                var secondsUntilDue = (int)(nextKpiData.NextRun!.Value - DateTime.UtcNow).TotalSeconds;

                if (secondsUntilDue > 0)
                {
                    var countdownUpdate = new CountdownUpdateDto
                    {
                        NextKpiId = nextKpiData.Kpi.KpiId,
                        Indicator = nextKpiData.Kpi.Indicator,
                        Owner = nextKpiData.Kpi.Owner,
                        SecondsUntilDue = secondsUntilDue,
                        ScheduledTime = nextKpiData.NextRun.Value.ToString("O")
                    };

                    await realtimeNotificationService.SendCountdownUpdateAsync(countdownUpdate);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending countdown update");
        }
    }
}
