using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using MonitoringGrid.StandaloneWorker.Models;
using System.Diagnostics;
using System.Text.Json;

namespace MonitoringGrid.StandaloneWorker.Services;

/// <summary>
/// Service for reporting worker status via multiple channels
/// </summary>
public class WorkerStatusReporter : IWorkerStatusReporter, IDisposable
{
    private readonly ILogger<WorkerStatusReporter> _logger;
    private readonly StandaloneWorkerConfig _config;
    private readonly Timer _statusTimer;
    private WorkerStatus _currentStatus;
    private HubConnection? _hubConnection;
    private readonly Process _currentProcess;

    public WorkerStatusReporter(ILogger<WorkerStatusReporter> logger, StandaloneWorkerConfig config)
    {
        _logger = logger;
        _config = config;
        _currentProcess = Process.GetCurrentProcess();

        _currentStatus = new WorkerStatus
        {
            WorkerId = config.WorkerId,
            State = WorkerState.Starting,
            StartTime = DateTime.UtcNow,
            ProcessId = _currentProcess.Id,
            Message = "Worker initializing"
        };

        // Start periodic status updates
        _statusTimer = new Timer(async _ => await UpdateSystemMetricsAndReport(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

        // Initialize SignalR connection
        _ = Task.Run(InitializeSignalRAsync);
    }

    public async Task ReportStatusAsync(WorkerStatus status)
    {
        _currentStatus = status;
        _currentStatus.LastUpdate = DateTime.UtcNow;
        _currentStatus.ProcessId = _currentProcess.Id;

        // Update system metrics
        UpdateSystemMetrics();

        // Write to status file
        await WriteStatusFileAsync();

        // Send via SignalR
        await SendSignalRUpdateAsync();

        // Log status
        _logger.LogInformation("Worker {WorkerId} status: {State} - {Message}", 
            status.WorkerId, status.State, status.Message);
    }

    public async Task UpdateStateAsync(WorkerState state, string message, string? activity = null)
    {
        _currentStatus.State = state;
        _currentStatus.Message = message;
        _currentStatus.CurrentActivity = activity;
        
        await ReportStatusAsync(_currentStatus);
    }

    public async Task ReportIndicatorProgressAsync(int indicatorId, string indicatorName, int progress, string step)
    {
        _currentStatus.CurrentActivity = $"Processing {indicatorName} ({progress}%): {step}";
        
        // Send specific progress update via SignalR
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("WorkerTestProgress", new
                {
                    TestId = _config.WorkerId,
                    WorkerId = _config.WorkerId,
                    IndicatorId = indicatorId,
                    IndicatorName = indicatorName,
                    Progress = progress,
                    Step = step,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send indicator progress via SignalR");
            }
        }

        await ReportStatusAsync(_currentStatus);
    }

    public async Task ReportIndicatorCompletionAsync(int indicatorId, string indicatorName, bool success, string? errorMessage = null)
    {
        if (success)
        {
            _currentStatus.SuccessfulExecutions++;
            _currentStatus.CurrentActivity = $"Completed {indicatorName} successfully";
        }
        else
        {
            _currentStatus.FailedExecutions++;
            _currentStatus.CurrentActivity = $"Failed {indicatorName}: {errorMessage}";
        }

        _currentStatus.IndicatorsProcessed++;

        // Send completion update via SignalR
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("IndicatorTestResult", new
                {
                    TestId = _config.WorkerId,
                    WorkerId = _config.WorkerId,
                    IndicatorId = indicatorId,
                    IndicatorName = indicatorName,
                    Success = success,
                    ErrorMessage = errorMessage,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send indicator completion via SignalR");
            }
        }

        await ReportStatusAsync(_currentStatus);
    }

    public WorkerStatus GetCurrentStatus()
    {
        UpdateSystemMetrics();
        return _currentStatus;
    }

    private void UpdateSystemMetrics()
    {
        try
        {
            _currentProcess.Refresh();
            _currentStatus.MemoryUsageBytes = _currentProcess.WorkingSet64;
            
            // Calculate time remaining if duration is set
            if (_config.DurationSeconds > 0)
            {
                var elapsed = DateTime.UtcNow - _currentStatus.StartTime;
                var remaining = TimeSpan.FromSeconds(_config.DurationSeconds) - elapsed;
                _currentStatus.TimeRemaining = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update system metrics");
        }
    }

    private async Task UpdateSystemMetricsAndReport()
    {
        UpdateSystemMetrics();
        await WriteStatusFileAsync();
        await SendSignalRUpdateAsync();
    }

    private async Task WriteStatusFileAsync()
    {
        if (string.IsNullOrEmpty(_config.StatusFilePath))
            return;

        try
        {
            var json = JsonSerializer.Serialize(_currentStatus, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_config.StatusFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write status file");
        }
    }

    private async Task SendSignalRUpdateAsync()
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            await _hubConnection.InvokeAsync("WorkerTestProgress", new
            {
                TestId = _config.WorkerId,
                WorkerId = _config.WorkerId,
                Status = _currentStatus.State.ToString(),
                Message = _currentStatus.Message,
                Activity = _currentStatus.CurrentActivity,
                IndicatorsProcessed = _currentStatus.IndicatorsProcessed,
                SuccessfulExecutions = _currentStatus.SuccessfulExecutions,
                FailedExecutions = _currentStatus.FailedExecutions,
                MemoryUsageBytes = _currentStatus.MemoryUsageBytes,
                ProcessId = _currentStatus.ProcessId,
                IsHealthy = _currentStatus.IsHealthy,
                TimeRemaining = _currentStatus.TimeRemaining?.TotalSeconds,
                Timestamp = _currentStatus.LastUpdate
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send status via SignalR");
        }
    }

    private async Task InitializeSignalRAsync()
    {
        try
        {
            var hubUrl = $"{_config.ApiBaseUrl}/monitoring-hub";
            _logger.LogInformation("Connecting to SignalR hub at {HubUrl}", hubUrl);

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.Closed += async (error) =>
            {
                _logger.LogWarning("SignalR connection closed: {Error}", error?.Message);
                await Task.Delay(5000);
            };

            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connection established");

            // Join worker test group
            await _hubConnection.InvokeAsync("JoinWorkerTestGroup", _config.WorkerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize SignalR connection");
        }
    }

    public void Dispose()
    {
        _statusTimer?.Dispose();
        _hubConnection?.DisposeAsync().AsTask().Wait(5000);
        _currentProcess?.Dispose();
    }
}
