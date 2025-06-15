using AutoMapper;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs;
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
        // Indicator mappings
        CreateMap<Indicator, IndicatorDto>()
            .ForMember(dest => dest.OwnerContact, opt => opt.MapFrom(src => src.OwnerContact))
            .ForMember(dest => dest.Contacts, opt => opt.MapFrom(src => src.IndicatorContacts.Select(ic => ic.Contact)))
            .ForMember(dest => dest.Scheduler, opt => opt.MapFrom(src => src.Scheduler));

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

        CreateMap<CreateIndicatorRequest, Indicator>()
            .ForMember(dest => dest.IndicatorID, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastRun, opt => opt.Ignore())
            .ForMember(dest => dest.LastRunResult, opt => opt.Ignore())
            .ForMember(dest => dest.AverageHour, opt => opt.Ignore())
            .ForMember(dest => dest.IsCurrentlyRunning, opt => opt.Ignore())
            .ForMember(dest => dest.ExecutionStartTime, opt => opt.Ignore())
            .ForMember(dest => dest.ExecutionContext, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerContact, opt => opt.Ignore())
            .ForMember(dest => dest.IndicatorContacts, opt => opt.Ignore())
            .ForMember(dest => dest.AlertLogs, opt => opt.Ignore())
            .ForMember(dest => dest.Scheduler, opt => opt.Ignore());

        CreateMap<UpdateIndicatorRequest, Indicator>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.LastRun, opt => opt.Ignore())
            .ForMember(dest => dest.LastRunResult, opt => opt.Ignore())
            .ForMember(dest => dest.AverageHour, opt => opt.Ignore())
            .ForMember(dest => dest.IsCurrentlyRunning, opt => opt.Ignore())
            .ForMember(dest => dest.ExecutionStartTime, opt => opt.Ignore())
            .ForMember(dest => dest.ExecutionContext, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerContact, opt => opt.Ignore())
            .ForMember(dest => dest.IndicatorContacts, opt => opt.Ignore())
            .ForMember(dest => dest.AlertLogs, opt => opt.Ignore())
            .ForMember(dest => dest.Scheduler, opt => opt.Ignore());

        // Indicator execution result mappings
        CreateMap<IndicatorExecutionResult, IndicatorExecutionResultDto>();

        // Dashboard mappings
        CreateMap<IndicatorDashboard, IndicatorDashboardDto>();
        CreateMap<IndicatorExecutionSummary, IndicatorExecutionSummaryDto>();
        CreateMap<IndicatorCountByPriority, IndicatorCountByPriorityDto>();

        // Statistics mappings
        CreateMap<IndicatorStatistics, IndicatorStatisticsDto>();
        CreateMap<IndicatorValueTrend, IndicatorValueTrendDto>();

        // Test result mappings
        CreateMap<IndicatorTestResult, IndicatorExecutionResultDto>()
            .ForMember(dest => dest.ExecutionTime, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.ExecutionContext, opt => opt.MapFrom(src => "Test"));

        // Execution status mappings
        CreateMap<IndicatorExecutionStatus, IndicatorStatusDto>()
            .ForMember(dest => dest.NextRun, opt => opt.MapFrom(src => src.NextRun))
            .ForMember(dest => dest.LastError, opt => opt.MapFrom(src => src.Status == "error" ? "Execution error" : null));

        // Execution history mappings
        CreateMap<IndicatorExecutionHistory, IndicatorExecutionSummaryDto>()
            .ForMember(dest => dest.IndicatorName, opt => opt.Ignore())
            .ForMember(dest => dest.Priority, opt => opt.Ignore());

        // Collector mappings
        CreateMap<Collector, MonitoringGrid.Core.Entities.CollectorDto>()
            .ForMember(dest => dest.AvailableItems, opt => opt.MapFrom(src => src.Statistics.Select(s => s.ItemName).Distinct()));

        CreateMap<CollectorStatistic, MonitoringGrid.Core.Entities.CollectorStatisticDto>();

        // Request to command mappings
        CreateMap<CreateIndicatorRequest, CreateIndicatorCommand>();
        CreateMap<UpdateIndicatorRequest, UpdateIndicatorCommand>();
        CreateMap<ExecuteIndicatorRequest, ExecuteIndicatorCommand>();
        CreateMap<TestIndicatorRequest, ExecuteIndicatorCommand>()
            .ForMember(dest => dest.ExecutionContext, opt => opt.MapFrom(src => "Test"))
            .ForMember(dest => dest.SaveResults, opt => opt.MapFrom(src => false));

        CreateMap<IndicatorFilterRequest, GetIndicatorsQuery>();

        // Bulk operation mappings
        CreateMap<BulkIndicatorOperationRequest, List<long>>()
            .ConvertUsing(src => src.IndicatorIDs);
    }
}
