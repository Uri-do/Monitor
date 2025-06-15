using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service interface for managing indicator execution history
/// </summary>
public interface IExecutionHistoryService
{
    /// <summary>
    /// Get execution history for an indicator
    /// </summary>
    Task<Result<List<IndicatorExecutionHistoryDto>>> GetExecutionHistoryAsync(long indicatorId, int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get execution history with pagination
    /// </summary>
    Task<Result<PaginatedExecutionHistoryDto>> GetExecutionHistoryPagedAsync(long indicatorId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get execution statistics for an indicator
    /// </summary>
    Task<Result<ExecutionStatisticsDto>> GetExecutionStatisticsAsync(long indicatorId, int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save execution result to history
    /// </summary>
    Task<Result<bool>> SaveExecutionResultAsync(SaveExecutionResultRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent executions across all indicators
    /// </summary>
    Task<Result<List<IndicatorExecutionHistoryDto>>> GetRecentExecutionsAsync(int count = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get execution trends for an indicator
    /// </summary>
    Task<Result<List<ExecutionTrendDto>>> GetExecutionTrendsAsync(long indicatorId, int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up old execution history
    /// </summary>
    Task<Result<int>> CleanupOldHistoryAsync(int daysToKeep = 90, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get execution summary for dashboard
    /// </summary>
    Task<Result<ExecutionSummaryDto>> GetExecutionSummaryAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO for indicator execution history
/// </summary>
public class IndicatorExecutionHistoryDto
{
    public int HistoryId { get; set; }
    public long IndicatorID { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; }
    public bool WasSuccessful { get; set; }
    public decimal? Value { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionDuration { get; set; }
    public string ExecutionContext { get; set; } = string.Empty;
    public bool AlertTriggered { get; set; }
    public string? AdditionalData { get; set; }
}

/// <summary>
/// Paginated execution history response
/// </summary>
public class PaginatedExecutionHistoryDto
{
    public List<IndicatorExecutionHistoryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>
/// Execution statistics DTO
/// </summary>
public class ExecutionStatisticsDto
{
    public long IndicatorID { get; set; }
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? LastSuccessfulExecution { get; set; }
    public int AlertsTriggered { get; set; }
}

/// <summary>
/// Request for saving execution result
/// </summary>
public class SaveExecutionResultRequest
{
    public long IndicatorID { get; set; }
    public bool WasSuccessful { get; set; }
    public decimal? Value { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionDuration { get; set; }
    public string ExecutionContext { get; set; } = "Manual";
    public bool AlertTriggered { get; set; }
    public string? AdditionalData { get; set; }
}

/// <summary>
/// Execution trend data point
/// </summary>
public class ExecutionTrendDto
{
    public DateTime Date { get; set; }
    public decimal? AverageValue { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public int ExecutionCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

/// <summary>
/// Execution summary for dashboard
/// </summary>
public class ExecutionSummaryDto
{
    public int TotalExecutionsToday { get; set; }
    public int SuccessfulExecutionsToday { get; set; }
    public int FailedExecutionsToday { get; set; }
    public double TodaySuccessRate { get; set; }
    public int ActiveIndicators { get; set; }
    public int AlertsTriggeredToday { get; set; }
    public TimeSpan AverageExecutionTime { get; set; }
}
