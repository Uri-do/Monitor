using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnterpriseApp.Core.Entities;

/// <summary>
/// Represents an item related to a DomainEntity
/// </summary>
[Table("DomainEntityItems")]
public class DomainEntityItem
{
    /// <summary>
    /// Unique identifier for the item
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the parent DomainEntity
    /// </summary>
    public int DomainEntityId { get; set; }

    /// <summary>
    /// Name of the item
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the item
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Value associated with the item
    /// </summary>
    public decimal? Value { get; set; }

    /// <summary>
    /// Quantity of the item
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Unit of measurement
    /// </summary>
    [StringLength(50)]
    public string? Unit { get; set; }

    /// <summary>
    /// Order/sequence of the item
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Indicates if the item is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the item was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the item was last modified
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created the item
    /// </summary>
    [StringLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Who last modified the item
    /// </summary>
    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    public string? Metadata { get; set; }

    // Navigation properties
    /// <summary>
    /// Parent DomainEntity
    /// </summary>
    [ForeignKey(nameof(DomainEntityId))]
    public virtual DomainEntity DomainEntity { get; set; } = null!;

    // Business logic methods
    /// <summary>
    /// Updates the item with new information
    /// </summary>
    public void Update(string name, string? description, decimal? value, int quantity, string? unit, string modifiedBy)
    {
        Name = name;
        Description = description;
        Value = value;
        Quantity = quantity;
        Unit = unit;
        ModifiedDate = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Calculates the total value (Value * Quantity)
    /// </summary>
    public decimal GetTotalValue()
    {
        return (Value ?? 0) * Quantity;
    }

    /// <summary>
    /// Activates the item
    /// </summary>
    public void Activate(string activatedBy)
    {
        IsActive = true;
        ModifiedDate = DateTime.UtcNow;
        ModifiedBy = activatedBy;
    }

    /// <summary>
    /// Deactivates the item
    /// </summary>
    public void Deactivate(string deactivatedBy)
    {
        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
        ModifiedBy = deactivatedBy;
    }
}
