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
            // This is a simplified approach - in a real implementation you might use WMI
            // For now, we'll just return null and rely on process name matching
            return null;
        }
        catch
        {
            return null;
        }
    }
}
