using System.Collections.Concurrent;
using System.Diagnostics;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Service to track and manage worker processes for proper cleanup
/// </summary>
public interface IProcessTrackingService
{
    void RegisterProcess(int processId, string processType, string description);
    void UnregisterProcess(int processId);
    Task CleanupAllTrackedProcessesAsync(CancellationToken cancellationToken = default);
    IEnumerable<TrackedProcess> GetTrackedProcesses();
}

public class TrackedProcess
{
    public int ProcessId { get; set; }
    public string ProcessType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public bool IsRunning { get; set; }
}

public class ProcessTrackingService : IProcessTrackingService, IDisposable
{
    private readonly ILogger<ProcessTrackingService> _logger;
    private readonly ConcurrentDictionary<int, TrackedProcess> _trackedProcesses = new();
    private readonly Timer _healthCheckTimer;
    private bool _disposed = false;

    public ProcessTrackingService(ILogger<ProcessTrackingService> logger)
    {
        _logger = logger;
        
        // Start a timer to periodically check if tracked processes are still running
        _healthCheckTimer = new Timer(CheckProcessHealth, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        
        _logger.LogInformation("ProcessTrackingService initialized");
    }

    public void RegisterProcess(int processId, string processType, string description)
    {
        var trackedProcess = new TrackedProcess
        {
            ProcessId = processId,
            ProcessType = processType,
            Description = description,
            RegisteredAt = DateTime.UtcNow,
            IsRunning = true
        };

        _trackedProcesses.TryAdd(processId, trackedProcess);
        _logger.LogInformation("Registered process {ProcessId} ({ProcessType}): {Description}", 
            processId, processType, description);
    }

    public void UnregisterProcess(int processId)
    {
        if (_trackedProcesses.TryRemove(processId, out var process))
        {
            _logger.LogInformation("Unregistered process {ProcessId} ({ProcessType}): {Description}", 
                processId, process.ProcessType, process.Description);
        }
    }

    public async Task CleanupAllTrackedProcessesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cleanup of {Count} tracked processes", _trackedProcesses.Count);

        var cleanupTasks = new List<Task>();

        foreach (var kvp in _trackedProcesses)
        {
            var processId = kvp.Key;
            var trackedProcess = kvp.Value;

            var cleanupTask = Task.Run(async () =>
            {
                try
                {
                    await CleanupProcessAsync(processId, trackedProcess, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up process {ProcessId}", processId);
                }
            }, cancellationToken);

            cleanupTasks.Add(cleanupTask);
        }

        try
        {
            await Task.WhenAll(cleanupTasks);
            _logger.LogInformation("Completed cleanup of all tracked processes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tracked process cleanup");
        }

        // Clear the tracking dictionary
        _trackedProcesses.Clear();
    }

    public IEnumerable<TrackedProcess> GetTrackedProcesses()
    {
        return _trackedProcesses.Values.ToList();
    }

    private async Task CleanupProcessAsync(int processId, TrackedProcess trackedProcess, CancellationToken cancellationToken)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            
            if (process.HasExited)
            {
                _logger.LogInformation("Process {ProcessId} ({ProcessType}) has already exited", 
                    processId, trackedProcess.ProcessType);
                return;
            }

            _logger.LogInformation("Terminating process {ProcessId} ({ProcessType}): {Description}", 
                processId, trackedProcess.ProcessType, trackedProcess.Description);

            // Try graceful shutdown first
            process.CloseMainWindow();
            
            // Wait for graceful shutdown
            var gracefulShutdownTask = Task.Run(() => process.WaitForExit(5000), cancellationToken);
            await gracefulShutdownTask;

            if (!process.HasExited)
            {
                // Force kill if graceful shutdown failed
                _logger.LogWarning("Force killing process {ProcessId} ({ProcessType})", 
                    processId, trackedProcess.ProcessType);
                process.Kill();
                
                var forceKillTask = Task.Run(() => process.WaitForExit(3000), cancellationToken);
                await forceKillTask;
            }

            _logger.LogInformation("Successfully terminated process {ProcessId} ({ProcessType})", 
                processId, trackedProcess.ProcessType);
        }
        catch (ArgumentException)
        {
            // Process doesn't exist anymore
            _logger.LogInformation("Process {ProcessId} ({ProcessType}) no longer exists", 
                processId, trackedProcess.ProcessType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error terminating process {ProcessId} ({ProcessType})", 
                processId, trackedProcess.ProcessType);
        }
    }

    private void CheckProcessHealth(object? state)
    {
        try
        {
            var processesToRemove = new List<int>();

            foreach (var kvp in _trackedProcesses)
            {
                var processId = kvp.Key;
                var trackedProcess = kvp.Value;

                try
                {
                    var process = Process.GetProcessById(processId);
                    trackedProcess.IsRunning = !process.HasExited;
                    
                    if (process.HasExited)
                    {
                        processesToRemove.Add(processId);
                        _logger.LogInformation("Process {ProcessId} ({ProcessType}) has exited and will be removed from tracking", 
                            processId, trackedProcess.ProcessType);
                    }
                }
                catch (ArgumentException)
                {
                    // Process doesn't exist anymore
                    processesToRemove.Add(processId);
                    trackedProcess.IsRunning = false;
                    _logger.LogInformation("Process {ProcessId} ({ProcessType}) no longer exists and will be removed from tracking", 
                        processId, trackedProcess.ProcessType);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error checking health of process {ProcessId}", processId);
                }
            }

            // Remove processes that are no longer running
            foreach (var processId in processesToRemove)
            {
                _trackedProcesses.TryRemove(processId, out _);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during process health check");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogInformation("ProcessTrackingService disposing...");
            
            _healthCheckTimer?.Dispose();
            
            // Cleanup all tracked processes synchronously
            try
            {
                CleanupAllTrackedProcessesAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ProcessTrackingService disposal cleanup");
            }
            
            _disposed = true;
            _logger.LogInformation("ProcessTrackingService disposed");
        }
    }
}
