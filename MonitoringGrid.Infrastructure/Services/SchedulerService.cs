using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services
{
    /// <summary>
    /// Service implementation for managing scheduler configurations
    /// </summary>
    public class SchedulerService : ISchedulerService
    {
        private readonly MonitoringContext _context;
        private readonly ILogger<SchedulerService> _logger;

        public SchedulerService(MonitoringContext context, ILogger<SchedulerService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<List<SchedulerDto>>> GetSchedulersAsync(bool includeDisabled = false)
        {
            try
            {
                var query = _context.Schedulers.AsQueryable();

                if (!includeDisabled)
                {
                    query = query.Where(s => s.IsEnabled);
                }

                var schedulers = await query
                    .Include(s => s.Indicators)
                    .OrderBy(s => s.ScheduleType)
                    .ThenBy(s => s.IntervalMinutes)
                    .ThenBy(s => s.SchedulerName)
                    .ToListAsync();

                var schedulerDtos = schedulers.Select(s => new SchedulerDto
                {
                    SchedulerID = s.SchedulerID,
                    SchedulerName = s.SchedulerName,
                    SchedulerDescription = s.SchedulerDescription,
                    ScheduleType = s.ScheduleType,
                    IntervalMinutes = s.IntervalMinutes,
                    CronExpression = s.CronExpression,
                    ExecutionDateTime = s.ExecutionDateTime,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    Timezone = s.Timezone,
                    IsEnabled = s.IsEnabled,
                    CreatedDate = s.CreatedDate,
                    CreatedBy = s.CreatedBy,
                    ModifiedDate = s.ModifiedDate,
                    ModifiedBy = s.ModifiedBy,
                    DisplayText = s.GetDisplayText(),
                    NextExecutionTime = s.GetNextExecutionTime(),
                    IsCurrentlyActive = s.IsCurrentlyActive(),
                    IndicatorCount = s.Indicators?.Count ?? 0
                }).ToList();

                _logger.LogDebug("Retrieved {Count} schedulers", schedulerDtos.Count);
                return Result<List<SchedulerDto>>.Success(schedulerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving schedulers");
                return Result.Failure<List<SchedulerDto>>("SCHEDULER_RETRIEVAL_ERROR", "Failed to retrieve schedulers");
            }
        }

        public async Task<Result<SchedulerDto>> GetSchedulerByIdAsync(int schedulerId)
        {
            try
            {
                var scheduler = await _context.Schedulers
                    .Include(s => s.Indicators)
                    .FirstOrDefaultAsync(s => s.SchedulerID == schedulerId);

                if (scheduler == null)
                {
                    return Result.Failure<SchedulerDto>("SCHEDULER_NOT_FOUND", $"Scheduler with ID {schedulerId} not found");
                }

                var schedulerDto = new SchedulerDto
                {
                    SchedulerID = scheduler.SchedulerID,
                    SchedulerName = scheduler.SchedulerName,
                    SchedulerDescription = scheduler.SchedulerDescription,
                    ScheduleType = scheduler.ScheduleType,
                    IntervalMinutes = scheduler.IntervalMinutes,
                    CronExpression = scheduler.CronExpression,
                    ExecutionDateTime = scheduler.ExecutionDateTime,
                    StartDate = scheduler.StartDate,
                    EndDate = scheduler.EndDate,
                    Timezone = scheduler.Timezone,
                    IsEnabled = scheduler.IsEnabled,
                    CreatedDate = scheduler.CreatedDate,
                    CreatedBy = scheduler.CreatedBy,
                    ModifiedDate = scheduler.ModifiedDate,
                    ModifiedBy = scheduler.ModifiedBy,
                    DisplayText = scheduler.GetDisplayText(),
                    NextExecutionTime = scheduler.GetNextExecutionTime(),
                    IsCurrentlyActive = scheduler.IsCurrentlyActive(),
                    IndicatorCount = scheduler.Indicators?.Count ?? 0
                };

                _logger.LogDebug("Retrieved scheduler {SchedulerId}: {SchedulerName}", schedulerId, scheduler.SchedulerName);
                return Result.Success(schedulerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scheduler {SchedulerId}", schedulerId);
                return Result.Failure<SchedulerDto>("SCHEDULER_RETRIEVAL_ERROR", "Failed to retrieve scheduler");
            }
        }

        public async Task<Result<SchedulerDto>> CreateSchedulerAsync(CreateSchedulerRequest request, string createdBy = "system")
        {
            try
            {
                // Validate the request
                var validationResult = await ValidateSchedulerConfigurationAsync(request);
                if (!validationResult.IsSuccess || !validationResult.Value.IsValid)
                {
                    return Result.Failure<SchedulerDto>("SCHEDULER_VALIDATION_ERROR", string.Join("; ", validationResult.Value.ValidationErrors));
                }

                var scheduler = new Scheduler
                {
                    SchedulerName = request.SchedulerName,
                    SchedulerDescription = request.SchedulerDescription,
                    ScheduleType = request.ScheduleType,
                    IntervalMinutes = request.IntervalMinutes,
                    CronExpression = request.CronExpression,
                    ExecutionDateTime = request.ExecutionDateTime,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Timezone = request.Timezone,
                    IsEnabled = request.IsEnabled,
                    CreatedBy = createdBy,
                    ModifiedBy = createdBy
                };

                _context.Schedulers.Add(scheduler);
                await _context.SaveChangesAsync();

                var schedulerDto = new SchedulerDto
                {
                    SchedulerID = scheduler.SchedulerID,
                    SchedulerName = scheduler.SchedulerName,
                    SchedulerDescription = scheduler.SchedulerDescription,
                    ScheduleType = scheduler.ScheduleType,
                    IntervalMinutes = scheduler.IntervalMinutes,
                    CronExpression = scheduler.CronExpression,
                    ExecutionDateTime = scheduler.ExecutionDateTime,
                    StartDate = scheduler.StartDate,
                    EndDate = scheduler.EndDate,
                    Timezone = scheduler.Timezone,
                    IsEnabled = scheduler.IsEnabled,
                    CreatedDate = scheduler.CreatedDate,
                    CreatedBy = scheduler.CreatedBy,
                    ModifiedDate = scheduler.ModifiedDate,
                    ModifiedBy = scheduler.ModifiedBy,
                    DisplayText = scheduler.GetDisplayText(),
                    NextExecutionTime = scheduler.GetNextExecutionTime(),
                    IsCurrentlyActive = scheduler.IsCurrentlyActive(),
                    IndicatorCount = 0
                };

                _logger.LogInformation("Created scheduler {SchedulerName} with ID {SchedulerId}", scheduler.SchedulerName, scheduler.SchedulerID);
                return Result.Success(schedulerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating scheduler {SchedulerName}", request.SchedulerName);
                return Result.Failure<SchedulerDto>("SCHEDULER_CREATION_ERROR", "Failed to create scheduler");
            }
        }

        public async Task<Result<SchedulerDto>> UpdateSchedulerAsync(int schedulerId, UpdateSchedulerRequest request, string modifiedBy = "system")
        {
            try
            {
                var scheduler = await _context.Schedulers.FindAsync(schedulerId);
                if (scheduler == null)
                {
                    return Result.Failure<SchedulerDto>("SCHEDULER_NOT_FOUND", $"Scheduler with ID {schedulerId} not found");
                }

                // Validate the request
                var createRequest = new CreateSchedulerRequest
                {
                    SchedulerName = request.SchedulerName,
                    SchedulerDescription = request.SchedulerDescription,
                    ScheduleType = request.ScheduleType,
                    IntervalMinutes = request.IntervalMinutes,
                    CronExpression = request.CronExpression,
                    ExecutionDateTime = request.ExecutionDateTime,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Timezone = request.Timezone,
                    IsEnabled = request.IsEnabled
                };

                var validationResult = await ValidateSchedulerConfigurationAsync(createRequest);
                if (!validationResult.IsSuccess || !validationResult.Value.IsValid)
                {
                    return Result.Failure<SchedulerDto>("SCHEDULER_VALIDATION_ERROR", string.Join("; ", validationResult.Value.ValidationErrors));
                }

                // Update scheduler properties
                scheduler.SchedulerName = request.SchedulerName;
                scheduler.SchedulerDescription = request.SchedulerDescription;
                scheduler.ScheduleType = request.ScheduleType;
                scheduler.IntervalMinutes = request.IntervalMinutes;
                scheduler.CronExpression = request.CronExpression;
                scheduler.ExecutionDateTime = request.ExecutionDateTime;
                scheduler.StartDate = request.StartDate;
                scheduler.EndDate = request.EndDate;
                scheduler.Timezone = request.Timezone;
                scheduler.IsEnabled = request.IsEnabled;
                scheduler.UpdateModifiedInfo(modifiedBy);

                await _context.SaveChangesAsync();

                var schedulerDto = new SchedulerDto
                {
                    SchedulerID = scheduler.SchedulerID,
                    SchedulerName = scheduler.SchedulerName,
                    SchedulerDescription = scheduler.SchedulerDescription,
                    ScheduleType = scheduler.ScheduleType,
                    IntervalMinutes = scheduler.IntervalMinutes,
                    CronExpression = scheduler.CronExpression,
                    ExecutionDateTime = scheduler.ExecutionDateTime,
                    StartDate = scheduler.StartDate,
                    EndDate = scheduler.EndDate,
                    Timezone = scheduler.Timezone,
                    IsEnabled = scheduler.IsEnabled,
                    CreatedDate = scheduler.CreatedDate,
                    CreatedBy = scheduler.CreatedBy,
                    ModifiedDate = scheduler.ModifiedDate,
                    ModifiedBy = scheduler.ModifiedBy,
                    DisplayText = scheduler.GetDisplayText(),
                    NextExecutionTime = scheduler.GetNextExecutionTime(),
                    IsCurrentlyActive = scheduler.IsCurrentlyActive(),
                    IndicatorCount = scheduler.Indicators.Count
                };

                _logger.LogInformation("Updated scheduler {SchedulerName} with ID {SchedulerId}", scheduler.SchedulerName, scheduler.SchedulerID);
                return Result.Success(schedulerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating scheduler {SchedulerId}", schedulerId);
                return Result.Failure<SchedulerDto>("SCHEDULER_UPDATE_ERROR", "Failed to update scheduler");
            }
        }

        public async Task<Result<bool>> DeleteSchedulerAsync(int schedulerId)
        {
            try
            {
                var scheduler = await _context.Schedulers
                    .Include(s => s.Indicators)
                    .FirstOrDefaultAsync(s => s.SchedulerID == schedulerId);

                if (scheduler == null)
                {
                    return Result.Failure<bool>("SCHEDULER_NOT_FOUND", $"Scheduler with ID {schedulerId} not found");
                }

                // Check if scheduler is being used by indicators
                if (scheduler.Indicators.Any())
                {
                    return Result.Failure<bool>("SCHEDULER_IN_USE", $"Cannot delete scheduler. It is currently assigned to {scheduler.Indicators.Count} indicator(s)");
                }

                _context.Schedulers.Remove(scheduler);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted scheduler {SchedulerName} with ID {SchedulerId}", scheduler.SchedulerName, scheduler.SchedulerID);
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting scheduler {SchedulerId}", schedulerId);
                return Result.Failure<bool>("SCHEDULER_DELETE_ERROR", "Failed to delete scheduler");
            }
        }

        public async Task<Result<bool>> ToggleSchedulerAsync(int schedulerId, bool isEnabled, string modifiedBy = "system")
        {
            try
            {
                var scheduler = await _context.Schedulers.FindAsync(schedulerId);
                if (scheduler == null)
                {
                    return Result.Failure<bool>("SCHEDULER_NOT_FOUND", $"Scheduler with ID {schedulerId} not found");
                }

                scheduler.IsEnabled = isEnabled;
                scheduler.UpdateModifiedInfo(modifiedBy);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Toggled scheduler {SchedulerName} to {Status}", scheduler.SchedulerName, isEnabled ? "enabled" : "disabled");
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling scheduler {SchedulerId}", schedulerId);
                return Result.Failure<bool>("SCHEDULER_TOGGLE_ERROR", "Failed to toggle scheduler");
            }
        }

        public async Task<Result<SchedulerValidationResult>> ValidateSchedulerConfigurationAsync(CreateSchedulerRequest request)
        {
            await Task.CompletedTask; // For async consistency

            var result = new SchedulerValidationResult();

            // Validate schedule type
            if (!new[] { "interval", "cron", "onetime" }.Contains(request.ScheduleType))
            {
                result.ValidationErrors.Add("Invalid schedule type. Must be 'interval', 'cron', or 'onetime'");
            }

            // Validate based on schedule type
            switch (request.ScheduleType)
            {
                case "interval":
                    if (!request.IntervalMinutes.HasValue || request.IntervalMinutes.Value <= 0)
                    {
                        result.ValidationErrors.Add("Interval minutes must be greater than 0 for interval schedules");
                    }
                    break;

                case "cron":
                    if (string.IsNullOrWhiteSpace(request.CronExpression))
                    {
                        result.ValidationErrors.Add("Cron expression is required for cron schedules");
                    }
                    else if (!IsValidCronExpression(request.CronExpression))
                    {
                        result.ValidationErrors.Add("Invalid cron expression format");
                    }
                    break;

                case "onetime":
                    if (!request.ExecutionDateTime.HasValue)
                    {
                        result.ValidationErrors.Add("Execution date/time is required for one-time schedules");
                    }
                    else if (request.ExecutionDateTime.Value <= DateTime.UtcNow)
                    {
                        result.ValidationErrors.Add("Execution date/time must be in the future for one-time schedules");
                    }
                    break;
            }

            // Validate date ranges
            if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate.Value >= request.EndDate.Value)
            {
                result.ValidationErrors.Add("Start date must be before end date");
            }

            result.IsValid = !result.ValidationErrors.Any();

            // Calculate display text and next execution if valid
            if (result.IsValid)
            {
                var tempScheduler = new Scheduler
                {
                    ScheduleType = request.ScheduleType,
                    IntervalMinutes = request.IntervalMinutes,
                    CronExpression = request.CronExpression,
                    ExecutionDateTime = request.ExecutionDateTime,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    IsEnabled = request.IsEnabled
                };

                result.DisplayText = tempScheduler.GetDisplayText();
                result.NextExecutionTime = tempScheduler.GetNextExecutionTime()?.ToString("yyyy-MM-dd HH:mm:ss UTC");
            }

            return Result.Success(result);
        }

        private bool IsValidCronExpression(string cronExpression)
        {
            // Basic cron validation - in a real implementation, use a proper cron library
            if (string.IsNullOrWhiteSpace(cronExpression))
                return false;

            var parts = cronExpression.Split(' ');
            return parts.Length == 5; // Standard cron has 5 parts: minute hour day month dayofweek
        }

        public async Task<Result<List<SchedulerPresetDto>>> GetSchedulerPresetsAsync()
        {
            await Task.CompletedTask; // For async consistency

            var presets = new List<SchedulerPresetDto>
            {
                // Interval presets
                new() { Name = "Every 5 Minutes", Description = "Execute every 5 minutes", ScheduleType = "interval", IntervalMinutes = 5, Category = "Common" },
                new() { Name = "Every 15 Minutes", Description = "Execute every 15 minutes", ScheduleType = "interval", IntervalMinutes = 15, Category = "Common" },
                new() { Name = "Every 30 Minutes", Description = "Execute every 30 minutes", ScheduleType = "interval", IntervalMinutes = 30, Category = "Common" },
                new() { Name = "Every Hour", Description = "Execute every hour", ScheduleType = "interval", IntervalMinutes = 60, Category = "Hourly" },
                new() { Name = "Every 2 Hours", Description = "Execute every 2 hours", ScheduleType = "interval", IntervalMinutes = 120, Category = "Hourly" },
                new() { Name = "Every 6 Hours", Description = "Execute every 6 hours", ScheduleType = "interval", IntervalMinutes = 360, Category = "Hourly" },
                
                // Cron presets
                new() { Name = "Hourly (Cron)", Description = "Execute at the top of every hour", ScheduleType = "cron", CronExpression = "0 * * * *", Category = "Hourly" },
                new() { Name = "Daily at Midnight", Description = "Execute daily at midnight", ScheduleType = "cron", CronExpression = "0 0 * * *", Category = "Daily" },
                new() { Name = "Daily at 9 AM", Description = "Execute daily at 9 AM", ScheduleType = "cron", CronExpression = "0 9 * * *", Category = "Daily" },
                new() { Name = "Daily at 6 PM", Description = "Execute daily at 6 PM", ScheduleType = "cron", CronExpression = "0 18 * * *", Category = "Daily" },
                new() { Name = "Weekly Monday 9 AM", Description = "Execute every Monday at 9 AM", ScheduleType = "cron", CronExpression = "0 9 * * 1", Category = "Weekly" },
                new() { Name = "Monthly 1st at Midnight", Description = "Execute on the 1st of every month at midnight", ScheduleType = "cron", CronExpression = "0 0 1 * *", Category = "Monthly" }
            };

            return Result.Success(presets);
        }

        public async Task<Result<SchedulerStatsDto>> GetSchedulerStatisticsAsync()
        {
            try
            {
                var schedulers = await _context.Schedulers
                    .Include(s => s.Indicators)
                    .ToListAsync();

                var stats = new SchedulerStatsDto
                {
                    TotalSchedulers = schedulers.Count,
                    ActiveSchedulers = schedulers.Count(s => s.IsEnabled),
                    InactiveSchedulers = schedulers.Count(s => !s.IsEnabled),
                    IntervalSchedulers = schedulers.Count(s => s.ScheduleType == "interval"),
                    CronSchedulers = schedulers.Count(s => s.ScheduleType == "cron"),
                    OneTimeSchedulers = schedulers.Count(s => s.ScheduleType == "onetime"),
                    SchedulersWithIndicators = schedulers.Count(s => s.Indicators.Any()),
                    UnusedSchedulers = schedulers.Count(s => !s.Indicators.Any())
                };

                // Find next scheduled execution
                var activeSchedulers = schedulers.Where(s => s.IsEnabled && s.IsCurrentlyActive()).ToList();
                var nextExecution = activeSchedulers
                    .Select(s => new { Scheduler = s, NextTime = s.GetNextExecutionTime() })
                    .Where(x => x.NextTime.HasValue)
                    .OrderBy(x => x.NextTime)
                    .FirstOrDefault();

                if (nextExecution != null)
                {
                    stats.NextScheduledExecution = nextExecution.NextTime;
                    stats.NextSchedulerName = nextExecution.Scheduler.SchedulerName;
                }

                return Result.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scheduler statistics");
                return Result.Failure<SchedulerStatsDto>("SCHEDULER_STATS_ERROR", "Failed to retrieve scheduler statistics");
            }
        }

        // Additional methods will be implemented in the next part due to length constraints
        public async Task<Result<List<IndicatorWithSchedulerDto>>> GetIndicatorsWithSchedulersAsync()
        {
            // Implementation will be added in next part
            await Task.CompletedTask;
            return Result.Success(new List<IndicatorWithSchedulerDto>());
        }

        public async Task<Result<AssignSchedulerResponse>> AssignSchedulerToIndicatorAsync(AssignSchedulerRequest request, string modifiedBy = "system")
        {
            // Implementation will be added in next part
            await Task.CompletedTask;
            return Result.Success(new AssignSchedulerResponse());
        }

        public async Task<Result<List<IndicatorWithSchedulerDto>>> GetIndicatorsBySchedulerAsync(int schedulerId)
        {
            // Implementation will be added in next part
            await Task.CompletedTask;
            return Result.Success(new List<IndicatorWithSchedulerDto>());
        }

        public async Task<Result<List<IndicatorWithSchedulerDto>>> GetDueIndicatorsAsync()
        {
            try
            {
                _logger.LogDebug("Getting due indicators");

                // Get indicators that are due for execution
                var indicators = await _context.Indicators
                    .Include(i => i.Scheduler)
                    .Include(i => i.OwnerContact)
                    .Where(i => i.IsActive && i.SchedulerID.HasValue && i.Scheduler != null && i.Scheduler.IsEnabled)
                    .ToListAsync();

                var dueList = new List<IndicatorWithSchedulerDto>();
                var now = DateTime.UtcNow;

                foreach (var indicator in indicators)
                {
                    if (indicator.Scheduler == null) continue;

                    // Calculate if indicator is due for execution
                    var nextExecution = CalculateNextExecutionTime(indicator.Scheduler, indicator.LastRun);

                    if (nextExecution.HasValue && nextExecution.Value <= now)
                    {
                        var dto = new IndicatorWithSchedulerDto
                        {
                            IndicatorID = indicator.IndicatorID,
                            IndicatorName = indicator.IndicatorName,
                            IndicatorCode = indicator.IndicatorCode ?? string.Empty,
                            IndicatorDesc = indicator.IndicatorDesc,
                            CollectorID = indicator.CollectorID,
                            CollectorItemName = indicator.CollectorItemName,
                            Priority = indicator.Priority ?? "Medium",
                            LastMinutes = indicator.LastMinutes,
                            ThresholdType = indicator.ThresholdType ?? string.Empty,
                            ThresholdField = indicator.ThresholdField ?? string.Empty,
                            ThresholdComparison = indicator.ThresholdComparison ?? string.Empty,
                            ThresholdValue = indicator.ThresholdValue,
                            OwnerContactId = indicator.OwnerContactId,
                            IsActive = indicator.IsActive,
                            CreatedDate = indicator.CreatedDate,
                            UpdatedDate = indicator.UpdatedDate,
                            LastRun = indicator.LastRun,
                            SchedulerID = indicator.SchedulerID,
                            SchedulerName = indicator.Scheduler.SchedulerName,
                            SchedulerDescription = indicator.Scheduler.SchedulerDescription,
                            ScheduleType = indicator.Scheduler.ScheduleType,
                            IntervalMinutes = indicator.Scheduler.IntervalMinutes,
                            CronExpression = indicator.Scheduler.CronExpression,
                            ExecutionDateTime = indicator.Scheduler.ExecutionDateTime,
                            Timezone = indicator.Scheduler.Timezone,
                            SchedulerEnabled = indicator.Scheduler.IsEnabled,
                            NextExecutionTime = nextExecution,
                            OwnerContactName = indicator.OwnerContact?.Name,
                            OwnerContactEmail = indicator.OwnerContact?.Email
                        };

                        dueList.Add(dto);
                    }
                }

                _logger.LogDebug("Found {Count} due indicators", dueList.Count);
                return Result.Success(dueList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting due indicators");
                return Result.Failure<List<IndicatorWithSchedulerDto>>("DUE_INDICATORS_ERROR", "Failed to get due indicators");
            }
        }

        public async Task<Result<DateTime?>> GetNextExecutionTimeAsync(int schedulerId, DateTime? lastExecution = null)
        {
            // Implementation will be added in next part
            await Task.CompletedTask;
            return Result.Success<DateTime?>(null);
        }

        public async Task<Result<List<IndicatorWithSchedulerDto>>> GetUpcomingExecutionsAsync(int hours = 24)
        {
            try
            {
                _logger.LogDebug("Getting upcoming executions for next {Hours} hours", hours);

                var cutoffTime = DateTime.UtcNow.AddHours(hours);

                // Get indicators with schedulers that have upcoming executions
                var indicators = await _context.Indicators
                    .Include(i => i.Scheduler)
                    .Include(i => i.OwnerContact)
                    .Where(i => i.IsActive &&
                               i.SchedulerID.HasValue &&
                               i.Scheduler != null &&
                               i.Scheduler.IsEnabled)
                    .ToListAsync();

                var upcomingList = new List<IndicatorWithSchedulerDto>();

                foreach (var indicator in indicators)
                {
                    if (indicator.Scheduler == null) continue;

                    // Calculate next execution time (simplified logic)
                    var nextExecution = CalculateNextExecutionTime(indicator.Scheduler, indicator.LastRun);

                    if (nextExecution.HasValue && nextExecution.Value <= cutoffTime)
                    {
                        var dto = new IndicatorWithSchedulerDto
                        {
                            IndicatorID = indicator.IndicatorID,
                            IndicatorName = indicator.IndicatorName,
                            IndicatorCode = indicator.IndicatorCode ?? string.Empty,
                            IndicatorDesc = indicator.IndicatorDesc,
                            CollectorID = indicator.CollectorID,
                            CollectorItemName = indicator.CollectorItemName,
                            Priority = indicator.Priority ?? "Medium",
                            LastMinutes = indicator.LastMinutes,
                            ThresholdType = indicator.ThresholdType ?? string.Empty,
                            ThresholdField = indicator.ThresholdField ?? string.Empty,
                            ThresholdComparison = indicator.ThresholdComparison ?? string.Empty,
                            ThresholdValue = indicator.ThresholdValue,
                            OwnerContactId = indicator.OwnerContactId,
                            IsActive = indicator.IsActive,
                            CreatedDate = indicator.CreatedDate,
                            UpdatedDate = indicator.UpdatedDate,
                            LastRun = indicator.LastRun,
                            SchedulerID = indicator.SchedulerID,
                            SchedulerName = indicator.Scheduler.SchedulerName,
                            SchedulerDescription = indicator.Scheduler.SchedulerDescription,
                            ScheduleType = indicator.Scheduler.ScheduleType,
                            IntervalMinutes = indicator.Scheduler.IntervalMinutes,
                            CronExpression = indicator.Scheduler.CronExpression,
                            ExecutionDateTime = indicator.Scheduler.ExecutionDateTime,
                            Timezone = indicator.Scheduler.Timezone,
                            SchedulerEnabled = indicator.Scheduler.IsEnabled,
                            NextExecutionTime = nextExecution,
                            OwnerContactName = indicator.OwnerContact?.Name,
                            OwnerContactEmail = indicator.OwnerContact?.Email
                        };

                        upcomingList.Add(dto);
                    }
                }

                // Sort by next execution time
                upcomingList = upcomingList.OrderBy(x => x.NextExecutionTime).ToList();

                _logger.LogDebug("Found {Count} upcoming executions for next {Hours} hours", upcomingList.Count, hours);
                return Result.Success(upcomingList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming executions for next {Hours} hours", hours);
                return Result.Failure<List<IndicatorWithSchedulerDto>>("UPCOMING_EXECUTIONS_ERROR", "Failed to get upcoming executions");
            }
        }

        public async Task<Result<bool>> BulkAssignSchedulerAsync(List<long> indicatorIds, int? schedulerId, string modifiedBy = "system")
        {
            // Implementation will be added in next part
            await Task.CompletedTask;
            return Result.Success(true);
        }

        public async Task<Result<bool>> BulkUpdateSchedulerStatusAsync(List<int> schedulerIds, bool isEnabled, string modifiedBy = "system")
        {
            // Implementation will be added in next part
            await Task.CompletedTask;
            return Result.Success(true);
        }

        private DateTime? CalculateNextExecutionTime(Scheduler scheduler, DateTime? lastExecution)
        {
            try
            {
                var now = DateTime.UtcNow;
                var baseTime = lastExecution ?? now;

                switch (scheduler.ScheduleType.ToLower())
                {
                    case "interval":
                        if (scheduler.IntervalMinutes.HasValue && scheduler.IntervalMinutes.Value > 0)
                        {
                            return baseTime.AddMinutes(scheduler.IntervalMinutes.Value);
                        }
                        break;

                    case "daily":
                        // For daily schedules, calculate next day at the same time
                        if (scheduler.ExecutionDateTime.HasValue)
                        {
                            var executionTime = scheduler.ExecutionDateTime.Value.TimeOfDay;
                            var nextExecution = now.Date.Add(executionTime);
                            if (nextExecution <= now)
                            {
                                nextExecution = nextExecution.AddDays(1);
                            }
                            return nextExecution;
                        }
                        break;

                    case "weekly":
                        // For weekly schedules, calculate next week
                        if (scheduler.ExecutionDateTime.HasValue)
                        {
                            var executionTime = scheduler.ExecutionDateTime.Value;
                            var nextExecution = executionTime;
                            while (nextExecution <= now)
                            {
                                nextExecution = nextExecution.AddDays(7);
                            }
                            return nextExecution;
                        }
                        break;

                    case "cron":
                        // For cron expressions, we would need a cron parser
                        // For now, return a default interval
                        return baseTime.AddHours(1);

                    default:
                        // Default to 1 hour interval
                        return baseTime.AddHours(1);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating next execution time for scheduler {SchedulerId}", scheduler.SchedulerID);
                return null;
            }
        }
    }
}
