using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MonitoringGrid.Api.DTOs;
using System.Diagnostics;
using System.Text.Json;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Controller for managing Worker service operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WorkerController : ControllerBase
{
    private readonly ILogger<WorkerController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private static Process? _workerProcess;
    private static readonly object _processLock = new object();

    public WorkerController(
        ILogger<WorkerController> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    /// <summary>
    /// Get Worker service status
    /// </summary>
    [HttpGet("status")]
    public ActionResult<WorkerStatusDto> GetStatus()
    {
        try
        {
            var isIntegrated = _configuration.GetValue<bool>("Monitoring:EnableWorkerServices", false);
            var status = new WorkerStatusDto();

            if (isIntegrated)
            {
                // Check integrated Worker services (when EnableWorkerServices = true)
                var hostedServices = _serviceProvider.GetServices<IHostedService>();
                var workerServices = hostedServices.Where(s => s.GetType().Namespace?.Contains("MonitoringGrid.Worker") == true).ToList();

                var currentProcess = Process.GetCurrentProcess();
                var originalStartTime = currentProcess.StartTime;
                var currentTime = DateTime.Now;
                var uptime = currentTime - originalStartTime;

                status.IsRunning = workerServices.Any();
                status.Mode = "Integrated";
                status.ProcessId = Environment.ProcessId;
                status.StartTime = originalStartTime;
                status.Uptime = uptime.TotalSeconds > 0 ? uptime : TimeSpan.Zero;
                status.Services = workerServices.Select(s => new WorkerServiceDto
                {
                    Name = s.GetType().Name,
                    Status = "Running",
                    LastActivity = DateTime.UtcNow
                }).ToList();
            }
            else
            {
                // Manual mode (when EnableWorkerServices = false)
                // Check external Worker process
                lock (_processLock)
                {
                    // First check if we have a tracked process
                    _logger.LogDebug("Checking worker status - _workerProcess is {IsNull}, HasExited: {HasExited}",
                        _workerProcess == null ? "null" : "not null",
                        _workerProcess?.HasExited.ToString() ?? "N/A");

                    if (_workerProcess != null && !_workerProcess.HasExited)
                    {
                        try
                        {
                            var originalStartTime = _workerProcess.StartTime;
                            var currentTime = DateTime.Now;
                            var uptime = currentTime - originalStartTime;

                            status.IsRunning = true;
                            status.Mode = "Manual";
                            status.ProcessId = _workerProcess.Id;
                            status.StartTime = originalStartTime;
                            status.Uptime = uptime.TotalSeconds > 0 ? uptime : TimeSpan.Zero;
                            status.Services = GetWorkerServices();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error accessing tracked Worker process details");
                            status.IsRunning = true;
                            status.Mode = "Manual";
                            status.ProcessId = _workerProcess.Id;
                            status.StartTime = DateTime.UtcNow; // Fallback to current time
                            status.Services = GetWorkerServices();
                        }
                    }
                    else
                    {
                        // If no tracked process, look for external worker processes
                        _logger.LogDebug("No tracked process found, looking for external worker processes");
                        var externalWorkerProcess = FindExternalWorkerProcess();
                        _logger.LogDebug("External worker process search result: {ProcessId}",
                            externalWorkerProcess?.Id.ToString() ?? "null");

                        if (externalWorkerProcess != null)
                        {
                            try
                            {
                                var originalStartTime = externalWorkerProcess.StartTime;
                                var currentTime = DateTime.Now;
                                var uptime = currentTime - originalStartTime;

                                status.IsRunning = true;
                                status.Mode = "Manual";
                                status.ProcessId = externalWorkerProcess.Id;
                                status.StartTime = originalStartTime;
                                status.Uptime = uptime.TotalSeconds > 0 ? uptime : TimeSpan.Zero;
                                status.Services = GetWorkerServices();

                                _logger.LogInformation("Detected external Worker process with PID: {ProcessId}", externalWorkerProcess.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error accessing external Worker process details");
                                status.IsRunning = true;
                                status.Mode = "Manual";
                                status.ProcessId = externalWorkerProcess.Id;
                                status.StartTime = DateTime.UtcNow; // Fallback to current time
                                status.Services = GetWorkerServices();
                            }
                        }
                        else
                        {
                            status.IsRunning = false;
                            status.Mode = "Manual";
                            status.Services = new List<WorkerServiceDto>();
                        }
                    }
                }
            }

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Worker status");
            return StatusCode(500, "Error getting Worker status");
        }
    }

    /// <summary>
    /// Start Worker service
    /// </summary>
    [HttpPost("start")]
    public ActionResult<WorkerActionResultDto> StartWorker()
    {
        try
        {
            var isIntegrated = _configuration.GetValue<bool>("Monitoring:EnableWorkerServices", false);
            
            if (isIntegrated)
            {
                return BadRequest(new WorkerActionResultDto
                {
                    Success = false,
                    Message = "Worker services are integrated. Restart the API to enable them.",
                    Timestamp = DateTime.UtcNow
                });
            }

            lock (_processLock)
            {
                // Check for ANY running worker processes (both tracked and external)
                var allWorkerProcesses = GetAllWorkerProcesses();
                if (allWorkerProcesses.Any())
                {
                    var runningWorker = allWorkerProcesses.First();
                    return BadRequest(new WorkerActionResultDto
                    {
                        Success = false,
                        Message = $"Worker is already running (PID: {runningWorker.Id}). Found {allWorkerProcesses.Count} worker process(es).",
                        ProcessId = runningWorker.Id,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Start external Worker process
                var workerPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "MonitoringGrid.Worker");
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "run --project MonitoringGrid.Worker.csproj",
                    WorkingDirectory = workerPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                _workerProcess = Process.Start(startInfo);
                
                if (_workerProcess == null)
                {
                    return StatusCode(500, new WorkerActionResultDto
                    {
                        Success = false,
                        Message = "Failed to start Worker process",
                        Timestamp = DateTime.UtcNow
                    });
                }

                _logger.LogInformation("Started Worker process with PID: {ProcessId}", _workerProcess.Id);

                return Ok(new WorkerActionResultDto
                {
                    Success = true,
                    Message = $"Worker started successfully (PID: {_workerProcess.Id})",
                    ProcessId = _workerProcess.Id,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Worker");
            return StatusCode(500, new WorkerActionResultDto
            {
                Success = false,
                Message = $"Error starting Worker: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Stop Worker service
    /// </summary>
    [HttpPost("stop")]
    public ActionResult<WorkerActionResultDto> StopWorker()
    {
        try
        {
            var isIntegrated = _configuration.GetValue<bool>("Monitoring:EnableWorkerServices", false);
            
            if (isIntegrated)
            {
                return BadRequest(new WorkerActionResultDto
                {
                    Success = false,
                    Message = "Worker services are integrated. Restart the API to disable them.",
                    Timestamp = DateTime.UtcNow
                });
            }

            lock (_processLock)
            {
                if (_workerProcess == null || _workerProcess.HasExited)
                {
                    return BadRequest(new WorkerActionResultDto
                    {
                        Success = false,
                        Message = "Worker is not running",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var processId = _workerProcess.Id;

                // Worker cleanup will be handled automatically by the application shutdown handler

                _workerProcess.Kill();
                _workerProcess.WaitForExit(5000); // Wait up to 5 seconds
                _workerProcess.Dispose();
                _workerProcess = null;

                _logger.LogInformation("Stopped Worker process with PID: {ProcessId}", processId);

                return Ok(new WorkerActionResultDto
                {
                    Success = true,
                    Message = $"Worker stopped successfully (PID: {processId})",
                    ProcessId = processId,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Worker");
            return StatusCode(500, new WorkerActionResultDto
            {
                Success = false,
                Message = $"Error stopping Worker: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Restart Worker service
    /// </summary>
    [HttpPost("restart")]
    public ActionResult<WorkerActionResultDto> RestartWorker()
    {
        try
        {
            // Stop first
            var stopResult = StopWorker();
            if (stopResult.Result is OkObjectResult)
            {
                // Wait a moment
                Thread.Sleep(2000);
                
                // Start again
                return StartWorker();
            }

            return stopResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting Worker");
            return StatusCode(500, new WorkerActionResultDto
            {
                Success = false,
                Message = $"Error restarting Worker: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Execute a specific Indicator manually
    /// </summary>
    [HttpPost("execute-indicator/{indicatorId}")]
    public async Task<IActionResult> ExecuteIndicator(long indicatorId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Infrastructure.Data.MonitoringContext>();

            var indicator = await context.Indicators.FindAsync(indicatorId);
            if (indicator == null)
            {
                return NotFound(new { success = false, message = "Indicator not found" });
            }

            // Mark as currently running
            indicator.IsCurrentlyRunning = true;
            await context.SaveChangesAsync();

            _logger.LogInformation("Manual execution requested for Indicator {IndicatorId}: {IndicatorName}", indicatorId, indicator.IndicatorName);

            return Ok(new {
                success = true,
                message = $"Indicator execution started: {indicator.IndicatorName}",
                indicatorId = indicatorId,
                indicatorName = indicator.IndicatorName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Indicator {IndicatorId}", indicatorId);
            return StatusCode(500, new { success = false, message = "Failed to execute Indicator" });
        }
    }

    /// <summary>
    /// Activate all KPIs (for testing)
    /// </summary>
    [HttpPost("activate-all-kpis")]
    public async Task<IActionResult> ActivateAllKpis()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Infrastructure.Data.MonitoringContext>();

            var indicators = await context.Indicators.ToListAsync();

            foreach (var indicator in indicators)
            {
                indicator.IsActive = true;
                indicator.LastRun = null; // Reset to make them due for execution
                indicator.IsCurrentlyRunning = false;
            }

            await context.SaveChangesAsync();

            _logger.LogInformation("Activated {Count} Indicators", indicators.Count);

            return Ok(new {
                success = true,
                message = $"Activated {indicators.Count} Indicators and reset their execution times",
                activatedIndicators = indicators.Select(i => new { i.IndicatorID, i.IndicatorName }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating Indicators");
            return StatusCode(500, new { success = false, message = "Failed to activate Indicators" });
        }
    }

    /// <summary>
    /// Get worker cleanup status
    /// </summary>
    [HttpGet("cleanup-status")]
    public IActionResult GetCleanupStatus()
    {
        try
        {
            // Check for running MonitoringGrid processes
            var monitoringProcesses = System.Diagnostics.Process.GetProcesses()
                .Where(p =>
                {
                    try
                    {
                        return p.ProcessName.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase) ||
                               (p.MainModule?.FileName?.Contains("MonitoringGrid", StringComparison.OrdinalIgnoreCase) == true);
                    }
                    catch
                    {
                        return false;
                    }
                })
                .Where(p => p.Id != Environment.ProcessId)
                .ToList();

            return Ok(new
            {
                success = true,
                totalProcesses = monitoringProcesses.Count,
                processes = monitoringProcesses.Select(p => new
                {
                    processId = p.Id,
                    processName = p.ProcessName,
                    startTime = p.StartTime,
                    isResponding = p.Responding
                }).ToList(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cleanup status");
            return StatusCode(500, new { success = false, message = "Failed to retrieve cleanup status" });
        }
    }

    /// <summary>
    /// Manually trigger worker cleanup (for testing)
    /// </summary>
    [HttpPost("cleanup-workers")]
    public IActionResult CleanupWorkers()
    {
        try
        {
            _logger.LogInformation("Manual worker cleanup requested");

            // Use the same cleanup logic as the application shutdown handler
            MonitoringGrid.Api.Extensions.ApplicationLifetimeExtensions.TriggerManualCleanup(_logger);

            return Ok(new
            {
                success = true,
                message = "Worker cleanup completed",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual worker cleanup");
            return StatusCode(500, new { success = false, message = "Failed to cleanup workers" });
        }
    }

    private Process? FindExternalWorkerProcess()
    {
        try
        {
            // Find running MonitoringGrid.Worker processes
            var workerProcesses = GetAllWorkerProcesses();

            // Return the first running worker process
            return workerProcesses.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error finding external worker process");
            return null;
        }
    }

    private List<Process> GetAllWorkerProcesses()
    {
        try
        {
            return System.Diagnostics.Process.GetProcesses()
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
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error getting worker processes");
            return new List<Process>();
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

    private List<WorkerServiceDto> GetWorkerServices()
    {
        return new List<WorkerServiceDto>
        {
            new WorkerServiceDto
            {
                Name = "KpiMonitoringWorker",
                Status = "Running",
                LastActivity = DateTime.UtcNow
            },
            new WorkerServiceDto
            {
                Name = "ScheduledTaskWorker",
                Status = "Running",
                LastActivity = DateTime.UtcNow
            },
            new WorkerServiceDto
            {
                Name = "HealthCheckWorker",
                Status = "Running",
                LastActivity = DateTime.UtcNow
            },
            new WorkerServiceDto
            {
                Name = "AlertProcessingWorker",
                Status = "Running",
                LastActivity = DateTime.UtcNow
            }
        };
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
