using Microsoft.AspNetCore.Mvc;
using MediatR;
using EnterpriseApp.Api.Controllers.Base;
using EnterpriseApp.Api.CQRS.Commands;
using EnterpriseApp.Api.CQRS.Queries;
using EnterpriseApp.Api.DTOs;
using EnterpriseApp.Core.Models;
<!--#if (enableAuth)-->
using Microsoft.AspNetCore.Authorization;
<!--#endif-->

namespace EnterpriseApp.Api.Controllers;

/// <summary>
/// Controller for managing DomainEntities
/// </summary>
[ApiController]
[Route("api/[controller]")]
<!--#if (enableAuth)-->
[Authorize]
<!--#endif-->
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class DomainEntitiesController : BaseApiController
{
    /// <summary>
    /// Initializes a new instance of the DomainEntitiesController
    /// </summary>
    public DomainEntitiesController(IMediator mediator, ILogger<DomainEntitiesController> logger)
        : base(mediator, logger)
    {
    }

    /// <summary>
    /// Gets a DomainEntity by ID
    /// </summary>
    /// <param name="id">The ID of the DomainEntity</param>
    /// <param name="includeItems">Whether to include items</param>
    /// <param name="includeAuditLogs">Whether to include audit logs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The DomainEntity</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DomainEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DomainEntityDto>> GetById(
        int id,
        [FromQuery] bool includeItems = false,
        [FromQuery] bool includeAuditLogs = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDomainEntityByIdQuery
        {
            Id = id,
            IncludeItems = includeItems,
            IncludeAuditLogs = includeAuditLogs,
            UserId = GetCurrentUserId()
        };

        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets all DomainEntities with pagination and filtering
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of DomainEntities</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DomainEntityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DomainEntityDto>>> GetAll(
        [FromQuery] GetDomainEntitiesQuery query,
        CancellationToken cancellationToken = default)
    {
        query.UserId = GetCurrentUserId();
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets active DomainEntities
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <param name="limit">Maximum number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active DomainEntities</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(List<DomainEntityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DomainEntityDto>>> GetActive(
        [FromQuery] string? category = null,
        [FromQuery] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetActiveDomainEntitiesQuery
        {
            Category = category,
            Limit = limit,
            UserId = GetCurrentUserId()
        };

        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Searches DomainEntities
    /// </summary>
    /// <param name="query">Search query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged search results</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<DomainEntityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DomainEntityDto>>> Search(
        [FromQuery] SearchDomainEntitiesQuery query,
        CancellationToken cancellationToken = default)
    {
        query.UserId = GetCurrentUserId();
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets DomainEntity statistics
    /// </summary>
    /// <param name="query">Statistics query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>DomainEntity statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(DomainEntityStatisticsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DomainEntityStatisticsDto>> GetStatistics(
        [FromQuery] GetDomainEntityStatisticsQuery query,
        CancellationToken cancellationToken = default)
    {
        query.UserId = GetCurrentUserId();
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets all categories
    /// </summary>
    /// <param name="activeOnly">Whether to include only active entities</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of categories</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetCategories(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDomainEntityCategoriesQuery
        {
            ActiveOnly = activeOnly,
            UserId = GetCurrentUserId()
        };

        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets all tags
    /// </summary>
    /// <param name="activeOnly">Whether to include only active entities</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of tags</returns>
    [HttpGet("tags")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<string>>> GetTags(
        [FromQuery] bool activeOnly = true,
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDomainEntityTagsQuery
        {
            ActiveOnly = activeOnly,
            Category = category,
            UserId = GetCurrentUserId()
        };

        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates a new DomainEntity
    /// </summary>
    /// <param name="command">Create command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created DomainEntity</returns>
    [HttpPost]
<!--#if (enableAuth)-->
    [Authorize(Policy = "CanCreateDomainEntity")]
<!--#endif-->
    [ProducesResponseType(typeof(DomainEntityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DomainEntityDto>> Create(
        [FromBody] CreateDomainEntityCommand command,
        CancellationToken cancellationToken = default)
    {
        command.UserId = GetCurrentUserId();
        command.IpAddress = GetClientIpAddress();
        command.UserAgent = GetUserAgent();

        var result = await Mediator.Send(command, cancellationToken);
        
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Updates an existing DomainEntity
    /// </summary>
    /// <param name="id">The ID of the DomainEntity to update</param>
    /// <param name="command">Update command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated DomainEntity</returns>
    [HttpPut("{id:int}")]
<!--#if (enableAuth)-->
    [Authorize(Policy = "CanUpdateDomainEntity")]
<!--#endif-->
    [ProducesResponseType(typeof(DomainEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<DomainEntityDto>> Update(
        int id,
        [FromBody] UpdateDomainEntityCommand command,
        CancellationToken cancellationToken = default)
    {
        command.Id = id;
        command.UserId = GetCurrentUserId();
        command.IpAddress = GetClientIpAddress();
        command.UserAgent = GetUserAgent();

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Deletes a DomainEntity
    /// </summary>
    /// <param name="id">The ID of the DomainEntity to delete</param>
    /// <param name="reason">Optional reason for deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:int}")]
<!--#if (enableAuth)-->
    [Authorize(Policy = "CanDeleteDomainEntity")]
<!--#endif-->
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete(
        int id,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteDomainEntityCommand
        {
            Id = id,
            Reason = reason,
            UserId = GetCurrentUserId(),
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent()
        };

        var result = await Mediator.Send(command, cancellationToken);
        
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Activates a DomainEntity
    /// </summary>
    /// <param name="id">The ID of the DomainEntity to activate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The activated DomainEntity</returns>
    [HttpPost("{id:int}/activate")]
<!--#if (enableAuth)-->
    [Authorize(Policy = "CanUpdateDomainEntity")]
<!--#endif-->
    [ProducesResponseType(typeof(DomainEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DomainEntityDto>> Activate(
        int id,
        CancellationToken cancellationToken = default)
    {
        var command = new ActivateDomainEntityCommand
        {
            Id = id,
            UserId = GetCurrentUserId(),
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent()
        };

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Deactivates a DomainEntity
    /// </summary>
    /// <param name="id">The ID of the DomainEntity to deactivate</param>
    /// <param name="reason">Optional reason for deactivation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deactivated DomainEntity</returns>
    [HttpPost("{id:int}/deactivate")]
<!--#if (enableAuth)-->
    [Authorize(Policy = "CanUpdateDomainEntity")]
<!--#endif-->
    [ProducesResponseType(typeof(DomainEntityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DomainEntityDto>> Deactivate(
        int id,
        [FromQuery] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var command = new DeactivateDomainEntityCommand
        {
            Id = id,
            Reason = reason,
            UserId = GetCurrentUserId(),
            IpAddress = GetClientIpAddress(),
            UserAgent = GetUserAgent()
        };

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Performs bulk operations on DomainEntities
    /// </summary>
    /// <param name="command">Bulk operation command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk operation result</returns>
    [HttpPost("bulk")]
<!--#if (enableAuth)-->
    [Authorize(Policy = "CanUpdateDomainEntity")]
<!--#endif-->
    [ProducesResponseType(typeof(BulkOperationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BulkOperationResultDto>> BulkOperation(
        [FromBody] BulkDomainEntityCommand command,
        CancellationToken cancellationToken = default)
    {
        command.UserId = GetCurrentUserId();
        command.IpAddress = GetClientIpAddress();
        command.UserAgent = GetUserAgent();

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets audit trail for a DomainEntity
    /// </summary>
    /// <param name="id">The ID of the DomainEntity</param>
    /// <param name="query">Audit query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged audit trail</returns>
    [HttpGet("{id:int}/audit")]
<!--#if (enableAuth)-->
    [Authorize(Policy = "CanViewAuditLogs")]
<!--#endif-->
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditTrail(
        int id,
        [FromQuery] GetDomainEntityAuditTrailQuery query,
        CancellationToken cancellationToken = default)
    {
        query.Id = id;
        query.UserId = GetCurrentUserId();

        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Exports DomainEntities
    /// </summary>
    /// <param name="query">Export query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported file</returns>
    [HttpGet("export")]
<!--#if (enableAuth)-->
    [Authorize(Policy = "CanExportData")]
<!--#endif-->
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<ActionResult> Export(
        [FromQuery] ExportDomainEntitiesQuery query,
        CancellationToken cancellationToken = default)
    {
        query.UserId = GetCurrentUserId();

        var result = await Mediator.Send(query, cancellationToken);
        
        if (result.IsSuccess)
        {
            var contentType = query.Format switch
            {
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.Csv => "text/csv",
                ExportFormat.Json => "application/json",
                ExportFormat.Xml => "application/xml",
                ExportFormat.Pdf => "application/pdf",
                _ => "application/octet-stream"
            };

            var fileName = $"domainentities_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{query.Format.ToString().ToLower()}";
            
            return File(result.Value, contentType, fileName);
        }

        return HandleResult(result);
    }
}
