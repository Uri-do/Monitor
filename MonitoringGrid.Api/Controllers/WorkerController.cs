using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Interfaces;
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

                            _logger.LogDebug("Tracked worker process {ProcessId} is running, uptime: {Uptime}",
                                _workerProcess.Id, uptime);
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

                                _logger.LogInformation("Detected external Worker process with PID: {ProcessId}, uptime: {Uptime}",
                                    externalWorkerProcess.Id, uptime);
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
                            _logger.LogDebug("No external worker processes found, status: stopped");
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
    public async Task<ActionResult<WorkerActionResultDto>> StartWorker()
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

            Process? newWorkerProcess = null;
            int? processId = null;

            lock (_processLock)
            {
                // Check for ANY running worker processes (both tracked and external)
                var allWorkerProcesses = GetAllWorkerProcesses();
                if (allWorkerProcesses.Any())
                {
                    var runningWorkers = allWorkerProcesses.Select(p => p.Id).ToList();
                    var message = allWorkerProcesses.Count == 1
                        ? $"Worker is already running (PID: {runningWorkers.First()})"
                        : $"Multiple workers are already running (PIDs: {string.Join(", ", runningWorkers)})";

                    return BadRequest(new WorkerActionResultDto
                    {
                        Success = false,
                        Message = message,
                        ProcessId = runningWorkers.First(),
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

                newWorkerProcess = Process.Start(startInfo);

                if (newWorkerProcess == null)
                {
                    return StatusCode(500, new WorkerActionResultDto
                    {
                        Success = false,
                        Message = "Failed to start Worker process",
                        Timestamp = DateTime.UtcNow
                    });
                }

                _workerProcess = newWorkerProcess;
                processId = newWorkerProcess.Id;
                _logger.LogInformation("Started Worker process with PID: {ProcessId}", processId);
            }

            // Give the process a moment to initialize (outside the lock)
            await Task.Delay(1000);

            // Verify the process is still running
            if (newWorkerProcess.HasExited)
            {
                _logger.LogWarning("Worker process {ProcessId} exited immediately after start", processId);
                return StatusCode(500, new WorkerActionResultDto
                {
                    Success = false,
                    Message = $"Worker process started but exited immediately (PID: {processId})",
                    ProcessId = processId,
                    Timestamp = DateTime.UtcNow
                });
            }

            return Ok(new WorkerActionResultDto
            {
                Success = true,
                Message = $"Worker started successfully (PID: {processId})",
                ProcessId = processId,
                Timestamp = DateTime.UtcNow
            });
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
                // Get all worker processes (both tracked and external)
                var allWorkerProcesses = GetAllWorkerProcesses();

                if (allWorkerProcesses.Count == 0)
                {
                    // Clean up tracked process reference if it exists
                    if (_workerProcess != null)
                    {
                        try
                        {
                            _workerProcess.Dispose();
                        }
                        catch { }
                        _workerProcess = null;
                    }

                    return BadRequest(new WorkerActionResultDto
                    {
                        Success = false,
                        Message = "No worker processes are currently running",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var stoppedProcesses = new List<int>();
                var failedProcesses = new List<int>();

                // Stop all worker processes
                foreach (var process in allWorkerProcesses)
                {
                    try
                    {
                        var processId = process.Id;
                        _logger.LogInformation("Attempting to stop Worker process with PID: {ProcessId}", processId);

                        // Try graceful shutdown first
                        if (!process.HasExited)
                        {
                            process.CloseMainWindow();
                            if (!process.WaitForExit(3000)) // Wait 3 seconds for graceful shutdown
                            {
                                // Force kill if graceful shutdown failed
                                process.Kill();
                                process.WaitForExit(5000); // Wait up to 5 seconds for force kill
                            }
                        }

                        stoppedProcesses.Add(processId);
                        _logger.LogInformation("Successfully stopped Worker process with PID: {ProcessId}", processId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to stop Worker process with PID: {ProcessId}", process.Id);
                        failedProcesses.Add(process.Id);
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch { }
                    }
                }

                // Clean up tracked process reference
                if (_workerProcess != null)
                {
                    try
                    {
                        _workerProcess.Dispose();
                    }
                    catch { }
                    _workerProcess = null;
                }

                // Return result
                if (stoppedProcesses.Count > 0)
                {
                    var message = failedProcesses.Count > 0
                        ? $"Stopped {stoppedProcesses.Count} worker process(es). Failed to stop {failedProcesses.Count} process(es)."
                        : $"Successfully stopped {stoppedProcesses.Count} worker process(es)";

                    return Ok(new WorkerActionResultDto
                    {
                        Success = failedProcesses.Count == 0,
                        Message = message,
                        ProcessId = stoppedProcesses.FirstOrDefault(),
                        Timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return StatusCode(500, new WorkerActionResultDto
                    {
                        Success = false,
                        Message = "Failed to stop any worker processes",
                        Timestamp = DateTime.UtcNow
                    });
                }
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
    public async Task<ActionResult<WorkerActionResultDto>> RestartWorker()
    {
        try
        {
            // Stop first
            var stopResult = StopWorker();
            if (stopResult.Result is OkObjectResult)
            {
                // Wait a moment
                await Task.Delay(2000);

                // Start again
                return await StartWorker();
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
    /// Activate all Indicators (for testing)
    /// </summary>
    [HttpPost("activate-all-indicators")]
    public async Task<IActionResult> ActivateAllIndicators()
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
    /// Force kill all worker processes (emergency cleanup)
    /// </summary>
    [HttpPost("force-stop")]
    public ActionResult<WorkerActionResultDto> ForceStopWorkers()
    {
        try
        {
            _logger.LogWarning("Force stop requested for all worker processes");

            var allWorkerProcesses = GetAllWorkerProcesses();
            if (allWorkerProcesses.Count == 0)
            {
                return Ok(new WorkerActionResultDto
                {
                    Success = true,
                    Message = "No worker processes found to stop",
                    Timestamp = DateTime.UtcNow
                });
            }

            var killedProcesses = new List<int>();
            var failedProcesses = new List<int>();

            foreach (var process in allWorkerProcesses)
            {
                try
                {
                    var processId = process.Id;
                    _logger.LogWarning("Force killing Worker process with PID: {ProcessId}", processId);

                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true); // Kill entire process tree
                        process.WaitForExit(10000); // Wait up to 10 seconds
                    }

                    killedProcesses.Add(processId);
                    _logger.LogInformation("Force killed Worker process with PID: {ProcessId}", processId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to force kill Worker process with PID: {ProcessId}", process.Id);
                    failedProcesses.Add(process.Id);
                }
                finally
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch { }
                }
            }

            // Clean up tracked process reference
            lock (_processLock)
            {
                if (_workerProcess != null)
                {
                    try
                    {
                        _workerProcess.Dispose();
                    }
                    catch { }
                    _workerProcess = null;
                }
            }

            var message = failedProcesses.Count > 0
                ? $"Force killed {killedProcesses.Count} process(es). Failed to kill {failedProcesses.Count} process(es)."
                : $"Successfully force killed {killedProcesses.Count} worker process(es)";

            return Ok(new WorkerActionResultDto
            {
                Success = failedProcesses.Count == 0,
                Message = message,
                ProcessId = killedProcesses.FirstOrDefault(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during force stop");
            return StatusCode(500, new WorkerActionResultDto
            {
                Success = false,
                Message = $"Error during force stop: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Debug indicator status for worker troubleshooting
    /// </summary>
    [HttpGet("debug-indicators")]
    public async Task<IActionResult> DebugIndicators()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Infrastructure.Data.MonitoringContext>();
            var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();

            // Get all indicators with their details
            var allIndicators = await context.Indicators
                .Include(i => i.Scheduler)
                .Include(i => i.OwnerContact)
                .ToListAsync();

            // Get due indicators using the service
            var dueIndicators = await indicatorService.GetDueIndicatorsAsync();

            var debugInfo = new
            {
                TotalIndicators = allIndicators.Count,
                ActiveIndicators = allIndicators.Count(i => i.IsActive),
                InactiveIndicators = allIndicators.Count(i => !i.IsActive),
                IndicatorsWithSchedulers = allIndicators.Count(i => i.Scheduler != null),
                IndicatorsWithoutSchedulers = allIndicators.Count(i => i.Scheduler == null),
                CurrentlyRunningIndicators = allIndicators.Count(i => i.IsCurrentlyRunning),
                DueIndicators = dueIndicators.Count,
                IndicatorDetails = allIndicators.Select(i => new
                {
                    ID = i.IndicatorID,
                    Name = i.IndicatorName,
                    IsActive = i.IsActive,
                    IsCurrentlyRunning = i.IsCurrentlyRunning,
                    LastRun = i.LastRun,
                    LastMinutes = i.LastMinutes,
                    HasScheduler = i.Scheduler != null,
                    SchedulerEnabled = i.Scheduler?.IsEnabled,
                    Owner = i.OwnerContact?.Name,
                    IsDue = i.IsDue(),
                    NextRunTime = i.GetNextRunTime()
                }).ToList(),
                DueIndicatorNames = dueIndicators.Select(i => i.IndicatorName).ToList(),
                Timestamp = DateTime.UtcNow
            };

            return Ok(debugInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error debugging indicators");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Assign default schedulers to indicators that don't have them
    /// </summary>
    [HttpPost("assign-default-schedulers")]
    public async Task<IActionResult> AssignDefaultSchedulers()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<MonitoringGrid.Infrastructure.Data.MonitoringContext>();

            // Get indicators without schedulers
            var indicatorsWithoutSchedulers = await context.Indicators
                .Where(i => i.SchedulerID == null)
                .ToListAsync();

            if (!indicatorsWithoutSchedulers.Any())
            {
                return Ok(new
                {
                    success = true,
                    message = "All indicators already have schedulers assigned",
                    indicatorsUpdated = 0,
                    timestamp = DateTime.UtcNow
                });
            }

            // Create or get a default scheduler
            var defaultScheduler = await context.Schedulers
                .FirstOrDefaultAsync(s => s.SchedulerName == "Default Interval Scheduler");

            if (defaultScheduler == null)
            {
                // Create default scheduler
                defaultScheduler = new MonitoringGrid.Core.Entities.Scheduler
                {
                    SchedulerName = "Default Interval Scheduler",
                    SchedulerDescription = "Default scheduler for indicators - runs every 5 minutes",
                    ScheduleType = "interval",
                    IntervalMinutes = 5,
                    IsEnabled = true,
                    Timezone = "UTC",
                    CreatedBy = "system",
                    ModifiedBy = "system"
                };

                context.Schedulers.Add(defaultScheduler);
                await context.SaveChangesAsync();
                _logger.LogInformation("Created default scheduler with ID: {SchedulerID}", defaultScheduler.SchedulerID);
            }

            // Assign the default scheduler to indicators without schedulers
            foreach (var indicator in indicatorsWithoutSchedulers)
            {
                indicator.SchedulerID = defaultScheduler.SchedulerID;
                _logger.LogInformation("Assigned default scheduler to indicator: {IndicatorName} (ID: {IndicatorID})",
                    indicator.IndicatorName, indicator.IndicatorID);
            }

            await context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Successfully assigned default scheduler to {indicatorsWithoutSchedulers.Count} indicators",
                indicatorsUpdated = indicatorsWithoutSchedulers.Count,
                defaultSchedulerID = defaultScheduler.SchedulerID,
                defaultSchedulerName = defaultScheduler.SchedulerName,
                updatedIndicators = indicatorsWithoutSchedulers.Select(i => new
                {
                    indicatorID = i.IndicatorID,
                    indicatorName = i.IndicatorName,
                    schedulerID = i.SchedulerID
                }).ToList(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning default schedulers");
            return StatusCode(500, new
            {
                success = false,
                message = $"Error assigning default schedulers: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Manually trigger execution of a specific indicator
    /// </summary>
    [HttpPost("execute-indicator/{indicatorId}")]
    public async Task<IActionResult> ExecuteIndicator(long indicatorId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

            _logger.LogInformation("Manual execution requested for indicator ID: {IndicatorID}", indicatorId);

            var result = await indicatorExecutionService.ExecuteIndicatorAsync(
                indicatorId,
                "Manual",
                saveResults: true,
                CancellationToken.None);

            if (result.WasSuccessful)
            {
                return Ok(new
                {
                    success = true,
                    message = $"Indicator {indicatorId} executed successfully",
                    indicatorId = indicatorId,
                    indicatorName = result.IndicatorName,
                    currentValue = result.CurrentValue,
                    executionDuration = result.ExecutionDuration.TotalMilliseconds,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Failed to execute indicator {indicatorId}: {result.ErrorMessage}",
                    indicatorId = indicatorId,
                    indicatorName = result.IndicatorName,
                    error = result.ErrorMessage,
                    timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing indicator {IndicatorID}", indicatorId);
            return StatusCode(500, new
            {
                success = false,
                message = $"Error executing indicator {indicatorId}: {ex.Message}",
                indicatorId = indicatorId,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Manually trigger execution of all due indicators
    /// </summary>
    [HttpPost("execute-due-indicators")]
    public async Task<IActionResult> ExecuteDueIndicators()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

            _logger.LogInformation("Manual execution of all due indicators requested");

            var results = await indicatorExecutionService.ExecuteDueIndicatorsAsync("Manual", CancellationToken.None);

            return Ok(new
            {
                success = true,
                message = $"Executed {results.Count} due indicators",
                executedCount = results.Count,
                results = results.Select(r => new
                {
                    indicatorId = r.IndicatorID,
                    indicatorName = r.IndicatorName,
                    success = r.WasSuccessful,
                    errorMessage = r.ErrorMessage,
                    executionDuration = r.ExecutionDuration.TotalMilliseconds,
                    currentValue = r.CurrentValue,
                    thresholdBreached = r.ThresholdBreached
                }).ToList(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing due indicators");
            return StatusCode(500, new
            {
                success = false,
                message = $"Error executing due indicators: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Test worker logic directly (bypass hosted service)
    /// </summary>
    [HttpPost("test-worker-logic")]
    public async Task<IActionResult> TestWorkerLogic()
    {
        try
        {
            _logger.LogInformation("🧪 Testing worker logic directly...");

            using var scope = _serviceProvider.CreateScope();
            var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();
            var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

            _logger.LogInformation("✅ Services resolved successfully");

            // Get due indicators
            _logger.LogInformation("🔍 Getting due indicators...");
            var dueIndicators = await indicatorService.GetDueIndicatorsAsync(CancellationToken.None);
            _logger.LogInformation("📊 Found {Count} due indicators", dueIndicators.Count());

            var results = new List<object>();

            foreach (var indicator in dueIndicators)
            {
                _logger.LogInformation("🚀 Executing indicator {IndicatorId}: {IndicatorName}",
                    indicator.IndicatorID, indicator.IndicatorName);

                var result = await indicatorExecutionService.ExecuteIndicatorAsync(
                    indicator.IndicatorID,
                    "DirectTest",
                    saveResults: true,
                    CancellationToken.None);

                results.Add(new
                {
                    indicatorId = indicator.IndicatorID,
                    indicatorName = indicator.IndicatorName,
                    success = result.WasSuccessful,
                    executionDuration = result.ExecutionDuration.TotalMilliseconds,
                    currentValue = result.CurrentValue,
                    errorMessage = result.ErrorMessage
                });

                _logger.LogInformation("✅ Completed indicator {IndicatorId}: Success={Success}, Duration={Duration}ms",
                    indicator.IndicatorID, result.WasSuccessful, result.ExecutionDuration.TotalMilliseconds);
            }

            return Ok(new
            {
                success = true,
                message = $"Successfully tested worker logic with {dueIndicators.Count()} indicators",
                indicatorsProcessed = dueIndicators.Count(),
                results = results,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error testing worker logic");
            return StatusCode(500, new
            {
                success = false,
                message = $"Error testing worker logic: {ex.Message}",
                timestamp = DateTime.UtcNow
            });
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
            var workerProcesses = new List<Process>();
            var allProcesses = System.Diagnostics.Process.GetProcesses();

            foreach (var process in allProcesses)
            {
                try
                {
                    // Skip current API process
                    if (process.Id == Environment.ProcessId)
                        continue;

                    // Skip if process has already exited
                    if (process.HasExited)
                        continue;

                    var isWorkerProcess = false;

                    // Check for direct MonitoringGrid.Worker process name
                    if (process.ProcessName.Contains("MonitoringGrid.Worker", StringComparison.OrdinalIgnoreCase))
                    {
                        isWorkerProcess = true;
                    }
                    // Check for dotnet processes running MonitoringGrid.Worker
                    else if (process.ProcessName.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            // Method 1: Check command line arguments
                            var commandLine = GetProcessCommandLine(process.Id);
                            if (commandLine?.Contains("MonitoringGrid.Worker", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                isWorkerProcess = true;
                            }
                            // Method 2: Check main module path
                            else if (process.MainModule?.FileName?.Contains("MonitoringGrid.Worker", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                isWorkerProcess = true;
                            }
                            // Method 3: Check working directory (if accessible)
                            else
                            {
                                try
                                {
                                    var startInfo = process.StartInfo;
                                    if (startInfo?.WorkingDirectory?.Contains("MonitoringGrid.Worker", StringComparison.OrdinalIgnoreCase) == true)
                                    {
                                        isWorkerProcess = true;
                                    }
                                }
                                catch
                                {
                                    // Working directory not accessible, ignore
                                }
                            }
                        }
                        catch
                        {
                            // If we can't check command line or module, skip this process
                        }
                    }

                    if (isWorkerProcess)
                    {
                        workerProcesses.Add(process);
                        _logger.LogDebug("Found worker process: PID {ProcessId}, Name: {ProcessName}",
                            process.Id, process.ProcessName);
                    }
                }
                catch (Exception ex)
                {
                    // Process might have exited or access denied, skip it
                    _logger.LogDebug(ex, "Error checking process {ProcessId}", process.Id);
                    try
                    {
                        process.Dispose();
                    }
                    catch { }
                }
            }

            // Sort by start time (most recent first)
            return workerProcesses.OrderByDescending(p =>
            {
                try
                {
                    return p.StartTime;
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting worker processes");
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
                Name = "IndicatorMonitoringWorker",
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
