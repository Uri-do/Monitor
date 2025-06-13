using EnterpriseApp.Core.Enums;

namespace EnterpriseApp.Api.DTOs;

/// <summary>
/// Data transfer object for DomainEntity
/// </summary>
public class DomainEntityDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the DomainEntity
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the DomainEntity
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category of the DomainEntity
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Priority level (1-5)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Status of the DomainEntity
    /// </summary>
    public DomainEntityStatus Status { get; set; }

    /// <summary>
    /// Indicates if the DomainEntity is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Tags associated with the DomainEntity
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// External reference ID
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When the DomainEntity was created
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// When the DomainEntity was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Who created the DomainEntity
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Who last modified the DomainEntity
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Items associated with this DomainEntity
    /// </summary>
    public List<DomainEntityItemDto> Items { get; set; } = new();

    /// <summary>
    /// Audit logs for this DomainEntity
    /// </summary>
    public List<AuditLogDto> AuditLogs { get; set; } = new();

    /// <summary>
    /// Computed properties
    /// </summary>
    public DomainEntityComputedDto Computed { get; set; } = new();
}

/// <summary>
/// Data transfer object for DomainEntityItem
/// </summary>
public class DomainEntityItemDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// DomainEntity ID this item belongs to
    /// </summary>
    public int DomainEntityId { get; set; }

    /// <summary>
    /// Name of the item
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the item
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Value associated with the item
    /// </summary>
    public decimal? Value { get; set; }

    /// <summary>
    /// Quantity of the item
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Sort order
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Indicates if the item is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the item was created
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// When the item was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Who created the item
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Who last modified the item
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Computed properties for DomainEntity
/// </summary>
public class DomainEntityComputedDto
{
    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Number of active items
    /// </summary>
    public int ActiveItems { get; set; }

    /// <summary>
    /// Total value of all items
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Average value per item
    /// </summary>
    public decimal AverageValue { get; set; }

    /// <summary>
    /// Days since creation
    /// </summary>
    public int DaysSinceCreation { get; set; }

    /// <summary>
    /// Days since last modification
    /// </summary>
    public int DaysSinceModification { get; set; }

    /// <summary>
    /// Parsed tags as a list
    /// </summary>
    public List<string> TagList { get; set; } = new();

    /// <summary>
    /// Status display name
    /// </summary>
    public string StatusDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Priority display name
    /// </summary>
    public string PriorityDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the entity can be deleted
    /// </summary>
    public bool CanDelete { get; set; }

    /// <summary>
    /// Indicates if the entity can be activated
    /// </summary>
    public bool CanActivate { get; set; }

    /// <summary>
    /// Indicates if the entity can be deactivated
    /// </summary>
    public bool CanDeactivate { get; set; }
}

/// <summary>
/// Statistics for DomainEntity
/// </summary>
public class DomainEntityStatisticsDto
{
    /// <summary>
    /// Total number of DomainEntities
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of active DomainEntities
    /// </summary>
    public int ActiveCount { get; set; }

    /// <summary>
    /// Number of inactive DomainEntities
    /// </summary>
    public int InactiveCount { get; set; }

    /// <summary>
    /// Count by status
    /// </summary>
    public Dictionary<DomainEntityStatus, int> StatusCounts { get; set; } = new();

    /// <summary>
    /// Count by category
    /// </summary>
    public Dictionary<string, int> CategoryCounts { get; set; } = new();

    /// <summary>
    /// Count by priority
    /// </summary>
    public Dictionary<int, int> PriorityCounts { get; set; } = new();

    /// <summary>
    /// Recent activity count (last 30 days)
    /// </summary>
    public int RecentActivityCount { get; set; }

    /// <summary>
    /// Average items per DomainEntity
    /// </summary>
    public double AverageItemsPerEntity { get; set; }

    /// <summary>
    /// Total value across all entities
    /// </summary>
    public decimal TotalValue { get; set; }

    /// <summary>
    /// Average value per entity
    /// </summary>
    public decimal AverageValuePerEntity { get; set; }

    /// <summary>
    /// Growth statistics
    /// </summary>
    public GrowthStatisticsDto Growth { get; set; } = new();

    /// <summary>
    /// Top categories by count
    /// </summary>
    public List<CategoryStatDto> TopCategories { get; set; } = new();

    /// <summary>
    /// Top tags by usage
    /// </summary>
    public List<TagStatDto> TopTags { get; set; } = new();

    /// <summary>
    /// Activity over time
    /// </summary>
    public List<ActivityStatDto> ActivityOverTime { get; set; } = new();
}

/// <summary>
/// Growth statistics
/// </summary>
public class GrowthStatisticsDto
{
    /// <summary>
    /// Growth this month
    /// </summary>
    public int ThisMonth { get; set; }

    /// <summary>
    /// Growth last month
    /// </summary>
    public int LastMonth { get; set; }

    /// <summary>
    /// Growth percentage
    /// </summary>
    public double GrowthPercentage { get; set; }

    /// <summary>
    /// Growth this year
    /// </summary>
    public int ThisYear { get; set; }

    /// <summary>
    /// Growth last year
    /// </summary>
    public int LastYear { get; set; }

    /// <summary>
    /// Yearly growth percentage
    /// </summary>
    public double YearlyGrowthPercentage { get; set; }
}

/// <summary>
/// Category statistics
/// </summary>
public class CategoryStatDto
{
    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Count of entities in this category
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Percentage of total
    /// </summary>
    public double Percentage { get; set; }
}

/// <summary>
/// Tag statistics
/// </summary>
public class TagStatDto
{
    /// <summary>
    /// Tag name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Usage count
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Percentage of total
    /// </summary>
    public double Percentage { get; set; }
}

/// <summary>
/// Activity statistics over time
/// </summary>
public class ActivityStatDto
{
    /// <summary>
    /// Date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Number of entities created
    /// </summary>
    public int Created { get; set; }

    /// <summary>
    /// Number of entities modified
    /// </summary>
    public int Modified { get; set; }

    /// <summary>
    /// Number of entities deleted
    /// </summary>
    public int Deleted { get; set; }

    /// <summary>
    /// Total activity
    /// </summary>
    public int Total => Created + Modified + Deleted;
}

/// <summary>
/// Bulk operation result DTO
/// </summary>
public class BulkOperationResultDto
{
    /// <summary>
    /// Number of entities processed successfully
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of entities that failed processing
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Total number of entities processed
    /// </summary>
    public int TotalCount => SuccessCount + FailureCount;

    /// <summary>
    /// Errors that occurred during processing
    /// </summary>
    public List<BulkOperationErrorDto> Errors { get; set; } = new();

    /// <summary>
    /// Indicates if the operation was completely successful
    /// </summary>
    public bool IsSuccess => FailureCount == 0;

    /// <summary>
    /// Operation that was performed
    /// </summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>
    /// When the operation was performed
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of the operation
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Bulk operation error DTO
/// </summary>
public class BulkOperationErrorDto
{
    /// <summary>
    /// Entity ID that failed
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error code
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}
