using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseApp.Core.Entities;

/// <summary>
/// Represents a DomainEntity in the system
/// </summary>
[Table("DomainEntities")]
public class DomainEntity
{
    /// <summary>
    /// Unique identifier for the DomainEntity
    /// </summary>
    [Key]
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
    /// Indicates if the DomainEntity is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the DomainEntity was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the DomainEntity was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created the DomainEntity
    /// </summary>
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Who last modified the DomainEntity
    /// </summary>
    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Additional properties specific to your domain
    /// </summary>
    [StringLength(500)]
    public string? Category { get; set; }

    /// <summary>
    /// Priority level (1-5, where 1 is highest priority)
    /// </summary>
    [Range(1, 5)]
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Status of the DomainEntity
    /// </summary>
    public DomainEntityStatus Status { get; set; } = DomainEntityStatus.Draft;

    /// <summary>
    /// Tags associated with this DomainEntity
    /// </summary>
    [StringLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// External reference ID if integrating with other systems
    /// </summary>
    [StringLength(100)]
    public string? ExternalId { get; set; }

    /// <summary>
    /// JSON metadata for flexible data storage
    /// </summary>
    public string? Metadata { get; set; }

    // Navigation properties
    /// <summary>
    /// Related DomainEntity items
    /// </summary>
    public virtual ICollection<DomainEntityItem> Items { get; set; } = new List<DomainEntityItem>();

    /// <summary>
    /// Audit logs for this DomainEntity
    /// </summary>
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // Business logic methods
    /// <summary>
    /// Validates if the DomainEntity can be activated
    /// </summary>
    public bool CanActivate()
    {
        return !IsActive && Status == DomainEntityStatus.Ready;
    }

    /// <summary>
    /// Activates the DomainEntity
    /// </summary>
    public void Activate(string activatedBy)
    {
        if (!CanActivate())
            throw new InvalidOperationException("DomainEntity cannot be activated in its current state");

        IsActive = true;
        Status = DomainEntityStatus.Active;
        ModifiedDate = DateTime.UtcNow;
        ModifiedBy = activatedBy;
    }

    /// <summary>
    /// Deactivates the DomainEntity
    /// </summary>
    public void Deactivate(string deactivatedBy)
    {
        IsActive = false;
        Status = DomainEntityStatus.Inactive;
        ModifiedDate = DateTime.UtcNow;
        ModifiedBy = deactivatedBy;
    }

    /// <summary>
    /// Updates the DomainEntity with new information
    /// </summary>
    public void Update(string name, string? description, string? category, int priority, string modifiedBy)
    {
        Name = name;
        Description = description;
        Category = category;
        Priority = priority;
        ModifiedDate = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Adds a tag to the DomainEntity
    /// </summary>
    public void AddTag(string tag)
    {
        var currentTags = Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
        
        if (!currentTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            currentTags.Add(tag);
            Tags = string.Join(",", currentTags);
            ModifiedDate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Removes a tag from the DomainEntity
    /// </summary>
    public void RemoveTag(string tag)
    {
        var currentTags = Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
        
        if (currentTags.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            Tags = string.Join(",", currentTags);
            ModifiedDate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Gets all tags as a list
    /// </summary>
    public List<string> GetTags()
    {
        return Tags?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
    }

    /// <summary>
    /// Checks if the DomainEntity has a specific tag
    /// </summary>
    public bool HasTag(string tag)
    {
        return GetTags().Contains(tag, StringComparer.OrdinalIgnoreCase);
    }
}
