using AutoMapper;

using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.DTOs.Indicators;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Models;
using System.Text.Json;

namespace MonitoringGrid.Api.Mapping;

/// <summary>
/// AutoMapper profile for mapping between entities and DTOs
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Contact mappings
        CreateMap<Contact, ContactDto>()
            .ForMember(dest => dest.ContactID, opt => opt.MapFrom(src => src.ContactId))
            .ForMember(dest => dest.AssignedIndicators, opt => opt.MapFrom(src =>
                src.IndicatorContacts != null ? src.IndicatorContacts.Where(ic => ic.IsActive).Select(ic => new IndicatorSummaryDto
                {
                    IndicatorId = ic.IndicatorID,
                    IndicatorName = ic.Indicator != null ? ic.Indicator.IndicatorName : "Unknown",
                    Owner = ic.Indicator != null ? ic.Indicator.OwnerContactId.ToString() : "Unknown",
                    Priority = ic.Indicator != null ? ic.Indicator.Priority : "Unknown",
                    IsActive = ic.Indicator != null ? ic.Indicator.IsActive : false
                }).ToList() : new List<IndicatorSummaryDto>()));

        // CreateContactRequest DTO doesn't exist - commented out
        // CreateMap<MonitoringGrid.Api.DTOs.CreateContactRequest, Contact>()
            // .ForMember(dest => dest.ContactId, opt => opt.Ignore())
            // .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            // .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            // .ForMember(dest => dest.IndicatorContacts, opt => opt.Ignore());

        // UpdateContactRequest DTO doesn't exist - commented out
        // CreateMap<MonitoringGrid.Api.DTOs.UpdateContactRequest, Contact>()
            // .ForMember(dest => dest.ContactId, opt => opt.MapFrom(src => src.ContactID))
            // .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            // .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            // .ForMember(dest => dest.IndicatorContacts, opt => opt.Ignore());

        // Alert mappings
        CreateMap<AlertLog, AlertLogDto>()
            .ForMember(dest => dest.IndicatorName, opt => opt.MapFrom(src => src.Indicator.IndicatorName))
            .ForMember(dest => dest.IndicatorOwner, opt => opt.MapFrom(src => src.Indicator.OwnerContactId.ToString()));

        // System status mappings
        CreateMap<SystemStatus, SystemStatusDto>();

        // Configuration mappings
        CreateMap<Config, ConfigDto>();
        CreateMap<CreateConfigRequest, Config>()
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore());
        CreateMap<UpdateConfigRequest, Config>()
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore());

        // Core Models to API DTOs mappings
        CreateMap<AlertFilterDto, AlertFilter>();
        CreateMap<AlertFilter, AlertFilterDto>();

        CreateMap<AlertStatistics, AlertStatisticsDto>();
        CreateMap<AlertTrend, AlertTrendDto>();
        // IndicatorAlertSummary mapping removed - entity not defined

        CreateMap<AlertDashboard, AlertDashboardDto>()
            .ForMember(dest => dest.RecentAlerts, opt => opt.Ignore())
            .ForMember(dest => dest.TopAlertingIndicators, opt => opt.Ignore());

        // Indicator mappings
        CreateMap<Indicator, IndicatorResponse>()
            .ForMember(dest => dest.IndicatorID, opt => opt.MapFrom(src => src.IndicatorID))
            .ForMember(dest => dest.IndicatorName, opt => opt.MapFrom(src => src.IndicatorName))
            .ForMember(dest => dest.IndicatorDescription, opt => opt.MapFrom(src => src.IndicatorDesc))
            .ForMember(dest => dest.OwnerContactId, opt => opt.MapFrom(src => src.OwnerContactId))
            .ForMember(dest => dest.OwnerName, opt => opt.MapFrom(src => src.OwnerContact != null ? src.OwnerContact.Name : null))
            .ForMember(dest => dest.CollectorId, opt => opt.MapFrom(src => src.CollectorID))
            .ForMember(dest => dest.CollectorName, opt => opt.MapFrom(src => src.CollectorItemName))
            .ForMember(dest => dest.SqlQuery, opt => opt.MapFrom(src => string.Empty)) // Not available in entity
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.LastMinutes, opt => opt.MapFrom(src => src.LastMinutes))
            .ForMember(dest => dest.SchedulerId, opt => opt.MapFrom(src => src.SchedulerID))
            .ForMember(dest => dest.SchedulerName, opt => opt.MapFrom(src => src.Scheduler != null ? src.Scheduler.SchedulerName : null))
            .ForMember(dest => dest.AlertThreshold, opt => opt.MapFrom(src => src.ThresholdValue))
            .ForMember(dest => dest.AlertOperator, opt => opt.MapFrom(src => src.ThresholdComparison))
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.CreatedDate ?? DateTime.UtcNow))
            .ForMember(dest => dest.ModifiedDate, opt => opt.MapFrom(src => src.UpdatedDate))
            .ForMember(dest => dest.LastRun, opt => opt.Ignore()) // Will be populated by service if needed
            .ForMember(dest => dest.LastRunStatus, opt => opt.Ignore()) // Will be populated by service if needed
            .ForMember(dest => dest.LastRunDurationMs, opt => opt.Ignore()) // Will be populated by service if needed
            .ForMember(dest => dest.LastRunResultCount, opt => opt.Ignore()) // Will be populated by service if needed
            .ForMember(dest => dest.NextRun, opt => opt.Ignore()) // Will be populated by service if needed
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Not available in entity
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore()) // Not available in entity
            .ForMember(dest => dest.Configuration, opt => opt.Ignore()) // Not available in entity
            .ForMember(dest => dest.ExecutionStats, opt => opt.Ignore()) // Will be populated by service if needed
            .ForMember(dest => dest.Scheduler, opt => opt.Ignore()) // Will be populated by service if needed
            .ForMember(dest => dest.Collector, opt => opt.Ignore()) // Will be populated by service if needed
            .ForMember(dest => dest.RecentExecutions, opt => opt.Ignore()) // Will be populated by service if needed
            .ForMember(dest => dest.Details, opt => opt.Ignore()); // Will be populated by service if needed
    }




}

/// <summary>
/// System status DTO
/// </summary>
public class SystemStatusDto
{
    public int StatusId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime LastHeartbeat { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int ProcessedIndicators { get; set; }
    public int AlertsSent { get; set; }
    public TimeSpan TimeSinceLastHeartbeat => DateTime.UtcNow - LastHeartbeat;
    public bool IsHealthy => TimeSinceLastHeartbeat.TotalMinutes < 5 && Status == "Running";
}

/// <summary>
/// Configuration DTO
/// </summary>
public class ConfigDto
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ModifiedDate { get; set; }
}

/// <summary>
/// Configuration creation request
/// </summary>
public class CreateConfigRequest
{
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Configuration update request
/// </summary>
public class UpdateConfigRequest : CreateConfigRequest
{
}
