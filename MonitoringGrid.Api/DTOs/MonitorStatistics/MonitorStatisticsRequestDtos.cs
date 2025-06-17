using System.ComponentModel.DataAnnotations;
using MonitoringGrid.Api.Validation;

namespace MonitoringGrid.Api.DTOs.MonitorStatistics;

/// <summary>
/// Request DTO for getting collectors
/// </summary>
public class GetCollectorsRequest
{
    /// <summary>
    /// Whether to include only active collectors
    /// </summary>
    [BooleanFlag]
    public bool ActiveOnly { get; set; } = true;

    /// <summary>
    /// Include detailed information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = false;

    /// <summary>
    /// Search term for filtering collectors
    /// </summary>
    [SearchTerm(0, 100)]
    public string? SearchTerm { get; set; }
}

/// <summary>
/// Request DTO for getting collector by ID
/// </summary>
public class GetCollectorRequest
{
    /// <summary>
    /// Collector ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long CollectorId { get; set; }

    /// <summary>
    /// Include detailed information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Include performance metrics
    /// </summary>
    [BooleanFlag]
    public bool IncludeMetrics { get; set; } = false;
}

/// <summary>
/// Request DTO for getting collector item names
/// </summary>
public class GetCollectorItemNamesRequest
{
    /// <summary>
    /// Collector ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long CollectorId { get; set; }

    /// <summary>
    /// Filter item names by search term
    /// </summary>
    [SearchTerm(0, 50)]
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Maximum number of items to return
    /// </summary>
    [PageSize(1, 1000)]
    public int? MaxItems { get; set; }
}

/// <summary>
/// Request DTO for getting statistics
/// </summary>
public class GetStatisticsRequest
{
    /// <summary>
    /// Collector ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long CollectorId { get; set; }

    /// <summary>
    /// Start date for statistics range
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// End date for statistics range
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Number of hours to look back (used when FromDate/ToDate not specified)
    /// </summary>
    [Range(1, 8760)] // 1 hour to 1 year
    public int Hours { get; set; } = 24;

    /// <summary>
    /// Specific item name to filter by
    /// </summary>
    [SearchTerm(0, 100)]
    public string? ItemName { get; set; }

    /// <summary>
    /// Include aggregated statistics
    /// </summary>
    [BooleanFlag]
    public bool IncludeAggregates { get; set; } = false;

    /// <summary>
    /// Group statistics by time period
    /// </summary>
    public StatisticsGroupBy? GroupBy { get; set; }
}

/// <summary>
/// Request DTO for updating collector
/// </summary>
public class UpdateCollectorRequest
{
    /// <summary>
    /// Collector ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long CollectorId { get; set; }

    /// <summary>
    /// Collector description
    /// </summary>
    [SearchTerm(1, 200)]
    public string? CollectorDesc { get; set; }

    /// <summary>
    /// Frequency in minutes
    /// </summary>
    [Range(1, 10080)] // 1 minute to 1 week
    public int? FrequencyMinutes { get; set; }

    /// <summary>
    /// Last minutes to consider
    /// </summary>
    [Range(1, 10080)]
    public int? LastMinutes { get; set; }

    /// <summary>
    /// Whether the collector is active
    /// </summary>
    [BooleanFlag]
    public bool? IsActive { get; set; }
}

/// <summary>
/// Request DTO for collector performance analysis
/// </summary>
public class GetCollectorPerformanceRequest
{
    /// <summary>
    /// Collector ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long CollectorId { get; set; }

    /// <summary>
    /// Number of days to analyze
    /// </summary>
    [Range(1, 365)]
    public int Days { get; set; } = 7;

    /// <summary>
    /// Include detailed performance metrics
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// Enumeration for statistics grouping options
/// </summary>
public enum StatisticsGroupBy
{
    Hour,
    Day,
    Week,
    Month
}
