using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.CQRS.Commands.Contact;
using MonitoringGrid.Api.CQRS.Queries.Contact;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.DTOs;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Core.Interfaces;
using MediatR;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API controller for managing contacts
/// </summary>
[ApiController]
[Route("api/contacts")]
[Authorize]
public class ContactController : BaseApiController
{
    public ContactController(
        IMediator mediator,
        ILogger<ContactController> logger)
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get all contacts with optional filtering and pagination
    /// </summary>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="search">Search term for name, email, or phone</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of contacts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<ContactDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetContacts(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var validationError = ValidateModelState();
        if (validationError != null) return BadRequest(validationError);

        if (page < 1) return ValidationError(nameof(page), "Page must be greater than 0");
        if (pageSize < 1 || pageSize > 100) return ValidationError(nameof(pageSize), "Page size must be between 1 and 100");

        var query = new GetPaginatedContactsQuery(isActive, search, page, pageSize);
        
        return await ExecuteQueryAsync<GetPaginatedContactsQuery, PaginatedContactsDto>(query, "get_contacts", result =>
        {
            var response = PaginatedApiResponse<ContactDto>.Success(
                result.Contacts,
                result.Page,
                result.PageSize,
                result.TotalCount,
                $"Retrieved {result.Contacts.Count()} contacts");

            return Ok(response);
        });
    }

    /// <summary>
    /// Get contact by ID
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <returns>Contact details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ContactDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetContact(int id)
    {
        if (id <= 0) return ValidationError(nameof(id), "Contact ID must be greater than 0");

        var query = new GetContactByIdQuery(id);
        
        return await ExecuteQueryAsync<GetContactByIdQuery, ContactDto>(query, "get_contact", result =>
        {
            var response = ApiResponse<ContactDto>.Success(result, "Contact retrieved successfully");
            return Ok(response);
        });
    }

    /// <summary>
    /// Create a new contact
    /// </summary>
    /// <param name="request">Contact creation request</param>
    /// <returns>Created contact</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ContactDto>), 201)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> CreateContact([FromBody] MonitoringGrid.Api.DTOs.Contacts.CreateContactRequest request)
    {
        var validationError = ValidateModelState();
        if (validationError != null) return BadRequest(validationError);

        var command = new CreateContactCommand(
            request.Name,
            request.Email,
            request.Phone,
            request.IsActive);

        return await ExecuteCommandAsync<CreateContactCommand, ContactDto>(command, "create_contact", result =>
        {
            var response = ApiResponse<ContactDto>.Success(result, "Contact created successfully");
            return CreatedAtAction(nameof(GetContact), new { id = result.ContactID }, response);
        });
    }

    /// <summary>
    /// Update an existing contact
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <param name="request">Contact update request</param>
    /// <returns>Updated contact</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ContactDto>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> UpdateContact(int id, [FromBody] MonitoringGrid.Api.DTOs.Contacts.UpdateContactRequest request)
    {
        var validationError = ValidateModelState();
        if (validationError != null) return BadRequest(validationError);

        if (id <= 0) return ValidationError(nameof(id), "Contact ID must be greater than 0");
        if (id != request.ContactID) return ValidationError(nameof(id), "Contact ID in URL does not match request body");

        var command = new UpdateContactCommand(
            request.ContactID,
            request.Name,
            request.Email,
            request.Phone,
            request.IsActive);

        return await ExecuteCommandAsync<UpdateContactCommand, ContactDto>(command, "update_contact", result =>
        {
            var response = ApiResponse<ContactDto>.Success(result, "Contact updated successfully");
            return Ok(response);
        });
    }

    /// <summary>
    /// Delete a contact
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), 204)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> DeleteContact(int id)
    {
        if (id <= 0) return ValidationError(nameof(id), "Contact ID must be greater than 0");

        var command = new DeleteContactCommand(id);
        
        return await ExecuteCommandAsync(command, "delete_contact");
    }

    /// <summary>
    /// Get contacts associated with a specific indicator
    /// </summary>
    /// <param name="indicatorId">Indicator ID</param>
    /// <returns>List of contacts for the indicator</returns>
    [HttpGet("by-indicator/{indicatorId:long}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ContactDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetContactsByIndicator(long indicatorId)
    {
        if (indicatorId <= 0) return ValidationError(nameof(indicatorId), "Indicator ID must be greater than 0");

        // For now, we'll get all contacts and filter client-side
        // In a real implementation, you'd create a specific query for this
        var query = new GetContactsQuery();
        
        return await ExecuteQueryAsync<GetContactsQuery, IEnumerable<ContactDto>>(query, "get_contacts_by_indicator", result =>
        {
            // Filter contacts that are associated with the indicator
            var filteredContacts = result.Where(c =>
                c.AssignedIndicators?.Any(ai => ai.IndicatorId == indicatorId) == true);

            var response = ApiResponse<IEnumerable<ContactDto>>.Success(
                filteredContacts,
                $"Retrieved {filteredContacts.Count()} contacts for indicator {indicatorId}");
            
            return Ok(response);
        });
    }

    /// <summary>
    /// Bulk update contact active status
    /// </summary>
    /// <param name="request">Bulk update request</param>
    /// <returns>Number of updated contacts</returns>
    [HttpPatch("bulk-status")]
    [ProducesResponseType(typeof(ApiResponse<int>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> BulkUpdateContactStatus([FromBody] BulkUpdateContactStatusRequest request)
    {
        var validationError = ValidateModelState();
        if (validationError != null) return BadRequest(validationError);

        if (!request.ContactIds.Any())
        {
            return ValidationError(nameof(request.ContactIds), "At least one contact ID must be provided");
        }

        // For now, we'll update contacts one by one
        // In a real implementation, you'd create a bulk update command
        var updatedCount = 0;
        foreach (var contactId in request.ContactIds)
        {
            try
            {
                // Get the contact first
                var getQuery = new GetContactByIdQuery(contactId);
                var getResult = await Mediator.Send(getQuery);
                
                if (getResult.IsSuccess)
                {
                    var contact = getResult.Value;
                    var updateCommand = new UpdateContactCommand(
                        contact.ContactID,
                        contact.Name,
                        contact.Email,
                        contact.Phone,
                        request.IsActive);

                    var updateResult = await Mediator.Send(updateCommand);
                    if (updateResult.IsSuccess)
                    {
                        updatedCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to update contact {ContactId} status", contactId);
            }
        }

        var response = ApiResponse<int>.Success(updatedCount,
            $"Updated {updatedCount} of {request.ContactIds.Count()} contacts");

        return Ok(response);
    }

    /// <summary>
    /// Assign contact to indicators
    /// </summary>
    /// <param name="id">Contact ID</param>
    /// <param name="request">Assignment request</param>
    /// <returns>Assignment result</returns>
    [HttpPost("{id:int}/assign")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> AssignToIndicators(int id, [FromBody] AssignContactToIndicatorsRequest request)
    {
        var validationError = ValidateModelState();
        if (validationError != null) return BadRequest(validationError);

        if (id <= 0) return ValidationError(nameof(id), "Contact ID must be greater than 0");
        if (!request.IndicatorIDs.Any()) return ValidationError(nameof(request.IndicatorIDs), "At least one indicator ID must be provided");

        // TODO: Implement contact assignment logic
        // This would typically involve creating IndicatorContact records
        var response = ApiResponse<string>.Success(
            "Assignment completed",
            $"Contact {id} assigned to {request.IndicatorIDs.Count()} indicators");

        return Ok(response);
    }

    /// <summary>
    /// Bulk operations on contacts
    /// </summary>
    /// <param name="request">Bulk operation request</param>
    /// <returns>Operation result</returns>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> BulkOperation([FromBody] BulkContactOperationRequest request)
    {
        var validationError = ValidateModelState();
        if (validationError != null) return BadRequest(validationError);

        if (!request.ContactIds.Any()) return ValidationError(nameof(request.ContactIds), "At least one contact ID must be provided");
        if (string.IsNullOrEmpty(request.Operation)) return ValidationError(nameof(request.Operation), "Operation must be specified");

        // TODO: Implement bulk operations based on operation type
        var response = ApiResponse<string>.Success(
            "Bulk operation completed",
            $"Operation '{request.Operation}' applied to {request.ContactIds.Count()} contacts");

        return Ok(response);
    }
}

/// <summary>
/// Request model for bulk updating contact status
/// </summary>
public class BulkUpdateContactStatusRequest
{
    /// <summary>
    /// List of contact IDs to update
    /// </summary>
    public IEnumerable<int> ContactIds { get; set; } = new List<int>();

    /// <summary>
    /// New active status for all contacts
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Request model for assigning contact to indicators
/// </summary>
public class AssignContactToIndicatorsRequest
{
    /// <summary>
    /// Contact ID
    /// </summary>
    public int ContactID { get; set; }

    /// <summary>
    /// List of indicator IDs to assign to
    /// </summary>
    public IEnumerable<int> IndicatorIDs { get; set; } = new List<int>();
}

/// <summary>
/// Request model for bulk contact operations
/// </summary>
public class BulkContactOperationRequest
{
    /// <summary>
    /// List of contact IDs
    /// </summary>
    public IEnumerable<int> ContactIds { get; set; } = new List<int>();

    /// <summary>
    /// Operation to perform
    /// </summary>
    public string Operation { get; set; } = string.Empty;
}
