using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Service to handle graceful shutdown and process cleanup
/// </summary>
public class GracefulShutdownService : IHostedService, IDisposable
{
    private readonly ILogger<GracefulShutdownService> _logger;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IConfiguration _configuration;
    private readonly List<int> _trackedProcessIds = new();
    private readonly object _processLock = new();
    private bool _disposed = false;

    // Windows API for handling console events
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate handler, bool add);

    private delegate bool ConsoleEventDelegate(int eventType);
    private ConsoleEventDelegate? _consoleHandler;

    public GracefulShutdownService(
        ILogger<GracefulShutdownService> logger,
        IHostApplicationLifetime applicationLifetime,
        IConfiguration configuration)
    {
        _logger = logger;
        _applicationLifetime = applicationLifetime;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Graceful Shutdown Service starting...");

        // Register for application lifetime events
        _applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);
        _applicationLifetime.ApplicationStopped.Register(OnApplicationStopped);

        // Set up console event handler for Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _consoleHandler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(_consoleHandler, true);
            _logger.LogInformation("Console event handler registered for graceful shutdown");
        }

        // Register for process exit event
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        _logger.LogInformation("Graceful Shutdown Service started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Graceful Shutdown Service stopping...");
        
        // Cleanup will be handled by the application stopping/stopped events
        return Task.CompletedTask;
    }

    /// <summary>
    /// Track a process ID for cleanup
    /// </summary>
    public void TrackProcess(int processId)
    {
        lock (_processLock)
        {
            if (!_trackedProcessIds.Contains(processId))
            {
                _trackedProcessIds.Add(processId);
                _logger.LogDebug("Tracking process {ProcessId} for cleanup", processId);
            }
        }
    }

    /// <summary>
    /// Stop tracking a process ID
    /// </summary>
    public void UntrackProcess(int processId)
    {
        lock (_processLock)
        {
            if (_trackedProcessIds.Remove(processId))
            {
                _logger.LogDebug("Stopped tracking process {ProcessId}", processId);
            }
        }
    }

    private bool ConsoleEventCallback(int eventType)
    {
        // Handle console events (Ctrl+C, Ctrl+Break, etc.)
        switch (eventType)
        {
            case 0: // CTRL_C_EVENT
            case 1: // CTRL_BREAK_EVENT
            case 2: // CTRL_CLOSE_EVENT
            case 5: // CTRL_LOGOFF_EVENT
            case 6: // CTRL_SHUTDOWN_EVENT
                _logger.LogWarning("Console event {EventType} received - initiating graceful shutdown", eventType);
                
                // Trigger application shutdown
                _applicationLifetime.StopApplication();
                
                // Give some time for graceful shutdown
                Thread.Sleep(5000);
                return true;
        }
        return false;
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        _logger.LogWarning("Process exit event received - performing emergency cleanup");
        PerformCleanup(emergency: true);
    }

    private void OnApplicationStopping()
    {
        _logger.LogWarning("Application is stopping - performing graceful cleanup");
        PerformCleanup(emergency: false);
    }

    private void OnApplicationStopped()
    {
        _logger.LogWarning("Application has stopped - performing final cleanup");
        PerformCleanup(emergency: true);
    }

    private void PerformCleanup(bool emergency)
    {
        try
        {
            var isIntegrated = _configuration.GetValue<bool>("Monitoring:EnableWorkerServices", false);
            _logger.LogInformation("Performing cleanup (Emergency: {Emergency}, Integrated: {Integrated})", 
                emergency, isIntegrated);

            // Cleanup tracked processes
            CleanupTrackedProcesses();

            // Cleanup any orphaned MonitoringGrid processes
            CleanupOrphanedMonitoringGridProcesses();

            _logger.LogInformation("Cleanup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
        }
    }

    private void CleanupTrackedProcesses()
    {
        List<int> processIds;
        lock (_processLock)
        {
            processIds = new List<int>(_trackedProcessIds);
            _trackedProcessIds.Clear();
        }

        if (!processIds.Any())
        {
            _logger.LogInformation("No tracked processes to cleanup");
            return;
        }

        _logger.LogInformation("Cleaning up {Count} tracked processes", processIds.Count);

        foreach (var processId in processIds)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                if (!process.HasExited)
                {
                    _logger.LogInformation("Terminating tracked process {ProcessId}", processId);
                    
                    // Try graceful shutdown first
                    process.CloseMainWindow();
                    if (!process.WaitForExit(3000))
                    {
                        // Force kill if graceful shutdown failed
                        _logger.LogWarning("Force killing tracked process {ProcessId}", processId);
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                    
                    _logger.LogInformation("Successfully terminated tracked process {ProcessId}", processId);
                }
                process.Dispose();
            }
            catch (ArgumentException)
            {
                // Process already exited
                _logger.LogDebug("Tracked process {ProcessId} already exited", processId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error terminating tracked process {ProcessId}", processId);
            }
        }
    }

    private void CleanupOrphanedMonitoringGridProcesses()
    {
        try
        {
            _logger.LogInformation("Searching for orphaned MonitoringGrid processes...");

            var currentProcessId = Environment.ProcessId;
            var dotnetProcesses = Process.GetProcesses()
                .Where(p => p.ProcessName.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                .Where(p => p.Id != currentProcessId)
                .ToList();

            var orphanedProcesses = new List<Process>();

            foreach (var process in dotnetProcesses)
            {
                try
                {
                    // Check if this process is running a MonitoringGrid project
                    var commandLine = GetProcessCommandLine(process.Id);
                    if (!string.IsNullOrEmpty(commandLine) &&
                        commandLine.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase))
                    {
                        orphanedProcesses.Add(process);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not check process {ProcessId}", process.Id);
                }
            }

            if (orphanedProcesses.Any())
            {
                _logger.LogWarning("Found {Count} orphaned MonitoringGrid processes", orphanedProcesses.Count);

                foreach (var process in orphanedProcesses)
                {
                    try
                    {
                        _logger.LogInformation("Terminating orphaned process {ProcessId}", process.Id);
                        
                        if (!process.HasExited)
                        {
                            process.Kill();
                            process.WaitForExit(2000);
                        }
                        
                        _logger.LogInformation("Successfully terminated orphaned process {ProcessId}", process.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error terminating orphaned process {ProcessId}", process.Id);
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error disposing orphaned process {ProcessId}", process.Id);
                        }
                    }
                }
            }
            else
            {
                _logger.LogInformation("No orphaned MonitoringGrid processes found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphaned process cleanup");
        }
    }

    private string GetProcessCommandLine(int processId)
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}");
            using var objects = searcher.Get();
            return objects.Cast<System.Management.ManagementObject>()
                .SingleOrDefault()?["CommandLine"]?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Unregister console handler
            if (_consoleHandler != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetConsoleCtrlHandler(_consoleHandler, false);
            }

            // Unregister process exit handler
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

            _disposed = true;
        }
    }
}
