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
                        status.IsRunning = true;
                        status.Mode = "External";
                        status.ProcessId = _workerProcess.Id;
                        status.StartTime = _workerProcess.StartTime;
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
    /// Get Worker logs (last 100 lines)
    /// </summary>
    [HttpGet("logs")]
    public ActionResult<WorkerLogsDto> GetLogs([FromQuery] int lines = 100)
    {
        try
        {
            var logs = new List<string>();
            
            lock (_processLock)
            {
                if (_workerProcess != null && !_workerProcess.HasExited)
                {
                    // In a real implementation, you would read from log files
                    // For now, return a placeholder
                    logs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Worker process running (PID: {_workerProcess.Id})");
                    logs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Started at: {_workerProcess.StartTime}");
                    logs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Memory usage: {_workerProcess.WorkingSet64 / 1024 / 1024} MB");
                }
                else
                {
                    logs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Worker is not running");
                }
            }

            return Ok(new WorkerLogsDto
            {
                Lines = logs,
                TotalLines = logs.Count,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Worker logs");
            return StatusCode(500, "Error getting Worker logs");
        }
    }

    /// <summary>
    /// Get Worker configuration
    /// </summary>
    [HttpGet("config")]
    public ActionResult<object> GetConfiguration()
    {
        try
        {
            var config = new
            {
                IsIntegrated = _configuration.GetValue<bool>("Monitoring:EnableWorkerServices", false),
                WorkerConfig = _configuration.GetSection("Worker").Get<object>(),
                MonitoringConfig = _configuration.GetSection("Monitoring").Get<object>()
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Worker configuration");
            return StatusCode(500, "Error getting Worker configuration");
        }
    }

    /// <summary>
    /// Get KPI status for debugging
    /// </summary>
    [HttpGet("kpi-status")]
    public async Task<IActionResult> GetKpiStatus()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Infrastructure.Data.MonitoringContext>();

            var kpis = await context.KPIs
                .Select(k => new
                {
                    k.KpiId,
                    k.Indicator,
                    k.Owner,
                    k.IsActive,
                    k.Frequency,
                    k.LastRun,
                    k.ScheduleConfiguration,
                    k.IsCurrentlyRunning,
                    NextDue = k.LastRun.HasValue ? k.LastRun.Value.AddMinutes(k.Frequency) : (DateTime?)null,
                    MinutesUntilDue = k.LastRun.HasValue ?
                        (int)Math.Max(0, (k.LastRun.Value.AddMinutes(k.Frequency) - DateTime.UtcNow).TotalMinutes) : 0,
                    IsDue = !k.LastRun.HasValue || DateTime.UtcNow >= k.LastRun.Value.AddMinutes(k.Frequency)
                })
                .ToListAsync();

            var summary = new
            {
                TotalKpis = kpis.Count,
                ActiveKpis = kpis.Count(k => k.IsActive),
                KpisWithScheduleConfig = kpis.Count(k => !string.IsNullOrEmpty(k.ScheduleConfiguration)),
                KpisNeverRun = kpis.Count(k => !k.LastRun.HasValue),
                KpisDue = kpis.Count(k => k.IsDue && k.IsActive),
                KpisRunning = kpis.Count(k => k.IsCurrentlyRunning),
                KpiDetails = kpis
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI status");
            return StatusCode(500, new { success = false, message = "Failed to retrieve KPI status" });
        }
    }

    /// <summary>
    /// Reset KPI LastRun times to make them due for execution (for testing)
    /// </summary>
    [HttpPost("reset-kpi-times")]
    public async Task<IActionResult> ResetKpiTimes()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Infrastructure.Data.MonitoringContext>();

            var kpis = await context.KPIs.Where(k => k.IsActive).ToListAsync();

            foreach (var kpi in kpis)
            {
                kpi.LastRun = null; // Reset to make them due for execution
                kpi.IsCurrentlyRunning = false; // Ensure they're not marked as running
            }

            await context.SaveChangesAsync();

            _logger.LogInformation("Reset LastRun times for {Count} active KPIs", kpis.Count);

            return Ok(new {
                success = true,
                message = $"Reset {kpis.Count} KPIs to be due for execution",
                resetKpis = kpis.Select(k => new { k.KpiId, k.Indicator }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting KPI times");
            return StatusCode(500, new { success = false, message = "Failed to reset KPI times" });
        }
    }
}
