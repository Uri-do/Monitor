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
                // Check integrated Worker services
                var hostedServices = _serviceProvider.GetServices<IHostedService>();
                var workerServices = hostedServices.Where(s => s.GetType().Namespace?.Contains("MonitoringGrid.Worker") == true).ToList();
                
                status.IsRunning = workerServices.Any();
                status.Mode = "Integrated";
                status.ProcessId = Environment.ProcessId;
                status.StartTime = Process.GetCurrentProcess().StartTime;
                status.Services = workerServices.Select(s => new WorkerServiceDto
                {
                    Name = s.GetType().Name,
                    Status = "Running",
                    LastActivity = DateTime.UtcNow
                }).ToList();
            }
            else
            {
                // Check external Worker process
                lock (_processLock)
                {
                    if (_workerProcess != null && !_workerProcess.HasExited)
                    {
                        try
                        {
                            status.IsRunning = true;
                            status.Mode = "External";
                            status.ProcessId = _workerProcess.Id;
                            status.StartTime = _workerProcess.StartTime;
                            status.Services = new List<WorkerServiceDto>
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
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error accessing Worker process details");
                            status.IsRunning = true;
                            status.Mode = "External";
                            status.ProcessId = _workerProcess.Id;
                            status.StartTime = DateTime.UtcNow; // Fallback to current time
                            status.Services = new List<WorkerServiceDto>
                            {
                                new WorkerServiceDto
                                {
                                    Name = "MonitoringGrid.Worker",
                                    Status = "Running",
                                    LastActivity = DateTime.UtcNow
                                }
                            };
                        }
                    }
                    else
                    {
                        status.IsRunning = false;
                        status.Mode = "External";
                        status.Services = new List<WorkerServiceDto>();
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
                if (_workerProcess != null && !_workerProcess.HasExited)
                {
                    return BadRequest(new WorkerActionResultDto
                    {
                        Success = false,
                        Message = "Worker is already running",
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
    /// Execute a specific KPI manually
    /// </summary>
    [HttpPost("execute-kpi/{kpiId}")]
    public async Task<IActionResult> ExecuteKpi(int kpiId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Infrastructure.Data.MonitoringContext>();

            var kpi = await context.KPIs.FindAsync(kpiId);
            if (kpi == null)
            {
                return NotFound(new { success = false, message = "KPI not found" });
            }

            // Mark as currently running
            kpi.IsCurrentlyRunning = true;
            await context.SaveChangesAsync();

            _logger.LogInformation("Manual execution requested for KPI {KpiId}: {Indicator}", kpiId, kpi.Indicator);

            return Ok(new {
                success = true,
                message = $"KPI execution started: {kpi.Indicator}",
                kpiId = kpiId,
                indicator = kpi.Indicator
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing KPI {KpiId}", kpiId);
            return StatusCode(500, new { success = false, message = "Failed to execute KPI" });
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

            var kpis = await context.KPIs.ToListAsync();

            foreach (var kpi in kpis)
            {
                kpi.IsActive = true;
                kpi.LastRun = null; // Reset to make them due for execution
                kpi.IsCurrentlyRunning = false;
            }

            await context.SaveChangesAsync();

            _logger.LogInformation("Activated {Count} KPIs", kpis.Count);

            return Ok(new {
                success = true,
                message = $"Activated {kpis.Count} KPIs and reset their execution times",
                activatedKpis = kpis.Select(k => new { k.KpiId, k.Indicator }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating KPIs");
            return StatusCode(500, new { success = false, message = "Failed to activate KPIs" });
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
}
