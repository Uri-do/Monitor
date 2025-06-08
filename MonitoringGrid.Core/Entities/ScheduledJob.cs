using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents a scheduled job for KPI execution using Quartz.NET scheduler.
/// Manages the scheduling metadata and execution timing for automated KPI monitoring.
/// </summary>
public class ScheduledJob
{
    /// <summary>
    /// Unique identifier for the scheduled job. Used by Quartz.NET scheduler.
    /// </summary>
    [Key]
    [Required(ErrorMessage = "Job ID is required")]
    [MaxLength(100, ErrorMessage = "Job ID cannot exceed 100 characters")]
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the KPI that this job will execute.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "KPI ID must be a positive number")]
    public int KpiId { get; set; }

    /// <summary>
    /// Human-readable name for the scheduled job.
    /// </summary>
    [Required(ErrorMessage = "Job name is required")]
    [MaxLength(255, ErrorMessage = "Job name cannot exceed 255 characters")]
    [MinLength(1, ErrorMessage = "Job name cannot be empty")]
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// Quartz.NET job group for organizing related jobs.
    /// </summary>
    [Required(ErrorMessage = "Job group is required")]
    [MaxLength(255)]
    public string JobGroup { get; set; } = "KPI_JOBS";

    /// <summary>
    /// Quartz.NET trigger name for this scheduled job.
    /// </summary>
    [Required(ErrorMessage = "Trigger name is required")]
    [MaxLength(255, ErrorMessage = "Trigger name cannot exceed 255 characters")]
    [MinLength(1, ErrorMessage = "Trigger name cannot be empty")]
    public string TriggerName { get; set; } = string.Empty;

    /// <summary>
    /// Quartz.NET trigger group for organizing related triggers.
    /// </summary>
    [Required(ErrorMessage = "Trigger group is required")]
    [MaxLength(255)]
    public string TriggerGroup { get; set; } = "KPI_TRIGGERS";

    /// <summary>
    /// Cron expression for complex scheduling patterns (e.g., "0 0 12 * * ?").
    /// Takes precedence over IntervalMinutes if both are specified.
    /// </summary>
    [MaxLength(255)]
    [RegularExpression(@"^(\*|([0-5]?\d))\s+(\*|([0-5]?\d))\s+(\*|(1?\d|2[0-3]))\s+(\*|([1-9]|[12]\d|3[01]))\s+(\*|([1-9]|1[0-2]))\s+(\*|[0-6])(\s+(\*|(\d{4})))?$",
        ErrorMessage = "Invalid cron expression format")]
    public string? CronExpression { get; set; }

    /// <summary>
    /// Simple interval-based scheduling in minutes.
    /// Used when CronExpression is not specified.
    /// </summary>
    [Range(1, 525600, ErrorMessage = "Interval must be between 1 minute and 1 year")]
    public int? IntervalMinutes { get; set; }

    /// <summary>
    /// When the schedule should start executing. Null means start immediately.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// When the schedule should stop executing. Null means no end date.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Next scheduled execution time as calculated by Quartz.NET.
    /// </summary>
    public DateTime? NextFireTime { get; set; }

    /// <summary>
    /// Previous execution time as recorded by Quartz.NET.
    /// </summary>
    public DateTime? PreviousFireTime { get; set; }

    /// <summary>
    /// Whether this scheduled job is active and should be executed.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this scheduled job was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this scheduled job was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    /// <summary>
    /// The KPI that this scheduled job will execute.
    /// </summary>
    public virtual KPI KPI { get; set; } = null!;

    // Domain methods
    /// <summary>
    /// Determines if this job uses cron-based scheduling.
    /// </summary>
    /// <returns>True if using cron expression, false if using interval.</returns>
    public bool UsesCronScheduling()
    {
        return !string.IsNullOrWhiteSpace(CronExpression);
    }

    /// <summary>
    /// Validates that the job has a valid scheduling configuration.
    /// </summary>
    /// <returns>True if the scheduling configuration is valid.</returns>
    public bool HasValidSchedule()
    {
        return UsesCronScheduling() || IntervalMinutes.HasValue;
    }

    /// <summary>
    /// Checks if the schedule is currently within its active time window.
    /// </summary>
    /// <returns>True if the schedule should be active now.</returns>
    public bool IsInActiveTimeWindow()
    {
        var now = DateTime.UtcNow;

        if (StartTime.HasValue && now < StartTime.Value)
            return false;

        if (EndTime.HasValue && now > EndTime.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Gets a human-readable description of the schedule.
    /// </summary>
    /// <returns>Schedule description string.</returns>
    public string GetScheduleDescription()
    {
        if (UsesCronScheduling())
            return $"Cron: {CronExpression}";

        if (IntervalMinutes.HasValue)
            return $"Every {IntervalMinutes} minutes";

        return "No schedule configured";
    }
}
