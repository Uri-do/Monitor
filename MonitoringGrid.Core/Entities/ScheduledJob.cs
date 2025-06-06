using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Represents a scheduled job for KPI execution
/// </summary>
public class ScheduledJob
{
    [Key]
    [MaxLength(100)]
    public string JobId { get; set; } = string.Empty;

    public int KpiId { get; set; }

    [Required]
    [MaxLength(255)]
    public string JobName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string JobGroup { get; set; } = "KPI_JOBS";

    [Required]
    [MaxLength(255)]
    public string TriggerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string TriggerGroup { get; set; } = "KPI_TRIGGERS";

    /// <summary>
    /// Cron expression for cron-based scheduling
    /// </summary>
    [MaxLength(255)]
    public string? CronExpression { get; set; }

    /// <summary>
    /// Interval in minutes for interval-based scheduling
    /// </summary>
    public int? IntervalMinutes { get; set; }

    /// <summary>
    /// Start time for the schedule
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// End time for the schedule
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Next scheduled execution time
    /// </summary>
    public DateTime? NextFireTime { get; set; }

    /// <summary>
    /// Previous execution time
    /// </summary>
    public DateTime? PreviousFireTime { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual KPI KPI { get; set; } = null!;
}
