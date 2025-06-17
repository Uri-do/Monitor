using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;

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
    Task<IndicatorExecutionResult> ExecuteIndicatorAsync(long indicatorId, string executionContext = "Manual",
        bool saveResults = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute multiple indicators
    /// </summary>
    Task<List<IndicatorExecutionResult>> ExecuteIndicatorsAsync(List<long> indicatorIds, string executionContext = "Scheduled",
        bool saveResults = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test indicator execution without saving results
    /// </summary>
    Task<IndicatorExecutionResult> TestIndicatorAsync(long indicatorId, int? overrideLastMinutes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute all due indicators
    /// </summary>
    Task<List<IndicatorExecutionResult>> ExecuteDueIndicatorsAsync(string executionContext = "Scheduled", 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicator execution status
    /// </summary>
    Task<IndicatorExecutionStatus> GetIndicatorExecutionStatusAsync(long indicatorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel indicator execution if running
    /// </summary>
    Task<bool> CancelIndicatorExecutionAsync(long indicatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get execution history for an indicator
    /// </summary>
    Task<List<IndicatorExecutionHistory>> GetIndicatorExecutionHistoryAsync(long indicatorId, int days = 30,
        CancellationToken cancellationToken = default);
}






