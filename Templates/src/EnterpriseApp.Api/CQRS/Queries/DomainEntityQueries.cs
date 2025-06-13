using System.ComponentModel.DataAnnotations;
using EnterpriseApp.Api.CQRS;
using EnterpriseApp.Api.DTOs;
using EnterpriseApp.Core.Enums;
using EnterpriseApp.Core.Models;

namespace EnterpriseApp.Api.CQRS.Queries;

/// <summary>
/// Query to get a DomainEntity by ID
/// </summary>
public class GetDomainEntityByIdQuery : BaseQuery<DomainEntityDto>
{
    /// <summary>
    /// ID of the DomainEntity
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Indicates whether to include items
    /// </summary>
    public bool IncludeItems { get; set; } = false;

    /// <summary>
    /// Indicates whether to include audit logs
    /// </summary>
    public bool IncludeAuditLogs { get; set; } = false;
}

/// <summary>
/// Query to get all DomainEntities with pagination and filtering
/// </summary>
public class GetDomainEntitiesQuery : PagedQuery<PagedResult<DomainEntityDto>>
{
    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public DomainEntityStatus? Status { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Filter by priority range
    /// </summary>
    public int? MinPriority { get; set; }

    /// <summary>
    /// Filter by priority range
    /// </summary>
    public int? MaxPriority { get; set; }

    /// <summary>
    /// Filter by tags (comma-separated)
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Filter by creation date range
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter by creation date range
    /// </summary>
    public DateTime? CreatedTo { get; set; }

    /// <summary>
    /// Filter by modification date range
    /// </summary>
    public DateTime? ModifiedFrom { get; set; }

    /// <summary>
    /// Filter by modification date range
    /// </summary>
    public DateTime? ModifiedTo { get; set; }

    /// <summary>
    /// Filter by creator
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Indicates whether to include items
    /// </summary>
    public bool IncludeItems { get; set; } = false;
}

/// <summary>
/// Query to get active DomainEntities
/// </summary>
public class GetActiveDomainEntitiesQuery : BaseQuery<List<DomainEntityDto>>
{
    /// <summary>
    /// Optional category filter
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Maximum number of results
    /// </summary>
    [Range(1, 1000)]
    public int? Limit { get; set; }
}

/// <summary>
/// Query to get DomainEntities by category
/// </summary>
public class GetDomainEntitiesByCategoryQuery : PagedQuery<PagedResult<DomainEntityDto>>
{
    /// <summary>
    /// Category to filter by
    /// </summary>
    [Required]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Query to search DomainEntities
/// </summary>
public class SearchDomainEntitiesQuery : PagedQuery<PagedResult<DomainEntityDto>>
{
    /// <summary>
    /// Search term (required)
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public new string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Search in specific fields
    /// </summary>
    public List<string> SearchFields { get; set; } = new() { "Name", "Description", "Category", "Tags" };

    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public DomainEntityStatus? Status { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Query to get DomainEntity statistics
/// </summary>
public class GetDomainEntityStatisticsQuery : BaseQuery<DomainEntityStatisticsDto>
{
    /// <summary>
    /// Optional category filter
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Date range for statistics
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Date range for statistics
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Include detailed breakdown
    /// </summary>
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// Query to get all categories
/// </summary>
public class GetDomainEntityCategoriesQuery : BaseQuery<List<string>>
{
    /// <summary>
    /// Filter by active entities only
    /// </summary>
    public bool ActiveOnly { get; set; } = true;
}

/// <summary>
/// Query to get all tags
/// </summary>
public class GetDomainEntityTagsQuery : BaseQuery<List<string>>
{
    /// <summary>
    /// Filter by active entities only
    /// </summary>
    public bool ActiveOnly { get; set; } = true;

    /// <summary>
    /// Optional category filter
    /// </summary>
    public string? Category { get; set; }
}

/// <summary>
/// Query to get DomainEntities by tag
/// </summary>
public class GetDomainEntitiesByTagQuery : PagedQuery<PagedResult<DomainEntityDto>>
{
    /// <summary>
    /// Tag to filter by
    /// </summary>
    [Required]
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Query to get recently modified DomainEntities
/// </summary>
public class GetRecentlyModifiedDomainEntitiesQuery : BaseQuery<List<DomainEntityDto>>
{
    /// <summary>
    /// Number of days to look back
    /// </summary>
    [Range(1, 365)]
    public int Days { get; set; } = 7;

    /// <summary>
    /// Maximum number of results
    /// </summary>
    [Range(1, 100)]
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Query to get DomainEntity by external ID
/// </summary>
public class GetDomainEntityByExternalIdQuery : BaseQuery<DomainEntityDto>
{
    /// <summary>
    /// External ID to search for
    /// </summary>
    [Required]
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether to include items
    /// </summary>
    public bool IncludeItems { get; set; } = false;
}

/// <summary>
/// Query to get DomainEntities by priority range
/// </summary>
public class GetDomainEntitiesByPriorityQuery : PagedQuery<PagedResult<DomainEntityDto>>
{
    /// <summary>
    /// Minimum priority (1-5)
    /// </summary>
    [Range(1, 5)]
    public int MinPriority { get; set; } = 1;

    /// <summary>
    /// Maximum priority (1-5)
    /// </summary>
    [Range(1, 5)]
    public int MaxPriority { get; set; } = 5;

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Query to get DomainEntity audit trail
/// </summary>
public class GetDomainEntityAuditTrailQuery : PagedQuery<PagedResult<AuditLogDto>>
{
    /// <summary>
    /// ID of the DomainEntity
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Filter by action type
    /// </summary>
    public AuditAction? Action { get; set; }

    /// <summary>
    /// Filter by user
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Date range filter
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Date range filter
    /// </summary>
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Query to export DomainEntities
/// </summary>
public class ExportDomainEntitiesQuery : BaseQuery<byte[]>
{
    /// <summary>
    /// Export format
    /// </summary>
    [Required]
    public ExportFormat Format { get; set; } = ExportFormat.Excel;

    /// <summary>
    /// Filter by category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public DomainEntityStatus? Status { get; set; }

    /// <summary>
    /// Filter by active status
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Include items in export
    /// </summary>
    public bool IncludeItems { get; set; } = false;

    /// <summary>
    /// Include audit logs in export
    /// </summary>
    public bool IncludeAuditLogs { get; set; } = false;
}

/// <summary>
/// Export format enumeration
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// Excel format
    /// </summary>
    Excel = 0,

    /// <summary>
    /// CSV format
    /// </summary>
    Csv = 1,

    /// <summary>
    /// JSON format
    /// </summary>
    Json = 2,

    /// <summary>
    /// XML format
    /// </summary>
    Xml = 3,

    /// <summary>
    /// PDF format
    /// </summary>
    Pdf = 4
}
