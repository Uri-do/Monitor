using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API controller for managing contacts
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ContactController : ControllerBase
{
    private readonly MonitoringContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ContactController> _logger;

    public ContactController(
        MonitoringContext context,
        IMapper mapper,
        ILogger<ContactController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all contacts with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ContactDto>>> GetContacts(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null)
    {
        var query = _context.Contacts
            .Include(c => c.KpiContacts)
            .ThenInclude(kc => kc.KPI)
            .AsQueryable();

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.Contains(search) || 
                                   (c.Email != null && c.Email.Contains(search)));

        var contacts = await query.OrderBy(c => c.Name).ToListAsync();
        return Ok(_mapper.Map<List<ContactDto>>(contacts));
    }

    /// <summary>
    /// Get contact by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ContactDto>> GetContact(int id)
    {
        var contact = await _context.Contacts
            .Include(c => c.KpiContacts)
            .ThenInclude(kc => kc.KPI)
            .FirstOrDefaultAsync(c => c.ContactId == id);

        if (contact == null)
            return NotFound($"Contact with ID {id} not found");

        return Ok(_mapper.Map<ContactDto>(contact));
    }

    /// <summary>
    /// Create a new contact
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ContactDto>> CreateContact([FromBody] CreateContactRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if contact name is unique
        if (await _context.Contacts.AnyAsync(c => c.Name == request.Name))
            return BadRequest($"Contact with name '{request.Name}' already exists");

        // Validate that at least email or phone is provided
        if (string.IsNullOrEmpty(request.Email) && string.IsNullOrEmpty(request.Phone))
            return BadRequest("Contact must have either email or phone number");

        var contact = _mapper.Map<Contact>(request);
        contact.CreatedDate = DateTime.UtcNow;
        contact.ModifiedDate = DateTime.UtcNow;

        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created contact {Name} with ID {ContactId}", contact.Name, contact.ContactId);

        return CreatedAtAction(nameof(GetContact), new { id = contact.ContactId }, _mapper.Map<ContactDto>(contact));
    }

    /// <summary>
    /// Update an existing contact
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ContactDto>> UpdateContact(int id, [FromBody] UpdateContactRequest request)
    {
        if (id != request.ContactId)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingContact = await _context.Contacts.FindAsync(id);
        if (existingContact == null)
            return NotFound($"Contact with ID {id} not found");

        // Check if contact name is unique (excluding current contact)
        if (await _context.Contacts.AnyAsync(c => c.Name == request.Name && c.ContactId != id))
            return BadRequest($"Contact with name '{request.Name}' already exists");

        // Validate that at least email or phone is provided
        if (string.IsNullOrEmpty(request.Email) && string.IsNullOrEmpty(request.Phone))
            return BadRequest("Contact must have either email or phone number");

        _mapper.Map(request, existingContact);
        existingContact.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload with KPI associations
        var updatedContact = await _context.Contacts
            .Include(c => c.KpiContacts)
            .ThenInclude(kc => kc.KPI)
            .FirstAsync(c => c.ContactId == id);

        _logger.LogInformation("Updated contact {Name} with ID {ContactId}", existingContact.Name, id);

        return Ok(_mapper.Map<ContactDto>(updatedContact));
    }

    /// <summary>
    /// Delete a contact
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContact(int id)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null)
            return NotFound($"Contact with ID {id} not found");

        // Check if contact is assigned to any KPIs
        var assignedKpis = await _context.KpiContacts.CountAsync(kc => kc.ContactId == id);
        if (assignedKpis > 0)
            return BadRequest($"Cannot delete contact. It is assigned to {assignedKpis} KPI(s). Remove assignments first.");

        _context.Contacts.Remove(contact);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted contact {Name} with ID {ContactId}", contact.Name, id);

        return NoContent();
    }

    /// <summary>
    /// Assign contact to KPIs
    /// </summary>
    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignToKpis(int id, [FromBody] ContactAssignmentRequest request)
    {
        if (id != request.ContactId)
            return BadRequest("ID mismatch");

        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null)
            return NotFound($"Contact with ID {id} not found");

        // Verify all KPI IDs exist
        var existingKpis = await _context.KPIs
            .Where(k => request.KpiIds.Contains(k.KpiId))
            .Select(k => k.KpiId)
            .ToListAsync();

        var invalidKpis = request.KpiIds.Except(existingKpis).ToList();
        if (invalidKpis.Any())
            return BadRequest($"Invalid KPI IDs: {string.Join(", ", invalidKpis)}");

        // Remove existing assignments
        var existingAssignments = await _context.KpiContacts
            .Where(kc => kc.ContactId == id)
            .ToListAsync();
        _context.KpiContacts.RemoveRange(existingAssignments);

        // Add new assignments
        var newAssignments = request.KpiIds.Select(kpiId => new KpiContact
        {
            ContactId = id,
            KpiId = kpiId
        });

        _context.KpiContacts.AddRange(newAssignments);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated KPI assignments for contact {ContactId}. Assigned to {Count} KPIs", 
            id, request.KpiIds.Count);

        return Ok(new { Message = $"Contact assigned to {request.KpiIds.Count} KPIs" });
    }

    /// <summary>
    /// Get contact validation status
    /// </summary>
    [HttpGet("{id}/validate")]
    public async Task<ActionResult<ContactValidationDto>> ValidateContact(int id)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null)
            return NotFound($"Contact with ID {id} not found");

        var validation = new ContactValidationDto
        {
            ContactId = id,
            Name = contact.Name,
            EmailValid = IsValidEmail(contact.Email),
            PhoneValid = IsValidPhone(contact.Phone),
            CanReceiveEmail = !string.IsNullOrEmpty(contact.Email) && IsValidEmail(contact.Email),
            CanReceiveSms = !string.IsNullOrEmpty(contact.Phone) && IsValidPhone(contact.Phone)
        };

        if (!validation.EmailValid && !string.IsNullOrEmpty(contact.Email))
            validation.EmailError = "Invalid email format";

        if (!validation.PhoneValid && !string.IsNullOrEmpty(contact.Phone))
            validation.PhoneError = "Invalid phone number format";

        return Ok(validation);
    }

    /// <summary>
    /// Bulk operations on contacts
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkOperation([FromBody] BulkContactOperationRequest request)
    {
        if (!request.ContactIds.Any())
            return BadRequest("No contact IDs provided");

        var contacts = await _context.Contacts
            .Where(c => request.ContactIds.Contains(c.ContactId))
            .ToListAsync();

        if (!contacts.Any())
            return NotFound("No contacts found with the provided IDs");

        switch (request.Operation.ToLower())
        {
            case "activate":
                contacts.ForEach(c => c.IsActive = true);
                break;
            case "deactivate":
                contacts.ForEach(c => c.IsActive = false);
                break;
            case "delete":
                // Check if any contacts are assigned to KPIs
                var assignedContacts = await _context.KpiContacts
                    .Where(kc => request.ContactIds.Contains(kc.ContactId))
                    .Select(kc => kc.ContactId)
                    .Distinct()
                    .ToListAsync();

                if (assignedContacts.Any())
                    return BadRequest($"Cannot delete contacts. {assignedContacts.Count} contact(s) are assigned to KPIs.");

                _context.Contacts.RemoveRange(contacts);
                break;
            default:
                return BadRequest($"Unknown operation: {request.Operation}");
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Bulk operation {Operation} performed on {Count} contacts", 
            request.Operation, contacts.Count);

        return Ok(new { Message = $"Operation '{request.Operation}' completed on {contacts.Count} contacts" });
    }

    /// <summary>
    /// Validate email format
    /// </summary>
    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate phone number format
    /// </summary>
    private static bool IsValidPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone))
            return false;

        var phoneRegex = new System.Text.RegularExpressions.Regex(@"^\+?[\d\s\-\(\)]{10,}$");
        return phoneRegex.IsMatch(phone);
    }
}
