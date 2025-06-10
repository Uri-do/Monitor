using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.CQRS.Commands.Kpi;
using MonitoringGrid.Api.CQRS.Queries.Kpi;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// Main API controller for managing KPIs using CQRS pattern with MediatR
/// Clean architecture principles with root-level API endpoints
/// </summary>
[ApiController]
[Route("api/kpi")]
[Produces("application/json")]
[PerformanceMonitor(slowThresholdMs: 2000)]
public class KpiController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<KpiController> _logger;
    private readonly IPerformanceMetricsService? _performanceMetrics;
    private readonly IUnitOfWork _unitOfWork;

    public KpiController(
        IMediator mediator,
        ILogger<KpiController> logger,
        IUnitOfWork unitOfWork,
        IPerformanceMetricsService? performanceMetrics = null)
    {
        _mediator = mediator;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _performanceMetrics = performanceMetrics;
    }

    /// <summary>
    /// Get all KPIs with optional filtering
    /// </summary>
    /// <param name="isActive">Filter by active status (true/false)</param>
    /// <param name="owner">Filter by owner email or name</param>
    /// <param name="priority">Filter by priority level (1=SMS+Email, 2=Email only)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of KPIs matching the filter criteria</returns>
    /// <response code="200">Returns the list of KPIs</response>
    /// <response code="400">Invalid filter parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "isActive", "owner", "priority" })]
    [DatabasePerformanceMonitor]
    [SwaggerOperation(
        Summary = "Get all KPIs with optional filtering",
        Description = "Retrieves all KPIs from the system with optional filtering by active status, owner, and priority level. Results are cached for 5 minutes.",
        OperationId = "GetKpis",
        Tags = new[] { "KPI Management" }
    )]
    [SwaggerResponse(200, "Successfully retrieved KPIs", typeof(List<KpiDto>))]
    [SwaggerResponse(400, "Invalid filter parameters", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<List<KpiDto>>> GetKpis(
        [FromQuery, SwaggerParameter("Filter by active status")] bool? isActive = null,
        [FromQuery, SwaggerParameter("Filter by owner email or name")] string? owner = null,
        [FromQuery, SwaggerParameter("Filter by priority level (1=SMS+Email, 2=Email only)")] byte? priority = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetKpisQuery(isActive, owner, priority);
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI query failed: {Error}", result.Error);
                return StatusCode(500, result.Error.Message);
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPIs");
            return StatusCode(500, "An error occurred while retrieving KPIs");
        }
    }

    /// <summary>
    /// Get KPI by ID
    /// </summary>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "id" })]
    [DatabasePerformanceMonitor]
    public async Task<ActionResult<KpiDto>> GetKpi(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetKpiByIdQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI query failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            if (result.Value == null)
                return NotFound($"KPI with ID {id} not found");

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI with ID: {KpiId}", id);
            return StatusCode(500, "An error occurred while retrieving the KPI");
        }
    }

    /// <summary>
    /// Create a new KPI
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<KpiDto>> CreateKpi([FromBody] CreateKpiCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI creation failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.Validation
                    ? BadRequest(result.Error.Message)
                    : result.Error.Type == ErrorType.Conflict
                    ? Conflict(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            return CreatedAtAction(nameof(GetKpi), new { id = result.Value.KpiId }, result.Value);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid KPI creation request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating KPI: {@Command}", command);
            return StatusCode(500, "An error occurred while creating the KPI");
        }
    }

    /// <summary>
    /// Update an existing KPI
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<KpiDto>> UpdateKpi(int id, [FromBody] UpdateKpiCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            if (id != command.KpiId)
                return BadRequest("ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI update failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : result.Error.Type == ErrorType.Validation
                    ? BadRequest(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            return Ok(result.Value);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid KPI update request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating KPI {KpiId}: {@Command}", id, command);
            return StatusCode(500, "An error occurred while updating the KPI");
        }
    }

    /// <summary>
    /// Delete a KPI
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKpi(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new DeleteKpiCommand(id);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI deletion failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            if (!result.Value)
                return NotFound($"KPI with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting KPI {KpiId}", id);
            return StatusCode(500, "An error occurred while deleting the KPI");
        }
    }

    /// <summary>
    /// Execute a KPI manually
    /// </summary>
    [HttpPost("{id}/execute")]
    [KpiPerformanceMonitor]
    public async Task<ActionResult<KpiExecutionResultDto>> ExecuteKpi(int id, [FromBody] TestKpiRequest? request = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var command = new ExecuteKpiCommand(id, request?.CustomFrequency);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("KPI execution failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.NotFound
                    ? NotFound(result.Error.Message)
                    : result.Error.Type == ErrorType.BusinessRule
                    ? BadRequest(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            return Ok(result.Value);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid KPI execution request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("KPI execution not allowed: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing KPI {KpiId}", id);
            return StatusCode(500, "An error occurred while executing the KPI");
        }
    }

    /// <summary>
    /// Get KPI dashboard data
    /// </summary>
    [HttpGet("dashboard")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [DatabasePerformanceMonitor]
    public async Task<ActionResult<KpiDashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetKpiDashboardQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Dashboard query failed: {Error}", result.Error);
                return StatusCode(500, result.Error.Message);
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard data");
            return StatusCode(500, "An error occurred while retrieving dashboard data");
        }
    }

    /// <summary>
    /// Perform bulk operations on KPIs
    /// </summary>
    [HttpPost("bulk")]
    [DatabasePerformanceMonitor(slowQueryThresholdMs: 1000)]
    public async Task<IActionResult> BulkOperation([FromBody] BulkKpiOperationCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Bulk operation failed: {Error}", result.Error);
                return result.Error.Type == ErrorType.Validation
                    ? BadRequest(result.Error.Message)
                    : StatusCode(500, result.Error.Message);
            }

            return Ok(new { Message = result.Value });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid bulk operation request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation: {Operation}", command.Operation);
            return StatusCode(500, "An error occurred while performing the bulk operation");
        }
    }

    /// <summary>
    /// Gets KPIs with optimized projections and pagination (v3.0+)
    /// </summary>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="owner">Filter by owner</param>
    /// <param name="priority">Filter by priority</param>
    /// <param name="searchTerm">Search term for indicator, owner, or description</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="sortBy">Sort field (default: Indicator)</param>
    /// <param name="sortDescending">Sort direction (default: false)</param>
    /// <returns>Paginated list of KPI summaries</returns>
    [HttpGet("optimized")]
    [ProducesResponseType(typeof(PagedResult<MonitoringGrid.Api.CQRS.Queries.Kpi.KpiListItemDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PagedResult<MonitoringGrid.Api.CQRS.Queries.Kpi.KpiListItemDto>>> GetKpisOptimized(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? owner = null,
        [FromQuery] byte? priority = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "Indicator",
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting optimized KPIs with filters: IsActive={IsActive}, Owner={Owner}, Priority={Priority}, Search={SearchTerm}",
            isActive, owner, priority, searchTerm);

        var query = new GetKpisOptimizedQuery
        {
            IsActive = isActive,
            Owner = owner,
            Priority = priority,
            SearchTerm = searchTerm,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogDebug("Retrieved {Count} of {Total} KPIs in page {Page}",
            result.Items.Count(), result.TotalCount, result.PageNumber);

        return Ok(result);
    }

    /// <summary>
    /// Gets performance metrics for the KPI system (v3.0+)
    /// </summary>
    /// <param name="from">Start date for metrics</param>
    /// <param name="to">End date for metrics</param>
    /// <returns>Performance report</returns>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(PerformanceReport), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PerformanceReport>> GetPerformanceMetrics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (_performanceMetrics == null)
        {
            return BadRequest("Performance metrics service not available");
        }

        var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
        var toDate = to ?? DateTime.UtcNow;

        _logger.LogDebug("Getting performance metrics from {From} to {To}", fromDate, toDate);

        var report = await _performanceMetrics.GetPerformanceReportAsync(fromDate, toDate);

        _logger.LogDebug("Retrieved performance report: {TotalRequests} requests, {AverageResponseTime}ms avg response time",
            report.TotalRequests, report.AverageResponseTime);

        return Ok(report);
    }





    /// <summary>
    /// Validate and test scheduling calculations (v3.0+)
    /// </summary>
    [HttpGet("scheduling/validate")]
    [ProducesResponseType(200)]
    public IActionResult ValidateScheduling(
        [FromQuery] int frequencyMinutes = 5,
        [FromQuery] DateTime? lastRun = null)
    {
        var now = DateTime.UtcNow;
        var testLastRun = lastRun ?? now.AddMinutes(-frequencyMinutes - 1);

        var nextExecution = MonitoringGrid.Core.Utilities.WholeTimeScheduler.GetNextWholeTimeExecution(frequencyMinutes, testLastRun);
        var isDue = MonitoringGrid.Core.Utilities.WholeTimeScheduler.IsKpiDueForWholeTimeExecution(testLastRun, frequencyMinutes, now);
        var description = MonitoringGrid.Core.Utilities.WholeTimeScheduler.GetScheduleDescription(frequencyMinutes);

        return Ok(new
        {
            CurrentTime = now,
            LastRun = testLastRun,
            FrequencyMinutes = frequencyMinutes,
            NextExecution = nextExecution,
            MinutesUntilNext = (nextExecution - now).TotalMinutes,
            IsDue = isDue,
            ScheduleDescription = description
        });
    }





    /// <summary>
    /// Converts Error objects to appropriate HTTP responses (for Result pattern support)
    /// </summary>
    private IActionResult HandleError(Error error)
    {
        var (statusCode, message) = error.Type switch
        {
            ErrorType.NotFound => (404, error.Message),
            ErrorType.Validation => (400, error.Message),
            ErrorType.Conflict => (409, error.Message),
            ErrorType.Unauthorized => (401, error.Message),
            ErrorType.Forbidden => (403, error.Message),
            ErrorType.BusinessRule => (400, error.Message),
            ErrorType.External => (502, "External service error"),
            ErrorType.Timeout => (408, "Request timeout"),
            _ => (500, "An internal error occurred")
        };

        _logger.LogWarning("API error: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);

        return StatusCode(statusCode, new
        {
            Error = error.Code,
            Message = message,
            Type = error.Type.ToString()
        });
    }

    #region Alert Management Endpoints

    /// <summary>
    /// Get alerts with filtering and pagination
    /// </summary>
    [HttpGet("alerts")]
    [SwaggerOperation(Summary = "Get alerts with filtering", Description = "Retrieve alerts with comprehensive filtering and pagination")]
    [SwaggerResponse(200, "Successfully retrieved alerts", typeof(PaginatedAlertsDto))]
    [SwaggerResponse(400, "Invalid filter parameters", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<PaginatedAlertsDto>> GetAlerts(
        [FromQuery] bool? isResolved = null,
        [FromQuery] string? searchText = null,
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "triggerTime",
        [FromQuery] string? sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For now, return empty result - this will be implemented with proper alert system
            var emptyResult = new PaginatedAlertsDto
            {
                Alerts = new List<AlertLogDto>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize,
                TotalPages = 0
            };

            return Ok(emptyResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alerts");
            return StatusCode(500, "An error occurred while retrieving alerts");
        }
    }

    /// <summary>
    /// Get alert by ID
    /// </summary>
    [HttpGet("alerts/{id}")]
    [SwaggerOperation(Summary = "Get alert by ID", Description = "Retrieve a specific alert by its ID")]
    [SwaggerResponse(200, "Successfully retrieved alert", typeof(AlertLogDto))]
    [SwaggerResponse(404, "Alert not found", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<AlertLogDto>> GetAlert(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            // For now, return not found - this will be implemented with proper alert system
            return NotFound($"Alert with ID {id} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert {AlertId}", id);
            return StatusCode(500, "An error occurred while retrieving the alert");
        }
    }

    /// <summary>
    /// Get alert statistics
    /// </summary>
    [HttpGet("alerts/statistics")]
    [SwaggerOperation(Summary = "Get alert statistics", Description = "Retrieve alert statistics for the specified time period")]
    [SwaggerResponse(200, "Successfully retrieved alert statistics", typeof(AlertStatisticsDto))]
    [SwaggerResponse(400, "Invalid parameters", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<AlertStatisticsDto>> GetAlertStatistics(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For now, return empty statistics - this will be implemented with proper alert system
            var emptyStats = new AlertStatisticsDto
            {
                TotalAlerts = 0,
                ResolvedAlerts = 0,
                UnresolvedAlerts = 0,
                CriticalAlerts = 0,
                AverageResolutionTimeHours = 0,
                DailyTrend = new List<AlertTrendDto>(),
                TopAlertingKpis = new List<KpiAlertSummaryDto>()
            };

            return Ok(emptyStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving alert statistics");
            return StatusCode(500, "An error occurred while retrieving alert statistics");
        }
    }

    #endregion

    #region Contact Management Endpoints

    /// <summary>
    /// Get all contacts with optional filtering
    /// </summary>
    [HttpGet("contacts")]
    [SwaggerOperation(Summary = "Get all contacts", Description = "Retrieve all notification contacts with optional filtering")]
    [SwaggerResponse(200, "Successfully retrieved contacts", typeof(List<ContactDto>))]
    [SwaggerResponse(400, "Invalid filter parameters", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<List<ContactDto>>> GetContacts(
        [FromQuery, SwaggerParameter("Filter by active status")] bool? isActive = null,
        [FromQuery, SwaggerParameter("Search by name, email, or phone")] string? search = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting contacts with filters - IsActive: {IsActive}, Search: {Search}", isActive, search);

            var contactRepository = _unitOfWork.Repository<Contact>();
            var contacts = await contactRepository.GetWithThenIncludesAsync(
                null, // no predicate - get all
                c => c.ContactId, // order by ContactId
                true, // ascending
                query => query.Include(c => c.KpiContacts).ThenInclude(kc => kc.KPI));

            // Apply filters
            var filteredContacts = contacts.AsEnumerable();

            if (isActive.HasValue)
            {
                filteredContacts = filteredContacts.Where(c => c.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                filteredContacts = filteredContacts.Where(c =>
                    c.Name.ToLower().Contains(searchLower) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchLower)) ||
                    (c.Phone != null && c.Phone.ToLower().Contains(searchLower)));
            }

            var contactDtos = filteredContacts.Select(c => new ContactDto
            {
                ContactId = c.ContactId,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                IsActive = c.IsActive,
                CreatedDate = c.CreatedDate,
                ModifiedDate = c.ModifiedDate,
                AssignedKpis = c.KpiContacts?.Select(kc => new KpiSummaryDto
                {
                    KpiId = kc.KPI?.KpiId ?? 0,
                    Indicator = kc.KPI?.Indicator ?? "",
                    Owner = kc.KPI?.Owner ?? "",
                    Priority = kc.KPI?.Priority ?? 0,
                    IsActive = kc.KPI?.IsActive ?? false
                }).ToList() ?? new List<KpiSummaryDto>()
            }).ToList();

            _logger.LogDebug("Retrieved {Count} contacts", contactDtos.Count);
            return Ok(contactDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contacts");
            return StatusCode(500, "An error occurred while retrieving contacts");
        }
    }

    /// <summary>
    /// Get contact by ID
    /// </summary>
    [HttpGet("contacts/{id}")]
    [SwaggerOperation(Summary = "Get contact by ID", Description = "Retrieve a specific contact by its ID")]
    [SwaggerResponse(200, "Successfully retrieved contact", typeof(ContactDto))]
    [SwaggerResponse(404, "Contact not found", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<ContactDto>> GetContact(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting contact with ID: {ContactId}", id);

            var contactRepository = _unitOfWork.Repository<Contact>();
            var contact = await contactRepository.GetByIdWithThenIncludesAsync(id,
                query => query.Include(c => c.KpiContacts).ThenInclude(kc => kc.KPI));

            if (contact == null)
            {
                _logger.LogWarning("Contact with ID {ContactId} not found", id);
                return NotFound($"Contact with ID {id} not found");
            }

            var contactDto = new ContactDto
            {
                ContactId = contact.ContactId,
                Name = contact.Name,
                Email = contact.Email,
                Phone = contact.Phone,
                IsActive = contact.IsActive,
                CreatedDate = contact.CreatedDate,
                ModifiedDate = contact.ModifiedDate,
                AssignedKpis = contact.KpiContacts?.Select(kc => new KpiSummaryDto
                {
                    KpiId = kc.KPI?.KpiId ?? 0,
                    Indicator = kc.KPI?.Indicator ?? "",
                    Owner = kc.KPI?.Owner ?? "",
                    Priority = kc.KPI?.Priority ?? 0,
                    IsActive = kc.KPI?.IsActive ?? false
                }).ToList() ?? new List<KpiSummaryDto>()
            };

            return Ok(contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting contact with ID: {ContactId}", id);
            return StatusCode(500, "An error occurred while retrieving the contact");
        }
    }

    /// <summary>
    /// Create a new contact
    /// </summary>
    [HttpPost("contacts")]
    [SwaggerOperation(Summary = "Create new contact", Description = "Create a new notification contact")]
    [SwaggerResponse(201, "Contact created successfully", typeof(ContactDto))]
    [SwaggerResponse(400, "Invalid contact data", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<ContactDto>> CreateContact(
        [FromBody] CreateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Creating new contact: {Name}", request.Name);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
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

            var contactRepository = _unitOfWork.Repository<Contact>();
            await contactRepository.AddAsync(contact, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var contactDto = new ContactDto
            {
                ContactId = contact.ContactId,
                Name = contact.Name,
                Email = contact.Email,
                Phone = contact.Phone,
                IsActive = contact.IsActive,
                CreatedDate = contact.CreatedDate,
                ModifiedDate = contact.ModifiedDate,
                AssignedKpis = new List<KpiSummaryDto>()
            };

            _logger.LogInformation("Created contact with ID: {ContactId}", contact.ContactId);
            return CreatedAtAction(nameof(GetContact), new { id = contact.ContactId }, contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contact");
            return StatusCode(500, "An error occurred while creating the contact");
        }
    }

    /// <summary>
    /// Update an existing contact
    /// </summary>
    [HttpPut("contacts/{id}")]
    [SwaggerOperation(Summary = "Update contact", Description = "Update an existing notification contact")]
    [SwaggerResponse(200, "Contact updated successfully", typeof(ContactDto))]
    [SwaggerResponse(400, "Invalid contact data", typeof(object))]
    [SwaggerResponse(404, "Contact not found", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<ContactDto>> UpdateContact(
        int id,
        [FromBody] UpdateContactRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Updating contact with ID: {ContactId}", id);

            if (id != request.ContactId)
            {
                return BadRequest("Contact ID in URL does not match request body");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var contactRepository = _unitOfWork.Repository<Contact>();
            var contact = await contactRepository.GetByIdAsync(id, cancellationToken);

            if (contact == null)
            {
                _logger.LogWarning("Contact with ID {ContactId} not found for update", id);
                return NotFound($"Contact with ID {id} not found");
            }

            // Update contact properties
            contact.Name = request.Name;
            contact.Email = request.Email;
            contact.Phone = request.Phone;
            contact.IsActive = request.IsActive;
            contact.ModifiedDate = DateTime.UtcNow;

            await contactRepository.UpdateAsync(contact, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var contactDto = new ContactDto
            {
                ContactId = contact.ContactId,
                Name = contact.Name,
                Email = contact.Email,
                Phone = contact.Phone,
                IsActive = contact.IsActive,
                CreatedDate = contact.CreatedDate,
                ModifiedDate = contact.ModifiedDate,
                AssignedKpis = new List<KpiSummaryDto>()
            };

            _logger.LogInformation("Updated contact with ID: {ContactId}", contact.ContactId);
            return Ok(contactDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contact with ID: {ContactId}", id);
            return StatusCode(500, "An error occurred while updating the contact");
        }
    }

    /// <summary>
    /// Delete a contact
    /// </summary>
    [HttpDelete("contacts/{id}")]
    [SwaggerOperation(Summary = "Delete contact", Description = "Delete a notification contact")]
    [SwaggerResponse(204, "Contact deleted successfully")]
    [SwaggerResponse(404, "Contact not found", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult> DeleteContact(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Deleting contact with ID: {ContactId}", id);

            var contactRepository = _unitOfWork.Repository<Contact>();
            var contact = await contactRepository.GetByIdAsync(id, cancellationToken);

            if (contact == null)
            {
                _logger.LogWarning("Contact with ID {ContactId} not found for deletion", id);
                return NotFound($"Contact with ID {id} not found");
            }

            await contactRepository.DeleteAsync(contact, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted contact with ID: {ContactId}", contact.ContactId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contact with ID: {ContactId}", id);
            return StatusCode(500, "An error occurred while deleting the contact");
        }
    }

    #endregion

    #region Execution History Endpoints

    /// <summary>
    /// Get execution history with pagination and filters
    /// </summary>
    [HttpGet("execution-history")]
    [SwaggerOperation(Summary = "Get execution history", Description = "Retrieve execution history with pagination and filtering")]
    [SwaggerResponse(200, "Successfully retrieved execution history", typeof(PaginatedExecutionHistoryDto))]
    [SwaggerResponse(400, "Invalid filter parameters", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<PaginatedExecutionHistoryDto>> GetExecutionHistory(
        [FromQuery, SwaggerParameter("Filter by KPI ID")] int? kpiId = null,
        [FromQuery, SwaggerParameter("Filter by executor")] string? executedBy = null,
        [FromQuery, SwaggerParameter("Filter by execution method")] string? executionMethod = null,
        [FromQuery, SwaggerParameter("Filter by success status")] bool? isSuccessful = null,
        [FromQuery, SwaggerParameter("Start date filter")] DateTime? startDate = null,
        [FromQuery, SwaggerParameter("End date filter")] DateTime? endDate = null,
        [FromQuery, SwaggerParameter("Page size")] int pageSize = 20,
        [FromQuery, SwaggerParameter("Page number")] int pageNumber = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting execution history with filters - KpiId: {KpiId}, ExecutedBy: {ExecutedBy}, Method: {Method}",
                kpiId, executedBy, executionMethod);

            var historyRepository = _unitOfWork.Repository<HistoricalData>();

            // Build query with filters
            var query = historyRepository.GetWithThenIncludesAsync(
                h => (kpiId == null || h.KpiId == kpiId) &&
                     (executedBy == null || h.ExecutedBy.Contains(executedBy)) &&
                     (executionMethod == null || h.ExecutionMethod.Contains(executionMethod)) &&
                     (isSuccessful == null || h.IsSuccessful == isSuccessful) &&
                     (startDate == null || h.Timestamp >= startDate) &&
                     (endDate == null || h.Timestamp <= endDate),
                h => h.Timestamp,
                false, // descending order
                query => query.Include(h => h.KPI));

            var allHistory = await query;
            var totalCount = allHistory.Count();

            // Apply pagination
            var pagedHistory = allHistory
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new ExecutionHistoryDto
                {
                    HistoricalId = h.HistoricalId,
                    KpiId = h.KpiId,
                    Indicator = h.KPI?.Indicator ?? "",
                    KpiOwner = h.KPI?.Owner ?? "",
                    SpName = h.KPI?.SpName ?? "",
                    Timestamp = h.Timestamp,
                    ExecutedBy = h.ExecutedBy,
                    ExecutionMethod = h.ExecutionMethod,
                    CurrentValue = h.Value,
                    HistoricalValue = h.HistoricalValue,
                    DeviationPercent = h.DeviationPercent,
                    Period = h.Period,
                    MetricKey = h.MetricKey,
                    IsSuccessful = h.IsSuccessful,
                    ErrorMessage = h.ErrorMessage,
                    ExecutionTimeMs = h.ExecutionTimeMs,
                    DatabaseName = h.DatabaseName,
                    ServerName = h.ServerName,
                    ShouldAlert = h.ShouldAlert,
                    AlertSent = h.AlertSent,
                    SessionId = h.SessionId,
                    IpAddress = h.IpAddress,
                    SqlCommand = h.SqlCommand,
                    RawResponse = h.RawResponse,
                    ExecutionContext = h.ExecutionContext,
                    PerformanceCategory = GetPerformanceCategory(h.ExecutionTimeMs),
                    DeviationCategory = GetDeviationCategory(h.DeviationPercent)
                }).ToList();

            var result = new PaginatedExecutionHistoryDto
            {
                Executions = pagedHistory,
                TotalCount = totalCount,
                Page = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                HasNextPage = pageNumber < (int)Math.Ceiling((double)totalCount / pageSize),
                HasPreviousPage = pageNumber > 1
            };

            _logger.LogDebug("Retrieved {Count} of {Total} execution history records", pagedHistory.Count, totalCount);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution history");
            return StatusCode(500, "An error occurred while retrieving execution history");
        }
    }

    /// <summary>
    /// Get execution statistics
    /// </summary>
    [HttpGet("execution-stats")]
    [SwaggerOperation(Summary = "Get execution statistics", Description = "Retrieve execution statistics for KPIs")]
    [SwaggerResponse(200, "Successfully retrieved execution statistics", typeof(List<ExecutionStatsDto>))]
    [SwaggerResponse(400, "Invalid parameters", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<List<ExecutionStatsDto>>> GetExecutionStats(
        [FromQuery, SwaggerParameter("Filter by KPI ID")] int? kpiId = null,
        [FromQuery, SwaggerParameter("Number of days to analyze")] int days = 7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting execution stats for KpiId: {KpiId}, Days: {Days}", kpiId, days);

            var historyRepository = _unitOfWork.Repository<HistoricalData>();
            var startDate = DateTime.UtcNow.AddDays(-days);

            var history = await historyRepository.GetWithThenIncludesAsync(
                h => (kpiId == null || h.KpiId == kpiId) && h.Timestamp >= startDate,
                h => h.Timestamp,
                false,
                query => query.Include(h => h.KPI));

            var stats = history
                .GroupBy(h => h.KpiId)
                .Select(g => new ExecutionStatsDto
                {
                    KpiId = g.Key,
                    Indicator = g.First().KPI?.Indicator ?? "",
                    Owner = g.First().KPI?.Owner ?? "",
                    TotalExecutions = g.Count(),
                    SuccessfulExecutions = g.Count(h => h.IsSuccessful),
                    FailedExecutions = g.Count(h => !h.IsSuccessful),
                    SuccessRate = g.Count() > 0 ? (double)g.Count(h => h.IsSuccessful) / g.Count() * 100 : 0,
                    AvgExecutionTimeMs = g.Where(h => h.ExecutionTimeMs.HasValue).Any()
                        ? g.Where(h => h.ExecutionTimeMs.HasValue).Average(h => h.ExecutionTimeMs.Value)
                        : null,
                    MinExecutionTimeMs = g.Where(h => h.ExecutionTimeMs.HasValue).Any()
                        ? g.Where(h => h.ExecutionTimeMs.HasValue).Min(h => h.ExecutionTimeMs.Value)
                        : null,
                    MaxExecutionTimeMs = g.Where(h => h.ExecutionTimeMs.HasValue).Any()
                        ? g.Where(h => h.ExecutionTimeMs.HasValue).Max(h => h.ExecutionTimeMs.Value)
                        : null,
                    LastExecution = g.Max(h => h.Timestamp),
                    AlertsTriggered = g.Count(h => h.ShouldAlert),
                    AlertsSent = g.Count(h => h.AlertSent),
                    UniqueExecutors = g.Where(h => !string.IsNullOrEmpty(h.ExecutedBy)).Select(h => h.ExecutedBy).Distinct().Count(),
                    ExecutionMethods = g.Where(h => !string.IsNullOrEmpty(h.ExecutionMethod)).Select(h => h.ExecutionMethod).Distinct().Count()
                }).ToList();

            _logger.LogDebug("Retrieved execution stats for {Count} KPIs", stats.Count);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution statistics");
            return StatusCode(500, "An error occurred while retrieving execution statistics");
        }
    }

    /// <summary>
    /// Get detailed execution information
    /// </summary>
    [HttpGet("execution-history/{historicalId}")]
    [SwaggerOperation(Summary = "Get execution detail", Description = "Retrieve detailed execution information by ID")]
    [SwaggerResponse(200, "Successfully retrieved execution detail", typeof(ExecutionHistoryDetailDto))]
    [SwaggerResponse(404, "Execution not found", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<ExecutionHistoryDetailDto>> GetExecutionDetail(
        long historicalId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting execution detail for HistoricalId: {HistoricalId}", historicalId);

            var historyRepository = _unitOfWork.Repository<HistoricalData>();
            var execution = await historyRepository.GetByIdWithThenIncludesAsync(historicalId,
                query => query.Include(h => h.KPI));

            if (execution == null)
            {
                _logger.LogWarning("Execution with ID {HistoricalId} not found", historicalId);
                return NotFound($"Execution with ID {historicalId} not found");
            }

            var detail = new ExecutionHistoryDetailDto
            {
                HistoricalId = execution.HistoricalId,
                KpiId = execution.KpiId,
                Indicator = execution.KPI?.Indicator ?? "",
                KpiOwner = execution.KPI?.Owner ?? "",
                SpName = execution.KPI?.SpName ?? "",
                Timestamp = execution.Timestamp,
                ExecutedBy = execution.ExecutedBy,
                ExecutionMethod = execution.ExecutionMethod,
                CurrentValue = execution.Value,
                HistoricalValue = execution.HistoricalValue,
                DeviationPercent = execution.DeviationPercent,
                Period = execution.Period,
                MetricKey = execution.MetricKey,
                IsSuccessful = execution.IsSuccessful,
                ErrorMessage = execution.ErrorMessage,
                ExecutionTimeMs = execution.ExecutionTimeMs,
                SqlCommand = execution.SqlCommand,
                SqlParameters = execution.SqlParameters,
                RawResponse = execution.RawResponse,
                ConnectionString = execution.ConnectionString,
                DatabaseName = execution.DatabaseName,
                ServerName = execution.ServerName,
                IpAddress = execution.IpAddress,
                UserAgent = execution.UserAgent,
                SessionId = execution.SessionId,
                ExecutionContext = execution.ExecutionContext,
                ShouldAlert = execution.ShouldAlert,
                AlertSent = execution.AlertSent
            };

            return Ok(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution detail for HistoricalId: {HistoricalId}", historicalId);
            return StatusCode(500, "An error occurred while retrieving execution detail");
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get performance category based on execution time
    /// </summary>
    private static string GetPerformanceCategory(int? executionTimeMs)
    {
        if (!executionTimeMs.HasValue) return "Unknown";

        return executionTimeMs.Value switch
        {
            < 1000 => "Fast",
            < 5000 => "Normal",
            < 10000 => "Slow",
            _ => "Very Slow"
        };
    }

    /// <summary>
    /// Get deviation category based on deviation percentage
    /// </summary>
    private static string GetDeviationCategory(decimal? deviationPercent)
    {
        if (!deviationPercent.HasValue) return "None";

        var absDeviation = Math.Abs(deviationPercent.Value);
        return absDeviation switch
        {
            < 1 => "Minimal",
            < 5 => "Low",
            < 10 => "Moderate",
            < 20 => "High",
            _ => "Critical"
        };
    }

    #endregion
}
