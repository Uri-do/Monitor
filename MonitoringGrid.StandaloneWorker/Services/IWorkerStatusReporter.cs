using MonitoringGrid.StandaloneWorker.Models;

namespace MonitoringGrid.StandaloneWorker.Services;

/// <summary>
/// Interface for reporting worker status
/// </summary>
public interface IWorkerStatusReporter
{
    /// <summary>
    /// Report current worker status
    /// </summary>
    Task ReportStatusAsync(WorkerStatus status);

    /// <summary>
    /// Update worker state
    /// </summary>
    Task UpdateStateAsync(WorkerState state, string message, string? activity = null);

    /// <summary>
    /// Report indicator execution progress
    /// </summary>
    Task ReportIndicatorProgressAsync(int indicatorId, string indicatorName, int progress, string step);

    /// <summary>
    /// Report indicator execution completion
    /// </summary>
    Task ReportIndicatorCompletionAsync(int indicatorId, string indicatorName, bool success, string? errorMessage = null);

    /// <summary>
    /// Get current worker status
    /// </summary>
    WorkerStatus GetCurrentStatus();
}
