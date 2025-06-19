using AutoMapper;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.DTOs.Indicators;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.DTOs;

namespace MonitoringGrid.Api.Mapping;

/// <summary>
/// AutoMapper profile for Indicator entities and DTOs
/// </summary>
public class IndicatorMappingProfile : Profile
{
    public IndicatorMappingProfile()
    {
        // Scheduler entity to DTO mapping
        CreateMap<Scheduler, IndicatorSchedulerInfo>()
            .ForMember(dest => dest.SchedulerId, opt => opt.MapFrom(src => src.SchedulerID))
            .ForMember(dest => dest.SchedulerName, opt => opt.MapFrom(src => src.SchedulerName))
            .ForMember(dest => dest.ScheduleType, opt => opt.MapFrom(src => src.ScheduleType))
            .ForMember(dest => dest.IntervalMinutes, opt => opt.MapFrom(src => src.IntervalMinutes))
            .ForMember(dest => dest.CronExpression, opt => opt.MapFrom(src => src.CronExpression))
            .ForMember(dest => dest.IsEnabled, opt => opt.MapFrom(src => src.IsEnabled))
            .ForMember(dest => dest.NextExecution, opt => opt.MapFrom(src => src.GetNextExecutionTime(null)));



        // Indicator entity to response DTO mappings
        CreateMap<Indicator, IndicatorResponse>()
            .ForMember(dest => dest.IndicatorID, opt => opt.MapFrom(src => src.IndicatorID))
            .ForMember(dest => dest.IndicatorName, opt => opt.MapFrom(src => src.IndicatorName))
            .ForMember(dest => dest.IndicatorDescription, opt => opt.MapFrom(src => src.IndicatorDesc))
            .ForMember(dest => dest.OwnerContactId, opt => opt.MapFrom(src => src.OwnerContactId))
            .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => src.OwnerContact != null ? src.OwnerContact.Name : null))
            .ForMember(dest => dest.CollectorId, opt => opt.MapFrom(src => src.CollectorID))
            .ForMember(dest => dest.CollectorName, opt => opt.MapFrom(src => src.CollectorItemName))
            .ForMember(dest => dest.SchedulerId, opt => opt.MapFrom(src => src.SchedulerID))
            .ForMember(dest => dest.SchedulerName, opt => opt.MapFrom(src => src.Scheduler != null ? src.Scheduler.SchedulerName : null))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.LastMinutes, opt => opt.MapFrom(src => src.LastMinutes))
            .ForMember(dest => dest.AlertThreshold, opt => opt.MapFrom(src => src.ThresholdValue))
            .ForMember(dest => dest.AlertOperator, opt => opt.MapFrom(src => src.ThresholdComparison))
            .ForMember(dest => dest.LastRun, opt => opt.MapFrom(src => src.LastRun))
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate))
            .ForMember(dest => dest.ModifiedDate, opt => opt.MapFrom(src => src.UpdatedDate))
            .ForMember(dest => dest.Scheduler, opt => opt.MapFrom(src => src.Scheduler));

        // Add mapping to frontend-compatible DTO structure
        CreateMap<Indicator, object>()
            .ConvertUsing(src => new
            {
                indicatorID = src.IndicatorID,
                indicatorName = src.IndicatorName,
                indicatorCode = src.IndicatorCode,
                indicatorDescription = src.IndicatorDesc,
                collectorId = src.CollectorID,
                collectorName = "Unknown", // Will be populated by controller if needed
                collectorItemName = src.CollectorItemName,
                schedulerId = src.SchedulerID,
                isActive = src.IsActive,
                lastMinutes = src.LastMinutes,
                thresholdType = src.ThresholdType,
                thresholdField = src.ThresholdField,
                thresholdComparison = src.ThresholdComparison,
                thresholdValue = src.ThresholdValue,
                alertThreshold = src.ThresholdValue, // Frontend expects this field name
                alertOperator = src.ThresholdComparison, // Frontend expects this field name
                priority = src.Priority,
                ownerContactId = src.OwnerContactId,
                ownerName = src.OwnerContact != null ? src.OwnerContact.Name : null,
                averageLastDays = src.AverageLastDays,
                createdDate = src.CreatedDate,
                modifiedDate = src.UpdatedDate,
                lastRun = src.LastRun,
                lastRunResult = src.LastRunResult,
                isCurrentlyRunning = src.IsCurrentlyRunning,
                executionStartTime = src.ExecutionStartTime,
                executionContext = src.ExecutionContext,
                ownerContact = src.OwnerContact != null ? new
                {
                    contactID = src.OwnerContact.ContactId,
                    name = src.OwnerContact.Name,
                    email = src.OwnerContact.Email,
                    phone = src.OwnerContact.Phone,
                    isActive = src.OwnerContact.IsActive,
                    createdDate = src.OwnerContact.CreatedDate,
                    modifiedDate = src.OwnerContact.ModifiedDate
                } : null,
                contacts = src.IndicatorContacts != null ? src.IndicatorContacts
                    .Where(ic => ic.IsActive && ic.Contact != null)
                    .Select(ic => new
                    {
                        contactID = ic.Contact.ContactId,
                        name = ic.Contact.Name,
                        email = ic.Contact.Email,
                        phone = ic.Contact.Phone,
                        isActive = ic.Contact.IsActive,
                        createdDate = ic.Contact.CreatedDate,
                        modifiedDate = ic.Contact.ModifiedDate
                    }).ToArray() : new object[0],
                scheduler = src.Scheduler != null ? new
                {
                    schedulerId = src.Scheduler.SchedulerID,
                    schedulerName = src.Scheduler.SchedulerName,
                    schedulerDescription = src.Scheduler.SchedulerDescription,
                    scheduleType = src.Scheduler.ScheduleType,
                    intervalMinutes = src.Scheduler.IntervalMinutes,
                    cronExpression = src.Scheduler.CronExpression,
                    isEnabled = src.Scheduler.IsEnabled,
                    timezone = src.Scheduler.Timezone,
                    executionDateTime = src.Scheduler.ExecutionDateTime,
                    startDate = src.Scheduler.StartDate,
                    endDate = src.Scheduler.EndDate,
                    createdDate = src.Scheduler.CreatedDate,
                    modifiedDate = src.Scheduler.ModifiedDate,
                    nextExecution = src.Scheduler.GetNextExecutionTime(src.LastRun)
                } : null
            });

        // List to paginated response mapping
        CreateMap<List<IndicatorResponse>, PaginatedIndicatorsResponse>()
            .ForMember(dest => dest.Indicators, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.TotalCount, opt => opt.Ignore())
            .ForMember(dest => dest.Page, opt => opt.Ignore())
            .ForMember(dest => dest.PageSize, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPages, opt => opt.Ignore())
            .ForMember(dest => dest.HasNextPage, opt => opt.Ignore())
            .ForMember(dest => dest.HasPreviousPage, opt => opt.Ignore())
            .ForMember(dest => dest.Summary, opt => opt.Ignore())
            .ForMember(dest => dest.QueryMetrics, opt => opt.Ignore());

        // Scheduler mappings
        CreateMap<Scheduler, SchedulerDto>()
            .ForMember(dest => dest.DisplayText, opt => opt.MapFrom(src => src.GetDisplayText()))
            .ForMember(dest => dest.NextExecutionTime, opt => opt.MapFrom(src => src.GetNextExecutionTime(null)))
            .ForMember(dest => dest.IsCurrentlyActive, opt => opt.MapFrom(src => src.IsCurrentlyActive()))
            .ForMember(dest => dest.IndicatorCount, opt => opt.MapFrom(src => src.Indicators.Count));

        CreateMap<CreateSchedulerRequest, Scheduler>()
            .ForMember(dest => dest.SchedulerID, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Indicators, opt => opt.Ignore());

        CreateMap<UpdateSchedulerRequest, Scheduler>()
            .ForMember(dest => dest.SchedulerID, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Indicators, opt => opt.Ignore());

        // CreateIndicatorRequest doesn't exist - commented out
        // CreateMap<CreateIndicatorRequest, Indicator>()
        //     .ForMember(dest => dest.IndicatorID, opt => opt.Ignore())
        //     .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
        //     .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
        //     .ForMember(dest => dest.LastRun, opt => opt.Ignore())
        //     .ForMember(dest => dest.LastRunResult, opt => opt.Ignore())
        //     .ForMember(dest => dest.AverageHour, opt => opt.Ignore())
        //     .ForMember(dest => dest.IsCurrentlyRunning, opt => opt.Ignore())
        //     .ForMember(dest => dest.ExecutionStartTime, opt => opt.Ignore())
        //     .ForMember(dest => dest.ExecutionContext, opt => opt.Ignore())
        //     .ForMember(dest => dest.OwnerContact, opt => opt.Ignore())
        //     .ForMember(dest => dest.IndicatorContacts, opt => opt.Ignore())
        //     .ForMember(dest => dest.AlertLogs, opt => opt.Ignore())
        //     .ForMember(dest => dest.Scheduler, opt => opt.Ignore());

        // UpdateIndicatorRequest doesn't exist - commented out
        // CreateMap<UpdateIndicatorRequest, Indicator>()
        //     .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
        //     .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
        //     .ForMember(dest => dest.LastRun, opt => opt.Ignore())
        //     .ForMember(dest => dest.LastRunResult, opt => opt.Ignore())
        //     .ForMember(dest => dest.AverageHour, opt => opt.Ignore())
        //     .ForMember(dest => dest.IsCurrentlyRunning, opt => opt.Ignore())
        //     .ForMember(dest => dest.ExecutionStartTime, opt => opt.Ignore())
        //     .ForMember(dest => dest.ExecutionContext, opt => opt.Ignore())
        //     .ForMember(dest => dest.OwnerContact, opt => opt.Ignore())
        //     .ForMember(dest => dest.IndicatorContacts, opt => opt.Ignore())
        //     .ForMember(dest => dest.AlertLogs, opt => opt.Ignore())
        //     .ForMember(dest => dest.Scheduler, opt => opt.Ignore());

        // Indicator execution result mappings
        CreateMap<MonitoringGrid.Core.Models.IndicatorExecutionResult, MonitoringGrid.Api.DTOs.Indicators.IndicatorExecutionResultResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.WasSuccessful ? "Success" : "Failed"))
            .ForMember(dest => dest.IsSuccess, opt => opt.MapFrom(src => src.WasSuccessful))
            .ForMember(dest => dest.DurationMs, opt => opt.MapFrom(src => (long)src.ExecutionDuration.TotalMilliseconds))
            .ForMember(dest => dest.ResultCount, opt => opt.MapFrom(src => src.RawData != null ? src.RawData.Count : 0))
            .ForMember(dest => dest.Results, opt => opt.Ignore()) // Will be populated separately if needed
            .ForMember(dest => dest.SqlQuery, opt => opt.Ignore()); // Will be populated separately if needed

        // Dashboard mappings
        CreateMap<MonitoringGrid.Core.Models.IndicatorDashboard, MonitoringGrid.Api.DTOs.Indicators.IndicatorDashboardResponse>();

        // Result<T> mappings for dashboard
        CreateMap<MonitoringGrid.Core.Common.Result<MonitoringGrid.Core.Models.IndicatorDashboard>, MonitoringGrid.Api.DTOs.Indicators.IndicatorDashboardResponse>()
            .ConvertUsing((src, dest, context) => src.IsSuccess ? context.Mapper.Map<MonitoringGrid.Api.DTOs.Indicators.IndicatorDashboardResponse>(src.Value) : new MonitoringGrid.Api.DTOs.Indicators.IndicatorDashboardResponse());

        // Statistics mappings - These DTOs don't exist - commented out
        // CreateMap<IndicatorStatistics, IndicatorStatisticsDto>();
        // CreateMap<IndicatorValueTrend, IndicatorValueTrendDto>();

        // Test result mappings - These DTOs don't exist - commented out
        // CreateMap<IndicatorTestResult, IndicatorExecutionResultDto>()
        //     .ForMember(dest => dest.ExecutionTime, opt => opt.MapFrom(src => DateTime.UtcNow))
        //     .ForMember(dest => dest.ExecutionContext, opt => opt.MapFrom(src => "Test"));

        // Execution status mappings - These DTOs don't exist - commented out
        // CreateMap<IndicatorExecutionStatus, IndicatorStatusDto>()
        //     .ForMember(dest => dest.NextRun, opt => opt.MapFrom(src => src.NextRun))
        //     .ForMember(dest => dest.LastError, opt => opt.MapFrom(src => src.Status == "error" ? "Execution error" : null));

        // Execution history mappings - These DTOs don't exist - commented out
        // CreateMap<IndicatorExecutionHistory, IndicatorExecutionSummaryDto>()
        //     .ForMember(dest => dest.IndicatorName, opt => opt.Ignore())
        //     .ForMember(dest => dest.Priority, opt => opt.Ignore());

        // Collector mappings
        CreateMap<Collector, MonitoringGrid.Core.Entities.CollectorDto>()
            .ForMember(dest => dest.AvailableItems, opt => opt.MapFrom(src => src.Statistics.Select(s => s.ItemName).Distinct()));

        // CollectorStatisticDto doesn't exist - commented out
        // CreateMap<CollectorStatistic, MonitoringGrid.Core.Entities.CollectorStatisticDto>();

        // Request to query/command mappings
        CreateMap<MonitoringGrid.Api.DTOs.Indicators.GetIndicatorsRequest, GetIndicatorsQuery>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.OwnerContactId, opt => opt.MapFrom(src => src.OwnerContactId.HasValue ? (int)src.OwnerContactId.Value : (int?)null))
            .ForMember(dest => dest.CollectorId, opt => opt.MapFrom(src => src.CollectorId.HasValue ? (int)src.CollectorId.Value : (int?)null))
            .ForMember(dest => dest.SearchTerm, opt => opt.MapFrom(src => src.SearchText))
            .ForMember(dest => dest.Page, opt => opt.MapFrom(src => src.Page))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PageSize))
            .ForMember(dest => dest.SortBy, opt => opt.MapFrom(src => src.SortBy ?? "IndicatorName"))
            .ForMember(dest => dest.SortDirection, opt => opt.MapFrom(src => src.SortDirection ?? "asc"));

        // CreateMap<CreateIndicatorRequest, CreateIndicatorCommand>();

        // Map UpdateIndicatorRequest DTO to UpdateIndicatorCommand
        CreateMap<MonitoringGrid.Api.DTOs.Indicators.UpdateIndicatorRequest, UpdateIndicatorCommand>()
            .ForMember(dest => dest.IndicatorID, opt => opt.MapFrom(src => src.IndicatorID))
            .ForMember(dest => dest.IndicatorName, opt => opt.MapFrom(src => src.IndicatorName))
            .ForMember(dest => dest.IndicatorCode, opt => opt.MapFrom(src => src.IndicatorName.Replace(" ", "_").ToUpper())) // Generate code from name
            .ForMember(dest => dest.IndicatorDesc, opt => opt.MapFrom(src => src.IndicatorDescription))
            .ForMember(dest => dest.CollectorID, opt => opt.MapFrom(src => src.CollectorId))
            .ForMember(dest => dest.CollectorItemName, opt => opt.MapFrom(src => "Bingo")) // Default to "Bingo" for BetsByPlayMode collector
            .ForMember(dest => dest.SchedulerID, opt => opt.MapFrom(src => src.SchedulerId))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.LastMinutes, opt => opt.MapFrom(src => src.LastMinutes))
            .ForMember(dest => dest.ThresholdType, opt => opt.MapFrom(src => "threshold_value")) // Default threshold type
            .ForMember(dest => dest.ThresholdField, opt => opt.MapFrom(src => "Total")) // Default threshold field
            .ForMember(dest => dest.ThresholdComparison, opt => opt.MapFrom(src => src.AlertOperator ?? "gt"))
            .ForMember(dest => dest.ThresholdValue, opt => opt.MapFrom(src => src.AlertThreshold ?? 0))
            .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => "medium")) // Default priority
            .ForMember(dest => dest.OwnerContactId, opt => opt.MapFrom(src => (int)src.OwnerContactId))
            .ForMember(dest => dest.AverageLastDays, opt => opt.MapFrom(src => (int?)null))
            .ForMember(dest => dest.ContactIds, opt => opt.MapFrom(src => new List<int>()));

        // CreateMap<ExecuteIndicatorRequest, ExecuteIndicatorCommand>();
        // CreateMap<TestIndicatorRequest, ExecuteIndicatorCommand>()
        //     .ForMember(dest => dest.ExecutionContext, opt => opt.MapFrom(src => "Test"))
        //     .ForMember(dest => dest.SaveResults, opt => opt.MapFrom(src => false));

        // Bulk operation mappings - BulkIndicatorOperationRequest doesn't exist - commented out
        // CreateMap<BulkIndicatorOperationRequest, List<long>>()
        //     .ConvertUsing(src => src.IndicatorIDs);
    }
}
