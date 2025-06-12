using AutoMapper;

using MonitoringGrid.Api.DTOs;
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
            .ForMember(dest => dest.AssignedIndicators, opt => opt.MapFrom(src => src.IndicatorContacts.Count))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

        CreateMap<MonitoringGrid.Api.DTOs.CreateContactRequest, Contact>()
            .ForMember(dest => dest.ContactId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IndicatorContacts, opt => opt.Ignore());

        CreateMap<MonitoringGrid.Api.DTOs.UpdateContactRequest, Contact>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IndicatorContacts, opt => opt.Ignore());

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



    /// <summary>
    /// Helper method to deserialize schedule configuration from JSON string
    /// </summary>
    private static object? DeserializeScheduleConfiguration(string? scheduleConfigurationJson)
    {
        if (string.IsNullOrEmpty(scheduleConfigurationJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<object>(scheduleConfigurationJson);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Helper method to serialize schedule configuration to JSON string
    /// </summary>
    private static string? SerializeScheduleConfiguration(object? scheduleConfiguration)
    {
        if (scheduleConfiguration == null)
            return null;

        try
        {
            return JsonSerializer.Serialize(scheduleConfiguration);
        }
        catch
        {
            return null;
        }
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
