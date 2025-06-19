using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities
{
    /// <summary>
    /// Represents a reusable scheduler configuration for indicators
    /// </summary>
    [Table("Schedulers", Schema = "monitoring")]
    public class Scheduler
    {
        [Key]
        public int SchedulerID { get; set; }

        [Required]
        [StringLength(100)]
        public string SchedulerName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SchedulerDescription { get; set; }

        [Required]
        [StringLength(20)]
        public string ScheduleType { get; set; } = "interval"; // interval, cron, onetime

        // Interval-based scheduling
        public int? IntervalMinutes { get; set; }

        // Cron-based scheduling
        [StringLength(255)]
        public string? CronExpression { get; set; }

        // One-time scheduling
        public DateTime? ExecutionDateTime { get; set; }

        // Common scheduling properties
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Timezone { get; set; } = "UTC";

        public bool IsEnabled { get; set; } = true;

        // Metadata
        public DateTime? CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string CreatedBy { get; set; } = "system";

        public DateTime? ModifiedDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string ModifiedBy { get; set; } = "system";

        // Navigation properties
        public virtual ICollection<Indicator> Indicators { get; set; } = new List<Indicator>();

        // Domain methods
        public bool IsValidConfiguration()
        {
            return ScheduleType switch
            {
                "interval" => IntervalMinutes.HasValue && IntervalMinutes.Value > 0,
                "cron" => !string.IsNullOrWhiteSpace(CronExpression),
                "onetime" => ExecutionDateTime.HasValue,
                _ => false
            };
        }

        public string GetDisplayText()
        {
            if (!IsValidConfiguration())
                return "Invalid Configuration";

            return ScheduleType switch
            {
                "interval" => $"Every {IntervalMinutes} minutes",
                "cron" => $"Cron: {CronExpression}",
                "onetime" => $"Once at {ExecutionDateTime:yyyy-MM-dd HH:mm}",
                _ => "Unknown"
            };
        }

        public DateTime? GetNextExecutionTime(DateTime? lastExecution = null)
        {
            if (!IsEnabled || !IsValidConfiguration())
                return null;

            var now = DateTime.UtcNow;

            // If never executed before, return current time (due immediately)
            if (!lastExecution.HasValue)
                return now;

            var baseTime = lastExecution.Value;

            return ScheduleType switch
            {
                "interval" when IntervalMinutes.HasValue =>
                    baseTime.AddMinutes(IntervalMinutes.Value),
                "onetime" when ExecutionDateTime.HasValue =>
                    ExecutionDateTime.Value > now ? ExecutionDateTime.Value : null,
                "cron" =>
                    CalculateNextCronExecution(baseTime),
                _ => null
            };
        }

        private DateTime? CalculateNextCronExecution(DateTime baseTime)
        {
            // This is a simplified cron calculation
            // In a real implementation, you'd use a library like Cronos or NCrontab
            if (string.IsNullOrWhiteSpace(CronExpression))
                return null;

            // For now, return a basic calculation for common patterns
            return CronExpression switch
            {
                "* * * * *" => baseTime.AddMinutes(1), // Every minute
                "*/1 * * * *" => baseTime.AddMinutes(1), // Every minute (alternative format)
                "0 * * * *" => baseTime.AddHours(1).Date.AddHours(baseTime.Hour + 1), // Hourly
                "*/5 * * * *" => baseTime.AddMinutes(5), // Every 5 minutes
                "*/15 * * * *" => baseTime.AddMinutes(15), // Every 15 minutes
                "0 0 * * *" => baseTime.AddDays(1).Date, // Daily at midnight
                "0 9 * * *" => GetNext9AM(baseTime), // Daily at 9 AM
                _ => baseTime.AddHours(1) // Default fallback
            };
        }

        private DateTime GetNext9AM(DateTime baseTime)
        {
            var next9AM = baseTime.Date.AddHours(9);
            if (next9AM <= baseTime)
                next9AM = next9AM.AddDays(1);
            return next9AM;
        }

        public bool IsCurrentlyActive()
        {
            if (!IsEnabled)
                return false;

            var now = DateTime.UtcNow;

            // Check start date
            if (StartDate.HasValue && now < StartDate.Value)
                return false;

            // Check end date
            if (EndDate.HasValue && now > EndDate.Value)
                return false;

            // For one-time schedules, check if execution time has passed
            if (ScheduleType == "onetime" && ExecutionDateTime.HasValue && now > ExecutionDateTime.Value)
                return false;

            return true;
        }

        public void UpdateModifiedInfo(string modifiedBy = "system")
        {
            ModifiedDate = DateTime.UtcNow;
            ModifiedBy = modifiedBy;
        }
    }

    /// <summary>
    /// Enum for schedule types
    /// </summary>
    public enum ScheduleType
    {
        Interval,
        Cron,
        OneTime
    }

    /// <summary>
    /// Common cron expressions for easy selection
    /// </summary>
    public static class CommonCronExpressions
    {
        public static readonly Dictionary<string, string> Presets = new()
        {
            { "Every minute", "* * * * *" },
            { "Every 5 minutes", "*/5 * * * *" },
            { "Every 15 minutes", "*/15 * * * *" },
            { "Every 30 minutes", "*/30 * * * *" },
            { "Hourly", "0 * * * *" },
            { "Daily at midnight", "0 0 * * *" },
            { "Daily at 6 AM", "0 6 * * *" },
            { "Daily at 9 AM", "0 9 * * *" },
            { "Daily at noon", "0 12 * * *" },
            { "Daily at 6 PM", "0 18 * * *" },
            { "Weekly on Monday at 9 AM", "0 9 * * 1" },
            { "Monthly on 1st at midnight", "0 0 1 * *" },
            { "Yearly on Jan 1st at midnight", "0 0 1 1 *" }
        };

        public static string GetDescription(string cronExpression)
        {
            var preset = Presets.FirstOrDefault(p => p.Value == cronExpression);
            return preset.Key ?? "Custom cron expression";
        }
    }
}
