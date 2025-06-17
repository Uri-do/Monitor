using System.ComponentModel.DataAnnotations;

namespace MonitoringGrid.Api.DTOs.Contacts;

/// <summary>
/// Request DTO for creating a new contact
/// </summary>
public class CreateContactRequest
{
    /// <summary>
    /// Contact name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contact email address
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number
    /// </summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Contact department
    /// </summary>
    [MaxLength(100)]
    public string? Department { get; set; }

    /// <summary>
    /// Contact role/title
    /// </summary>
    [MaxLength(100)]
    public string? Role { get; set; }

    /// <summary>
    /// Whether the contact is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Additional notes about the contact
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// Request DTO for updating an existing contact
/// </summary>
public class UpdateContactRequest
{
    /// <summary>
    /// Contact ID
    /// </summary>
    [Required]
    public int ContactId { get; set; }

    /// <summary>
    /// Contact ID (legacy property name for compatibility)
    /// </summary>
    [Required]
    public int ContactID { get; set; }

    /// <summary>
    /// Contact name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contact email address
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number
    /// </summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Contact department
    /// </summary>
    [MaxLength(100)]
    public string? Department { get; set; }

    /// <summary>
    /// Contact role/title
    /// </summary>
    [MaxLength(100)]
    public string? Role { get; set; }

    /// <summary>
    /// Whether the contact is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Additional notes about the contact
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
