using System.ComponentModel.DataAnnotations;
using EnterpriseApp.Api.CQRS;
using EnterpriseApp.Api.DTOs;
using EnterpriseApp.Core.Enums;

namespace EnterpriseApp.Api.CQRS.Commands;

/// <summary>
/// Command to create a new DomainEntity
/// </summary>
public class CreateDomainEntityCommand : BaseCommand<DomainEntityDto>
{
    /// <summary>
    /// Name of the DomainEntity
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the DomainEntity
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Category of the DomainEntity
    /// </summary>
    [StringLength(500)]
    public string? Category { get; set; }

    /// <summary>
    /// Priority level (1-5)
    /// </summary>
    [Range(1, 5)]
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Tags associated with the DomainEntity
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// External reference ID
    /// </summary>
    [StringLength(100)]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Command to update an existing DomainEntity
/// </summary>
public class UpdateDomainEntityCommand : BaseCommand<DomainEntityDto>
{
    /// <summary>
    /// ID of the DomainEntity to update
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Name of the DomainEntity
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the DomainEntity
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Category of the DomainEntity
    /// </summary>
    [StringLength(500)]
    public string? Category { get; set; }

    /// <summary>
    /// Priority level (1-5)
    /// </summary>
    [Range(1, 5)]
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Status of the DomainEntity
    /// </summary>
    public DomainEntityStatus Status { get; set; }

    /// <summary>
    /// Tags associated with the DomainEntity
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// External reference ID
    /// </summary>
    [StringLength(100)]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Command to delete a DomainEntity
/// </summary>
public class DeleteDomainEntityCommand : BaseCommand
{
    /// <summary>
    /// ID of the DomainEntity to delete
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Reason for deletion
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// Command to activate a DomainEntity
/// </summary>
public class ActivateDomainEntityCommand : BaseCommand<DomainEntityDto>
{
    /// <summary>
    /// ID of the DomainEntity to activate
    /// </summary>
    [Required]
    public int Id { get; set; }
}

/// <summary>
/// Command to deactivate a DomainEntity
/// </summary>
public class DeactivateDomainEntityCommand : BaseCommand<DomainEntityDto>
{
    /// <summary>
    /// ID of the DomainEntity to deactivate
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Reason for deactivation
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// Command to perform bulk operations on DomainEntities
/// </summary>
public class BulkDomainEntityCommand : BaseCommand<BulkOperationResultDto>
{
    /// <summary>
    /// Operation to perform
    /// </summary>
    [Required]
    public BulkOperationType Operation { get; set; }

    /// <summary>
    /// IDs of entities to operate on
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<int> EntityIds { get; set; } = new();

    /// <summary>
    /// Additional parameters for the operation
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Command to add a tag to a DomainEntity
/// </summary>
public class AddDomainEntityTagCommand : BaseCommand<DomainEntityDto>
{
    /// <summary>
    /// ID of the DomainEntity
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Tag to add
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Tag { get; set; } = string.Empty;
}

/// <summary>
/// Command to remove a tag from a DomainEntity
/// </summary>
public class RemoveDomainEntityTagCommand : BaseCommand<DomainEntityDto>
{
    /// <summary>
    /// ID of the DomainEntity
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Tag to remove
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Tag { get; set; } = string.Empty;
}

/// <summary>
/// Command to update DomainEntity metadata
/// </summary>
public class UpdateDomainEntityMetadataCommand : BaseCommand<DomainEntityDto>
{
    /// <summary>
    /// ID of the DomainEntity
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Metadata to update
    /// </summary>
    [Required]
    public string Metadata { get; set; } = string.Empty;
}

/// <summary>
/// Command to archive a DomainEntity
/// </summary>
public class ArchiveDomainEntityCommand : BaseCommand<DomainEntityDto>
{
    /// <summary>
    /// ID of the DomainEntity to archive
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Reason for archiving
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// Command to restore an archived DomainEntity
/// </summary>
public class RestoreDomainEntityCommand : BaseCommand<DomainEntityDto>
{
    /// <summary>
    /// ID of the DomainEntity to restore
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// Reason for restoration
    /// </summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// Command to duplicate a DomainEntity
/// </summary>
public class DuplicateDomainEntityCommand : BaseCommand<DomainEntityDto>
{
    /// <summary>
    /// ID of the DomainEntity to duplicate
    /// </summary>
    [Required]
    public int Id { get; set; }

    /// <summary>
    /// New name for the duplicated entity
    /// </summary>
    [Required]
    [StringLength(200)]
    public string NewName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether to include items in the duplication
    /// </summary>
    public bool IncludeItems { get; set; } = true;
}

/// <summary>
/// Bulk operation types
/// </summary>
public enum BulkOperationType
{
    /// <summary>
    /// Activate entities
    /// </summary>
    Activate = 0,

    /// <summary>
    /// Deactivate entities
    /// </summary>
    Deactivate = 1,

    /// <summary>
    /// Delete entities
    /// </summary>
    Delete = 2,

    /// <summary>
    /// Archive entities
    /// </summary>
    Archive = 3,

    /// <summary>
    /// Restore entities
    /// </summary>
    Restore = 4,

    /// <summary>
    /// Update category
    /// </summary>
    UpdateCategory = 5,

    /// <summary>
    /// Update priority
    /// </summary>
    UpdatePriority = 6,

    /// <summary>
    /// Add tag
    /// </summary>
    AddTag = 7,

    /// <summary>
    /// Remove tag
    /// </summary>
    RemoveTag = 8
}
