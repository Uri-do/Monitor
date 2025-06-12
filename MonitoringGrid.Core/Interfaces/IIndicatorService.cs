using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service interface for Indicator management operations
/// Replaces IKpiService
/// </summary>
public interface IIndicatorService
{
    /// <summary>
    /// Get all indicators
    /// </summary>
    Task<List<Indicator>> GetAllIndicatorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicator by ID
    /// </summary>
    Task<Indicator?> GetIndicatorByIdAsync(int indicatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active indicators
    /// </summary>
    Task<List<Indicator>> GetActiveIndicatorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicators by owner contact ID
    /// </summary>
    Task<List<Indicator>> GetIndicatorsByOwnerAsync(int ownerContactId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicators by priority
    /// </summary>
    Task<List<Indicator>> GetIndicatorsByPriorityAsync(string priority, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicators that are due for execution
    /// </summary>
    Task<List<Indicator>> GetDueIndicatorsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new indicator
    /// </summary>
    Task<Indicator> CreateIndicatorAsync(Indicator indicator, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing indicator
    /// </summary>
    Task<Indicator> UpdateIndicatorAsync(Indicator indicator, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an indicator
    /// </summary>
    Task<bool> DeleteIndicatorAsync(int indicatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add contacts to an indicator
    /// </summary>
    Task<bool> AddContactsToIndicatorAsync(int indicatorId, List<int> contactIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove contacts from an indicator
    /// </summary>
    Task<bool> RemoveContactsFromIndicatorAsync(int indicatorId, List<int> contactIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicator execution history
    /// </summary>
    Task<List<HistoricalData>> GetIndicatorHistoryAsync(int indicatorId, int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicator dashboard data
    /// </summary>
    Task<IndicatorDashboard> GetIndicatorDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Test indicator execution without saving results
    /// </summary>
    Task<IndicatorTestResult> TestIndicatorAsync(int indicatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get indicator statistics
    /// </summary>
    Task<IndicatorStatistics> GetIndicatorStatisticsAsync(int indicatorId, int days = 30, CancellationToken cancellationToken = default);
}

/// <summary>
/// Dashboard data for indicators
/// </summary>
public class IndicatorDashboard
{
    public int TotalIndicators { get; set; }
    public int ActiveIndicators { get; set; }
    public int DueIndicators { get; set; }
    public int RunningIndicators { get; set; }
    public int IndicatorsExecutedToday { get; set; }
    public int AlertsTriggeredToday { get; set; }
    public List<IndicatorExecutionSummary> RecentExecutions { get; set; } = new();
    public List<IndicatorCountByPriority> CountByPriority { get; set; } = new();
}

/// <summary>
/// Indicator execution summary
/// </summary>
public class IndicatorExecutionSummary
{
    public int IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; }
    public bool WasSuccessful { get; set; }
    public decimal? Value { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Indicator count by priority
/// </summary>
public class IndicatorCountByPriority
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Indicator test result
/// </summary>
public class IndicatorTestResult
{
    public bool WasSuccessful { get; set; }
    public decimal? CurrentValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public bool ThresholdBreached { get; set; }
    public string? ErrorMessage { get; set; }
    public List<CollectorStatisticDto> RawData { get; set; } = new();
    public TimeSpan ExecutionDuration { get; set; }
}

/// <summary>
/// Indicator statistics
/// </summary>
public class IndicatorStatistics
{
    public int IndicatorId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int AlertsTriggered { get; set; }
    public decimal? AverageValue { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public DateTime? LastExecution { get; set; }
    public DateTime? NextExecution { get; set; }
    public List<IndicatorValueTrend> ValueTrend { get; set; } = new();
}

/// <summary>
/// Indicator value trend data
/// </summary>
public class IndicatorValueTrend
{
    public DateTime Timestamp { get; set; }
    public decimal Value { get; set; }
    public bool AlertTriggered { get; set; }
}
