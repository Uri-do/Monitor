using System.Diagnostics;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Unified lifecycle management service interface consolidating graceful shutdown and worker cleanup
/// Replaces: IGracefulShutdownService, IWorkerCleanupService
/// </summary>
public interface ILifecycleManagementService
{
    #region Application Lifecycle Domain
    
    /// <summary>
    /// Starts the lifecycle management service
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the lifecycle management service
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Initiates graceful shutdown of the application
    /// </summary>
    Task InitiateGracefulShutdownAsync(string reason = "Manual shutdown", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs emergency shutdown procedures
    /// </summary>
    Task PerformEmergencyShutdownAsync(string reason = "Emergency shutdown", CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current application lifecycle status
    /// </summary>
    ApplicationLifecycleStatus GetLifecycleStatus();
    
    /// <summary>
    /// Registers a shutdown callback
    /// </summary>
    void RegisterShutdownCallback(Func<CancellationToken, Task> callback, int priority = 0);
    
    /// <summary>
    /// Unregisters a shutdown callback
    /// </summary>
    void UnregisterShutdownCallback(Func<CancellationToken, Task> callback);

    #endregion

    #region Worker Process Management Domain
    
    /// <summary>
    /// Starts tracking a worker process
    /// </summary>
    void TrackWorkerProcess(Process process, string description = "");
    
    /// <summary>
    /// Stops tracking a worker process
    /// </summary>
    void UntrackWorkerProcess(Process process);
    
    /// <summary>
    /// Gets all tracked worker processes
    /// </summary>
    IReadOnlyList<TrackedProcess> GetTrackedProcesses();
    
    /// <summary>
    /// Terminates all tracked worker processes gracefully
    /// </summary>
    Task TerminateAllWorkersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Terminates a specific worker process
    /// </summary>
    Task TerminateWorkerAsync(int processId, bool forceKill = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a worker process is still running
    /// </summary>
    bool IsWorkerRunning(int processId);
    
    /// <summary>
    /// Gets worker process status
    /// </summary>
    WorkerProcessStatus GetWorkerStatus(int processId);

    #endregion

    #region Process Cleanup Domain
    
    /// <summary>
    /// Performs comprehensive process cleanup
    /// </summary>
    Task PerformProcessCleanupAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleans up orphaned MonitoringGrid processes
    /// </summary>
    Task CleanupOrphanedProcessesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleans up temporary files and resources
    /// </summary>
    Task CleanupTemporaryResourcesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleans up database connections and resources
    /// </summary>
    Task CleanupDatabaseResourcesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs memory cleanup and garbage collection
    /// </summary>
    Task PerformMemoryCleanupAsync();

    #endregion

    #region Health Monitoring Domain
    
    /// <summary>
    /// Gets the health status of all managed processes
    /// </summary>
    Task<ProcessHealthStatus> GetProcessHealthStatusAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Monitors process health and performs automatic recovery
    /// </summary>
    Task MonitorProcessHealthAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Restarts unhealthy processes
    /// </summary>
    Task RestartUnhealthyProcessesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets system resource usage
    /// </summary>
    SystemResourceUsage GetSystemResourceUsage();

    #endregion

    #region Configuration and Events Domain
    
    /// <summary>
    /// Configures shutdown timeout settings
    /// </summary>
    void ConfigureShutdownTimeout(TimeSpan gracefulTimeout, TimeSpan forceTimeout);
    
    /// <summary>
    /// Configures process monitoring settings
    /// </summary>
    void ConfigureProcessMonitoring(TimeSpan monitoringInterval, bool enableAutoRestart);
    
    /// <summary>
    /// Event raised when graceful shutdown is initiated
    /// </summary>
    event EventHandler<ShutdownEventArgs>? ShutdownInitiated;
    
    /// <summary>
    /// Event raised when a worker process terminates
    /// </summary>
    event EventHandler<WorkerTerminatedEventArgs>? WorkerTerminated;

    #endregion
}

/// <summary>
/// Application lifecycle status
/// </summary>
public enum ApplicationLifecycleStatus
{
    Starting,
    Running,
    ShuttingDown,
    Stopped,
    Error
}

/// <summary>
/// Worker process status
/// </summary>
public enum WorkerProcessStatus
{
    Running,
    Stopping,
    Stopped,
    Error,
    NotFound
}

/// <summary>
/// Tracked process information
/// </summary>
public class TrackedProcess
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public WorkerProcessStatus Status { get; set; }
    public Process? Process { get; set; }
}

/// <summary>
/// Process health status
/// </summary>
public class ProcessHealthStatus
{
    public bool IsHealthy { get; set; }
    public int TotalProcesses { get; set; }
    public int HealthyProcesses { get; set; }
    public int UnhealthyProcesses { get; set; }
    public List<string> Issues { get; set; } = new();
    public DateTime CheckTime { get; set; }
}

/// <summary>
/// System resource usage information
/// </summary>
public class SystemResourceUsage
{
    public double CpuUsagePercent { get; set; }
    public long MemoryUsageBytes { get; set; }
    public long AvailableMemoryBytes { get; set; }
    public double MemoryUsagePercent { get; set; }
    public int ProcessCount { get; set; }
    public int ThreadCount { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Shutdown event arguments
/// </summary>
public class ShutdownEventArgs : EventArgs
{
    public string Reason { get; set; } = string.Empty;
    public bool IsEmergency { get; set; }
    public DateTime InitiatedAt { get; set; }
}

/// <summary>
/// Worker terminated event arguments
/// </summary>
public class WorkerTerminatedEventArgs : EventArgs
{
    public int ProcessId { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool WasGraceful { get; set; }
    public DateTime TerminatedAt { get; set; }
}


