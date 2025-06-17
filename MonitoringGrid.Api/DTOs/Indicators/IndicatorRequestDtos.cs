using System.ComponentModel.DataAnnotations;
using MonitoringGrid.Api.DTOs.Common;
using MonitoringGrid.Api.Validation;

namespace MonitoringGrid.Api.DTOs.Indicators;

/// <summary>
/// Enhanced request DTO for getting indicators with filtering and pagination
/// </summary>
public class GetIndicatorsRequest
{
    /// <summary>
    /// Search text for filtering indicators
    /// </summary>
    [SearchTerm(0, 200)]
    public string? SearchText { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    [BooleanFlag]
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filter by owner contact ID
    /// </summary>
    [PositiveInteger]
    public long? OwnerContactId { get; set; }

    /// <summary>
    /// Filter by collector ID
    /// </summary>
    [PositiveInteger]
    public long? CollectorId { get; set; }

    /// <summary>
    /// Filter by scheduler ID
    /// </summary>
    [PositiveInteger]
    public int? SchedulerId { get; set; }

    /// <summary>
    /// Filter by last run date (from)
    /// </summary>
    public DateTime? LastRunFrom { get; set; }

    /// <summary>
    /// Filter by last run date (to)
    /// </summary>
    public DateTime? LastRunTo { get; set; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    [PositiveInteger]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    [PageSize(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Sort field
    /// </summary>
    [SearchTerm(0, 50)]
    public string? SortBy { get; set; } = "indicatorName";

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    [SearchTerm(0, 10)]
    public string? SortDirection { get; set; } = "asc";

    /// <summary>
    /// Include detailed information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = false;

    /// <summary>
    /// Include execution statistics
    /// </summary>
    [BooleanFlag]
    public bool IncludeStatistics { get; set; } = false;

    /// <summary>
    /// Include scheduler information
    /// </summary>
    [BooleanFlag]
    public bool IncludeScheduler { get; set; } = false;
}

/// <summary>
/// Enhanced request DTO for getting indicator by ID
/// </summary>
public class GetIndicatorRequest
{
    /// <summary>
    /// Indicator ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long IndicatorId { get; set; }

    /// <summary>
    /// Include detailed information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Include execution history
    /// </summary>
    [BooleanFlag]
    public bool IncludeHistory { get; set; } = false;

    /// <summary>
    /// Include scheduler information
    /// </summary>
    [BooleanFlag]
    public bool IncludeScheduler { get; set; } = true;

    /// <summary>
    /// Include collector information
    /// </summary>
    [BooleanFlag]
    public bool IncludeCollector { get; set; } = true;

    /// <summary>
    /// Number of recent executions to include
    /// </summary>
    [Range(0, 100)]
    public int RecentExecutionsCount { get; set; } = 10;
}

/// <summary>
/// Enhanced request DTO for creating indicators
/// </summary>
public class CreateIndicatorRequest
{
    /// <summary>
    /// Indicator name
    /// </summary>
    [Required]
    [SearchTerm(1, 100)]
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Indicator description
    /// </summary>
    [SearchTerm(0, 500)]
    public string? IndicatorDescription { get; set; }

    /// <summary>
    /// Owner contact ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long OwnerContactId { get; set; }

    /// <summary>
    /// Collector ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long CollectorId { get; set; }

    /// <summary>
    /// SQL query for the indicator
    /// </summary>
    [Required]
    [SearchTerm(1, 4000)]
    public string SqlQuery { get; set; } = string.Empty;

    /// <summary>
    /// Whether the indicator is active
    /// </summary>
    [BooleanFlag]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last minutes parameter for the indicator
    /// </summary>
    [Range(1, 10080)] // 1 minute to 1 week
    public int LastMinutes { get; set; } = 60;

    /// <summary>
    /// Scheduler ID (optional)
    /// </summary>
    [PositiveInteger]
    public int? SchedulerId { get; set; }

    /// <summary>
    /// Alert threshold value (optional)
    /// </summary>
    public decimal? AlertThreshold { get; set; }

    /// <summary>
    /// Alert comparison operator (optional)
    /// </summary>
    [SearchTerm(0, 10)]
    public string? AlertOperator { get; set; }

    /// <summary>
    /// Additional configuration (JSON)
    /// </summary>
    [SearchTerm(0, 2000)]
    public string? Configuration { get; set; }
}

/// <summary>
/// Enhanced request DTO for updating indicators
/// </summary>
public class UpdateIndicatorRequest
{
    /// <summary>
    /// Indicator ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long IndicatorID { get; set; }

    /// <summary>
    /// Indicator name
    /// </summary>
    [Required]
    [SearchTerm(1, 100)]
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Indicator description
    /// </summary>
    [SearchTerm(0, 500)]
    public string? IndicatorDescription { get; set; }

    /// <summary>
    /// Owner contact ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long OwnerContactId { get; set; }

    /// <summary>
    /// Collector ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long CollectorId { get; set; }

    /// <summary>
    /// SQL query for the indicator
    /// </summary>
    [Required]
    [SearchTerm(1, 4000)]
    public string SqlQuery { get; set; } = string.Empty;

    /// <summary>
    /// Whether the indicator is active
    /// </summary>
    [BooleanFlag]
    public bool IsActive { get; set; }

    /// <summary>
    /// Last minutes parameter for the indicator
    /// </summary>
    [Range(1, 10080)] // 1 minute to 1 week
    public int LastMinutes { get; set; }

    /// <summary>
    /// Scheduler ID (optional)
    /// </summary>
    [PositiveInteger]
    public int? SchedulerId { get; set; }

    /// <summary>
    /// Alert threshold value (optional)
    /// </summary>
    public decimal? AlertThreshold { get; set; }

    /// <summary>
    /// Alert comparison operator (optional)
    /// </summary>
    [SearchTerm(0, 10)]
    public string? AlertOperator { get; set; }

    /// <summary>
    /// Additional configuration (JSON)
    /// </summary>
    [SearchTerm(0, 2000)]
    public string? Configuration { get; set; }

    /// <summary>
    /// Reason for the update
    /// </summary>
    [SearchTerm(0, 500)]
    public string? UpdateReason { get; set; }
}

/// <summary>
/// Enhanced request DTO for deleting indicators
/// </summary>
public class DeleteIndicatorRequest
{
    /// <summary>
    /// Indicator ID to delete
    /// </summary>
    [Required]
    [PositiveInteger]
    public long IndicatorId { get; set; }

    /// <summary>
    /// Force deletion even if there are dependencies
    /// </summary>
    [BooleanFlag]
    public bool Force { get; set; } = false;

    /// <summary>
    /// Reason for deletion
    /// </summary>
    [SearchTerm(0, 500)]
    public string? DeletionReason { get; set; }

    /// <summary>
    /// Archive data before deletion
    /// </summary>
    [BooleanFlag]
    public bool ArchiveData { get; set; } = true;
}



/// <summary>
/// Request DTO for getting indicator dashboard
/// </summary>
public class GetIndicatorDashboardRequest
{
    /// <summary>
    /// Include execution statistics
    /// </summary>
    [BooleanFlag]
    public bool IncludeStatistics { get; set; } = true;

    /// <summary>
    /// Include recent executions
    /// </summary>
    [BooleanFlag]
    public bool IncludeRecentExecutions { get; set; } = true;

    /// <summary>
    /// Number of hours for trend analysis
    /// </summary>
    [Range(1, 168)] // 1 hour to 1 week
    public int TrendHours { get; set; } = 24;

    /// <summary>
    /// Include detailed information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = false;

    /// <summary>
    /// Refresh cache data
    /// </summary>
    [BooleanFlag]
    public bool RefreshCache { get; set; } = false;
}
