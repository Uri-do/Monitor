using AutoMapper;

using MonitoringGrid.Api.DTOs;
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
