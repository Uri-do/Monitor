using System.Diagnostics;

namespace MonitoringGrid.Api.Extensions;

/// <summary>
/// Extensions for handling application lifetime events and worker cleanup
/// </summary>
public static class ApplicationLifetimeExtensions
{
    /// <summary>
    /// Configure worker cleanup on application shutdown
    /// </summary>
    public static void ConfigureWorkerCleanup(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        var configuration = app.Services.GetRequiredService<IConfiguration>();

        // Check if we're running in integrated mode
        var isIntegrated = configuration.GetValue<bool>("Monitoring:EnableWorkerServices", false);

        // Debug logging to verify configuration reading
        logger.LogInformation("Configuration check - EnableWorkerServices: {EnableWorkerServices}", isIntegrated);

        // Log all monitoring configuration values for debugging
        var monitoringSection = configuration.GetSection("Monitoring");
        foreach (var child in monitoringSection.GetChildren())
        {
            logger.LogInformation("Monitoring config - {Key}: {Value}", child.Key, child.Value);
        }

        // Register shutdown event handlers
        lifetime.ApplicationStopping.Register(() =>
        {
            logger.LogWarning("Application is stopping - initiating cleanup...");

            // Always cleanup hosted services first
            CleanupHostedServices(app, logger);

            // Cleanup tracked processes
            CleanupTrackedProcesses(app, logger);

            if (isIntegrated)
            {
                logger.LogInformation("Running in integrated mode - hosted services will be stopped automatically");
                // In integrated mode, hosted services are stopped automatically by the host
                // But we can still cleanup any orphaned processes from previous runs
                CleanupOrphanedProcesses(logger);
            }
            else
            {
                logger.LogInformation("Running in external mode - cleaning up worker processes");
                CleanupAllWorkerProcesses(logger);
            }
        });

        lifetime.ApplicationStopped.Register(() =>
        {
            logger.LogWarning("Application has stopped - performing final cleanup...");

            // Always perform final cleanup to ensure no orphaned processes remain
            CleanupOrphanedProcesses(logger);
        });

        logger.LogInformation("Worker cleanup handlers registered for application shutdown (Integrated: {IsIntegrated})", isIntegrated);
    }

    /// <summary>
    /// Manually trigger worker cleanup (for testing/manual operations)
    /// </summary>
    public static void TriggerManualCleanup(ILogger logger)
    {
        logger.LogInformation("Manual worker cleanup triggered");
        CleanupAllWorkerProcesses(logger);
    }

    /// <summary>
    /// Cleanup tracked processes using the ProcessTrackingService
    /// </summary>
    private static void CleanupTrackedProcesses(WebApplication app, ILogger logger)
    {
        try
        {
            logger.LogInformation("Cleaning up tracked processes...");

            var processTrackingService = app.Services.GetService<MonitoringGrid.Api.Services.IProcessTrackingService>();
            if (processTrackingService != null)
            {
                var trackedProcesses = processTrackingService.GetTrackedProcesses().ToList();
                logger.LogInformation("Found {Count} tracked processes to cleanup", trackedProcesses.Count);

                // Use async cleanup but wait for completion
                var cleanupTask = processTrackingService.CleanupAllTrackedProcessesAsync();
                cleanupTask.Wait(TimeSpan.FromSeconds(30));

                logger.LogInformation("Tracked processes cleanup completed");
            }
            else
            {
                logger.LogWarning("ProcessTrackingService not found - skipping tracked process cleanup");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during tracked processes cleanup");
        }
    }

    /// <summary>
    /// Cleanup hosted services gracefully
    /// </summary>
    private static void CleanupHostedServices(WebApplication app, ILogger logger)
    {
        try
        {
            logger.LogInformation("Initiating graceful shutdown of hosted services...");

            // Get all hosted services from the DI container
            var hostedServices = app.Services.GetServices<IHostedService>().ToList();

            if (hostedServices.Any())
            {
                logger.LogInformation("Found {Count} hosted services to stop", hostedServices.Count);

                var stopTasks = new List<Task>();
                var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                foreach (var service in hostedServices)
                {
                    try
                    {
                        logger.LogInformation("Stopping hosted service: {ServiceType}", service.GetType().Name);

                        // Create a task to stop the service with timeout
                        var stopTask = Task.Run(async () =>
                        {
                            try
                            {
                                await service.StopAsync(cancellationTokenSource.Token);
                                logger.LogInformation("Successfully stopped hosted service: {ServiceType}", service.GetType().Name);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error stopping hosted service: {ServiceType}", service.GetType().Name);
                            }
                        }, cancellationTokenSource.Token);

                        stopTasks.Add(stopTask);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error initiating stop for hosted service: {ServiceType}", service.GetType().Name);
                    }
                }

                // Wait for all services to stop or timeout
                try
                {
                    Task.WaitAll(stopTasks.ToArray(), TimeSpan.FromSeconds(30));
                    logger.LogInformation("All hosted services stopped successfully");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Some hosted services did not stop gracefully within timeout");
                }
            }
            else
            {
                logger.LogInformation("No hosted services found to stop");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during hosted services cleanup");
        }
    }

    /// <summary>
    /// Cleanup orphaned processes from previous runs (safer than full cleanup)
    /// </summary>
    private static void CleanupOrphanedProcesses(ILogger logger)
    {
        try
        {
            logger.LogInformation("Searching for orphaned MonitoringGrid processes...");

            // Find dotnet processes that might be running MonitoringGrid projects
            var currentProcessId = Environment.ProcessId;
            var dotnetProcesses = Process.GetProcesses()
                .Where(p => p.ProcessName.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                .Where(p => p.Id != currentProcessId) // Don't kill ourselves
                .ToList();

            var orphanedProcesses = new List<Process>();

            foreach (var process in dotnetProcesses)
            {
                try
                {
                    // Check if this process is running a MonitoringGrid project
                    var commandLine = GetProcessCommandLine(process.Id);
                    if (!string.IsNullOrEmpty(commandLine) &&
                        (commandLine.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase) ||
                         commandLine.Contains("Monitor", StringComparison.OrdinalIgnoreCase)))
                    {
                        // Check if it's been running for more than 5 minutes (likely orphaned)
                        var runningTime = DateTime.Now - process.StartTime;
                        if (runningTime.TotalMinutes > 5)
                        {
                            orphanedProcesses.Add(process);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not check process {ProcessId} - likely access denied", process.Id);
                }
            }

            if (orphanedProcesses.Any())
            {
                logger.LogWarning("Found {Count} potentially orphaned MonitoringGrid processes", orphanedProcesses.Count);

                foreach (var process in orphanedProcesses)
                {
                    try
                    {
                        logger.LogInformation("Terminating orphaned process {ProcessId} (running for {RunningTime})",
                            process.Id, DateTime.Now - process.StartTime);

                        if (!process.HasExited)
                        {
                            process.Kill();
                            process.WaitForExit(2000);
                        }

                        logger.LogInformation("Successfully terminated orphaned process {ProcessId}", process.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error terminating orphaned process {ProcessId}", process.Id);
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error disposing orphaned process {ProcessId}", process.Id);
                        }
                    }
                }
            }
            else
            {
                logger.LogInformation("No orphaned MonitoringGrid processes found");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during orphaned process cleanup");
        }
    }

    /// <summary>
    /// Get command line for a process (Windows only)
    /// </summary>
    private static string GetProcessCommandLine(int processId)
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

    /// <summary>
    /// Cleanup all MonitoringGrid worker processes
    /// </summary>
    private static void CleanupAllWorkerProcesses(ILogger logger)
    {
        try
        {
            logger.LogInformation("Searching for MonitoringGrid worker processes to cleanup...");

            // Find all MonitoringGrid related processes
            var monitoringProcesses = Process.GetProcesses()
                .Where(p => 
                {
                    try
                    {
                        // Check process name
                        if (p.ProcessName.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase))
                            return true;

                        // Check main module filename if accessible
                        if (p.MainModule?.FileName?.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase) == true)
                            return true;

                        // Check command line arguments if accessible
                        try
                        {
                            var startInfo = p.StartInfo;
                            if (startInfo.Arguments?.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase) == true)
                                return true;
                        }
                        catch
                        {
                            // Ignore access denied errors
                        }

                        return false;
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
                logger.LogWarning("Found {Count} MonitoringGrid processes to cleanup", monitoringProcesses.Count);

                foreach (var process in monitoringProcesses)
                {
                    try
                    {
                        logger.LogInformation("Terminating MonitoringGrid process {ProcessId} ({ProcessName})", 
                            process.Id, process.ProcessName);
                        
                        // Try graceful shutdown first
                        if (!process.HasExited)
                        {
                            process.CloseMainWindow();
                            
                            // Wait a short time for graceful shutdown
                            if (!process.WaitForExit(3000))
                            {
                                // Force kill if graceful shutdown failed
                                logger.LogWarning("Force killing MonitoringGrid process {ProcessId}", process.Id);
                                process.Kill();
                                process.WaitForExit(2000);
                            }
                        }
                        
                        logger.LogInformation("Successfully terminated MonitoringGrid process {ProcessId}", process.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error terminating MonitoringGrid process {ProcessId}", process.Id);
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error disposing MonitoringGrid process {ProcessId}", process.Id);
                        }
                    }
                }

                logger.LogInformation("Worker process cleanup completed");
            }
            else
            {
                logger.LogInformation("No MonitoringGrid worker processes found to cleanup");
            }

            // Also cleanup any dotnet processes running MonitoringGrid
            CleanupDotnetMonitoringGridProcesses(logger);

            // Enhanced cleanup for .NET Host processes
            CleanupDotnetHostProcesses(logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during worker process cleanup");
        }
    }

    /// <summary>
    /// Cleanup dotnet processes running MonitoringGrid projects
    /// </summary>
    private static void CleanupDotnetMonitoringGridProcesses(ILogger logger)
    {
        try
        {
            logger.LogInformation("Searching for dotnet processes running MonitoringGrid...");

            var dotnetProcesses = Process.GetProcesses()
                .Where(p => p.ProcessName.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                .Where(p => p.Id != Environment.ProcessId) // Don't kill ourselves
                .ToList();

            var monitoringGridDotnetProcesses = new List<Process>();

            foreach (var process in dotnetProcesses)
            {
                try
                {
                    // Try to get command line arguments to see if it's running MonitoringGrid
                    var commandLine = GetProcessCommandLine(process);
                    if (!string.IsNullOrEmpty(commandLine) && 
                        commandLine.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase))
                    {
                        monitoringGridDotnetProcesses.Add(process);
                    }
                }
                catch
                {
                    // Ignore access denied errors
                }
            }

            if (monitoringGridDotnetProcesses.Any())
            {
                logger.LogWarning("Found {Count} dotnet processes running MonitoringGrid", monitoringGridDotnetProcesses.Count);

                foreach (var process in monitoringGridDotnetProcesses)
                {
                    try
                    {
                        logger.LogInformation("Terminating dotnet MonitoringGrid process {ProcessId}", process.Id);
                        
                        if (!process.HasExited)
                        {
                            process.Kill();
                            process.WaitForExit(2000);
                        }
                        
                        logger.LogInformation("Successfully terminated dotnet MonitoringGrid process {ProcessId}", process.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error terminating dotnet MonitoringGrid process {ProcessId}", process.Id);
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error disposing dotnet MonitoringGrid process {ProcessId}", process.Id);
                        }
                    }
                }
            }
            else
            {
                logger.LogInformation("No dotnet MonitoringGrid processes found");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during dotnet MonitoringGrid process cleanup");
        }
    }

    /// <summary>
    /// Get command line arguments for a process (Windows specific)
    /// </summary>
    private static string? GetProcessCommandLine(Process process)
    {
        try
        {
            return GetProcessCommandLine(process.Id);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Enhanced cleanup for .NET Host processes that might be running MonitoringGrid services
    /// </summary>
    private static void CleanupDotnetHostProcesses(ILogger logger)
    {
        try
        {
            logger.LogInformation("Searching for .NET Host processes running MonitoringGrid services...");

            var currentProcessId = Environment.ProcessId;
            var dotnetProcesses = Process.GetProcesses()
                .Where(p => p.ProcessName.Equals("dotnet", StringComparison.OrdinalIgnoreCase) ||
                           p.ProcessName.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase))
                .Where(p => p.Id != currentProcessId) // Don't kill ourselves
                .ToList();

            var monitoringGridProcesses = new List<(Process Process, string Reason)>();

            foreach (var process in dotnetProcesses)
            {
                try
                {
                    var reason = "";
                    var isMonitoringGrid = false;

                    // Check process name
                    if (process.ProcessName.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase))
                    {
                        isMonitoringGrid = true;
                        reason = "Process name contains MonitoringGrid";
                    }
                    else
                    {
                        // Check command line for dotnet processes
                        var commandLine = GetProcessCommandLine(process.Id);
                        if (!string.IsNullOrEmpty(commandLine))
                        {
                            if (commandLine.Contains("MonitoringGrid.Worker", StringComparison.OrdinalIgnoreCase) ||
                                commandLine.Contains("MonitoringGrid.Api", StringComparison.OrdinalIgnoreCase) ||
                                commandLine.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase))
                            {
                                isMonitoringGrid = true;
                                reason = "Command line contains MonitoringGrid";
                            }
                        }
                    }

                    if (isMonitoringGrid)
                    {
                        monitoringGridProcesses.Add((process, reason));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not check process {ProcessId} - likely access denied", process.Id);
                }
            }

            if (monitoringGridProcesses.Any())
            {
                logger.LogWarning("Found {Count} .NET processes running MonitoringGrid services", monitoringGridProcesses.Count);

                foreach (var (process, reason) in monitoringGridProcesses)
                {
                    try
                    {
                        logger.LogInformation("Terminating .NET MonitoringGrid process {ProcessId} ({ProcessName}) - {Reason}",
                            process.Id, process.ProcessName, reason);

                        if (!process.HasExited)
                        {
                            // Try graceful shutdown first
                            process.CloseMainWindow();

                            // Wait for graceful shutdown
                            if (!process.WaitForExit(5000))
                            {
                                // Force kill if graceful shutdown failed
                                logger.LogWarning("Force killing .NET MonitoringGrid process {ProcessId}", process.Id);
                                process.Kill();
                                process.WaitForExit(3000);
                            }
                        }

                        logger.LogInformation("Successfully terminated .NET MonitoringGrid process {ProcessId}", process.Id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error terminating .NET MonitoringGrid process {ProcessId}", process.Id);
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error disposing .NET MonitoringGrid process {ProcessId}", process.Id);
                        }
                    }
                }
            }
            else
            {
                logger.LogInformation("No .NET MonitoringGrid processes found");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during .NET Host process cleanup");
        }
    }
}
