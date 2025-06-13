using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.CQRS.Commands.Indicator;
using MonitoringGrid.Api.CQRS.Queries.Collector;
using MonitoringGrid.Api.CQRS.Queries.Indicator;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API controller for managing Indicators using CQRS pattern with MediatR
/// Replaces KpiController with enhanced functionality for ProgressPlayDB integration
/// </summary>
[ApiController]
[Route("api/indicator")]
[Produces("application/json")]
[PerformanceMonitor(slowThresholdMs: 2000)]
public class IndicatorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<IndicatorController> _logger;
    private readonly IPerformanceMetricsService? _performanceMetrics;

    public IndicatorController(
        IMediator mediator,
        IMapper mapper,
        ILogger<IndicatorController> logger,
        IPerformanceMetricsService? performanceMetrics = null)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
        _performanceMetrics = performanceMetrics;
    }

    /// <summary>
    /// Get all indicators with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<IndicatorDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetIndicators([FromQuery] IndicatorFilterRequest request)
    {
        try
        {
            _logger.LogDebug("Getting indicators with filters");
            _performanceMetrics?.IncrementCounter("indicator.get_all.requests");

            var query = _mapper.Map<GetIndicatorsQuery>(request);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.get_all.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.get_all.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indicators");
            _performanceMetrics?.IncrementCounter("indicator.get_all.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get indicator by ID
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(IndicatorDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetIndicator(long id)
    {
        try
        {
            _logger.LogDebug("Getting indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.get_by_id.requests");

            var query = new GetIndicatorByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.get_by_id.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.get_by_id.failure");
            return NotFound(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.get_by_id.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new indicator
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IndicatorDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateIndicator([FromBody] CreateIndicatorRequest request)
    {
        try
        {
            _logger.LogDebug("Creating indicator {IndicatorName}", request.IndicatorName);
            _performanceMetrics?.IncrementCounter("indicator.create.requests");

            var command = _mapper.Map<CreateIndicatorCommand>(request);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.create.success");
                return CreatedAtAction(nameof(GetIndicator), new { id = result.Value.IndicatorID }, result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.create.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating indicator {IndicatorName}", request.IndicatorName);
            _performanceMetrics?.IncrementCounter("indicator.create.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing indicator
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(IndicatorDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateIndicator(int id, [FromBody] UpdateIndicatorRequest request)
    {
        try
        {
            if (id != request.IndicatorID)
            {
                return BadRequest("ID mismatch");
            }

            _logger.LogDebug("Updating indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.update.requests");

            var command = _mapper.Map<UpdateIndicatorCommand>(request);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.update.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.update.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.update.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete an indicator
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteIndicator(long id)
    {
        try
        {
            _logger.LogDebug("Deleting indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.delete.requests");

            var command = new DeleteIndicatorCommand(id);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.delete.success");
                return NoContent();
            }

            _performanceMetrics?.IncrementCounter("indicator.delete.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.delete.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Execute an indicator manually
    /// </summary>
    [HttpPost("{id:long}/execute")]
    [ProducesResponseType(typeof(IndicatorExecutionResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ExecuteIndicator(long id, [FromBody] ExecuteIndicatorRequest? request = null)
    {
        try
        {
            _logger.LogDebug("Executing indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.execute.requests");

            var command = new ExecuteIndicatorCommand(
                id, 
                request?.ExecutionContext ?? "Manual", 
                request?.SaveResults ?? true);
            
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.execute.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.execute.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.execute.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Test an indicator without saving results
    /// </summary>
    [HttpPost("{id:long}/test")]
    [ProducesResponseType(typeof(IndicatorExecutionResultDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> TestIndicator(long id, [FromBody] TestIndicatorRequest? request = null)
    {
        try
        {
            _logger.LogDebug("Testing indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.test.requests");

            var command = new ExecuteIndicatorCommand(id, "Test", saveResults: false);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.test.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.test.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing indicator {IndicatorId}", id);
            _performanceMetrics?.IncrementCounter("indicator.test.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get indicator dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IndicatorDashboardDto), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            _logger.LogDebug("Getting indicator dashboard");
            _performanceMetrics?.IncrementCounter("indicator.dashboard.requests");

            var query = new GetIndicatorDashboardQuery();
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.dashboard.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.dashboard.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indicator dashboard");
            _performanceMetrics?.IncrementCounter("indicator.dashboard.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get available collectors from ProgressPlayDB
    /// </summary>
    [HttpGet("collectors")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CollectorDto>), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetCollectors([FromQuery] bool? isActive = true)
    {
        try
        {
            _logger.LogDebug("Getting collectors");
            _performanceMetrics?.IncrementCounter("indicator.collectors.requests");

            var query = new GetCollectorsQuery { IsActive = isActive };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.collectors.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.collectors.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting collectors");
            _performanceMetrics?.IncrementCounter("indicator.collectors.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get available item names for a specific collector
    /// </summary>
    [HttpGet("collectors/{collectorId:int}/items")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<string>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetCollectorItems(int collectorId)
    {
        try
        {
            _logger.LogDebug("Getting items for collector {CollectorId}", collectorId);
            _performanceMetrics?.IncrementCounter("indicator.collector_items.requests");

            var query = new GetCollectorItemNamesQuery(collectorId);
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                _performanceMetrics?.IncrementCounter("indicator.collector_items.success");
                return Ok(result.Value);
            }

            _performanceMetrics?.IncrementCounter("indicator.collector_items.failure");
            return BadRequest(result.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting items for collector {CollectorId}", collectorId);
            _performanceMetrics?.IncrementCounter("indicator.collector_items.error");
            return StatusCode(500, "Internal server error");
        }
    }

    #region Contact Management Endpoints

    /// <summary>
    /// Get all contacts with optional filtering
    /// </summary>
    [HttpGet("contacts")]
    [ProducesResponseType(typeof(List<ContactDto>), 200)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetContacts([FromQuery] bool? isActive = null, [FromQuery] string? search = null)
    {
        try
        {
            _logger.LogDebug("Getting contacts with filters - IsActive: {IsActive}, Search: {Search}", isActive, search);
            _performanceMetrics?.IncrementCounter("indicator.contacts.get_all.requests");

            // For now, we'll use direct database access until we implement CQRS for contacts
            var unitOfWork = HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var contactRepository = unitOfWork.Repository<Contact>();

            // Build predicate for filtering
            Expression<Func<Contact, bool>>? predicate = null;

            if (isActive.HasValue && !string.IsNullOrWhiteSpace(search))
            {
                predicate = c => c.IsActive == isActive.Value &&
                               (c.Name.Contains(search) || (c.Email != null && c.Email.Contains(search)));
            }
            else if (isActive.HasValue)
            {
                predicate = c => c.IsActive == isActive.Value;
            }
            else if (!string.IsNullOrWhiteSpace(search))
            {
                predicate = c => c.Name.Contains(search) || (c.Email != null && c.Email.Contains(search));
            }

            var contacts = predicate != null
                ? await contactRepository.GetAsync(predicate)
                : await contactRepository.GetAllAsync();

            // Load indicator contacts separately for each contact
            var indicatorContactRepository = unitOfWork.Repository<IndicatorContact>();
            foreach (var contact in contacts)
            {
                var indicatorContacts = await indicatorContactRepository.GetWithIncludesAsync(
                    ic => ic.ContactId == contact.ContactId,
                    ic => ic.Indicator);

                contact.IndicatorContacts = indicatorContacts.ToList();
            }

            var contactDtos = _mapper.Map<List<ContactDto>>(contacts);

            _performanceMetrics?.IncrementCounter("indicator.contacts.get_all.success");
            return Ok(contactDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts");
            _performanceMetrics?.IncrementCounter("indicator.contacts.get_all.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get contact by ID
    /// </summary>
    [HttpGet("contacts/{id:int}")]
    [ProducesResponseType(typeof(ContactDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> GetContact(int id)
    {
        try
        {
            _logger.LogDebug("Getting contact {ContactId}", id);
            _performanceMetrics?.IncrementCounter("indicator.contacts.get_by_id.requests");

            var unitOfWork = HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var contactRepository = unitOfWork.Repository<Contact>();

            var contact = await contactRepository.GetByIdAsync(id);

            // If contact exists, load the related data separately
            if (contact != null)
            {
                var indicatorContactRepository = unitOfWork.Repository<IndicatorContact>();
                var indicatorContacts = await indicatorContactRepository.GetWithIncludesAsync(
                    ic => ic.ContactId == id,
                    ic => ic.Indicator);

                contact.IndicatorContacts = indicatorContacts.ToList();
            }

            if (contact == null)
            {
                _performanceMetrics?.IncrementCounter("indicator.contacts.get_by_id.not_found");
                return NotFound($"Contact with ID {id} not found");
            }

            var contactDto = _mapper.Map<ContactDto>(contact);

            _performanceMetrics?.IncrementCounter("indicator.contacts.get_by_id.success");
            return Ok(contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contact {ContactId}", id);
            _performanceMetrics?.IncrementCounter("indicator.contacts.get_by_id.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new contact
    /// </summary>
    [HttpPost("contacts")]
    [ProducesResponseType(typeof(ContactDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> CreateContact([FromBody] CreateContactRequest request)
    {
        try
        {
            _logger.LogDebug("Creating contact {ContactName}", request.Name);
            _performanceMetrics?.IncrementCounter("indicator.contacts.create.requests");

            var unitOfWork = HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var contactRepository = unitOfWork.Repository<Contact>();

            var contact = new Contact
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            await contactRepository.AddAsync(contact);
            await unitOfWork.SaveChangesAsync();

            var contactDto = _mapper.Map<ContactDto>(contact);

            _performanceMetrics?.IncrementCounter("indicator.contacts.create.success");
            return CreatedAtAction(nameof(GetContact), new { id = contact.ContactId }, contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact {ContactName}", request.Name);
            _performanceMetrics?.IncrementCounter("indicator.contacts.create.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update an existing contact
    /// </summary>
    [HttpPut("contacts/{id:int}")]
    [ProducesResponseType(typeof(ContactDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateContact(int id, [FromBody] UpdateContactRequest request)
    {
        try
        {
            _logger.LogDebug("Updating contact {ContactId}", id);
            _performanceMetrics?.IncrementCounter("indicator.contacts.update.requests");

            if (id != request.ContactID)
            {
                return BadRequest("Contact ID in URL does not match request body");
            }

            var unitOfWork = HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var contactRepository = unitOfWork.Repository<Contact>();

            var contact = await contactRepository.GetByIdAsync(id);
            if (contact == null)
            {
                _performanceMetrics?.IncrementCounter("indicator.contacts.update.not_found");
                return NotFound($"Contact with ID {id} not found");
            }

            // Update contact properties
            contact.Name = request.Name;
            contact.Email = request.Email;
            contact.Phone = request.Phone;
            contact.IsActive = request.IsActive;
            contact.ModifiedDate = DateTime.UtcNow;

            await contactRepository.UpdateAsync(contact);
            await unitOfWork.SaveChangesAsync();

            var contactDto = _mapper.Map<ContactDto>(contact);

            _performanceMetrics?.IncrementCounter("indicator.contacts.update.success");
            return Ok(contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact {ContactId}", id);
            _performanceMetrics?.IncrementCounter("indicator.contacts.update.error");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Delete a contact
    /// </summary>
    [HttpDelete("contacts/{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> DeleteContact(int id)
    {
        try
        {
            _logger.LogDebug("Deleting contact {ContactId}", id);
            _performanceMetrics?.IncrementCounter("indicator.contacts.delete.requests");

            var unitOfWork = HttpContext.RequestServices.GetRequiredService<IUnitOfWork>();
            var contactRepository = unitOfWork.Repository<Contact>();

            var contact = await contactRepository.GetByIdAsync(id);
            if (contact == null)
            {
                _performanceMetrics?.IncrementCounter("indicator.contacts.delete.not_found");
                return NotFound($"Contact with ID {id} not found");
            }

            await contactRepository.DeleteAsync(contact);
            await unitOfWork.SaveChangesAsync();

            _performanceMetrics?.IncrementCounter("indicator.contacts.delete.success");
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact {ContactId}", id);
            _performanceMetrics?.IncrementCounter("indicator.contacts.delete.error");
            return StatusCode(500, "Internal server error");
        }
    }

    #endregion
}
