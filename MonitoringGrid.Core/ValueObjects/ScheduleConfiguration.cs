using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MonitoringGrid.Core.ValueObjects
{
    /// <summary>
    /// Value object representing schedule configuration for KPIs
    /// </summary>
    public class ScheduleConfiguration
    {
        /// <summary>
        /// Type of scheduling (interval, cron, onetime)
        /// </summary>
        [Required]
        public string ScheduleType { get; set; } = "interval";

        /// <summary>
        /// Cron expression for complex scheduling patterns
        /// </summary>
        public string? CronExpression { get; set; }

        /// <summary>
        /// Simple interval in minutes for interval-based scheduling
        /// </summary>
        [Range(1, 525600, ErrorMessage = "Interval must be between 1 minute and 1 year")]
        public int? IntervalMinutes { get; set; }

        /// <summary>
        /// Start date for the schedule
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for the schedule
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Timezone for the schedule
        /// </summary>
        public string? Timezone { get; set; } = "UTC";

        /// <summary>
        /// Whether the schedule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets the interval in minutes for scheduling calculations
        /// </summary>
        /// <returns>Interval in minutes, or 60 as default</returns>
        public int GetIntervalMinutes()
        {
            if (!IsEnabled)
                return 60; // Default fallback

            return ScheduleType?.ToLower() switch
            {
                "interval" => IntervalMinutes ?? 60,
                "cron" => ParseCronToMinutes(),
                "onetime" => 0, // One-time schedules don't have intervals
                _ => 60 // Default fallback
            };
        }

        /// <summary>
        /// Checks if the schedule configuration is valid
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            if (!IsEnabled)
                return true; // Disabled schedules are always valid

            return ScheduleType?.ToLower() switch
            {
                "interval" => IntervalMinutes.HasValue && IntervalMinutes > 0,
                "cron" => !string.IsNullOrWhiteSpace(CronExpression),
                "onetime" => StartDate.HasValue,
                _ => false
            };
        }

        /// <summary>
        /// Gets a human-readable description of the schedule
        /// </summary>
        /// <returns>Description string</returns>
        public string GetDescription()
        {
            if (!IsEnabled)
                return "Disabled";

            return ScheduleType?.ToLower() switch
            {
                "interval" => $"Every {IntervalMinutes} minutes",
                "cron" => $"Cron: {CronExpression}",
                "onetime" => $"One time at {StartDate:yyyy-MM-dd HH:mm}",
                _ => "Unknown schedule type"
            };
        }

        /// <summary>
        /// Attempts to parse a cron expression to approximate minutes interval
        /// This is a simplified approach for common cron patterns
        /// </summary>
        /// <returns>Approximate interval in minutes</returns>
        private int ParseCronToMinutes()
        {
            if (string.IsNullOrWhiteSpace(CronExpression))
                return 60;

            // Simple parsing for common patterns
            // This is a basic implementation - for production, consider using a proper cron parser
            var parts = CronExpression.Split(' ');
            if (parts.Length < 5)
                return 60;

            // Check for minute patterns
            var minutePart = parts[0];
            if (minutePart.StartsWith("*/"))
            {
                if (int.TryParse(minutePart.Substring(2), out var interval))
                    return interval;
            }

            // Check for hourly patterns (0 * * * *)
            if (minutePart == "0" && parts[1] == "*")
                return 60;

            // Check for daily patterns (0 0 * * *)
            if (minutePart == "0" && parts[1] == "0")
                return 1440; // 24 hours

            // Default fallback
            return 60;
        }

        /// <summary>
        /// Creates a default interval-based schedule configuration
        /// </summary>
        /// <param name="intervalMinutes">Interval in minutes</param>
        /// <returns>New schedule configuration</returns>
        public static ScheduleConfiguration CreateInterval(int intervalMinutes)
        {
            return new ScheduleConfiguration
            {
                ScheduleType = "interval",
                IntervalMinutes = intervalMinutes,
                IsEnabled = true,
                Timezone = "UTC"
            };
        }

        /// <summary>
        /// Creates a cron-based schedule configuration
        /// </summary>
        /// <param name="cronExpression">Cron expression</param>
        /// <returns>New schedule configuration</returns>
        public static ScheduleConfiguration CreateCron(string cronExpression)
        {
            return new ScheduleConfiguration
            {
                ScheduleType = "cron",
                CronExpression = cronExpression,
                IsEnabled = true,
                Timezone = "UTC"
            };
        }

        /// <summary>
        /// Creates a one-time schedule configuration
        /// </summary>
        /// <param name="startDate">Execution date</param>
        /// <returns>New schedule configuration</returns>
        public static ScheduleConfiguration CreateOneTime(DateTime startDate)
        {
            return new ScheduleConfiguration
            {
                ScheduleType = "onetime",
                StartDate = startDate,
                IsEnabled = true,
                Timezone = "UTC"
            };
        }
    }
}
