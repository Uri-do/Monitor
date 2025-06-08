using System;

namespace MonitoringGrid.Core.Utilities
{
    /// <summary>
    /// Utility class for calculating "whole time" scheduling intervals.
    /// Ensures KPIs run at clean time intervals (e.g., xx:00 for hourly, xx:05,10,15 for 5-minute intervals)
    /// </summary>
    public static class WholeTimeScheduler
    {
        /// <summary>
        /// Calculates the next "whole time" execution based on frequency
        /// </summary>
        /// <param name="frequencyMinutes">Frequency in minutes</param>
        /// <param name="fromTime">Base time to calculate from (defaults to current UTC time)</param>
        /// <returns>Next whole time execution</returns>
        public static DateTime GetNextWholeTimeExecution(int frequencyMinutes, DateTime? fromTime = null)
        {
            var baseTime = fromTime ?? DateTime.UtcNow;
            
            // Handle special cases for common intervals
            return frequencyMinutes switch
            {
                // Every minute - next minute boundary
                1 => GetNextMinuteBoundary(baseTime),
                
                // Every 5 minutes - next 5-minute boundary (xx:00, xx:05, xx:10, etc.)
                5 => GetNextIntervalBoundary(baseTime, 5),
                
                // Every 10 minutes - next 10-minute boundary (xx:00, xx:10, xx:20, etc.)
                10 => GetNextIntervalBoundary(baseTime, 10),
                
                // Every 15 minutes - next 15-minute boundary (xx:00, xx:15, xx:30, xx:45)
                15 => GetNextIntervalBoundary(baseTime, 15),
                
                // Every 30 minutes - next 30-minute boundary (xx:00, xx:30)
                30 => GetNextIntervalBoundary(baseTime, 30),
                
                // Every hour - next hour boundary (xx:00)
                60 => GetNextHourBoundary(baseTime),
                
                // Every 2 hours - next 2-hour boundary (00:00, 02:00, 04:00, etc.)
                120 => GetNextHourIntervalBoundary(baseTime, 2),
                
                // Every 3 hours - next 3-hour boundary (00:00, 03:00, 06:00, etc.)
                180 => GetNextHourIntervalBoundary(baseTime, 3),
                
                // Every 4 hours - next 4-hour boundary (00:00, 04:00, 08:00, etc.)
                240 => GetNextHourIntervalBoundary(baseTime, 4),
                
                // Every 6 hours - next 6-hour boundary (00:00, 06:00, 12:00, 18:00)
                360 => GetNextHourIntervalBoundary(baseTime, 6),
                
                // Every 12 hours - next 12-hour boundary (00:00, 12:00)
                720 => GetNextHourIntervalBoundary(baseTime, 12),
                
                // Every 24 hours (daily) - next day boundary (00:00)
                1440 => GetNextDayBoundary(baseTime),
                
                // For other intervals, use generic calculation
                _ => GetNextGenericIntervalBoundary(baseTime, frequencyMinutes)
            };
        }

        /// <summary>
        /// Checks if a KPI is due for execution based on whole time scheduling
        /// </summary>
        /// <param name="lastRun">Last execution time</param>
        /// <param name="frequencyMinutes">Frequency in minutes</param>
        /// <param name="currentTime">Current time (defaults to UTC now)</param>
        /// <returns>True if the KPI is due for execution</returns>
        public static bool IsKpiDueForWholeTimeExecution(DateTime? lastRun, int frequencyMinutes, DateTime? currentTime = null)
        {
            var now = currentTime ?? DateTime.UtcNow;
            
            // If never run, it's due
            if (!lastRun.HasValue)
                return true;
            
            // Calculate the next whole time execution from the last run
            var nextExecution = GetNextWholeTimeExecution(frequencyMinutes, lastRun.Value);
            
            // Check if current time has passed the next execution time
            return now >= nextExecution;
        }

        /// <summary>
        /// Gets a human-readable description of the whole time schedule
        /// </summary>
        /// <param name="frequencyMinutes">Frequency in minutes</param>
        /// <returns>Description string</returns>
        public static string GetScheduleDescription(int frequencyMinutes)
        {
            return frequencyMinutes switch
            {
                1 => "Every minute at xx:00 seconds",
                5 => "Every 5 minutes (xx:00, xx:05, xx:10, xx:15, etc.)",
                10 => "Every 10 minutes (xx:00, xx:10, xx:20, xx:30, etc.)",
                15 => "Every 15 minutes (xx:00, xx:15, xx:30, xx:45)",
                30 => "Every 30 minutes (xx:00, xx:30)",
                60 => "Every hour at xx:00",
                120 => "Every 2 hours (00:00, 02:00, 04:00, etc.)",
                180 => "Every 3 hours (00:00, 03:00, 06:00, etc.)",
                240 => "Every 4 hours (00:00, 04:00, 08:00, etc.)",
                360 => "Every 6 hours (00:00, 06:00, 12:00, 18:00)",
                720 => "Every 12 hours (00:00, 12:00)",
                1440 => "Daily at 00:00",
                _ when frequencyMinutes < 60 => $"Every {frequencyMinutes} minutes at whole minute boundaries",
                _ when frequencyMinutes % 60 == 0 => $"Every {frequencyMinutes / 60} hours at hour boundaries",
                _ => $"Every {frequencyMinutes} minutes at calculated boundaries"
            };
        }

        #region Private Helper Methods

        private static DateTime GetNextMinuteBoundary(DateTime baseTime)
        {
            return new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, 
                               baseTime.Hour, baseTime.Minute, 0, DateTimeKind.Utc)
                   .AddMinutes(1);
        }

        private static DateTime GetNextIntervalBoundary(DateTime baseTime, int intervalMinutes)
        {
            var currentMinute = baseTime.Minute;
            var nextIntervalMinute = ((currentMinute / intervalMinutes) + 1) * intervalMinutes;
            
            var result = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, 
                                     baseTime.Hour, 0, 0, DateTimeKind.Utc);
            
            if (nextIntervalMinute >= 60)
            {
                result = result.AddHours(1);
                nextIntervalMinute = 0;
            }
            
            return result.AddMinutes(nextIntervalMinute);
        }

        private static DateTime GetNextHourBoundary(DateTime baseTime)
        {
            return new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, 
                               baseTime.Hour, 0, 0, DateTimeKind.Utc)
                   .AddHours(1);
        }

        private static DateTime GetNextHourIntervalBoundary(DateTime baseTime, int intervalHours)
        {
            var currentHour = baseTime.Hour;
            var nextIntervalHour = ((currentHour / intervalHours) + 1) * intervalHours;
            
            var result = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, 
                                     0, 0, 0, DateTimeKind.Utc);
            
            if (nextIntervalHour >= 24)
            {
                result = result.AddDays(1);
                nextIntervalHour = 0;
            }
            
            return result.AddHours(nextIntervalHour);
        }

        private static DateTime GetNextDayBoundary(DateTime baseTime)
        {
            return new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, 
                               0, 0, 0, DateTimeKind.Utc)
                   .AddDays(1);
        }

        private static DateTime GetNextGenericIntervalBoundary(DateTime baseTime, int intervalMinutes)
        {
            // For generic intervals, calculate based on minutes since midnight
            var midnight = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, 
                                       0, 0, 0, DateTimeKind.Utc);
            var minutesSinceMidnight = (int)(baseTime - midnight).TotalMinutes;
            var nextIntervalMinutes = ((minutesSinceMidnight / intervalMinutes) + 1) * intervalMinutes;
            
            var result = midnight.AddMinutes(nextIntervalMinutes);
            
            // If we've gone past midnight, move to next day
            if (result.Day != baseTime.Day)
            {
                result = midnight.AddDays(1);
            }
            
            return result;
        }

        #endregion
    }
}
