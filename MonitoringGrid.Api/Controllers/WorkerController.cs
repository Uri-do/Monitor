using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using MonitoringGrid.Api.Constants;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.DTOs.Common;
using MonitoringGrid.Api.DTOs.Worker;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;
using System.Diagnostics;
using System.Text.Json;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Controller for managing Worker service operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
[ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
public class WorkerController : BaseApiController
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IRepository<Indicator> _indicatorRepository;
    private static Process? _workerProcess;
    private static readonly object _processLock = new object();

    public WorkerController(
        IMediator mediator,
        ILogger<WorkerController> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IRepository<Indicator> indicatorRepository)
        : base(mediator, logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _indicatorRepository = indicatorRepository;
    }

    /// <summary>
    /// Get Worker service status
    /// </summary>
    /// <param name="request">Status request parameters</param>
    /// <returns>Worker status information</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(WorkerStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<WorkerStatusResponse> GetStatus([FromQuery] GetWorkerStatusRequest? request = null)
    {
        try
        {
            request ??= new GetWorkerStatusRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            var isIntegrated = _configuration.GetValue<bool>(WorkerConstants.ConfigKeys.EnableWorkerServices, false);
            var status = isIntegrated ? GetIntegratedWorkerStatus(request) : GetManualWorkerStatus(request);

            Logger.LogDebug("Worker status retrieved successfully. Mode: {Mode}, IsRunning: {IsRunning}",
                status.Mode, status.IsRunning);

            return Ok(CreateSuccessResponse(status, "Worker status retrieved successfully"));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting Worker status");
            return StatusCode(500, CreateErrorResponse("Failed to retrieve worker status", "WORKER_STATUS_ERROR"));
        }
    }

    private WorkerStatusResponse GetIntegratedWorkerStatus(GetWorkerStatusRequest request)
    {
        var hostedServices = _serviceProvider.GetServices<IHostedService>();
        var workerServices = hostedServices.Where(s => s.GetType().Namespace?.Contains(WorkerConstants.Names.WorkerNamespace) == true).ToList();

        var currentProcess = Process.GetCurrentProcess();
        var uptime = DateTime.Now - currentProcess.StartTime;

        var response = new WorkerStatusResponse
        {
            IsRunning = workerServices.Any(),
            Mode = WorkerConstants.Modes.Integrated,
            ProcessId = Environment.ProcessId,
            StartTime = currentProcess.StartTime,
            Uptime = uptime.TotalSeconds > 0 ? uptime : TimeSpan.Zero,
            Services = workerServices.Select(s => new WorkerServiceInfo
            {
                Name = s.GetType().Name,
                Status = WorkerConstants.Status.Running,
                LastActivity = DateTime.UtcNow,
                Details = request.IncludeDetails ? new Dictionary<string, object>
                {
                    ["ServiceType"] = s.GetType().FullName ?? s.GetType().Name,
                    ["Assembly"] = s.GetType().Assembly.GetName().Name ?? "Unknown"
                } : null
            }).ToList()
        };

        if (request.IncludeMetrics)
        {
            response.Metrics = GetWorkerMetrics(currentProcess);
        }

        return response;
    }

    private WorkerStatusResponse GetManualWorkerStatus(GetWorkerStatusRequest request)
    {
        lock (_processLock)
        {
            Logger.LogDebug("Checking worker status - _workerProcess is {IsNull}, HasExited: {HasExited}",
                _workerProcess == null ? "null" : "not null",
                _workerProcess?.HasExited.ToString() ?? "N/A");

            // Check tracked process first
            if (_workerProcess != null && !_workerProcess.HasExited)
            {
                return CreateStatusFromProcess(_workerProcess, "tracked", request);
            }

            // Look for external worker processes
            Logger.LogDebug("No tracked process found, looking for external worker processes");
            var externalWorkerProcess = FindExternalWorkerProcess();
            Logger.LogDebug("External worker process search result: {ProcessId}",
                externalWorkerProcess?.Id.ToString() ?? "null");

            if (externalWorkerProcess != null)
            {
                return CreateStatusFromProcess(externalWorkerProcess, "external", request);
            }

            Logger.LogDebug("No external worker processes found, status: stopped");
            return new WorkerStatusResponse
            {
                IsRunning = false,
                Mode = WorkerConstants.Modes.Manual,
                Services = new List<WorkerServiceInfo>()
            };
        }
    }

    private WorkerStatusResponse CreateStatusFromProcess(Process process, string processType, GetWorkerStatusRequest request)
    {
        try
        {
            var uptime = DateTime.Now - process.StartTime;
            var status = new WorkerStatusResponse
            {
                IsRunning = true,
                Mode = WorkerConstants.Modes.Manual,
                ProcessId = process.Id,
                StartTime = process.StartTime,
                Uptime = uptime.TotalSeconds > 0 ? uptime : TimeSpan.Zero,
                Services = GetWorkerServicesInfo(request.IncludeDetails)
            };

            if (request.IncludeMetrics)
            {
                status.Metrics = GetWorkerMetrics(process);
            }

            Logger.LogDebug("{ProcessType} worker process {ProcessId} is running, uptime: {Uptime}",
                processType, process.Id, uptime);

            return status;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error accessing {ProcessType} Worker process details", processType);
            return new WorkerStatusResponse
            {
                IsRunning = true,
                Mode = WorkerConstants.Modes.Manual,
                ProcessId = process.Id,
                StartTime = DateTime.UtcNow, // Fallback to current time
                Services = GetWorkerServicesInfo(false)
            };
        }
    }

    private List<WorkerServiceInfo> GetWorkerServicesInfo(bool includeDetails)
    {
        // This is a simplified implementation - in a real scenario, you'd query actual services
        return new List<WorkerServiceInfo>
        {
            new WorkerServiceInfo
            {
                Name = "IndicatorExecutionService",
                Status = WorkerConstants.Status.Running,
                LastActivity = DateTime.UtcNow,
                Details = includeDetails ? new Dictionary<string, object>
                {
                    ["ServiceType"] = "Background Service",
                    ["LastExecution"] = DateTime.UtcNow.AddMinutes(-5)
                } : null
            }
        };
    }

    private WorkerMetrics? GetWorkerMetrics(Process process)
    {
        try
        {
            return new WorkerMetrics
            {
                CpuUsage = null, // Would need performance counters for accurate CPU usage
                MemoryUsageMB = process.WorkingSet64 / (1024 * 1024),
                ThreadCount = process.Threads.Count,
                TotalIndicatorsProcessed = null, // Would come from application metrics
                SuccessfulExecutions = null,
                FailedExecutions = null,
                AverageExecutionTimeMs = null
            };
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get worker metrics for process {ProcessId}", process.Id);
            return null;
        }
    }

    /// <summary>
    /// Start Worker service
    /// </summary>
    /// <param name="request">Start worker request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Worker operation result</returns>
    [HttpPost("start")]
    [ProducesResponseType(typeof(WorkerOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkerOperationResponse>> StartWorker(
        [FromBody] StartWorkerRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new StartWorkerRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            var isIntegrated = _configuration.GetValue<bool>(WorkerConstants.ConfigKeys.EnableWorkerServices, false);

            if (isIntegrated)
            {
                return BadRequest(CreateErrorResponse(WorkerConstants.Messages.WorkerServicesIntegrated, "WORKER_INTEGRATED"));
            }

            // Check if workers are already running
            if (!request.Force)
            {
                var existingWorkersResult = CheckForExistingWorkers();
                if (existingWorkersResult != null)
                {
                    return existingWorkersResult;
                }
            }

            // Start new worker process
            var startResult = await StartNewWorkerProcessAsync(request, cancellationToken);
            stopwatch.Stop();

            if (startResult.Value is WorkerOperationResponse response)
            {
                response.DurationMs = stopwatch.ElapsedMilliseconds;
            }

            return startResult;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Worker start operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Worker start operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error starting Worker");
            return StatusCode(500, CreateErrorResponse($"Error starting Worker: {ex.Message}", "WORKER_START_ERROR"));
        }
    }

    private ActionResult<WorkerOperationResponse>? CheckForExistingWorkers()
    {
        lock (_processLock)
        {
            var allWorkerProcesses = GetAllWorkerProcesses();
            if (allWorkerProcesses.Any())
            {
                var runningWorkers = allWorkerProcesses.Select(p => p.Id).ToList();
                var message = allWorkerProcesses.Count == 1
                    ? $"Worker is already running (PID: {runningWorkers.First()})"
                    : $"Multiple workers are already running (PIDs: {string.Join(", ", runningWorkers)})";

                var response = new WorkerOperationResponse
                {
                    Success = false,
                    Message = message,
                    ProcessId = runningWorkers.First(),
                    Details = new Dictionary<string, object>
                    {
                        ["RunningProcessIds"] = runningWorkers,
                        ["ProcessCount"] = allWorkerProcesses.Count
                    }
                };

                return BadRequest(CreateErrorResponse(message, "WORKER_ALREADY_RUNNING"));
            }
        }
        return null;
    }

    private async Task<ActionResult<WorkerOperationResponse>> StartNewWorkerProcessAsync(
        StartWorkerRequest request,
        CancellationToken cancellationToken)
    {
        Process? newWorkerProcess = null;
        int? processId = null;

        lock (_processLock)
        {
            var startInfo = CreateWorkerProcessStartInfo(request);
            newWorkerProcess = Process.Start(startInfo);

            if (newWorkerProcess == null)
            {
                return StatusCode(500, CreateErrorResponse(WorkerConstants.Messages.FailedToStartWorker, "PROCESS_START_FAILED"));
            }

            _workerProcess = newWorkerProcess;
            processId = newWorkerProcess.Id;
            Logger.LogInformation("Started Worker process with PID: {ProcessId}", processId);
        }

        // Give the process a moment to initialize (outside the lock)
        var initTimeout = request.TimeoutMs ?? WorkerConstants.Timeouts.ProcessInitializationMs;
        await Task.Delay(initTimeout, cancellationToken);

        // Verify the process is still running
        if (newWorkerProcess.HasExited)
        {
            Logger.LogWarning("Worker process {ProcessId} exited immediately after start", processId);

            var response = new WorkerOperationResponse
            {
                Success = false,
                Message = $"Worker process started but exited immediately (PID: {processId})",
                ProcessId = processId,
                ErrorCode = "PROCESS_EXITED_IMMEDIATELY",
                Details = new Dictionary<string, object>
                {
                    ["ExitCode"] = newWorkerProcess.ExitCode,
                    ["ExitTime"] = newWorkerProcess.ExitTime
                }
            };

            return StatusCode(500, CreateErrorResponse(response.Message, response.ErrorCode));
        }

        var successResponse = new WorkerOperationResponse
        {
            Success = true,
            Message = $"Worker started successfully (PID: {processId})",
            ProcessId = processId,
            Details = new Dictionary<string, object>
            {
                ["StartTime"] = newWorkerProcess.StartTime,
                ["ProcessName"] = newWorkerProcess.ProcessName
            }
        };

        return Ok(CreateSuccessResponse(successResponse, "Worker started successfully"));
    }

    private static ProcessStartInfo CreateWorkerProcessStartInfo(StartWorkerRequest? request = null)
    {
        var workerPath = Path.Combine(Directory.GetCurrentDirectory(), "..", WorkerConstants.Paths.WorkerProjectPath);
        var arguments = WorkerConstants.Paths.DotnetRunArgs;

        if (!string.IsNullOrEmpty(request?.Arguments))
        {
            arguments += $" {request.Arguments}";
        }

        return new ProcessStartInfo
        {
            FileName = WorkerConstants.Paths.DotnetExecutable,
            Arguments = arguments,
            WorkingDirectory = workerPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }



    /// <summary>
    /// Stop Worker service
    /// </summary>
    /// <param name="request">Stop worker request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Worker operation result</returns>
    [HttpPost("stop")]
    [AllowAnonymous] // Temporarily allow anonymous access for development
    [ProducesResponseType(typeof(WorkerOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public ActionResult<WorkerOperationResponse> StopWorker(
        [FromBody] StopWorkerRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new StopWorkerRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            var isIntegrated = _configuration.GetValue<bool>(WorkerConstants.ConfigKeys.EnableWorkerServices, false);

            if (isIntegrated)
            {
                return BadRequest(CreateErrorResponse(WorkerConstants.Messages.WorkerServicesIntegratedDisable, "WORKER_INTEGRATED"));
            }

            var result = StopAllWorkerProcesses(request);
            stopwatch.Stop();

            if (result.Value is WorkerOperationResponse response)
            {
                response.DurationMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Worker stop operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Worker stop operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error stopping Worker");
            return StatusCode(500, CreateErrorResponse($"Error stopping Worker: {ex.Message}", "WORKER_STOP_ERROR"));
        }
    }

    private ActionResult<WorkerOperationResponse> StopAllWorkerProcesses(StopWorkerRequest request)
    {
        lock (_processLock)
        {
            var allWorkerProcesses = GetAllWorkerProcesses();

            if (allWorkerProcesses.Count == 0)
            {
                CleanupTrackedProcess();
                return BadRequest(CreateErrorResponse(WorkerConstants.Messages.NoWorkerProcesses, "NO_WORKER_PROCESSES"));
            }

            var (stoppedProcesses, failedProcesses) = StopProcesses(allWorkerProcesses, request);
            CleanupTrackedProcess();

            return CreateStopResult(stoppedProcesses, failedProcesses);
        }
    }

    private (List<int> stopped, List<int> failed) StopProcesses(List<Process> processes, StopWorkerRequest request)
    {
        var stoppedProcesses = new List<int>();
        var failedProcesses = new List<int>();

        foreach (var process in processes)
        {
            try
            {
                var processId = process.Id;
                Logger.LogInformation("Attempting to stop Worker process with PID: {ProcessId}", processId);

                if (!process.HasExited)
                {
                    StopProcessGracefully(process, request);
                }

                stoppedProcesses.Add(processId);
                Logger.LogInformation("Successfully stopped Worker process with PID: {ProcessId}", processId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to stop Worker process with PID: {ProcessId}", process.Id);
                failedProcesses.Add(process.Id);
            }
            finally
            {
                SafeDisposeProcess(process);
            }
        }

        return (stoppedProcesses, failedProcesses);
    }

    private static void StopProcessGracefully(Process process, StopWorkerRequest request)
    {
        var gracefulTimeout = request.TimeoutMs ?? WorkerConstants.Timeouts.GracefulShutdownMs;

        process.CloseMainWindow();
        if (!process.WaitForExit(gracefulTimeout))
        {
            if (request.Force)
            {
                // Force kill if graceful shutdown failed and force is requested
                process.Kill();
                process.WaitForExit(WorkerConstants.Timeouts.ForceKillMs);
            }
            else
            {
                // Just try a regular kill without force
                process.Kill();
                process.WaitForExit(WorkerConstants.Timeouts.ForceKillMs);
            }
        }
    }

    private void CleanupTrackedProcess()
    {
        if (_workerProcess != null)
        {
            SafeDisposeProcess(_workerProcess);
            _workerProcess = null;
        }
    }

    private static void SafeDisposeProcess(Process process)
    {
        try
        {
            process.Dispose();
        }
        catch { }
    }

    private ActionResult<WorkerOperationResponse> CreateStopResult(List<int> stoppedProcesses, List<int> failedProcesses)
    {
        if (stoppedProcesses.Count > 0)
        {
            var message = failedProcesses.Count > 0
                ? $"Stopped {stoppedProcesses.Count} worker process(es). Failed to stop {failedProcesses.Count} process(es)."
                : $"Successfully stopped {stoppedProcesses.Count} worker process(es)";

            var response = new WorkerOperationResponse
            {
                Success = failedProcesses.Count == 0,
                Message = message,
                ProcessId = stoppedProcesses.FirstOrDefault(),
                Details = new Dictionary<string, object>
                {
                    ["StoppedProcessIds"] = stoppedProcesses,
                    ["FailedProcessIds"] = failedProcesses,
                    ["TotalStopped"] = stoppedProcesses.Count,
                    ["TotalFailed"] = failedProcesses.Count
                }
            };

            return Ok(CreateSuccessResponse(response, message));
        }

        return StatusCode(500, CreateErrorResponse(WorkerConstants.Messages.FailedToStopWorkers, "STOP_WORKERS_FAILED"));
    }

    /// <summary>
    /// Restart Worker service
    /// </summary>
    /// <param name="request">Restart worker request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Worker operation result</returns>
    [HttpPost("restart")]
    [AllowAnonymous] // Temporarily allow anonymous access for development
    [ProducesResponseType(typeof(WorkerOperationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkerOperationResponse>> RestartWorker(
        [FromBody] RestartWorkerRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new RestartWorkerRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogInformation("Restarting worker service with delay: {DelayMs}ms", request.DelayMs);

            // Stop first
            var stopRequest = new StopWorkerRequest { Force = request.Force };
            var stopResult = StopWorker(stopRequest, cancellationToken);

            if (stopResult.Result is not OkObjectResult && !request.Force)
            {
                return stopResult;
            }

            // Wait for the specified delay
            await Task.Delay(request.DelayMs, cancellationToken);

            // Start again
            var startRequest = new StartWorkerRequest
            {
                TimeoutMs = request.StartupTimeoutMs,
                Force = request.Force
            };
            var startResult = await StartWorker(startRequest, cancellationToken);

            stopwatch.Stop();

            if (startResult.Value is WorkerOperationResponse response)
            {
                response.DurationMs = stopwatch.ElapsedMilliseconds;
                response.Message = $"Worker restarted successfully. {response.Message}";
            }

            return startResult;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Worker restart operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Worker restart operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error restarting Worker");
            return StatusCode(500, CreateErrorResponse($"Error restarting Worker: {ex.Message}", "WORKER_RESTART_ERROR"));
        }
    }



    /// <summary>
    /// Activate indicators
    /// </summary>
    /// <param name="request">Activate indicators request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk indicator operation result</returns>
    [HttpPost("activate-indicators")]
    [ProducesResponseType(typeof(BulkIndicatorExecutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkIndicatorExecutionResponse>> ActivateIndicators(
        [FromBody] ActivateIndicatorsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new ActivateIndicatorsRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Get indicators to activate
            var indicators = request.IndicatorIds?.Any() == true
                ? await _indicatorRepository.GetAsync(i => request.IndicatorIds.Contains(i.IndicatorID), cancellationToken)
                : await _indicatorRepository.GetAllAsync(cancellationToken);

            if (!indicators.Any())
            {
                return BadRequest(CreateErrorResponse("No indicators found to activate", "NO_INDICATORS_FOUND"));
            }

            // Update all indicators in memory first
            foreach (var indicator in indicators)
            {
                indicator.IsActive = true;
                if (request.ResetLastRun)
                {
                    indicator.LastRun = null; // Reset to make them due for execution
                }
                if (request.ResetRunningStatus)
                {
                    indicator.IsCurrentlyRunning = false;
                }
                indicator.UpdatedDate = DateTime.UtcNow;
                // Note: ModifiedBy property doesn't exist on Indicator entity
            }

            // Perform bulk update to avoid N+1 queries
            await _indicatorRepository.BulkUpdateAsync(indicators, cancellationToken);

            stopwatch.Stop();

            Logger.LogInformation("Activated {Count} Indicators", indicators.Count());

            var response = new BulkIndicatorExecutionResponse
            {
                TotalProcessed = indicators.Count(),
                SuccessCount = indicators.Count(),
                FailureCount = 0,
                TotalDurationMs = stopwatch.ElapsedMilliseconds,
                Context = "Activation",
                Results = indicators.Select(i => new IndicatorExecutionResponse
                {
                    IndicatorId = i.IndicatorID,
                    IndicatorName = i.IndicatorName,
                    Success = true,
                    DurationMs = 0,
                    Context = "Activation"
                }).ToList()
            };

            return Ok(CreateSuccessResponse(response, $"Successfully activated {indicators.Count()} indicators"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Indicator activation operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Indicator activation operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error activating Indicators");
            return StatusCode(500, CreateErrorResponse($"Failed to activate indicators: {ex.Message}", "INDICATOR_ACTIVATION_ERROR"));
        }
    }

    #region Helper Methods

    /// <summary>
    /// Gets all worker processes (both tracked and external)
    /// </summary>
    private List<Process> GetAllWorkerProcesses()
    {
        var processes = new List<Process>();

        try
        {
            // Add tracked process if it exists and is running
            if (_workerProcess != null && !_workerProcess.HasExited)
            {
                processes.Add(_workerProcess);
            }

            // Find external worker processes
            var externalProcesses = Process.GetProcesses()
                .Where(p =>
                {
                    try
                    {
                        return (p.ProcessName.Contains(WorkerConstants.Names.ProcessNamePattern, StringComparison.OrdinalIgnoreCase) ||
                               p.ProcessName.Contains(WorkerConstants.Names.MonitorProcessPattern, StringComparison.OrdinalIgnoreCase) ||
                               (p.MainModule?.FileName?.Contains(WorkerConstants.Names.WorkerNamespace, StringComparison.OrdinalIgnoreCase) == true)) &&
                               p.Id != Environment.ProcessId && // Exclude current API process
                               !processes.Any(tracked => tracked.Id == p.Id); // Exclude already tracked processes
                    }
                    catch
                    {
                        return false;
                    }
                })
                .ToList();

            processes.AddRange(externalProcesses);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error getting worker processes");
        }

        return processes;
    }

    /// <summary>
    /// Finds external worker processes (not tracked by this instance)
    /// </summary>
    private Process? FindExternalWorkerProcess()
    {
        try
        {
            return Process.GetProcesses()
                .FirstOrDefault(p =>
                {
                    try
                    {
                        return (p.ProcessName.Contains(WorkerConstants.Names.ProcessNamePattern, StringComparison.OrdinalIgnoreCase) ||
                               p.ProcessName.Contains(WorkerConstants.Names.MonitorProcessPattern, StringComparison.OrdinalIgnoreCase) ||
                               (p.MainModule?.FileName?.Contains(WorkerConstants.Names.WorkerNamespace, StringComparison.OrdinalIgnoreCase) == true)) &&
                               p.Id != Environment.ProcessId && // Exclude current API process
                               (_workerProcess == null || p.Id != _workerProcess.Id); // Exclude tracked process
                    }
                    catch
                    {
                        return false;
                    }
                });
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error finding external worker process");
            return null;
        }
    }

    #endregion

    /// <summary>
    /// Get worker cleanup status
    /// </summary>
    /// <param name="request">Cleanup status request parameters</param>
    /// <returns>Cleanup status information</returns>
    [HttpGet("cleanup-status")]
    [ProducesResponseType(typeof(CleanupResponse), StatusCodes.Status200OK)]
    public ActionResult<CleanupResponse> GetCleanupStatus([FromQuery] CleanupRequest? request = null)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new CleanupRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Check for running MonitoringGrid processes
            var monitoringProcesses = GetAllWorkerProcesses();

            stopwatch.Stop();

            var response = new CleanupResponse
            {
                Success = true,
                ProcessesCleanedUp = 0, // This is just status, not actual cleanup
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = monitoringProcesses.Select(p =>
                    $"Process {p.Id} ({p.ProcessName}) - Started: {p.StartTime}, Responding: {p.Responding}")
                    .ToList()
            };

            response.Details.Insert(0, $"Found {monitoringProcesses.Count} worker processes");

            Logger.LogDebug("Retrieved cleanup status: {ProcessCount} processes found", monitoringProcesses.Count);

            return Ok(CreateSuccessResponse(response, $"Found {monitoringProcesses.Count} worker processes"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error retrieving cleanup status");
            return StatusCode(500, CreateErrorResponse("Failed to retrieve cleanup status", "CLEANUP_STATUS_ERROR"));
        }
    }

    /// <summary>
    /// Force kill all worker processes (emergency cleanup)
    /// </summary>
    /// <param name="request">Force stop request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Worker operation result</returns>
    [HttpPost("force-stop")]
    [ProducesResponseType(typeof(WorkerOperationResponse), StatusCodes.Status200OK)]
    public ActionResult<WorkerOperationResponse> ForceStopWorkers(
        [FromBody] CleanupRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new CleanupRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            Logger.LogWarning("Force stop requested for all worker processes");

            var allWorkerProcesses = GetAllWorkerProcesses();
            if (allWorkerProcesses.Count == 0)
            {
                var noProcessResponse = new WorkerOperationResponse
                {
                    Success = true,
                    Message = WorkerConstants.Messages.NoWorkerProcessesToStop,
                    DurationMs = stopwatch.ElapsedMilliseconds
                };
                return Ok(CreateSuccessResponse(noProcessResponse, noProcessResponse.Message));
            }

            var killedProcesses = new List<int>();
            var failedProcesses = new List<int>();

            foreach (var process in allWorkerProcesses)
            {
                try
                {
                    var processId = process.Id;
                    Logger.LogWarning("Force killing Worker process with PID: {ProcessId}", processId);

                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true); // Kill entire process tree
                        process.WaitForExit(request.TimeoutMs);
                    }

                    killedProcesses.Add(processId);
                    Logger.LogInformation("Force killed Worker process with PID: {ProcessId}", processId);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to force kill Worker process with PID: {ProcessId}", process.Id);
                    failedProcesses.Add(process.Id);
                }
                finally
                {
                    SafeDisposeProcess(process);
                }
            }

            // Clean up tracked process reference
            CleanupTrackedProcess();

            stopwatch.Stop();

            var message = failedProcesses.Count > 0
                ? $"Force killed {killedProcesses.Count} process(es). Failed to kill {failedProcesses.Count} process(es)."
                : $"Successfully force killed {killedProcesses.Count} worker process(es)";

            var response = new WorkerOperationResponse
            {
                Success = failedProcesses.Count == 0,
                Message = message,
                ProcessId = killedProcesses.FirstOrDefault(),
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = new Dictionary<string, object>
                {
                    ["KilledProcessIds"] = killedProcesses,
                    ["FailedProcessIds"] = failedProcesses,
                    ["TotalKilled"] = killedProcesses.Count,
                    ["TotalFailed"] = failedProcesses.Count
                }
            };

            return Ok(CreateSuccessResponse(response, message));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error during force stop");
            return StatusCode(500, CreateErrorResponse($"Error during force stop: {ex.Message}", "FORCE_STOP_ERROR"));
        }
    }

    /// <summary>
    /// Debug indicator status for worker troubleshooting
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Debug information about indicators</returns>
    [HttpGet("debug-indicators")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DebugIndicators(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();

            // Get all indicators with their details
            var allIndicators = await _indicatorRepository.GetAllAsync(cancellationToken);

            // Get due indicators using the service
            var priorityFilter = new PriorityFilterOptions();
            var dueIndicators = await indicatorService.GetDueIndicatorsAsync(priorityFilter, cancellationToken);

            stopwatch.Stop();

            var debugInfo = new
            {
                Summary = new
                {
                    TotalIndicators = allIndicators.Count(),
                    ActiveIndicators = allIndicators.Count(i => i.IsActive),
                    InactiveIndicators = allIndicators.Count(i => !i.IsActive),
                    IndicatorsWithSchedulers = allIndicators.Count(i => i.Scheduler != null),
                    IndicatorsWithoutSchedulers = allIndicators.Count(i => i.Scheduler == null),
                    CurrentlyRunningIndicators = allIndicators.Count(i => i.IsCurrentlyRunning),
                    DueIndicators = dueIndicators.IsSuccess ? dueIndicators.Value.Count : 0
                },
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
                DueIndicatorNames = dueIndicators.IsSuccess ? dueIndicators.Value.Select(i => i.IndicatorName).ToList() : new List<string>(),
                Performance = new
                {
                    QueryDurationMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow
                }
            };

            Logger.LogDebug("Debug indicators completed: {TotalIndicators} indicators, {DueIndicators} due",
                allIndicators.Count(), dueIndicators.IsSuccess ? dueIndicators.Value.Count : 0);

            return Ok(CreateSuccessResponse(debugInfo, $"Retrieved debug information for {allIndicators.Count()} indicators"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Debug indicators operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Debug indicators operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error debugging indicators");
            return StatusCode(500, CreateErrorResponse($"Error debugging indicators: {ex.Message}", "DEBUG_INDICATORS_ERROR"));
        }
    }

    /// <summary>
    /// Assign schedulers to indicators
    /// </summary>
    /// <param name="request">Assign schedulers request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk indicator operation result</returns>
    [HttpPost("assign-schedulers")]
    [ProducesResponseType(typeof(BulkIndicatorExecutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkIndicatorExecutionResponse>> AssignSchedulers(
        [FromBody] AssignSchedulersRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new AssignSchedulersRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            using var scope = _serviceProvider.CreateScope();
            var schedulerRepository = scope.ServiceProvider.GetRequiredService<IRepository<MonitoringGrid.Core.Entities.Scheduler>>();

            // Get indicators to update
            var indicators = request.IndicatorIds?.Any() == true
                ? await _indicatorRepository.GetAsync(i => request.IndicatorIds.Contains(i.IndicatorID), cancellationToken)
                : await _indicatorRepository.GetAsync(i => i.SchedulerID == null || request.Force, cancellationToken);

            if (!indicators.Any())
            {
                return BadRequest(CreateErrorResponse(
                    request.IndicatorIds?.Any() == true
                        ? "No indicators found with the specified IDs"
                        : WorkerConstants.Messages.AllIndicatorsHaveSchedulers,
                    "NO_INDICATORS_TO_UPDATE"));
            }

            // Get or create scheduler
            MonitoringGrid.Core.Entities.Scheduler scheduler;
            if (request.SchedulerId.HasValue)
            {
                scheduler = await schedulerRepository.GetByIdAsync(request.SchedulerId.Value, cancellationToken);
                if (scheduler == null)
                {
                    return BadRequest(CreateErrorResponse($"Scheduler with ID {request.SchedulerId} not found", "SCHEDULER_NOT_FOUND"));
                }
            }
            else
            {
                // Create or get default scheduler
                scheduler = await schedulerRepository.GetFirstOrDefaultAsync(s => s.SchedulerName == WorkerConstants.Names.DefaultSchedulerName, cancellationToken);

                if (scheduler == null)
                {
                    scheduler = new MonitoringGrid.Core.Entities.Scheduler
                    {
                        SchedulerName = WorkerConstants.Names.DefaultSchedulerName,
                        SchedulerDescription = WorkerConstants.Defaults.SchedulerDescription,
                        ScheduleType = WorkerConstants.Defaults.ScheduleType,
                        IntervalMinutes = WorkerConstants.Defaults.SchedulerIntervalMinutes,
                        IsEnabled = true,
                        Timezone = WorkerConstants.Defaults.Timezone,
                        CreatedBy = WorkerConstants.Names.SystemUser,
                        ModifiedBy = WorkerConstants.Names.SystemUser
                    };

                    await schedulerRepository.AddAsync(scheduler, cancellationToken);
                    Logger.LogInformation("Created default scheduler with ID: {SchedulerID}", scheduler.SchedulerID);
                }
            }

            // Assign scheduler to indicators (bulk operation)
            foreach (var indicator in indicators)
            {
                indicator.SchedulerID = scheduler.SchedulerID;
                indicator.UpdatedDate = DateTime.UtcNow;
                // Note: ModifiedBy property doesn't exist on Indicator entity
            }

            // Perform bulk update to avoid N+1 queries
            await _indicatorRepository.BulkUpdateAsync(indicators, cancellationToken);

            stopwatch.Stop();

            Logger.LogInformation("Assigned scheduler {SchedulerID} to {Count} indicators", scheduler.SchedulerID, indicators.Count());

            var response = new BulkIndicatorExecutionResponse
            {
                TotalProcessed = indicators.Count(),
                SuccessCount = indicators.Count(),
                FailureCount = 0,
                TotalDurationMs = stopwatch.ElapsedMilliseconds,
                Context = "SchedulerAssignment",
                Results = indicators.Select(i => new IndicatorExecutionResponse
                {
                    IndicatorId = i.IndicatorID,
                    IndicatorName = i.IndicatorName,
                    Success = true,
                    DurationMs = 0,
                    Context = "SchedulerAssignment"
                }).ToList()
            };

            return Ok(CreateSuccessResponse(response, $"Successfully assigned scheduler to {indicators.Count()} indicators"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Assign schedulers operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Assign schedulers operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error assigning schedulers");
            return StatusCode(500, CreateErrorResponse($"Error assigning schedulers: {ex.Message}", "ASSIGN_SCHEDULERS_ERROR"));
        }
    }

    /// <summary>
    /// Manually trigger execution of a specific indicator
    /// </summary>
    /// <param name="indicatorId">Indicator ID to execute</param>
    /// <param name="request">Execute indicator request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Indicator execution result</returns>
    [HttpPost("execute-indicator/{indicatorId}")]
    [ProducesResponseType(typeof(IndicatorExecutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IndicatorExecutionResponse>> ExecuteIndicator(
        long indicatorId,
        [FromBody] ExecuteIndicatorRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new ExecuteIndicatorRequest { IndicatorId = indicatorId };

            // Validate indicator ID matches route parameter
            if (request.IndicatorId != indicatorId)
            {
                request.IndicatorId = indicatorId; // Use route parameter
            }

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            // Additional validation for indicator ID
            var paramValidation = ValidateParameter(indicatorId, nameof(indicatorId),
                id => id > 0, "Indicator ID must be a positive integer");
            if (paramValidation != null) return BadRequest(paramValidation);

            using var scope = _serviceProvider.CreateScope();
            var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

            Logger.LogInformation("Manual execution requested for indicator ID: {IndicatorID}", indicatorId);

            var result = await indicatorExecutionService.ExecuteIndicatorAsync(
                indicatorId,
                request.Context,
                saveResults: request.SaveResults,
                cancellationToken);

            stopwatch.Stop();

            var response = new IndicatorExecutionResponse
            {
                IndicatorId = indicatorId,
                IndicatorName = result.IndicatorName,
                Success = result.WasSuccessful,
                DurationMs = stopwatch.ElapsedMilliseconds,
                CurrentValue = result.Value,
                ThresholdBreached = result.ThresholdBreached,
                ErrorMessage = result.WasSuccessful ? null : result.ErrorMessage,
                Context = request.Context
            };

            if (result.WasSuccessful)
            {
                return Ok(CreateSuccessResponse(response, $"Indicator {indicatorId} executed successfully"));
            }
            else
            {
                return BadRequest(CreateErrorResponse($"Failed to execute indicator {indicatorId}: {result.ErrorMessage}", "INDICATOR_EXECUTION_FAILED"));
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Indicator execution operation was cancelled for indicator {IndicatorID}", indicatorId);
            return StatusCode(499, CreateErrorResponse("Indicator execution operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error executing indicator {IndicatorID}", indicatorId);
            return StatusCode(500, CreateErrorResponse($"Error executing indicator {indicatorId}: {ex.Message}", "INDICATOR_EXECUTION_ERROR"));
        }
    }

    /// <summary>
    /// Manually trigger execution of all due indicators
    /// </summary>
    /// <param name="request">Execute due indicators request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk indicator execution result</returns>
    [HttpPost("execute-due-indicators")]
    [ProducesResponseType(typeof(BulkIndicatorExecutionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkIndicatorExecutionResponse>> ExecuteDueIndicators(
        [FromBody] ExecuteDueIndicatorsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            request ??= new ExecuteDueIndicatorsRequest();

            var validationError = ValidateModelState();
            if (validationError != null) return BadRequest(validationError);

            using var scope = _serviceProvider.CreateScope();
            var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

            Logger.LogInformation("Manual execution of all due indicators requested. Context: {Context}, MaxCount: {MaxCount}",
                request.Context, request.MaxCount);

            var results = await indicatorExecutionService.ExecuteDueIndicatorsAsync(request.Context, cancellationToken);

            // Apply max count limit if specified
            if (request.MaxCount.HasValue && results.Count > request.MaxCount.Value)
            {
                results = results.Take(request.MaxCount.Value).ToList();
            }

            stopwatch.Stop();

            var successCount = results.Count(r => r.WasSuccessful);
            var failureCount = results.Count - successCount;

            var response = new BulkIndicatorExecutionResponse
            {
                TotalProcessed = results.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                TotalDurationMs = stopwatch.ElapsedMilliseconds,
                Context = request.Context,
                Results = results.Select(r => new IndicatorExecutionResponse
                {
                    IndicatorId = r.IndicatorId,
                    IndicatorName = r.IndicatorName,
                    Success = r.WasSuccessful,
                    DurationMs = (long)r.ExecutionDuration.TotalMilliseconds,
                    CurrentValue = r.Value,
                    ThresholdBreached = r.ThresholdBreached,
                    ErrorMessage = r.WasSuccessful ? null : r.ErrorMessage,
                    Context = request.Context
                }).ToList()
            };

            Logger.LogInformation("Executed {TotalProcessed} due indicators: {SuccessCount} successful, {FailureCount} failed",
                results.Count, successCount, failureCount);

            return Ok(CreateSuccessResponse(response, $"Executed {results.Count} due indicators: {successCount} successful, {failureCount} failed"));
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Execute due indicators operation was cancelled");
            return StatusCode(499, CreateErrorResponse("Execute due indicators operation was cancelled", "OPERATION_CANCELLED"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Error executing due indicators");
            return StatusCode(500, CreateErrorResponse($"Error executing due indicators: {ex.Message}", "EXECUTE_DUE_INDICATORS_ERROR"));
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
            Logger.LogInformation("🧪 Testing worker logic directly...");

            using var scope = _serviceProvider.CreateScope();
            var indicatorService = scope.ServiceProvider.GetRequiredService<IIndicatorService>();
            var indicatorExecutionService = scope.ServiceProvider.GetRequiredService<IIndicatorExecutionService>();

            Logger.LogInformation("✅ Services resolved successfully");

            // Get due indicators
            Logger.LogInformation("🔍 Getting due indicators...");
            var priorityFilter = new PriorityFilterOptions();
            var dueIndicatorsResult = await indicatorService.GetDueIndicatorsAsync(priorityFilter, CancellationToken.None);
            var dueIndicators = dueIndicatorsResult.IsSuccess ? dueIndicatorsResult.Value : new List<Indicator>();
            Logger.LogInformation("📊 Found {Count} due indicators", dueIndicators.Count());

            var results = new List<object>();

            foreach (var indicator in dueIndicators)
            {
                Logger.LogInformation("🚀 Executing indicator {IndicatorId}: {IndicatorName}",
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
                    currentValue = result.Value,
                    errorMessage = result.ErrorMessage
                });

                Logger.LogInformation("✅ Completed indicator {IndicatorId}: Success={Success}, Duration={Duration}ms",
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
            Logger.LogError(ex, "❌ Error testing worker logic");
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
            Logger.LogInformation("Manual worker cleanup requested");

            // Use the same cleanup logic as the application shutdown handler
            MonitoringGrid.Api.Extensions.ApplicationLifetimeExtensions.TriggerManualCleanup(Logger);

            return Ok(new
            {
                success = true,
                message = "Worker cleanup completed",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during manual worker cleanup");
            return StatusCode(500, new { success = false, message = "Failed to cleanup workers" });
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


}
