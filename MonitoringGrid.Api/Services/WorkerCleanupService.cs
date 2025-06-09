using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Service responsible for cleaning up worker processes during application shutdown
/// </summary>
public class WorkerCleanupService : IHostedService
{
    private readonly ILogger<WorkerCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly List<Process> _trackedProcesses = new();
    private readonly object _processLock = new object();

    public WorkerCleanupService(
        ILogger<WorkerCleanupService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker Cleanup Service started - monitoring for worker processes");
        
        // Register for application shutdown
        var applicationLifetime = _serviceProvider.GetService<IHostApplicationLifetime>();
        if (applicationLifetime != null)
        {
            applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);
            applicationLifetime.ApplicationStopped.Register(OnApplicationStopped);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker Cleanup Service stopping...");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Register a worker process for tracking and cleanup
    /// </summary>
    public void RegisterWorkerProcess(Process process)
    {
        if (process == null || process.HasExited)
            return;

        lock (_processLock)
        {
            _trackedProcesses.Add(process);
            _logger.LogInformation("Registered worker process {ProcessId} for cleanup tracking", process.Id);
        }
    }

    /// <summary>
    /// Unregister a worker process from tracking
    /// </summary>
    public void UnregisterWorkerProcess(Process process)
    {
        if (process == null)
            return;

        lock (_processLock)
        {
            _trackedProcesses.Remove(process);
            _logger.LogInformation("Unregistered worker process {ProcessId} from cleanup tracking", process.Id);
        }
    }

    /// <summary>
    /// Called when application is stopping
    /// </summary>
    private void OnApplicationStopping()
    {
        _logger.LogWarning("Application is stopping - initiating worker cleanup...");
        
        try
        {
            CleanupAllWorkers();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during application stopping worker cleanup");
        }
    }

    /// <summary>
    /// Called when application has stopped
    /// </summary>
    private void OnApplicationStopped()
    {
        _logger.LogWarning("Application has stopped - performing final worker cleanup...");
        
        try
        {
            // Final cleanup attempt
            CleanupAllWorkers();
            
            // Also cleanup any remaining MonitoringGrid processes
            CleanupMonitoringGridProcesses();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during application stopped worker cleanup");
        }
    }

    /// <summary>
    /// Cleanup all tracked worker processes
    /// </summary>
    private void CleanupAllWorkers()
    {
        lock (_processLock)
        {
            _logger.LogInformation("Cleaning up {Count} tracked worker processes...", _trackedProcesses.Count);

            foreach (var process in _trackedProcesses.ToList())
            {
                try
                {
                    if (!process.HasExited)
                    {
                        _logger.LogInformation("Terminating worker process {ProcessId}...", process.Id);
                        
                        // Try graceful shutdown first
                        process.CloseMainWindow();
                        
                        // Wait a short time for graceful shutdown
                        if (!process.WaitForExit(5000))
                        {
                            // Force kill if graceful shutdown failed
                            _logger.LogWarning("Force killing worker process {ProcessId}", process.Id);
                            process.Kill();
                            process.WaitForExit(2000);
                        }
                        
                        _logger.LogInformation("Successfully terminated worker process {ProcessId}", process.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error terminating worker process {ProcessId}", process.Id);
                }
                finally
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error disposing worker process {ProcessId}", process.Id);
                    }
                }
            }

            _trackedProcesses.Clear();
        }
    }

    /// <summary>
    /// Cleanup any remaining MonitoringGrid processes that might be running
    /// </summary>
    private void CleanupMonitoringGridProcesses()
    {
        try
        {
            _logger.LogInformation("Searching for remaining MonitoringGrid processes...");

            // Find all MonitoringGrid related processes
            var monitoringProcesses = Process.GetProcesses()
                .Where(p => 
                {
                    try
                    {
                        return p.ProcessName.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase) ||
                               (p.MainModule?.FileName?.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase) == true);
                    }
                    catch
                    {
                        return false; // Access denied or process already exited
                    }
                })
                .Where(p => p.Id != Environment.ProcessId) // Don't kill ourselves
                .ToList();

            if (monitoringProcesses.Any())
            {
                _logger.LogWarning("Found {Count} MonitoringGrid processes to cleanup", monitoringProcesses.Count);

                foreach (var process in monitoringProcesses)
                {
                    try
                    {
                        _logger.LogInformation("Terminating MonitoringGrid process {ProcessId} ({ProcessName})", 
                            process.Id, process.ProcessName);
                        
                        process.Kill();
                        process.WaitForExit(2000);
                        
                        _logger.LogInformation("Successfully terminated MonitoringGrid process {ProcessId}", process.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error terminating MonitoringGrid process {ProcessId}", process.Id);
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error disposing MonitoringGrid process {ProcessId}", process.Id);
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("No additional MonitoringGrid processes found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MonitoringGrid process cleanup");
        }
    }

    /// <summary>
    /// Get current status of tracked processes
    /// </summary>
    public WorkerCleanupStatus GetStatus()
    {
        lock (_processLock)
        {
            var activeProcesses = _trackedProcesses.Where(p => !p.HasExited).ToList();
            var exitedProcesses = _trackedProcesses.Where(p => p.HasExited).ToList();

            return new WorkerCleanupStatus
            {
                TotalTrackedProcesses = _trackedProcesses.Count,
                ActiveProcesses = activeProcesses.Count,
                ExitedProcesses = exitedProcesses.Count,
                ProcessDetails = activeProcesses.Select(p => new ProcessInfo
                {
                    ProcessId = p.Id,
                    ProcessName = p.ProcessName,
                    StartTime = p.StartTime,
                    IsResponding = p.Responding
                }).ToList()
            };
        }
    }
}

/// <summary>
/// Status information for worker cleanup service
/// </summary>
public class WorkerCleanupStatus
{
    public int TotalTrackedProcesses { get; set; }
    public int ActiveProcesses { get; set; }
    public int ExitedProcesses { get; set; }
    public List<ProcessInfo> ProcessDetails { get; set; } = new();
}

/// <summary>
/// Information about a tracked process
/// </summary>
public class ProcessInfo
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public bool IsResponding { get; set; }
}
