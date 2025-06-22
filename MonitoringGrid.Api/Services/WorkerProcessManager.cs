using System.Diagnostics;
using System.Text.Json;
using MonitoringGrid.Api.DTOs.Worker;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Service for managing actual worker processes
/// </summary>
public interface IWorkerProcessManager
{
    /// <summary>
    /// Start a new worker process
    /// </summary>
    Task<WorkerProcessInfo> StartWorkerProcessAsync(StartWorkerProcessRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop a worker process
    /// </summary>
    Task<bool> StopWorkerProcessAsync(string workerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get status of a worker process
    /// </summary>
    Task<WorkerProcessStatus?> GetWorkerProcessStatusAsync(string workerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active worker processes
    /// </summary>
    Task<List<WorkerProcessInfo>> GetActiveWorkerProcessesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Kill all worker processes (emergency cleanup)
    /// </summary>
    Task KillAllWorkerProcessesAsync();
}

/// <summary>
/// Implementation of worker process manager
/// </summary>
public class WorkerProcessManager : IWorkerProcessManager, IDisposable
{
    private readonly ILogger<WorkerProcessManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, WorkerProcessInfo> _activeProcesses = new();
    private readonly Timer _monitoringTimer;
    private string? _workerExecutablePath;
    private readonly string _statusFilesDirectory;

    public WorkerProcessManager(ILogger<WorkerProcessManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Don't determine worker executable path during construction to avoid startup failures
        // It will be resolved lazily when first needed
        _workerExecutablePath = null;

        // Create status files directory
        _statusFilesDirectory = Path.Combine(Path.GetTempPath(), "MonitoringGrid", "WorkerStatus");
        Directory.CreateDirectory(_statusFilesDirectory);

        // Start monitoring timer
        _monitoringTimer = new Timer(MonitorWorkerProcesses, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        _logger.LogInformation("Worker Process Manager initialized. Status Directory: {StatusDirectory}", _statusFilesDirectory);
    }

    public async Task<WorkerProcessInfo> StartWorkerProcessAsync(StartWorkerProcessRequest request, CancellationToken cancellationToken = default)
    {
        var workerId = request.WorkerId ?? Guid.NewGuid().ToString("N")[..8];
        var statusFilePath = Path.Combine(_statusFilesDirectory, $"worker-{workerId}.json");

        _logger.LogInformation("Starting worker process {WorkerId}", workerId);

        try
        {
            // Build command line arguments
            var arguments = BuildWorkerArguments(workerId, request, statusFilePath);
            
            _logger.LogDebug("Starting worker with arguments: {Arguments}", arguments);

            // Get worker executable path (lazy initialization)
            var workerExecutablePath = GetWorkerExecutablePathLazy();

            // Start the process
            var processStartInfo = new ProcessStartInfo
            {
                FileName = workerExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(workerExecutablePath)
            };

            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start worker process");
            }

            var workerInfo = new WorkerProcessInfo
            {
                WorkerId = workerId,
                ProcessId = process.Id,
                Process = process,
                StartTime = DateTime.UtcNow,
                StatusFilePath = statusFilePath,
                TestType = request.TestType,
                IndicatorIds = request.IndicatorIds ?? new List<int>(),
                DurationSeconds = request.DurationSeconds,
                IsTestMode = request.TestMode
            };

            // Store in active processes
            lock (_activeProcesses)
            {
                _activeProcesses[workerId] = workerInfo;
            }

            _logger.LogInformation("✅ Worker process {WorkerId} started with PID {ProcessId}", workerId, process.Id);

            return workerInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to start worker process {WorkerId}", workerId);
            throw;
        }
    }

    public async Task<bool> StopWorkerProcessAsync(string workerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping worker process {WorkerId}", workerId);

        WorkerProcessInfo? workerInfo;
        lock (_activeProcesses)
        {
            if (!_activeProcesses.TryGetValue(workerId, out workerInfo))
            {
                _logger.LogWarning("Worker process {WorkerId} not found", workerId);
                return false;
            }
        }

        try
        {
            if (!workerInfo.Process.HasExited)
            {
                // Try graceful shutdown first
                workerInfo.Process.CloseMainWindow();
                
                // Wait for graceful shutdown
                if (!workerInfo.Process.WaitForExit(5000))
                {
                    // Force kill if graceful shutdown fails
                    _logger.LogWarning("Worker {WorkerId} did not exit gracefully, forcing termination", workerId);
                    workerInfo.Process.Kill();
                    workerInfo.Process.WaitForExit(2000);
                }
            }

            // Clean up
            lock (_activeProcesses)
            {
                _activeProcesses.Remove(workerId);
            }

            // Clean up status file
            if (File.Exists(workerInfo.StatusFilePath))
            {
                try
                {
                    File.Delete(workerInfo.StatusFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete status file for worker {WorkerId}", workerId);
                }
            }

            _logger.LogInformation("✅ Worker process {WorkerId} stopped", workerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error stopping worker process {WorkerId}", workerId);
            return false;
        }
    }

    public async Task<WorkerProcessStatus?> GetWorkerProcessStatusAsync(string workerId, CancellationToken cancellationToken = default)
    {
        WorkerProcessInfo? workerInfo;
        lock (_activeProcesses)
        {
            if (!_activeProcesses.TryGetValue(workerId, out workerInfo))
            {
                return null;
            }
        }

        // Read status from file
        if (File.Exists(workerInfo.StatusFilePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(workerInfo.StatusFilePath, cancellationToken);
                var status = JsonSerializer.Deserialize<WorkerProcessStatus>(json);
                
                // Update with process info
                if (status != null)
                {
                    status.ProcessId = workerInfo.ProcessId;
                    status.IsRunning = !workerInfo.Process.HasExited;
                    
                    if (!workerInfo.Process.HasExited)
                    {
                        workerInfo.Process.Refresh();
                        status.MemoryUsageBytes = workerInfo.Process.WorkingSet64;
                    }
                }
                
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read status file for worker {WorkerId}", workerId);
            }
        }

        // Return basic status if file not available
        return new WorkerProcessStatus
        {
            WorkerId = workerId,
            ProcessId = workerInfo.ProcessId,
            IsRunning = !workerInfo.Process.HasExited,
            State = workerInfo.Process.HasExited ? "Stopped" : "Running",
            StartTime = workerInfo.StartTime,
            LastUpdate = DateTime.UtcNow,
            Message = "Status file not available"
        };
    }

    public async Task<List<WorkerProcessInfo>> GetActiveWorkerProcessesAsync(CancellationToken cancellationToken = default)
    {
        lock (_activeProcesses)
        {
            return _activeProcesses.Values.ToList();
        }
    }

    public async Task KillAllWorkerProcessesAsync()
    {
        _logger.LogWarning("🚨 Emergency shutdown: Killing all worker processes");

        List<WorkerProcessInfo> processesToKill;
        lock (_activeProcesses)
        {
            processesToKill = _activeProcesses.Values.ToList();
            _activeProcesses.Clear();
        }

        foreach (var workerInfo in processesToKill)
        {
            try
            {
                if (!workerInfo.Process.HasExited)
                {
                    workerInfo.Process.Kill();
                    workerInfo.Process.WaitForExit(2000);
                }
                
                _logger.LogInformation("Killed worker process {WorkerId} (PID: {ProcessId})", 
                    workerInfo.WorkerId, workerInfo.ProcessId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill worker process {WorkerId}", workerInfo.WorkerId);
            }
        }

        // Clean up status files
        try
        {
            if (Directory.Exists(_statusFilesDirectory))
            {
                Directory.Delete(_statusFilesDirectory, true);
                Directory.CreateDirectory(_statusFilesDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up status files directory");
        }
    }

    private string GetWorkerExecutablePathLazy()
    {
        if (_workerExecutablePath == null)
        {
            _workerExecutablePath = GetWorkerExecutablePath();
        }
        return _workerExecutablePath;
    }

    private string GetWorkerExecutablePath()
    {
        _logger.LogInformation("Looking for worker executable. AppContext.BaseDirectory: {BaseDirectory}", AppContext.BaseDirectory);

        // Try to find the standalone worker executable
        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "MonitoringGrid.StandaloneWorker.exe"),
            Path.Combine(AppContext.BaseDirectory, "..", "MonitoringGrid.StandaloneWorker", "bin", "Debug", "net8.0", "win-x64", "MonitoringGrid.StandaloneWorker.exe"),
            Path.Combine(AppContext.BaseDirectory, "..", "MonitoringGrid.StandaloneWorker", "bin", "Release", "net8.0", "win-x64", "MonitoringGrid.StandaloneWorker.exe"),
            Path.Combine(AppContext.BaseDirectory, "..", "MonitoringGrid.StandaloneWorker", "bin", "Debug", "net8.0", "MonitoringGrid.StandaloneWorker.exe"),
            Path.Combine(AppContext.BaseDirectory, "..", "MonitoringGrid.StandaloneWorker", "bin", "Release", "net8.0", "MonitoringGrid.StandaloneWorker.exe"),
            "MonitoringGrid.StandaloneWorker.exe" // Assume it's in PATH
        };

        foreach (var path in possiblePaths)
        {
            _logger.LogDebug("Checking path: {Path}", path);
            if (File.Exists(path))
            {
                _logger.LogInformation("Found worker executable at: {Path}", path);
                return Path.GetFullPath(path);
            }
        }

        // If not found, use dotnet run as fallback
        var projectPath = Path.Combine(AppContext.BaseDirectory, "..", "MonitoringGrid.StandaloneWorker", "MonitoringGrid.StandaloneWorker.csproj");
        _logger.LogInformation("Checking for project file at: {ProjectPath}", projectPath);

        if (File.Exists(projectPath))
        {
            _logger.LogWarning("Standalone worker executable not found, will use 'dotnet run' as fallback");
            return "dotnet";
        }

        _logger.LogError("Could not find MonitoringGrid.StandaloneWorker executable or project file");
        throw new FileNotFoundException("Could not find MonitoringGrid.StandaloneWorker executable");
    }

    private string BuildWorkerArguments(string workerId, StartWorkerProcessRequest request, string statusFilePath)
    {
        var args = new List<string>
        {
            "--worker-id", workerId,
            "--status-file", $"\"{statusFilePath}\"",
            "--api-url", _configuration.GetValue<string>("MonitoringGrid:ApiBaseUrl") ?? "http://localhost:57653"
        };

        if (request.TestMode)
        {
            args.Add("--test-mode");
        }

        if (request.DurationSeconds.HasValue && request.DurationSeconds > 0)
        {
            args.AddRange(new[] { "--duration", request.DurationSeconds.Value.ToString() });
        }

        if (request.IndicatorIds?.Any() == true)
        {
            args.AddRange(new[] { "--indicator-ids", string.Join(",", request.IndicatorIds) });
        }

        // If using dotnet run, prepend the run command and project path
        var workerExecutablePath = GetWorkerExecutablePathLazy();
        if (workerExecutablePath == "dotnet")
        {
            var projectPath = Path.Combine(AppContext.BaseDirectory, "..", "MonitoringGrid.StandaloneWorker");
            return $"run --project \"{projectPath}\" -- {string.Join(" ", args)}";
        }

        return string.Join(" ", args);
    }

    private async void MonitorWorkerProcesses(object? state)
    {
        try
        {
            List<WorkerProcessInfo> processesToCheck;
            lock (_activeProcesses)
            {
                processesToCheck = _activeProcesses.Values.ToList();
            }

            foreach (var workerInfo in processesToCheck)
            {
                try
                {
                    if (workerInfo.Process.HasExited)
                    {
                        _logger.LogInformation("Worker process {WorkerId} has exited with code {ExitCode}", 
                            workerInfo.WorkerId, workerInfo.Process.ExitCode);

                        lock (_activeProcesses)
                        {
                            _activeProcesses.Remove(workerInfo.WorkerId);
                        }

                        // Clean up status file
                        if (File.Exists(workerInfo.StatusFilePath))
                        {
                            try
                            {
                                File.Delete(workerInfo.StatusFilePath);
                            }
                            catch { /* Ignore cleanup errors */ }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error monitoring worker process {WorkerId}", workerInfo.WorkerId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in worker process monitoring");
        }
    }

    public void Dispose()
    {
        _monitoringTimer?.Dispose();
        
        // Clean up all processes
        Task.Run(async () => await KillAllWorkerProcessesAsync()).Wait(5000);
    }
}
