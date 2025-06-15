using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Common;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Infrastructure.Services;

/// <summary>
/// Implementation of contact service
/// </summary>
public class ContactService : IContactService
{
    private readonly MonitoringContext _context;
    private readonly ILogger<ContactService> _logger;

    public ContactService(MonitoringContext context, ILogger<ContactService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<ContactDto>>> GetContactsAsync(bool? isActive = null, string? search = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Contacts.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search) || c.Email.Contains(search));
            }

            var contacts = await query
                .Include(c => c.IndicatorContacts)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var contactDtos = contacts.Select(c => new ContactDto
            {
                ContactID = c.ContactId,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate,
                ModifiedDate = c.ModifiedDate,
                IndicatorCount = c.IndicatorContacts.Count
            }).ToList();

            return Result.Success(contactDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts");
            return Result.Failure<List<ContactDto>>("CONTACT_RETRIEVAL_ERROR", "Failed to retrieve contacts");
        }
    }

    public async Task<Result<ContactDto>> GetContactByIdAsync(int contactId, CancellationToken cancellationToken = default)
    {
        try
        {
            var contact = await _context.Contacts
                .Include(c => c.IndicatorContacts)
                    .ThenInclude(ic => ic.Indicator)
                .FirstOrDefaultAsync(c => c.ContactId == contactId, cancellationToken);

            if (contact == null)
            {
                return Result.Failure<ContactDto>("CONTACT_NOT_FOUND", $"Contact with ID {contactId} not found");
            }

            var contactDto = new ContactDto
            {
                ContactID = contact.ContactId,
                Name = contact.Name,
                Email = contact.Email,
                Phone = contact.Phone,
                IsActive = contact.IsActive,
                CreatedDate = contact.CreatedDate,
                ModifiedDate = contact.ModifiedDate,
                IndicatorCount = contact.IndicatorContacts.Count
            };

            return Result.Success(contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contact {ContactId}", contactId);
            return Result.Failure<ContactDto>("CONTACT_RETRIEVAL_ERROR", "Failed to retrieve contact");
        }
    }

    public async Task<Result<ContactDto>> CreateContactAsync(CreateContactRequest request, string createdBy = "system", CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if contact with same email already exists
            var existingContact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Email == request.Email, cancellationToken);

            if (existingContact != null)
            {
                return Result.Failure<ContactDto>("CONTACT_EMAIL_EXISTS", "A contact with this email already exists");
            }

            var contact = new Contact
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync(cancellationToken);

            var contactDto = new ContactDto
            {
                ContactID = contact.ContactId,
                Name = contact.Name,
                Email = contact.Email,
                Phone = contact.Phone,
                IsActive = contact.IsActive,
                CreatedDate = contact.CreatedDate,
                ModifiedDate = contact.ModifiedDate,
                IndicatorCount = 0
            };

            _logger.LogInformation("Created contact {ContactName} with ID {ContactId}", contact.Name, contact.ContactId);
            return Result.Success(contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact {ContactName}", request.Name);
            return Result.Failure<ContactDto>("CONTACT_CREATION_ERROR", "Failed to create contact");
        }
    }

    public async Task<Result<ContactDto>> UpdateContactAsync(int contactId, UpdateContactRequest request, string modifiedBy = "system", CancellationToken cancellationToken = default)
    {
        try
        {
            var contact = await _context.Contacts.FindAsync(contactId);
            if (contact == null)
            {
                return Result.Failure<ContactDto>("CONTACT_NOT_FOUND", $"Contact with ID {contactId} not found");
            }

            // Check if another contact with same email exists
            var existingContact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.Email == request.Email && c.ContactId != contactId, cancellationToken);

            if (existingContact != null)
            {
                return Result.Failure<ContactDto>("CONTACT_EMAIL_EXISTS", "Another contact with this email already exists");
            }

            contact.Name = request.Name;
            contact.Email = request.Email;
            contact.Phone = request.Phone;
            contact.IsActive = request.IsActive;
            contact.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var contactDto = new ContactDto
            {
                ContactID = contact.ContactId,
                Name = contact.Name,
                Email = contact.Email,
                Phone = contact.Phone,
                IsActive = contact.IsActive,
                CreatedDate = contact.CreatedDate,
                ModifiedDate = contact.ModifiedDate,
                IndicatorCount = contact.IndicatorContacts?.Count ?? 0
            };

            _logger.LogInformation("Updated contact {ContactName} with ID {ContactId}", contact.Name, contact.ContactId);
            return Result.Success(contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact {ContactId}", contactId);
            return Result.Failure<ContactDto>("CONTACT_UPDATE_ERROR", "Failed to update contact");
        }
    }

    public async Task<Result<bool>> DeleteContactAsync(int contactId, CancellationToken cancellationToken = default)
    {
        try
        {
            var contact = await _context.Contacts
                .Include(c => c.IndicatorContacts)
                .FirstOrDefaultAsync(c => c.ContactId == contactId, cancellationToken);

            if (contact == null)
            {
                return Result.Failure<bool>("CONTACT_NOT_FOUND", $"Contact with ID {contactId} not found");
            }

            // Check if contact is being used by indicators
            if (contact.IndicatorContacts.Any())
            {
                return Result.Failure<bool>("CONTACT_IN_USE", $"Cannot delete contact. It is currently assigned to {contact.IndicatorContacts.Count} indicator(s)");
            }

            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted contact {ContactName} with ID {ContactId}", contact.Name, contact.ContactId);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact {ContactId}", contactId);
            return Result.Failure<bool>("CONTACT_DELETE_ERROR", "Failed to delete contact");
        }
    }

    public async Task<Result<List<ContactDto>>> GetContactsByIndicatorAsync(long indicatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var contacts = await _context.IndicatorContacts
                .Where(ic => ic.IndicatorID == indicatorId)
                .Include(ic => ic.Contact)
                .Select(ic => ic.Contact)
                .ToListAsync(cancellationToken);

            var contactDtos = contacts.Select(c => new ContactDto
            {
                ContactID = c.ContactId,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate,
                ModifiedDate = c.ModifiedDate,
                IndicatorCount = c.IndicatorContacts.Count
            }).ToList();

            return Result.Success(contactDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts for indicator {IndicatorId}", indicatorId);
            return Result.Failure<List<ContactDto>>("CONTACT_RETRIEVAL_ERROR", "Failed to retrieve contacts for indicator");
        }
    }

    public async Task<Result<bool>> AddContactsToIndicatorAsync(long indicatorId, List<int> contactIds, string modifiedBy = "system", CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if indicator exists
            var indicator = await _context.Indicators.FindAsync(indicatorId);
            if (indicator == null)
            {
                return Result.Failure<bool>("INDICATOR_NOT_FOUND", $"Indicator with ID {indicatorId} not found");
            }

            // Get existing relationships
            var existingRelationships = await _context.IndicatorContacts
                .Where(ic => ic.IndicatorID == indicatorId)
                .Select(ic => ic.ContactId)
                .ToListAsync(cancellationToken);

            // Add new relationships
            var newContactIds = contactIds.Except(existingRelationships).ToList();
            foreach (var contactId in newContactIds)
            {
                var contact = await _context.Contacts.FindAsync(contactId);
                if (contact != null)
                {
                    _context.IndicatorContacts.Add(new IndicatorContact
                    {
                        IndicatorID = indicatorId,
                        ContactId = contactId
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added {Count} contacts to indicator {IndicatorId}", newContactIds.Count, indicatorId);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding contacts to indicator {IndicatorId}", indicatorId);
            return Result.Failure<bool>("CONTACT_ASSIGNMENT_ERROR", "Failed to add contacts to indicator");
        }
    }

    public async Task<Result<bool>> RemoveContactsFromIndicatorAsync(long indicatorId, List<int> contactIds, string modifiedBy = "system", CancellationToken cancellationToken = default)
    {
        try
        {
            var relationships = await _context.IndicatorContacts
                .Where(ic => ic.IndicatorID == indicatorId && contactIds.Contains(ic.ContactId))
                .ToListAsync(cancellationToken);

            _context.IndicatorContacts.RemoveRange(relationships);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed {Count} contacts from indicator {IndicatorId}", relationships.Count, indicatorId);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing contacts from indicator {IndicatorId}", indicatorId);
            return Result.Failure<bool>("CONTACT_REMOVAL_ERROR", "Failed to remove contacts from indicator");
        }
    }

    public async Task<Result<bool>> BulkUpdateContactStatusAsync(List<int> contactIds, bool isActive, string modifiedBy = "system", CancellationToken cancellationToken = default)
    {
        try
        {
            var contacts = await _context.Contacts
                .Where(c => contactIds.Contains(c.ContactId))
                .ToListAsync(cancellationToken);

            foreach (var contact in contacts)
            {
                contact.IsActive = isActive;
                contact.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Bulk updated {Count} contacts status to {Status}", contacts.Count, isActive);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk updating contact status");
            return Result.Failure<bool>("CONTACT_BULK_UPDATE_ERROR", "Failed to bulk update contact status");
        }
    }
}
