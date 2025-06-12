using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoringGrid.Core.Entities;

/// <summary>
/// Junction entity for many-to-many relationship between Indicators and Contacts
/// Manages notification contacts for each indicator
/// </summary>
[Table("IndicatorContacts", Schema = "monitoring")]
public class IndicatorContact
{
    /// <summary>
    /// Primary key for the indicator-contact relationship
    /// </summary>
    [Key]
    [Column("IndicatorContactID")]
    public int IndicatorContactId { get; set; }

    /// <summary>
    /// Foreign key reference to the Indicators table
    /// </summary>
    [Required]
    [Column("IndicatorID")]
    public long IndicatorId { get; set; }

    /// <summary>
    /// Foreign key reference to the Contacts table
    /// </summary>
    [Required]
    [Column("ContactID")]
    public int ContactId { get; set; }

    /// <summary>
    /// Timestamp when the relationship was created (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created the relationship
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Indicates if the relationship is active (soft delete support)
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    // Navigation properties

    /// <summary>
    /// Navigation property to the associated Indicator
    /// </summary>
    public virtual Indicator Indicator { get; set; } = null!;

    /// <summary>
    /// Navigation property to the associated Contact
    /// </summary>
    public virtual Contact Contact { get; set; } = null!;
}
