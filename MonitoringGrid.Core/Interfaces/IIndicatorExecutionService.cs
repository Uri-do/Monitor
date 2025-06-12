using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service interface for indicator execution operations
/// Replaces IKpiExecutionService
/// </summary>
public interface IIndicatorExecutionService
{
    /// <summary>
    /// Execute a single indicator
    /// </summary>
    Task<IndicatorExecutionResult> ExecuteIndicatorAsync(int indicatorId, string executionContext = "Manual", 
        bool saveResults = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute multiple indicators
    /// </summary>
    Task<List<IndicatorExecutionResult>> ExecuteIndicatorsAsync(List<int> indicatorIds, string executionContext = "Scheduled", 
        bool saveResults = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test indicator execution without saving results
    /// </summary>
    Task<IndicatorExecutionResult> TestIndicatorAsync(int indicatorId, int? overrideLastMinutes = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute all due indicators
    /// </summary>
    Task<List<IndicatorExecutionResult>> ExecuteDueIndicatorsAsync(string executionContext = "Scheduled", 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicator execution status
    /// </summary>
    Task<IndicatorExecutionStatus> GetIndicatorExecutionStatusAsync(int indicatorId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel indicator execution if running
    /// </summary>
    Task<bool> CancelIndicatorExecutionAsync(int indicatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get execution history for an indicator
    /// </summary>
    Task<List<IndicatorExecutionHistory>> GetIndicatorExecutionHistoryAsync(int indicatorId, int days = 30, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of indicator execution
/// </summary>
public class IndicatorExecutionResult
{
    public int IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public bool WasSuccessful { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public bool ThresholdBreached { get; set; }
    public string? ErrorMessage { get; set; }
    public List<CollectorStatisticDto> RawData { get; set; } = new();
    public TimeSpan ExecutionDuration { get; set; }
    public DateTime ExecutionTime { get; set; }
    public string ExecutionContext { get; set; } = string.Empty;
    public decimal? HistoricalAverage { get; set; }
    public string? AlertMessage { get; set; }
}

/// <summary>
/// Indicator execution status
/// </summary>
public class IndicatorExecutionStatus
{
    public int IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public bool IsCurrentlyRunning { get; set; }
    public DateTime? ExecutionStartTime { get; set; }
    public string? ExecutionContext { get; set; }
    public TimeSpan? ExecutionDuration { get; set; }
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
    public string Status { get; set; } = string.Empty; // "idle", "running", "error", "completed"
}

/// <summary>
/// Indicator execution history entry
/// </summary>
public class IndicatorExecutionHistory
{
    public int HistoryId { get; set; }
    public int IndicatorId { get; set; }
    public DateTime ExecutionTime { get; set; }
    public bool WasSuccessful { get; set; }
    public decimal? Value { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionDuration { get; set; }
    public string ExecutionContext { get; set; } = string.Empty;
    public bool AlertTriggered { get; set; }
}
