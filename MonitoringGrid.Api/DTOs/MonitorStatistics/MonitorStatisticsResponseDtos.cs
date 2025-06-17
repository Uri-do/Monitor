using MonitoringGrid.Api.DTOs.Common;

namespace MonitoringGrid.Api.DTOs.MonitorStatistics;

/// <summary>
/// Enhanced collector response DTO
/// </summary>
public class CollectorResponse
{
    /// <summary>
    /// Internal ID
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// Collector ID
    /// </summary>
    public long CollectorID { get; set; }

    /// <summary>
    /// Collector code
    /// </summary>
    public string CollectorCode { get; set; } = string.Empty;

    /// <summary>
    /// Collector description
    /// </summary>
    public string CollectorDesc { get; set; } = string.Empty;

    /// <summary>
    /// Frequency in minutes
    /// </summary>
    public int FrequencyMinutes { get; set; }

    /// <summary>
    /// Last minutes to consider
    /// </summary>
    public int LastMinutes { get; set; }

    /// <summary>
    /// Stored procedure name
    /// </summary>
    public string StoreProcedure { get; set; } = string.Empty;

    /// <summary>
    /// Whether the collector is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Last updated date
    /// </summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Last run timestamp
    /// </summary>
    public DateTime? LastRun { get; set; }

    /// <summary>
    /// Last run result
    /// </summary>
    public string? LastRunResult { get; set; }

    /// <summary>
    /// Performance metrics (if requested)
    /// </summary>
    public CollectorMetrics? Metrics { get; set; }

    /// <summary>
    /// Additional details (if requested)
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Collector performance metrics
/// </summary>
public class CollectorMetrics
{
    /// <summary>
    /// Average execution time in milliseconds
    /// </summary>
    public double? AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double? SuccessRate { get; set; }

    /// <summary>
    /// Total executions in the analyzed period
    /// </summary>
    public long? TotalExecutions { get; set; }

    /// <summary>
    /// Failed executions count
    /// </summary>
    public long? FailedExecutions { get; set; }

    /// <summary>
    /// Last successful execution
    /// </summary>
    public DateTime? LastSuccessfulExecution { get; set; }

    /// <summary>
    /// Next scheduled execution
    /// </summary>
    public DateTime? NextScheduledExecution { get; set; }
}

/// <summary>
/// Enhanced statistics response DTO
/// </summary>
public class StatisticsResponse
{
    /// <summary>
    /// Day of the statistic
    /// </summary>
    public DateTime Day { get; set; }

    /// <summary>
    /// Hour of the statistic
    /// </summary>
    public int Hour { get; set; }

    /// <summary>
    /// Collector ID
    /// </summary>
    public long CollectorID { get; set; }

    /// <summary>
    /// Item name
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Total count
    /// </summary>
    public long Total { get; set; }

    /// <summary>
    /// Marked count
    /// </summary>
    public long Marked { get; set; }

    /// <summary>
    /// Marked percentage
    /// </summary>
    public decimal MarkedPercent { get; set; }

    /// <summary>
    /// Last updated date
    /// </summary>
    public DateTime UpdatedDate { get; set; }

    /// <summary>
    /// Trend information (if available)
    /// </summary>
    public StatisticsTrend? Trend { get; set; }
}

/// <summary>
/// Statistics trend information
/// </summary>
public class StatisticsTrend
{
    /// <summary>
    /// Trend direction (Increasing, Decreasing, Stable)
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Percentage change from previous period
    /// </summary>
    public decimal? PercentageChange { get; set; }

    /// <summary>
    /// Absolute change from previous period
    /// </summary>
    public long? AbsoluteChange { get; set; }
}

/// <summary>
/// Bulk statistics response
/// </summary>
public class BulkStatisticsResponse
{
    /// <summary>
    /// Collector ID
    /// </summary>
    public long CollectorId { get; set; }

    /// <summary>
    /// Total statistics retrieved
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Date range of statistics
    /// </summary>
    public DateRange DateRange { get; set; } = new();

    /// <summary>
    /// Individual statistics
    /// </summary>
    public List<StatisticsResponse> Statistics { get; set; } = new();

    /// <summary>
    /// Aggregated statistics (if requested)
    /// </summary>
    public StatisticsAggregates? Aggregates { get; set; }

    /// <summary>
    /// Query performance metrics
    /// </summary>
    public QueryMetrics QueryMetrics { get; set; } = new();
}

/// <summary>
/// Date range information
/// </summary>
public class DateRange
{
    /// <summary>
    /// Start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Total hours in range
    /// </summary>
    public int TotalHours { get; set; }
}

/// <summary>
/// Aggregated statistics
/// </summary>
public class StatisticsAggregates
{
    /// <summary>
    /// Total across all items
    /// </summary>
    public long TotalSum { get; set; }

    /// <summary>
    /// Marked total across all items
    /// </summary>
    public long MarkedSum { get; set; }

    /// <summary>
    /// Average marked percentage
    /// </summary>
    public decimal AverageMarkedPercent { get; set; }

    /// <summary>
    /// Peak values
    /// </summary>
    public StatisticsPeak Peak { get; set; } = new();

    /// <summary>
    /// Item-specific aggregates
    /// </summary>
    public List<ItemAggregate> ItemAggregates { get; set; } = new();
}

/// <summary>
/// Peak statistics information
/// </summary>
public class StatisticsPeak
{
    /// <summary>
    /// Highest total value
    /// </summary>
    public long HighestTotal { get; set; }

    /// <summary>
    /// Highest marked value
    /// </summary>
    public long HighestMarked { get; set; }

    /// <summary>
    /// Highest marked percentage
    /// </summary>
    public decimal HighestMarkedPercent { get; set; }

    /// <summary>
    /// When the peak occurred
    /// </summary>
    public DateTime PeakTime { get; set; }
}

/// <summary>
/// Item-specific aggregate information
/// </summary>
public class ItemAggregate
{
    /// <summary>
    /// Item name
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Total for this item
    /// </summary>
    public long Total { get; set; }

    /// <summary>
    /// Marked total for this item
    /// </summary>
    public long Marked { get; set; }

    /// <summary>
    /// Average marked percentage for this item
    /// </summary>
    public decimal AverageMarkedPercent { get; set; }
}


