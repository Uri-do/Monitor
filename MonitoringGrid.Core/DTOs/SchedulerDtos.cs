using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Core.DTOs
{
    /// <summary>
    /// DTO for scheduler information
    /// </summary>
    public class SchedulerDto
    {
        public int SchedulerID { get; set; }
        public string SchedulerName { get; set; } = string.Empty;
        public string? SchedulerDescription { get; set; }
        public string ScheduleType { get; set; } = string.Empty;
        public int? IntervalMinutes { get; set; }
        public string? CronExpression { get; set; }
        public DateTime? ExecutionDateTime { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Timezone { get; set; } = "UTC";
        public bool IsEnabled { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        
        // Computed properties
        public string DisplayText { get; set; } = string.Empty;
        public DateTime? NextExecutionTime { get; set; }
        public bool IsCurrentlyActive { get; set; }
        public int IndicatorCount { get; set; }
    }

    /// <summary>
    /// DTO for creating a new scheduler
    /// </summary>
    public class CreateSchedulerRequest
    {
        [Required]
        [StringLength(100)]
        public string SchedulerName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SchedulerDescription { get; set; }

        [Required]
        public string ScheduleType { get; set; } = "interval";

        public int? IntervalMinutes { get; set; }

        [StringLength(255)]
        public string? CronExpression { get; set; }

        public DateTime? ExecutionDateTime { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [StringLength(50)]
        public string Timezone { get; set; } = "UTC";

        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// DTO for updating an existing scheduler
    /// </summary>
    public class UpdateSchedulerRequest
    {
        [Required]
        [StringLength(100)]
        public string SchedulerName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SchedulerDescription { get; set; }

        [Required]
        public string ScheduleType { get; set; } = "interval";

        public int? IntervalMinutes { get; set; }

        [StringLength(255)]
        public string? CronExpression { get; set; }

        public DateTime? ExecutionDateTime { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [StringLength(50)]
        public string Timezone { get; set; } = "UTC";

        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// DTO for scheduler validation response
    /// </summary>
    public class SchedulerValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public string? NextExecutionTime { get; set; }
        public string? DisplayText { get; set; }
    }

    /// <summary>
    /// DTO for scheduler preset options
    /// </summary>
    public class SchedulerPresetDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ScheduleType { get; set; } = string.Empty;
        public int? IntervalMinutes { get; set; }
        public string? CronExpression { get; set; }
        public string Category { get; set; } = string.Empty; // "Common", "Hourly", "Daily", "Weekly", "Monthly"
    }

    /// <summary>
    /// DTO for scheduler statistics
    /// </summary>
    public class SchedulerStatsDto
    {
        public int TotalSchedulers { get; set; }
        public int ActiveSchedulers { get; set; }
        public int InactiveSchedulers { get; set; }
        public int IntervalSchedulers { get; set; }
        public int CronSchedulers { get; set; }
        public int OneTimeSchedulers { get; set; }
        public int SchedulersWithIndicators { get; set; }
        public int UnusedSchedulers { get; set; }
        public DateTime? NextScheduledExecution { get; set; }
        public string? NextSchedulerName { get; set; }
    }

    /// <summary>
    /// DTO for enhanced indicator with scheduler information
    /// </summary>
    public class IndicatorWithSchedulerDto
    {
        public long IndicatorID { get; set; }
        public string IndicatorName { get; set; } = string.Empty;
        public string IndicatorCode { get; set; } = string.Empty;
        public string? IndicatorDesc { get; set; }
        public long? CollectorID { get; set; }
        public string CollectorItemName { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public int LastMinutes { get; set; }
        public string ThresholdType { get; set; } = string.Empty;
        public string ThresholdField { get; set; } = string.Empty;
        public string ThresholdComparison { get; set; } = string.Empty;
        public decimal ThresholdValue { get; set; }
        public int OwnerContactId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? LastRun { get; set; }
        
        // Scheduler information
        public int? SchedulerID { get; set; }
        public string? SchedulerName { get; set; }
        public string? SchedulerDescription { get; set; }
        public string? ScheduleType { get; set; }
        public int? IntervalMinutes { get; set; }
        public string? CronExpression { get; set; }
        public DateTime? ExecutionDateTime { get; set; }
        public string? Timezone { get; set; }
        public bool? SchedulerEnabled { get; set; }
        public string? ScheduleStatus { get; set; }
        public DateTime? NextExecutionTime { get; set; }
        public string? ScheduleDisplayText { get; set; }
        
        // Contact information
        public string? OwnerContactName { get; set; }
        public string? OwnerContactEmail { get; set; }
    }

    /// <summary>
    /// Request DTO for assigning scheduler to indicator
    /// </summary>
    public class AssignSchedulerRequest
    {
        [Required]
        public long IndicatorID { get; set; }
        
        public int? SchedulerID { get; set; } // null to remove scheduler assignment
    }

    /// <summary>
    /// Response DTO for scheduler assignment
    /// </summary>
    public class AssignSchedulerResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long IndicatorID { get; set; }
        public int? SchedulerID { get; set; }
        public string? SchedulerName { get; set; }
        public DateTime? NextExecutionTime { get; set; }
    }
}
