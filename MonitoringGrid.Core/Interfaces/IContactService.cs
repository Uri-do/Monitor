using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Core.Interfaces;

/// <summary>
/// Service interface for managing contacts
/// </summary>
public interface IContactService
{
    /// <summary>
    /// Get all contacts
    /// </summary>
    Task<Result<List<ContactDto>>> GetContactsAsync(bool? isActive = null, string? search = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get contact by ID
    /// </summary>
    Task<Result<ContactDto>> GetContactByIdAsync(int contactId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new contact
    /// </summary>
    Task<Result<ContactDto>> CreateContactAsync(CreateContactRequest request, string createdBy = "system", CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing contact
    /// </summary>
    Task<Result<ContactDto>> UpdateContactAsync(int contactId, UpdateContactRequest request, string modifiedBy = "system", CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a contact
    /// </summary>
    Task<Result<bool>> DeleteContactAsync(int contactId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get contacts for a specific indicator
    /// </summary>
    Task<Result<List<ContactDto>>> GetContactsByIndicatorAsync(long indicatorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add contacts to an indicator
    /// </summary>
    Task<Result<bool>> AddContactsToIndicatorAsync(long indicatorId, List<int> contactIds, string modifiedBy = "system", CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove contacts from an indicator
    /// </summary>
    Task<Result<bool>> RemoveContactsFromIndicatorAsync(long indicatorId, List<int> contactIds, string modifiedBy = "system", CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk operations on contacts
    /// </summary>
    Task<Result<bool>> BulkUpdateContactStatusAsync(List<int> contactIds, bool isActive, string modifiedBy = "system", CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for creating a contact
/// </summary>
public class CreateContactRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request model for updating a contact
/// </summary>
public class UpdateContactRequest
{
    public int ContactID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}
