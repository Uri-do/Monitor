using AutoMapper;
using EnterpriseApp.Api.CQRS.Commands;
using EnterpriseApp.Api.DTOs;
using EnterpriseApp.Core.Entities;
using EnterpriseApp.Core.Models;

namespace EnterpriseApp.Api.Mappings;

/// <summary>
/// AutoMapper profile for DomainEntity mappings
/// </summary>
public class DomainEntityMappingProfile : Profile
{
    /// <summary>
    /// Initializes the mapping profile
    /// </summary>
    public DomainEntityMappingProfile()
    {
        CreateEntityMappings();
        CreateCommandMappings();
        CreateStatisticsMappings();
        CreateAuditMappings();
    }

    /// <summary>
    /// Creates entity to DTO mappings
    /// </summary>
    private void CreateEntityMappings()
    {
        // DomainEntity mappings
        CreateMap<DomainEntity, DomainEntityDto>()
            .ForMember(dest => dest.Computed, opt => opt.MapFrom(src => CreateComputedProperties(src)))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items.Where(i => i.IsActive)))
            .ForMember(dest => dest.AuditLogs, opt => opt.MapFrom(src => src.AuditLogs.OrderByDescending(a => a.Timestamp).Take(10)));

        CreateMap<DomainEntityDto, DomainEntity>()
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ForMember(dest => dest.AuditLogs, opt => opt.Ignore());

        // DomainEntityItem mappings
        CreateMap<DomainEntityItem, DomainEntityItemDto>();
        CreateMap<DomainEntityItemDto, DomainEntityItem>();

        // Computed properties mapping
        CreateMap<DomainEntity, DomainEntityComputedDto>()
            .ForMember(dest => dest.TotalItems, opt => opt.MapFrom(src => src.Items.Count))
            .ForMember(dest => dest.ActiveItems, opt => opt.MapFrom(src => src.Items.Count(i => i.IsActive)))
            .ForMember(dest => dest.TotalValue, opt => opt.MapFrom(src => src.Items.Where(i => i.Value.HasValue).Sum(i => i.Value!.Value)))
            .ForMember(dest => dest.AverageValue, opt => opt.MapFrom(src => 
                src.Items.Where(i => i.Value.HasValue).Any() 
                    ? src.Items.Where(i => i.Value.HasValue).Average(i => i.Value!.Value) 
                    : 0))
            .ForMember(dest => dest.DaysSinceCreation, opt => opt.MapFrom(src => (DateTime.UtcNow - src.CreatedDate).Days))
            .ForMember(dest => dest.DaysSinceModification, opt => opt.MapFrom(src => (DateTime.UtcNow - src.ModifiedDate).Days))
            .ForMember(dest => dest.TagList, opt => opt.MapFrom(src => ParseTags(src.Tags)))
            .ForMember(dest => dest.StatusDisplayName, opt => opt.MapFrom(src => GetStatusDisplayName(src.Status)))
            .ForMember(dest => dest.PriorityDisplayName, opt => opt.MapFrom(src => GetPriorityDisplayName(src.Priority)))
            .ForMember(dest => dest.CanDelete, opt => opt.MapFrom(src => src.CanDelete()))
            .ForMember(dest => dest.CanActivate, opt => opt.MapFrom(src => src.CanActivate()))
            .ForMember(dest => dest.CanDeactivate, opt => opt.MapFrom(src => !src.CanActivate()));
    }

    /// <summary>
    /// Creates command to model mappings
    /// </summary>
    private void CreateCommandMappings()
    {
        // Create command mappings
        CreateMap<CreateDomainEntityCommand, CreateDomainEntityRequest>();

        // Update command mappings
        CreateMap<UpdateDomainEntityCommand, UpdateDomainEntityRequest>();

        // Bulk operation mappings
        CreateMap<BulkDomainEntityCommand, BulkOperationRequest>()
            .ForMember(dest => dest.Operation, opt => opt.MapFrom(src => MapBulkOperationType(src.Operation)));

        CreateMap<BulkOperationResult, BulkOperationResultDto>()
            .ForMember(dest => dest.Errors, opt => opt.MapFrom(src => src.Errors));

        CreateMap<BulkOperationError, BulkOperationErrorDto>();
    }

    /// <summary>
    /// Creates statistics mappings
    /// </summary>
    private void CreateStatisticsMappings()
    {
        CreateMap<DomainEntityStatistics, DomainEntityStatisticsDto>()
            .ForMember(dest => dest.Growth, opt => opt.MapFrom(src => CreateGrowthStatistics(src)))
            .ForMember(dest => dest.TopCategories, opt => opt.MapFrom(src => CreateTopCategories(src.CategoryCounts)))
            .ForMember(dest => dest.TopTags, opt => opt.MapFrom(src => CreateTopTags(src)))
            .ForMember(dest => dest.ActivityOverTime, opt => opt.MapFrom(src => CreateActivityOverTime(src)));

        CreateMap<Dictionary<string, int>, List<CategoryStatDto>>()
            .ConvertUsing(src => src.Select(kvp => new CategoryStatDto
            {
                Name = kvp.Key,
                Count = kvp.Value,
                Percentage = src.Values.Sum() > 0 ? (double)kvp.Value / src.Values.Sum() * 100 : 0
            }).OrderByDescending(x => x.Count).Take(10).ToList());
    }

    /// <summary>
    /// Creates audit log mappings
    /// </summary>
    private void CreateAuditMappings()
    {
        CreateMap<AuditLog, AuditLogDto>()
            .ForMember(dest => dest.Computed, opt => opt.MapFrom(src => CreateAuditComputedProperties(src)));

        CreateMap<AuditLog, AuditLogComputedDto>()
            .ForMember(dest => dest.ActionDisplayName, opt => opt.MapFrom(src => GetActionDisplayName(src.Action)))
            .ForMember(dest => dest.SeverityDisplayName, opt => opt.MapFrom(src => GetSeverityDisplayName(src.Severity)))
            .ForMember(dest => dest.TimeAgo, opt => opt.MapFrom(src => GetTimeAgo(src.Timestamp)))
            .ForMember(dest => dest.FormattedTimestamp, opt => opt.MapFrom(src => src.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC")))
            .ForMember(dest => dest.OldValuesDict, opt => opt.MapFrom(src => ParseJsonToDictionary(src.OldValues)))
            .ForMember(dest => dest.NewValuesDict, opt => opt.MapFrom(src => ParseJsonToDictionary(src.NewValues)))
            .ForMember(dest => dest.Changes, opt => opt.MapFrom(src => CreateChangesList(src)))
            .ForMember(dest => dest.IsSecurityEvent, opt => opt.MapFrom(src => IsSecurityEvent(src)))
            .ForMember(dest => dest.RiskLevel, opt => opt.MapFrom(src => GetRiskLevel(src)));
    }

    /// <summary>
    /// Creates computed properties for DomainEntity
    /// </summary>
    private static DomainEntityComputedDto CreateComputedProperties(DomainEntity entity)
    {
        return new DomainEntityComputedDto
        {
            TotalItems = entity.Items.Count,
            ActiveItems = entity.Items.Count(i => i.IsActive),
            TotalValue = entity.Items.Where(i => i.Value.HasValue).Sum(i => i.Value!.Value),
            AverageValue = entity.Items.Where(i => i.Value.HasValue).Any() 
                ? entity.Items.Where(i => i.Value.HasValue).Average(i => i.Value!.Value) 
                : 0,
            DaysSinceCreation = (DateTime.UtcNow - entity.CreatedDate).Days,
            DaysSinceModification = (DateTime.UtcNow - entity.ModifiedDate).Days,
            TagList = ParseTags(entity.Tags),
            StatusDisplayName = GetStatusDisplayName(entity.Status),
            PriorityDisplayName = GetPriorityDisplayName(entity.Priority),
            CanDelete = entity.CanDelete(),
            CanActivate = entity.CanActivate(),
            CanDeactivate = !entity.CanActivate()
        };
    }

    /// <summary>
    /// Parses tags string into a list
    /// </summary>
    private static List<string> ParseTags(string? tags)
    {
        if (string.IsNullOrEmpty(tags))
            return new List<string>();

        return tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(t => t.Trim())
                  .Where(t => !string.IsNullOrEmpty(t))
                  .ToList();
    }

    /// <summary>
    /// Gets display name for status
    /// </summary>
    private static string GetStatusDisplayName(Core.Enums.DomainEntityStatus status)
    {
        return status switch
        {
            Core.Enums.DomainEntityStatus.Draft => "Draft",
            Core.Enums.DomainEntityStatus.Active => "Active",
            Core.Enums.DomainEntityStatus.Inactive => "Inactive",
            Core.Enums.DomainEntityStatus.Archived => "Archived",
            Core.Enums.DomainEntityStatus.Deleted => "Deleted",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Gets display name for priority
    /// </summary>
    private static string GetPriorityDisplayName(int priority)
    {
        return priority switch
        {
            1 => "Critical",
            2 => "High",
            3 => "Medium",
            4 => "Low",
            5 => "Very Low",
            _ => $"Priority {priority}"
        };
    }

    /// <summary>
    /// Maps bulk operation type
    /// </summary>
    private static Core.Models.BulkOperationType MapBulkOperationType(BulkOperationType operationType)
    {
        return operationType switch
        {
            BulkOperationType.Activate => Core.Models.BulkOperationType.Activate,
            BulkOperationType.Deactivate => Core.Models.BulkOperationType.Deactivate,
            BulkOperationType.Delete => Core.Models.BulkOperationType.Delete,
            BulkOperationType.Archive => Core.Models.BulkOperationType.Archive,
            BulkOperationType.Restore => Core.Models.BulkOperationType.Restore,
            _ => Core.Models.BulkOperationType.Activate
        };
    }

    /// <summary>
    /// Creates growth statistics
    /// </summary>
    private static GrowthStatisticsDto CreateGrowthStatistics(DomainEntityStatistics stats)
    {
        // This would typically calculate growth based on historical data
        // For now, return empty growth statistics
        return new GrowthStatisticsDto();
    }

    /// <summary>
    /// Creates top categories list
    /// </summary>
    private static List<CategoryStatDto> CreateTopCategories(Dictionary<string, int> categoryCounts)
    {
        var total = categoryCounts.Values.Sum();
        return categoryCounts
            .Select(kvp => new CategoryStatDto
            {
                Name = kvp.Key,
                Count = kvp.Value,
                Percentage = total > 0 ? (double)kvp.Value / total * 100 : 0
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();
    }

    /// <summary>
    /// Creates top tags list
    /// </summary>
    private static List<TagStatDto> CreateTopTags(DomainEntityStatistics stats)
    {
        // This would typically analyze tag usage across entities
        // For now, return empty list
        return new List<TagStatDto>();
    }

    /// <summary>
    /// Creates activity over time data
    /// </summary>
    private static List<ActivityStatDto> CreateActivityOverTime(DomainEntityStatistics stats)
    {
        // This would typically analyze activity patterns over time
        // For now, return empty list
        return new List<ActivityStatDto>();
    }

    /// <summary>
    /// Creates computed properties for audit log
    /// </summary>
    private static AuditLogComputedDto CreateAuditComputedProperties(AuditLog auditLog)
    {
        return new AuditLogComputedDto
        {
            ActionDisplayName = GetActionDisplayName(auditLog.Action),
            SeverityDisplayName = GetSeverityDisplayName(auditLog.Severity),
            TimeAgo = GetTimeAgo(auditLog.Timestamp),
            FormattedTimestamp = auditLog.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            OldValuesDict = ParseJsonToDictionary(auditLog.OldValues),
            NewValuesDict = ParseJsonToDictionary(auditLog.NewValues),
            Changes = CreateChangesList(auditLog),
            IsSecurityEvent = IsSecurityEvent(auditLog),
            RiskLevel = GetRiskLevel(auditLog)
        };
    }

    /// <summary>
    /// Gets display name for audit action
    /// </summary>
    private static string GetActionDisplayName(Core.Enums.AuditAction action)
    {
        return action switch
        {
            Core.Enums.AuditAction.Created => "Created",
            Core.Enums.AuditAction.Updated => "Updated",
            Core.Enums.AuditAction.Deleted => "Deleted",
            Core.Enums.AuditAction.Custom => "Custom Action",
            _ => action.ToString()
        };
    }

    /// <summary>
    /// Gets display name for severity
    /// </summary>
    private static string GetSeverityDisplayName(string severity)
    {
        return severity switch
        {
            "Critical" => "🔴 Critical",
            "Error" => "🟠 Error",
            "Warning" => "🟡 Warning",
            "Information" => "🔵 Information",
            "Debug" => "⚪ Debug",
            _ => severity
        };
    }

    /// <summary>
    /// Gets time ago string
    /// </summary>
    private static string GetTimeAgo(DateTime timestamp)
    {
        var timeSpan = DateTime.UtcNow - timestamp;

        if (timeSpan.TotalDays >= 365)
            return $"{(int)(timeSpan.TotalDays / 365)} year(s) ago";
        if (timeSpan.TotalDays >= 30)
            return $"{(int)(timeSpan.TotalDays / 30)} month(s) ago";
        if (timeSpan.TotalDays >= 1)
            return $"{(int)timeSpan.TotalDays} day(s) ago";
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours} hour(s) ago";
        if (timeSpan.TotalMinutes >= 1)
            return $"{(int)timeSpan.TotalMinutes} minute(s) ago";
        
        return "Just now";
    }

    /// <summary>
    /// Parses JSON string to dictionary
    /// </summary>
    private static Dictionary<string, object>? ParseJsonToDictionary(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates changes list from audit log
    /// </summary>
    private static List<ChangeDto> CreateChangesList(AuditLog auditLog)
    {
        var changes = new List<ChangeDto>();

        var oldValues = ParseJsonToDictionary(auditLog.OldValues);
        var newValues = ParseJsonToDictionary(auditLog.NewValues);

        if (oldValues == null && newValues == null)
            return changes;

        var allKeys = new HashSet<string>();
        if (oldValues != null) allKeys.UnionWith(oldValues.Keys);
        if (newValues != null) allKeys.UnionWith(newValues.Keys);

        foreach (var key in allKeys)
        {
            var oldValue = oldValues?.GetValueOrDefault(key);
            var newValue = newValues?.GetValueOrDefault(key);

            var changeType = (oldValue, newValue) switch
            {
                (null, not null) => ChangeType.Added,
                (not null, null) => ChangeType.Removed,
                (not null, not null) when !Equals(oldValue, newValue) => ChangeType.Modified,
                _ => ChangeType.NoChange
            };

            if (changeType != ChangeType.NoChange)
            {
                changes.Add(new ChangeDto
                {
                    PropertyName = key,
                    OldValue = oldValue,
                    NewValue = newValue,
                    DisplayName = GetPropertyDisplayName(key),
                    ChangeType = changeType
                });
            }
        }

        return changes;
    }

    /// <summary>
    /// Gets display name for property
    /// </summary>
    private static string GetPropertyDisplayName(string propertyName)
    {
        return propertyName switch
        {
            "Name" => "Name",
            "Description" => "Description",
            "Category" => "Category",
            "Priority" => "Priority",
            "Status" => "Status",
            "IsActive" => "Active Status",
            "Tags" => "Tags",
            "ExternalId" => "External ID",
            "Metadata" => "Metadata",
            _ => propertyName
        };
    }

    /// <summary>
    /// Determines if audit log is a security event
    /// </summary>
    private static bool IsSecurityEvent(AuditLog auditLog)
    {
        var securityEntities = new[] { "User", "Role", "Permission", "Authentication" };
        var securityActions = new[] { "Login", "Logout", "PasswordChange", "PermissionChange" };

        return securityEntities.Contains(auditLog.EntityName) ||
               securityActions.Any(action => auditLog.ActionDescription?.Contains(action, StringComparison.OrdinalIgnoreCase) == true);
    }

    /// <summary>
    /// Gets risk level for audit log
    /// </summary>
    private static RiskLevel GetRiskLevel(AuditLog auditLog)
    {
        if (IsSecurityEvent(auditLog))
            return RiskLevel.High;

        if (auditLog.Action == Core.Enums.AuditAction.Deleted)
            return RiskLevel.Medium;

        if (auditLog.Severity == "Critical" || auditLog.Severity == "Error")
            return RiskLevel.High;

        if (auditLog.Severity == "Warning")
            return RiskLevel.Medium;

        return RiskLevel.Low;
    }
}
