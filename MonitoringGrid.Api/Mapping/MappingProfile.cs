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
        // KPI mappings
        CreateMap<KPI, KpiDto>()
            .ForMember(dest => dest.Contacts, opt => opt.MapFrom(src => src.KpiContacts.Select(kc => kc.Contact)))
            .ForMember(dest => dest.ScheduleConfiguration, opt => opt.MapFrom(src => DeserializeScheduleConfiguration(src.ScheduleConfiguration)));

        CreateMap<CreateKpiRequest, KPI>()
            .ForMember(dest => dest.KpiId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.KpiContacts, opt => opt.Ignore())
            .ForMember(dest => dest.AlertLogs, opt => opt.Ignore())
            .ForMember(dest => dest.HistoricalData, opt => opt.Ignore())
            .ForMember(dest => dest.ScheduleConfiguration, opt => opt.MapFrom(src => SerializeScheduleConfiguration(src.ScheduleConfiguration)));

        CreateMap<UpdateKpiRequest, KPI>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.KpiContacts, opt => opt.Ignore())
            .ForMember(dest => dest.AlertLogs, opt => opt.Ignore())
            .ForMember(dest => dest.HistoricalData, opt => opt.Ignore())
            .ForMember(dest => dest.ScheduleConfiguration, opt => opt.MapFrom(src => SerializeScheduleConfiguration(src.ScheduleConfiguration)));

        CreateMap<KpiExecutionResult, KpiExecutionResultDto>()
            .ForMember(dest => dest.KpiId, opt => opt.Ignore())
            .ForMember(dest => dest.Indicator, opt => opt.Ignore())
            .ForMember(dest => dest.ExecutionTime, opt => opt.MapFrom(src => DateTime.UtcNow));

        // Enhanced execution result mappings
        CreateMap<ExecutionTimingInfo, ExecutionTimingInfoDto>();
        CreateMap<DatabaseExecutionInfo, DatabaseExecutionInfoDto>();
        CreateMap<ExecutionStepInfo, ExecutionStepInfoDto>();

        CreateMap<KPI, KpiStatusDto>()
            .ForMember(dest => dest.NextRun, opt => opt.MapFrom(src => 
                src.LastRun.HasValue ? src.LastRun.Value.AddMinutes(src.Frequency) : (DateTime?)null))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => GetKpiStatus(src)))
            .ForMember(dest => dest.LastAlert, opt => opt.Ignore())
            .ForMember(dest => dest.AlertsToday, opt => opt.Ignore())
            .ForMember(dest => dest.LastCurrentValue, opt => opt.Ignore())
            .ForMember(dest => dest.LastHistoricalValue, opt => opt.Ignore())
            .ForMember(dest => dest.LastDeviation, opt => opt.Ignore());

        CreateMap<KPI, KpiSummaryDto>();

        // Contact mappings
        CreateMap<Contact, ContactDto>()
            .ForMember(dest => dest.AssignedKpis, opt => opt.MapFrom(src => src.KpiContacts.Select(kc => kc.KPI)));

        CreateMap<CreateContactRequest, Contact>()
            .ForMember(dest => dest.ContactId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.KpiContacts, opt => opt.Ignore());

        CreateMap<UpdateContactRequest, Contact>()
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedDate, opt => opt.Ignore())
            .ForMember(dest => dest.KpiContacts, opt => opt.Ignore());

        // Alert mappings
        CreateMap<AlertLog, AlertLogDto>()
            .ForMember(dest => dest.KpiIndicator, opt => opt.MapFrom(src => src.KPI.Indicator))
            .ForMember(dest => dest.KpiOwner, opt => opt.MapFrom(src => src.KPI.Owner));

        // Historical data mappings
        CreateMap<HistoricalData, KpiTrendDataDto>()
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.Timestamp))
            .ForMember(dest => dest.MetricKey, opt => opt.MapFrom(src => src.MetricKey))
            .ForMember(dest => dest.Period, opt => opt.MapFrom(src => src.Period));

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
        CreateMap<KpiAlertSummary, KpiAlertSummaryDto>();

        CreateMap<AlertDashboard, AlertDashboardDto>()
            .ForMember(dest => dest.RecentAlerts, opt => opt.Ignore())
            .ForMember(dest => dest.TopAlertingKpis, opt => opt.Ignore());
    }

    /// <summary>
    /// Determines KPI status based on its properties
    /// </summary>
    private static string GetKpiStatus(KPI kpi)
    {
        if (!kpi.IsActive)
            return "Inactive";

        if (!kpi.LastRun.HasValue)
            return "Never Run";

        var nextRun = kpi.LastRun.Value.AddMinutes(kpi.Frequency);
        var now = DateTime.UtcNow;

        if (nextRun <= now)
            return "Due";

        if (nextRun <= now.AddMinutes(5))
            return "Due Soon";

        return "Running";
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
    public int ProcessedKpis { get; set; }
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
