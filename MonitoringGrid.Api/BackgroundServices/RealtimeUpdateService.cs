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
                _logger.LogInformation("Countdown updates cancelled");
                break;
            }
            catch (ObjectDisposedException)
            {
                _logger.LogInformation("Service provider disposed, stopping countdown updates");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending countdown update");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Wait before retrying
                }
                catch (OperationCanceledException)
                {
                    break;
                }
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
                _logger.LogInformation("Periodic updates cancelled");
                break;
            }
            catch (ObjectDisposedException)
            {
                _logger.LogInformation("Service provider disposed, stopping periodic updates");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending periodic updates");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait before retrying
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task SendPeriodicUpdatesAsync()
    {
        try
        {
            // Check if service provider is disposed before creating scope
            if (_serviceProvider == null)
            {
                _logger.LogWarning("Service provider is null, skipping periodic updates");
                return;
            }

            using var scope = _serviceProvider.CreateScope();

            // Send worker status update
            await SendWorkerStatusUpdateAsync(scope);

            // Send running KPIs update
            await SendRunningKpisUpdateAsync(scope);

            // Send next KPI schedule update
            await SendNextKpiScheduleUpdateAsync(scope);
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning(ex, "Service provider disposed, stopping periodic updates");
            return; // Gracefully exit when service provider is disposed
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
            // Check if scope is disposed
            if (scope?.ServiceProvider == null)
            {
                _logger.LogWarning("Service scope is null or disposed, skipping worker status update");
                return;
            }

            var realtimeNotificationService = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Check if worker services are integrated or manual
            var isIntegrated = configuration.GetValue<bool>("Monitoring:EnableWorkerServices", false);
            var currentProcess = Process.GetCurrentProcess();

            WorkerStatusUpdateDto workerStatus;

            if (isIntegrated)
            {
                // Integrated mode - worker services are running within the API process
                workerStatus = new WorkerStatusUpdateDto
                {
                    IsRunning = true,
                    Mode = "Integrated",
                    ProcessId = currentProcess.Id,
                    Services = new List<WorkerServiceDto>
                    {
                        new() { Name = "KpiMonitoring", Status = "Running", LastActivity = DateTime.UtcNow },
                        new() { Name = "HealthCheck", Status = "Running", LastActivity = DateTime.UtcNow },
                        new() { Name = "RealtimeUpdates", Status = "Running", LastActivity = DateTime.UtcNow }
                    },
                    LastHeartbeat = DateTime.UtcNow.ToString("O"),
                    Uptime = CalculateUptime(currentProcess.StartTime)
                };
            }
            else
            {
                // Manual mode - check for external worker processes
                var externalWorkerProcess = FindExternalWorkerProcess();

                if (externalWorkerProcess != null)
                {
                    workerStatus = new WorkerStatusUpdateDto
                    {
                        IsRunning = true,
                        Mode = "Manual",
                        ProcessId = externalWorkerProcess.Id,
                        Services = new List<WorkerServiceDto>
                        {
                            new() { Name = "KpiMonitoringWorker", Status = "Running", LastActivity = DateTime.UtcNow },
                            new() { Name = "ScheduledTaskWorker", Status = "Running", LastActivity = DateTime.UtcNow },
                            new() { Name = "HealthCheckWorker", Status = "Running", LastActivity = DateTime.UtcNow },
                            new() { Name = "AlertProcessingWorker", Status = "Running", LastActivity = DateTime.UtcNow }
                        },
                        LastHeartbeat = DateTime.UtcNow.ToString("O"),
                        Uptime = CalculateUptime(externalWorkerProcess.StartTime)
                    };
                }
                else
                {
                    workerStatus = new WorkerStatusUpdateDto
                    {
                        IsRunning = false,
                        Mode = "Manual",
                        ProcessId = null,
                        Services = new List<WorkerServiceDto>(),
                        LastHeartbeat = DateTime.UtcNow.ToString("O"),
                        Uptime = "00:00:00"
                    };
                }
            }

            await realtimeNotificationService.SendWorkerStatusUpdateAsync(workerStatus);

            _logger.LogDebug("Sent worker status update: IsRunning={IsRunning}, Mode={Mode}, ProcessId={ProcessId}, Uptime={Uptime}",
                workerStatus.IsRunning, workerStatus.Mode, workerStatus.ProcessId, workerStatus.Uptime);
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning(ex, "Service provider disposed during worker status update");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending worker status update");
        }
    }

    private Process? FindExternalWorkerProcess()
    {
        try
        {
            // Find running MonitoringGrid.Worker processes
            var workerProcesses = Process.GetProcesses()
                .Where(p =>
                {
                    try
                    {
                        // Check for direct MonitoringGrid.Worker process name
                        if (p.ProcessName.Contains("MonitoringGrid.Worker", StringComparison.OrdinalIgnoreCase))
                            return true;

                        // Check for dotnet processes running MonitoringGrid.Worker
                        if (p.ProcessName.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                // Check command line arguments for MonitoringGrid.Worker
                                var commandLine = GetProcessCommandLine(p.Id);
                                return commandLine?.Contains("MonitoringGrid.Worker", StringComparison.OrdinalIgnoreCase) == true;
                            }
                            catch
                            {
                                // Fallback to checking main module
                                return p.MainModule?.FileName?.Contains("MonitoringGrid.Worker", StringComparison.OrdinalIgnoreCase) == true;
                            }
                        }

                        return false;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .Where(p => p.Id != Environment.ProcessId) // Exclude current API process
                .OrderByDescending(p => p.StartTime) // Get the most recently started process
                .ToList();

            // Return the first running worker process
            return workerProcesses.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error finding external worker process");
            return null;
        }
    }

    private string? GetProcessCommandLine(int processId)
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
            using var objects = searcher.Get();
            return objects.Cast<System.Management.ManagementObject>()
                .FirstOrDefault()?["CommandLine"]?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private async Task SendRunningKpisUpdateAsync(IServiceScope scope)
    {
        try
        {
            // Check if scope is disposed
            if (scope?.ServiceProvider == null)
            {
                _logger.LogWarning("Service scope is null or disposed, skipping running KPIs update");
                return;
            }

            var realtimeNotificationService = scope.ServiceProvider.GetRequiredService<IRealtimeNotificationService>();
            var kpiRepository = scope.ServiceProvider.GetRequiredService<IRepository<KPI>>();
            var kpis = await kpiRepository.GetAllAsync();

            // Get only actually running KPIs from the database
            var runningKpis = kpis
                .Where(k => k.IsActive && k.IsCurrentlyRunning)
                .Select(k => new RunningKpiDto
                {
                    KpiId = k.KpiId,
                    Indicator = k.Indicator,
                    Owner = k.Owner,
                    StartTime = k.ExecutionStartTime?.ToString("O") ?? DateTime.UtcNow.ToString("O"),
                    Progress = CalculateActualProgress(k), // Use actual progress calculation
                    EstimatedCompletion = CalculateEstimatedCompletion(k)?.ToString("O")
                })
                .ToList();

            var runningKpisUpdate = new RunningKpisUpdateDto
            {
                RunningKpis = runningKpis
            };

            await realtimeNotificationService.SendRunningKpisUpdateAsync(runningKpisUpdate);
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning(ex, "Service provider disposed during running KPIs update");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending running KPIs update");
        }
    }

    private int CalculateActualProgress(KPI kpi)
    {
        // Calculate actual progress based on execution start time
        if (!kpi.ExecutionStartTime.HasValue)
            return 0;

        var elapsed = DateTime.UtcNow - kpi.ExecutionStartTime.Value;
        var estimatedDuration = TimeSpan.FromMinutes(2); // Assume 2 minutes average execution time

        var progress = (int)Math.Min(95, (elapsed.TotalMilliseconds / estimatedDuration.TotalMilliseconds) * 100);
        return Math.Max(0, progress);
    }

    private DateTime? CalculateEstimatedCompletion(KPI kpi)
    {
        // Calculate estimated completion based on execution start time
        if (!kpi.ExecutionStartTime.HasValue)
            return null;

        var estimatedDuration = TimeSpan.FromMinutes(2); // Assume 2 minutes average execution time
        return kpi.ExecutionStartTime.Value.Add(estimatedDuration);
    }

    private async Task SendNextKpiScheduleUpdateAsync(IServiceScope scope)
    {
        try
        {
            // Check if scope is disposed
            if (scope?.ServiceProvider == null)
            {
                _logger.LogWarning("Service scope is null or disposed, skipping next KPI schedule update");
                return;
            }

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
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning(ex, "Service provider disposed during next KPI schedule update");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending next KPI schedule update");
        }
    }

    private async Task SendCountdownUpdateAsync()
    {
        try
        {
            // Check if service provider is disposed before creating scope
            if (_serviceProvider == null)
            {
                _logger.LogWarning("Service provider is null, skipping countdown update");
                return;
            }

            using var scope = _serviceProvider.CreateScope();

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
        catch (ObjectDisposedException ex)
        {
            _logger.LogWarning(ex, "Service provider disposed, stopping countdown updates");
            return; // Gracefully exit when service provider is disposed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending countdown update");
        }
    }

    private string CalculateUptime(DateTime startTime)
    {
        try
        {
            // Use local time for both start and current time to avoid timezone issues
            var currentTime = DateTime.Now;
            var uptime = currentTime - startTime;

            // Ensure uptime is not negative
            if (uptime.TotalSeconds < 0)
                uptime = TimeSpan.Zero;

            return uptime.ToString(@"hh\:mm\:ss");
        }
        catch
        {
            return "00:00:00";
        }
    }
}
