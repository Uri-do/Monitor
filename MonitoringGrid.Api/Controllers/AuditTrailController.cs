using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Core.EventSourcing;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API controller for audit trail and event sourcing operations
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[Authorize]
[PerformanceMonitor(slowThresholdMs: 3000)]
public class AuditTrailController : ControllerBase
{
    private readonly IEventSourcingService _eventSourcingService;
    private readonly ILogger<AuditTrailController> _logger;

    public AuditTrailController(
        IEventSourcingService eventSourcingService,
        ILogger<AuditTrailController> logger)
    {
        _eventSourcingService = eventSourcingService;
        _logger = logger;
    }

    /// <summary>
    /// Get audit trail for a specific entity
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "entityType", "entityId" })]
    public async Task<ActionResult<IEnumerable<AuditTrailEntryDto>>> GetEntityAuditTrail(
        string entityType, 
        string entityId)
    {
        try
        {
            var auditTrail = await _eventSourcingService.GetAuditTrailAsync(entityType, entityId);
            var dtos = auditTrail.Select(MapToDto).ToList();

            _logger.LogInformation("Retrieved {Count} audit trail entries for {EntityType} {EntityId}",
                dtos.Count, entityType, entityId);

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for {EntityType} {EntityId}", entityType, entityId);
            return StatusCode(500, "An error occurred while retrieving the audit trail");
        }
    }

    /// <summary>
    /// Search audit trail with filtering options
    /// </summary>
    [HttpPost("search")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "query" })]
    public async Task<ActionResult<AuditTrailSearchResultDto>> SearchAuditTrail([FromBody] AuditTrailSearchRequest request)
    {
        try
        {
            var query = new AuditTrailQuery
            {
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                UserId = request.UserId,
                EventType = request.EventType,
                SearchTerm = request.SearchTerm,
                From = request.From,
                To = request.To,
                Skip = request.Skip,
                Take = Math.Min(request.Take, 1000) // Limit to prevent large responses
            };

            var auditTrail = await _eventSourcingService.GetAuditTrailAsync(query);
            var entries = auditTrail.Select(MapToDto).ToList();

            var result = new AuditTrailSearchResultDto
            {
                Entries = entries,
                TotalCount = entries.Count,
                HasMore = entries.Count == query.Take,
                Query = request
            };

            _logger.LogInformation("Audit trail search returned {Count} entries for query: {Query}",
                entries.Count, System.Text.Json.JsonSerializer.Serialize(request));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching audit trail");
            return StatusCode(500, "An error occurred while searching the audit trail");
        }
    }

    /// <summary>
    /// Get system-wide audit trail
    /// </summary>
    [HttpGet("system")]
    [Authorize(Roles = "Admin")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = new[] { "from", "to", "limit" })]
    public async Task<ActionResult<IEnumerable<AuditTrailEntryDto>>> GetSystemAuditTrail(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-7); // Default to last 7 days
            var toDate = to ?? DateTime.UtcNow;
            
            if (limit > 1000) limit = 1000; // Prevent large responses

            var auditTrail = await _eventSourcingService.GetSystemAuditTrailAsync(fromDate, toDate);
            var dtos = auditTrail.Take(limit).Select(MapToDto).ToList();

            _logger.LogInformation("Retrieved {Count} system audit trail entries from {From} to {To}",
                dtos.Count, fromDate, toDate);

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system audit trail");
            return StatusCode(500, "An error occurred while retrieving the system audit trail");
        }
    }

    /// <summary>
    /// Get audit trail statistics
    /// </summary>
    [HttpGet("statistics")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "from", "to" })]
    public async Task<ActionResult<AuditTrailStatisticsDto>> GetAuditTrailStatistics(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddDays(-30); // Default to last 30 days
            var toDate = to ?? DateTime.UtcNow;

            var auditTrail = await _eventSourcingService.GetSystemAuditTrailAsync(fromDate, toDate);
            var entries = auditTrail.ToList();

            var statistics = new AuditTrailStatisticsDto
            {
                TotalEvents = entries.Count,
                EventsByType = entries.GroupBy(e => e.EventType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByEntityType = entries.GroupBy(e => e.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByUser = entries.Where(e => !string.IsNullOrEmpty(e.UserId))
                    .GroupBy(e => e.UserId!)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByDay = entries.GroupBy(e => e.Timestamp.Date)
                    .ToDictionary(g => g.Key, g => g.Count()),
                From = fromDate,
                To = toDate
            };

            _logger.LogInformation("Generated audit trail statistics for {From} to {To}: {TotalEvents} events",
                fromDate, toDate, statistics.TotalEvents);

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating audit trail statistics");
            return StatusCode(500, "An error occurred while generating audit trail statistics");
        }
    }

    private static AuditTrailEntryDto MapToDto(AuditTrailEntry entry)
    {
        return new AuditTrailEntryDto
        {
            Id = entry.Id,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            EventType = entry.EventType,
            Description = entry.Description,
            Details = entry.Details,
            UserId = entry.UserId,
            Timestamp = entry.Timestamp,
            CorrelationId = entry.CorrelationId,
            CausationId = entry.CausationId
        };
    }
}

/// <summary>
/// DTO for audit trail entry
/// </summary>
public class AuditTrailEntryDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? CorrelationId { get; set; }
    public string? CausationId { get; set; }
}

/// <summary>
/// Request for searching audit trail
/// </summary>
public class AuditTrailSearchRequest
{
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? UserId { get; set; }
    public string? EventType { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 100;
}

/// <summary>
/// Result of audit trail search
/// </summary>
public class AuditTrailSearchResultDto
{
    public List<AuditTrailEntryDto> Entries { get; set; } = new();
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
    public AuditTrailSearchRequest Query { get; set; } = new();
}

/// <summary>
/// Audit trail statistics
/// </summary>
public class AuditTrailStatisticsDto
{
    public int TotalEvents { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<string, int> EventsByEntityType { get; set; } = new();
    public Dictionary<string, int> EventsByUser { get; set; } = new();
    public Dictionary<DateTime, int> EventsByDay { get; set; } = new();
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}
