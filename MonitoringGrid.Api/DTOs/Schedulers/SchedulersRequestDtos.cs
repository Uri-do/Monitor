using System.ComponentModel.DataAnnotations;
using MonitoringGrid.Api.Validation;

namespace MonitoringGrid.Api.DTOs.Schedulers;

/// <summary>
/// Request DTO for getting schedulers
/// </summary>
public class GetSchedulersRequest
{
    /// <summary>
    /// Whether to include only enabled schedulers
    /// </summary>
    [BooleanFlag]
    public bool EnabledOnly { get; set; } = false;

    /// <summary>
    /// Include detailed information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = false;

    /// <summary>
    /// Search term for filtering schedulers
    /// </summary>
    [SearchTerm(0, 100)]
    public string? SearchTerm { get; set; }

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
}

/// <summary>
/// Request DTO for getting scheduler by ID
/// </summary>
public class GetSchedulerRequest
{
    /// <summary>
    /// Scheduler ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long SchedulerId { get; set; }

    /// <summary>
    /// Include detailed information
    /// </summary>
    [BooleanFlag]
    public bool IncludeDetails { get; set; } = true;

    /// <summary>
    /// Include associated indicators
    /// </summary>
    [BooleanFlag]
    public bool IncludeIndicators { get; set; } = false;

    /// <summary>
    /// Include execution history
    /// </summary>
    [BooleanFlag]
    public bool IncludeHistory { get; set; } = false;
}

/// <summary>
/// Request DTO for creating a scheduler
/// </summary>
public class CreateSchedulerRequest
{
    /// <summary>
    /// Scheduler name
    /// </summary>
    [Required]
    [SearchTerm(1, 100)]
    public string SchedulerName { get; set; } = string.Empty;

    /// <summary>
    /// Scheduler description
    /// </summary>
    [SearchTerm(0, 500)]
    public string? SchedulerDescription { get; set; }

    /// <summary>
    /// Schedule type (Interval, Cron, etc.)
    /// </summary>
    [Required]
    [SearchTerm(1, 50)]
    public string ScheduleType { get; set; } = string.Empty;

    /// <summary>
    /// Interval in minutes (for interval-based schedules)
    /// </summary>
    [Range(1, 10080)] // 1 minute to 1 week
    public int? IntervalMinutes { get; set; }

    /// <summary>
    /// Cron expression (for cron-based schedules)
    /// </summary>
    [SearchTerm(0, 100)]
    public string? CronExpression { get; set; }

    /// <summary>
    /// Whether the scheduler is enabled
    /// </summary>
    [BooleanFlag]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Timezone for the scheduler
    /// </summary>
    [SearchTerm(0, 50)]
    public string? Timezone { get; set; }

    /// <summary>
    /// Start date for the scheduler
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for the scheduler
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Request DTO for updating a scheduler
/// </summary>
public class UpdateSchedulerRequest
{
    /// <summary>
    /// Scheduler ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long SchedulerId { get; set; }

    /// <summary>
    /// Scheduler name
    /// </summary>
    [SearchTerm(1, 100)]
    public string? SchedulerName { get; set; }

    /// <summary>
    /// Scheduler description
    /// </summary>
    [SearchTerm(0, 500)]
    public string? SchedulerDescription { get; set; }

    /// <summary>
    /// Schedule type (Interval, Cron, etc.)
    /// </summary>
    [SearchTerm(1, 50)]
    public string? ScheduleType { get; set; }

    /// <summary>
    /// Interval in minutes (for interval-based schedules)
    /// </summary>
    [Range(1, 10080)] // 1 minute to 1 week
    public int? IntervalMinutes { get; set; }

    /// <summary>
    /// Cron expression (for cron-based schedules)
    /// </summary>
    [SearchTerm(0, 100)]
    public string? CronExpression { get; set; }

    /// <summary>
    /// Whether the scheduler is enabled
    /// </summary>
    [BooleanFlag]
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Timezone for the scheduler
    /// </summary>
    [SearchTerm(0, 50)]
    public string? Timezone { get; set; }

    /// <summary>
    /// Start date for the scheduler
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for the scheduler
    /// </summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Request DTO for deleting a scheduler
/// </summary>
public class DeleteSchedulerRequest
{
    /// <summary>
    /// Scheduler ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long SchedulerId { get; set; }

    /// <summary>
    /// Force delete even if scheduler has associated indicators
    /// </summary>
    [BooleanFlag]
    public bool Force { get; set; } = false;

    /// <summary>
    /// Reassign indicators to another scheduler ID
    /// </summary>
    [PositiveInteger]
    public long? ReassignToSchedulerId { get; set; }
}

/// <summary>
/// Request DTO for scheduler execution testing
/// </summary>
public class TestSchedulerRequest
{
    /// <summary>
    /// Scheduler ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long SchedulerId { get; set; }

    /// <summary>
    /// Test execution context
    /// </summary>
    [ExecutionContext]
    public string Context { get; set; } = "Test";

    /// <summary>
    /// Whether to save test results
    /// </summary>
    [BooleanFlag]
    public bool SaveResults { get; set; } = false;

    /// <summary>
    /// Maximum number of indicators to test
    /// </summary>
    [Range(1, 100)]
    public int? MaxIndicators { get; set; }
}

/// <summary>
/// Request DTO for scheduler performance analysis
/// </summary>
public class GetSchedulerPerformanceRequest
{
    /// <summary>
    /// Scheduler ID
    /// </summary>
    [Required]
    [PositiveInteger]
    public long SchedulerId { get; set; }

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
