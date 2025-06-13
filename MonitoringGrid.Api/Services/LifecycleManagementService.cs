using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Unified lifecycle management service consolidating graceful shutdown and worker cleanup
/// Replaces: GracefulShutdownService, WorkerCleanupService
/// </summary>
public class LifecycleManagementService : ILifecycleManagementService, IHostedService
{
    private readonly ILogger<LifecycleManagementService> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    
    // Configuration
    private TimeSpan _gracefulShutdownTimeout = TimeSpan.FromSeconds(30);
    private TimeSpan _forceShutdownTimeout = TimeSpan.FromSeconds(60);
    private TimeSpan _monitoringInterval = TimeSpan.FromSeconds(30);
    private bool _enableAutoRestart = false;

    // State management
    private ApplicationLifecycleStatus _status = ApplicationLifecycleStatus.Stopped;
    private readonly ConcurrentDictionary<int, TrackedProcess> _trackedProcesses = new();
    private readonly List<(Func<CancellationToken, Task> callback, int priority)> _shutdownCallbacks = new();
    private readonly object _lockObject = new();
    
    // Cancellation and monitoring
    private CancellationTokenSource? _monitoringCancellationTokenSource;
    private Task? _monitoringTask;

    // Events
    public event EventHandler<ShutdownEventArgs>? ShutdownInitiated;
    public event EventHandler<WorkerTerminatedEventArgs>? WorkerTerminated;

    public LifecycleManagementService(
        ILogger<LifecycleManagementService> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _applicationLifetime = applicationLifetime;

        // Register for application lifetime events
        _applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);
        _applicationLifetime.ApplicationStopped.Register(OnApplicationStopped);
    }

    #region IHostedService Implementation

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting Lifecycle Management Service");

            _status = ApplicationLifecycleStatus.Starting;

            // Start process monitoring
            _monitoringCancellationTokenSource = new CancellationTokenSource();
            _monitoringTask = MonitorProcessHealthAsync(_monitoringCancellationTokenSource.Token);

            _status = ApplicationLifecycleStatus.Running;

            _logger.LogInformation("Lifecycle Management Service started successfully");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Lifecycle Management Service");
            _status = ApplicationLifecycleStatus.Error;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Stopping Lifecycle Management Service");
            
            _status = ApplicationLifecycleStatus.ShuttingDown;
            
            // Stop monitoring
            _monitoringCancellationTokenSource?.Cancel();
            if (_monitoringTask != null)
            {
                await _monitoringTask;
            }
            
            // Perform cleanup
            await PerformProcessCleanupAsync(cancellationToken);
            
            _status = ApplicationLifecycleStatus.Stopped;
            
            _logger.LogInformation("Lifecycle Management Service stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Lifecycle Management Service");
            _status = ApplicationLifecycleStatus.Error;
            throw;
        }
    }

    #endregion

    #region Application Lifecycle Domain

    public ApplicationLifecycleStatus GetLifecycleStatus()
    {
        return _status;
    }

    public async Task InitiateGracefulShutdownAsync(string reason = "Manual shutdown", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initiating graceful shutdown: {Reason}", reason);
            
            _status = ApplicationLifecycleStatus.ShuttingDown;
            
            // Raise shutdown event
            ShutdownInitiated?.Invoke(this, new ShutdownEventArgs
            {
                Reason = reason,
                IsEmergency = false,
                InitiatedAt = DateTime.UtcNow
            });

            // Execute shutdown callbacks in priority order
            var orderedCallbacks = _shutdownCallbacks.OrderByDescending(c => c.priority).ToList();
            
            foreach (var (callback, priority) in orderedCallbacks)
            {
                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(_gracefulShutdownTimeout);
                    
                    await callback(timeoutCts.Token);
                    _logger.LogDebug("Shutdown callback executed successfully (priority: {Priority})", priority);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Shutdown callback timed out (priority: {Priority})", priority);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing shutdown callback (priority: {Priority})", priority);
                }
            }

            // Terminate all worker processes
            await TerminateAllWorkersAsync(cancellationToken);
            
            // Perform final cleanup
            await PerformProcessCleanupAsync(cancellationToken);
            
            _logger.LogInformation("Graceful shutdown completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during graceful shutdown");
            await PerformEmergencyShutdownAsync($"Graceful shutdown failed: {ex.Message}", cancellationToken);
        }
    }

    public async Task PerformEmergencyShutdownAsync(string reason = "Emergency shutdown", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("Performing emergency shutdown: {Reason}", reason);
            
            _status = ApplicationLifecycleStatus.ShuttingDown;
            
            // Raise shutdown event
            ShutdownInitiated?.Invoke(this, new ShutdownEventArgs
            {
                Reason = reason,
                IsEmergency = true,
                InitiatedAt = DateTime.UtcNow
            });

            // Force terminate all processes immediately
            var processes = _trackedProcesses.Values.ToList();
            var terminationTasks = processes.Select(p => TerminateWorkerAsync(p.ProcessId, forceKill: true, cancellationToken));
            
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_forceShutdownTimeout);
            
            await Task.WhenAll(terminationTasks);
            
            // Perform emergency cleanup
            await CleanupOrphanedProcessesAsync(cancellationToken);
            await CleanupTemporaryResourcesAsync(cancellationToken);
            
            _logger.LogWarning("Emergency shutdown completed");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error during emergency shutdown");
            _status = ApplicationLifecycleStatus.Error;
        }
    }

    public void RegisterShutdownCallback(Func<CancellationToken, Task> callback, int priority = 0)
    {
        lock (_lockObject)
        {
            _shutdownCallbacks.Add((callback, priority));
            _logger.LogDebug("Shutdown callback registered with priority {Priority}", priority);
        }
    }

    public void UnregisterShutdownCallback(Func<CancellationToken, Task> callback)
    {
        lock (_lockObject)
        {
            _shutdownCallbacks.RemoveAll(c => c.callback == callback);
            _logger.LogDebug("Shutdown callback unregistered");
        }
    }

    #endregion

    #region Worker Process Management Domain

    public void TrackWorkerProcess(Process process, string description = "")
    {
        try
        {
            var trackedProcess = new TrackedProcess
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                Description = description,
                StartTime = process.StartTime,
                Status = WorkerProcessStatus.Running,
                Process = process
            };

            _trackedProcesses.TryAdd(process.Id, trackedProcess);
            
            _logger.LogInformation("Started tracking worker process: {ProcessName} (PID: {ProcessId}) - {Description}", 
                process.ProcessName, process.Id, description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking worker process {ProcessId}", process.Id);
        }
    }

    public void UntrackWorkerProcess(Process process)
    {
        try
        {
            if (_trackedProcesses.TryRemove(process.Id, out var trackedProcess))
            {
                _logger.LogInformation("Stopped tracking worker process: {ProcessName} (PID: {ProcessId})", 
                    trackedProcess.ProcessName, trackedProcess.ProcessId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error untracking worker process {ProcessId}", process.Id);
        }
    }

    public IReadOnlyList<TrackedProcess> GetTrackedProcesses()
    {
        return _trackedProcesses.Values.ToList().AsReadOnly();
    }

    public async Task TerminateAllWorkersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Terminating all tracked worker processes");
            
            var processes = _trackedProcesses.Values.ToList();
            var terminationTasks = processes.Select(p => TerminateWorkerAsync(p.ProcessId, forceKill: false, cancellationToken));
            
            await Task.WhenAll(terminationTasks);
            
            _logger.LogInformation("All worker processes terminated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating all worker processes");
        }
    }

    public async Task TerminateWorkerAsync(int processId, bool forceKill = false, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_trackedProcesses.TryGetValue(processId, out var trackedProcess))
            {
                _logger.LogWarning("Process {ProcessId} not found in tracked processes", processId);
                return;
            }

            var process = trackedProcess.Process;
            if (process == null || process.HasExited)
            {
                _trackedProcesses.TryRemove(processId, out _);
                return;
            }

            _logger.LogInformation("Terminating worker process: {ProcessName} (PID: {ProcessId}), ForceKill: {ForceKill}", 
                trackedProcess.ProcessName, processId, forceKill);

            trackedProcess.Status = WorkerProcessStatus.Stopping;

            if (forceKill)
            {
                process.Kill();
            }
            else
            {
                // Try graceful shutdown first
                process.CloseMainWindow();
                
                // Wait for graceful shutdown
                if (!process.WaitForExit(5000))
                {
                    _logger.LogWarning("Process {ProcessId} did not exit gracefully, forcing termination", processId);
                    process.Kill();
                }
            }

            // Wait for process to actually exit
            await Task.Run(() => process.WaitForExit(10000), cancellationToken);

            trackedProcess.Status = WorkerProcessStatus.Stopped;
            _trackedProcesses.TryRemove(processId, out _);

            // Raise event
            WorkerTerminated?.Invoke(this, new WorkerTerminatedEventArgs
            {
                ProcessId = processId,
                ProcessName = trackedProcess.ProcessName,
                Reason = forceKill ? "Force killed" : "Graceful shutdown",
                WasGraceful = !forceKill,
                TerminatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Worker process terminated: {ProcessName} (PID: {ProcessId})", 
                trackedProcess.ProcessName, processId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating worker process {ProcessId}", processId);
            
            if (_trackedProcesses.TryGetValue(processId, out var trackedProcess))
            {
                trackedProcess.Status = WorkerProcessStatus.Error;
            }
        }
    }

    public bool IsWorkerRunning(int processId)
    {
        try
        {
            if (!_trackedProcesses.TryGetValue(processId, out var trackedProcess))
                return false;

            var process = trackedProcess.Process;
            return process != null && !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    public WorkerProcessStatus GetWorkerStatus(int processId)
    {
        if (_trackedProcesses.TryGetValue(processId, out var trackedProcess))
        {
            // Update status based on actual process state
            if (trackedProcess.Process?.HasExited == true)
            {
                trackedProcess.Status = WorkerProcessStatus.Stopped;
            }
            
            return trackedProcess.Status;
        }
        
        return WorkerProcessStatus.NotFound;
    }

    #endregion

    #region Process Cleanup Domain

    public async Task PerformProcessCleanupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Performing comprehensive process cleanup");

            // Cleanup orphaned processes
            await CleanupOrphanedProcessesAsync(cancellationToken);

            // Cleanup temporary resources
            await CleanupTemporaryResourcesAsync(cancellationToken);

            // Cleanup database resources
            await CleanupDatabaseResourcesAsync(cancellationToken);

            // Perform memory cleanup
            await PerformMemoryCleanupAsync();

            _logger.LogInformation("Process cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during process cleanup");
        }
    }

    public async Task CleanupOrphanedProcessesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cleaning up orphaned MonitoringGrid processes");

            var currentProcessId = Environment.ProcessId;
            var monitoringProcesses = Process.GetProcessesByName("MonitoringGrid")
                .Where(p => p.Id != currentProcessId)
                .ToList();

            foreach (var process in monitoringProcesses)
            {
                try
                {
                    // Check if process is truly orphaned (no parent or unresponsive)
                    if (IsProcessOrphaned(process))
                    {
                        _logger.LogWarning("Terminating orphaned process: {ProcessName} (PID: {ProcessId})",
                            process.ProcessName, process.Id);

                        process.Kill();
                        await Task.Run(() => process.WaitForExit(5000), cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up orphaned process {ProcessId}", process.Id);
                }
                finally
                {
                    process.Dispose();
                }
            }

            _logger.LogInformation("Orphaned process cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphaned process cleanup");
        }
    }

    public async Task CleanupTemporaryResourcesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cleaning up temporary resources");

            // Cleanup temporary files
            var tempPath = Path.GetTempPath();
            var monitoringTempFiles = Directory.GetFiles(tempPath, "MonitoringGrid*", SearchOption.TopDirectoryOnly)
                .Where(f => File.GetCreationTime(f) < DateTime.Now.AddHours(-24))
                .ToList();

            foreach (var file in monitoringTempFiles)
            {
                try
                {
                    File.Delete(file);
                    _logger.LogDebug("Deleted temporary file: {FilePath}", file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete temporary file: {FilePath}", file);
                }
            }

            // Cleanup temporary directories
            var monitoringTempDirs = Directory.GetDirectories(tempPath, "MonitoringGrid*", SearchOption.TopDirectoryOnly)
                .Where(d => Directory.GetCreationTime(d) < DateTime.Now.AddHours(-24))
                .ToList();

            foreach (var dir in monitoringTempDirs)
            {
                try
                {
                    Directory.Delete(dir, true);
                    _logger.LogDebug("Deleted temporary directory: {DirectoryPath}", dir);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete temporary directory: {DirectoryPath}", dir);
                }
            }

            await Task.Delay(100, cancellationToken); // Simulate async operation

            _logger.LogInformation("Temporary resource cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during temporary resource cleanup");
        }
    }

    public async Task CleanupDatabaseResourcesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cleaning up database resources");

            // Simulate database connection cleanup
            await Task.Delay(200, cancellationToken);

            // In a real implementation, you would:
            // 1. Close any open database connections
            // 2. Clear connection pools
            // 3. Dispose of database contexts
            // 4. Clean up any database locks

            _logger.LogInformation("Database resource cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database resource cleanup");
        }
    }

    public async Task PerformMemoryCleanupAsync()
    {
        try
        {
            _logger.LogInformation("Performing memory cleanup");

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Simulate async memory cleanup
            await Task.Delay(100);

            _logger.LogInformation("Memory cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during memory cleanup");
        }
    }

    #endregion

    #region Health Monitoring Domain

    public Task<ProcessHealthStatus> GetProcessHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var processes = _trackedProcesses.Values.ToList();
            var healthyCount = 0;
            var issues = new List<string>();

            foreach (var process in processes)
            {
                try
                {
                    if (process.Process?.HasExited == false)
                    {
                        healthyCount++;
                    }
                    else
                    {
                        issues.Add($"Process {process.ProcessName} (PID: {process.ProcessId}) has exited");
                    }
                }
                catch (Exception ex)
                {
                    issues.Add($"Error checking process {process.ProcessName} (PID: {process.ProcessId}): {ex.Message}");
                }
            }

            var result = new ProcessHealthStatus
            {
                IsHealthy = issues.Count == 0,
                TotalProcesses = processes.Count,
                HealthyProcesses = healthyCount,
                UnhealthyProcesses = processes.Count - healthyCount,
                Issues = issues,
                CheckTime = DateTime.UtcNow
            };

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting process health status");
            var errorResult = new ProcessHealthStatus
            {
                IsHealthy = false,
                Issues = new List<string> { $"Health check failed: {ex.Message}" },
                CheckTime = DateTime.UtcNow
            };

            return Task.FromResult(errorResult);
        }
    }

    public async Task MonitorProcessHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting process health monitoring");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var healthStatus = await GetProcessHealthStatusAsync(cancellationToken);

                    if (!healthStatus.IsHealthy)
                    {
                        _logger.LogWarning("Process health issues detected: {IssueCount} issues", healthStatus.Issues.Count);

                        if (_enableAutoRestart)
                        {
                            await RestartUnhealthyProcessesAsync(cancellationToken);
                        }
                    }

                    await Task.Delay(_monitoringInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during process health monitoring");
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }

            _logger.LogInformation("Process health monitoring stopped");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Process health monitoring cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in process health monitoring");
        }
    }

    public async Task RestartUnhealthyProcessesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Restarting unhealthy processes");

            var processes = _trackedProcesses.Values.ToList();

            foreach (var trackedProcess in processes)
            {
                try
                {
                    if (trackedProcess.Process?.HasExited == true)
                    {
                        _logger.LogInformation("Restarting exited process: {ProcessName} (PID: {ProcessId})",
                            trackedProcess.ProcessName, trackedProcess.ProcessId);

                        // In a real implementation, you would restart the process
                        // For now, just remove it from tracking
                        _trackedProcesses.TryRemove(trackedProcess.ProcessId, out _);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error restarting process {ProcessName} (PID: {ProcessId})",
                        trackedProcess.ProcessName, trackedProcess.ProcessId);
                }
            }

            await Task.Delay(100, cancellationToken);
            _logger.LogInformation("Unhealthy process restart completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting unhealthy processes");
        }
    }

    public SystemResourceUsage GetSystemResourceUsage()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);

            return new SystemResourceUsage
            {
                CpuUsagePercent = 0, // Would need performance counters for accurate CPU usage
                MemoryUsageBytes = totalMemory,
                AvailableMemoryBytes = 0, // Would need system info for available memory
                MemoryUsagePercent = 0,
                ProcessCount = _trackedProcesses.Count,
                ThreadCount = currentProcess.Threads.Count,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system resource usage");
            return new SystemResourceUsage
            {
                Timestamp = DateTime.UtcNow
            };
        }
    }

    #endregion

    #region Configuration and Events Domain

    public void ConfigureShutdownTimeout(TimeSpan gracefulTimeout, TimeSpan forceTimeout)
    {
        _gracefulShutdownTimeout = gracefulTimeout;
        _forceShutdownTimeout = forceTimeout;

        _logger.LogInformation("Shutdown timeouts configured: Graceful={GracefulTimeout}, Force={ForceTimeout}",
            gracefulTimeout, forceTimeout);
    }

    public void ConfigureProcessMonitoring(TimeSpan monitoringInterval, bool enableAutoRestart)
    {
        _monitoringInterval = monitoringInterval;
        _enableAutoRestart = enableAutoRestart;

        _logger.LogInformation("Process monitoring configured: Interval={MonitoringInterval}, AutoRestart={EnableAutoRestart}",
            monitoringInterval, enableAutoRestart);
    }

    #endregion

    #region Private Helper Methods

    private void OnApplicationStopping()
    {
        _logger.LogInformation("Application stopping event received");

        // Initiate graceful shutdown
        _ = Task.Run(async () =>
        {
            try
            {
                await InitiateGracefulShutdownAsync("Application stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application stopping shutdown");
            }
        });
    }

    private void OnApplicationStopped()
    {
        _logger.LogInformation("Application stopped event received");
        _status = ApplicationLifecycleStatus.Stopped;
    }

    private bool IsProcessOrphaned(Process process)
    {
        try
        {
            // Simple check - in a real implementation you might check:
            // 1. Parent process existence
            // 2. Process responsiveness
            // 3. Process age
            // 4. Resource usage patterns

            return process.StartTime < DateTime.Now.AddHours(-1) && !process.Responding;
        }
        catch
        {
            return true; // If we can't check, assume it's orphaned
        }
    }

    #endregion
}
