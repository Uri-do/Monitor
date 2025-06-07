using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Services;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Filters;

namespace MonitoringGrid.Api.Controllers.v2;

/// <summary>
/// Enhanced bulk operations controller with performance optimizations
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/bulk")]
[Produces("application/json")]
public class BulkOperationsController : ControllerBase
{
    private readonly IBulkOperationsService _bulkOperationsService;
    private readonly ILogger<BulkOperationsController> _logger;

    public BulkOperationsController(
        IBulkOperationsService bulkOperationsService,
        ILogger<BulkOperationsController> logger)
    {
        _bulkOperationsService = bulkOperationsService;
        _logger = logger;
    }

    /// <summary>
    /// Creates multiple KPIs in a single operation
    /// </summary>
    /// <param name="requests">List of KPI creation requests</param>
    /// <returns>Bulk operation result</returns>
    [HttpPost("kpis")]
    [ProducesResponseType(typeof(BulkOperationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BulkOperationResult>> BulkCreateKpis(
        [FromBody] List<CreateKpiRequest> requests)
    {
        if (!requests.Any())
        {
            return BadRequest("At least one KPI request is required");
        }

        _logger.LogInformation("Starting bulk creation of {Count} KPIs", requests.Count);

        var result = await _bulkOperationsService.BulkCreateKpisAsync(requests);

        _logger.LogInformation("Bulk KPI creation completed: {SuccessCount}/{TotalCount} successful in {ExecutionTime}ms",
            result.SuccessCount, result.TotalRequested, result.ExecutionTimeMs);

        return Ok(result);
    }

    /// <summary>
    /// Updates the active status of multiple KPIs
    /// </summary>
    /// <param name="request">Bulk status update request</param>
    /// <returns>Bulk operation result</returns>
    [HttpPut("kpis/status")]
    [ProducesResponseType(typeof(BulkOperationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BulkOperationResult>> BulkUpdateKpiStatus(
        [FromBody] BulkStatusUpdateRequest request)
    {
        if (!request.KpiIds.Any())
        {
            return BadRequest("At least one KPI ID is required");
        }

        _logger.LogInformation("Starting bulk status update for {Count} KPIs to {Status}",
            request.KpiIds.Count, request.IsActive ? "active" : "inactive");

        var result = await _bulkOperationsService.BulkUpdateKpiStatusAsync(request.KpiIds, request.IsActive);

        _logger.LogInformation("Bulk status update completed: {SuccessCount}/{TotalCount} successful in {ExecutionTime}ms",
            result.SuccessCount, result.TotalRequested, result.ExecutionTimeMs);

        return Ok(result);
    }

    /// <summary>
    /// Updates the frequency of multiple KPIs
    /// </summary>
    /// <param name="request">Bulk frequency update request</param>
    /// <returns>Bulk operation result</returns>
    [HttpPut("kpis/frequency")]
    [ProducesResponseType(typeof(BulkOperationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BulkOperationResult>> BulkUpdateKpiFrequency(
        [FromBody] BulkFrequencyUpdateRequest request)
    {
        if (!request.KpiIds.Any())
        {
            return BadRequest("At least one KPI ID is required");
        }

        if (request.NewFrequency <= 0)
        {
            return BadRequest("Frequency must be greater than 0");
        }

        _logger.LogInformation("Starting bulk frequency update for {Count} KPIs to {Frequency} minutes",
            request.KpiIds.Count, request.NewFrequency);

        var result = await _bulkOperationsService.BulkUpdateKpiFrequencyAsync(request.KpiIds, request.NewFrequency);

        _logger.LogInformation("Bulk frequency update completed: {SuccessCount}/{TotalCount} successful in {ExecutionTime}ms",
            result.SuccessCount, result.TotalRequested, result.ExecutionTimeMs);

        return Ok(result);
    }

    /// <summary>
    /// Assigns contacts to multiple KPIs
    /// </summary>
    /// <param name="request">Bulk contact assignment request</param>
    /// <returns>Bulk operation result</returns>
    [HttpPut("kpis/contacts")]
    [ProducesResponseType(typeof(BulkOperationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BulkOperationResult>> BulkAssignContacts(
        [FromBody] BulkContactAssignmentRequest request)
    {
        if (!request.KpiIds.Any())
        {
            return BadRequest("At least one KPI ID is required");
        }

        if (!request.ContactIds.Any())
        {
            return BadRequest("At least one Contact ID is required");
        }

        _logger.LogInformation("Starting bulk contact assignment: {ContactCount} contacts to {KpiCount} KPIs",
            request.ContactIds.Count, request.KpiIds.Count);

        var result = await _bulkOperationsService.BulkAssignContactsAsync(request.KpiIds, request.ContactIds);

        _logger.LogInformation("Bulk contact assignment completed: {SuccessCount} assignments in {ExecutionTime}ms",
            result.SuccessCount, result.ExecutionTimeMs);

        return Ok(result);
    }

    /// <summary>
    /// Executes multiple KPIs
    /// </summary>
    /// <param name="request">Bulk execution request</param>
    /// <returns>Bulk operation result</returns>
    [HttpPost("kpis/execute")]
    [ProducesResponseType(typeof(BulkOperationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BulkOperationResult>> BulkExecuteKpis(
        [FromBody] BulkExecutionRequest request)
    {
        if (!request.KpiIds.Any())
        {
            return BadRequest("At least one KPI ID is required");
        }

        _logger.LogInformation("Starting bulk execution of {Count} KPIs", request.KpiIds.Count);

        var result = await _bulkOperationsService.BulkExecuteKpisAsync(request.KpiIds);

        _logger.LogInformation("Bulk execution completed: {SuccessCount}/{TotalCount} successful in {ExecutionTime}ms",
            result.SuccessCount, result.TotalRequested, result.ExecutionTimeMs);

        return Ok(result);
    }

    /// <summary>
    /// Deletes historical data older than specified date
    /// </summary>
    /// <param name="request">Bulk deletion request</param>
    /// <returns>Bulk operation result</returns>
    [HttpDelete("historical-data")]
    [ProducesResponseType(typeof(BulkOperationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BulkOperationResult>> BulkDeleteHistoricalData(
        [FromBody] BulkHistoricalDataDeletionRequest request)
    {
        if (request.OlderThan >= DateTime.UtcNow)
        {
            return BadRequest("Date must be in the past");
        }

        _logger.LogInformation("Starting bulk deletion of historical data older than {Date}", request.OlderThan);

        var result = await _bulkOperationsService.BulkDeleteHistoricalDataAsync(request.OlderThan);

        _logger.LogInformation("Bulk historical data deletion completed: {SuccessCount} records deleted in {ExecutionTime}ms",
            result.SuccessCount, result.ExecutionTimeMs);

        return Ok(result);
    }

    /// <summary>
    /// Archives historical data older than specified date
    /// </summary>
    /// <param name="request">Bulk archiving request</param>
    /// <returns>Bulk operation result</returns>
    [HttpPost("historical-data/archive")]
    [ProducesResponseType(typeof(BulkOperationResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BulkOperationResult>> BulkArchiveHistoricalData(
        [FromBody] BulkHistoricalDataArchiveRequest request)
    {
        if (request.OlderThan >= DateTime.UtcNow)
        {
            return BadRequest("Date must be in the past");
        }

        if (string.IsNullOrWhiteSpace(request.ArchiveLocation))
        {
            return BadRequest("Archive location is required");
        }

        _logger.LogInformation("Starting bulk archiving of historical data older than {Date} to {Location}",
            request.OlderThan, request.ArchiveLocation);

        var result = await _bulkOperationsService.BulkArchiveHistoricalDataAsync(request.OlderThan, request.ArchiveLocation);

        _logger.LogInformation("Bulk historical data archiving completed: {SuccessCount} records archived in {ExecutionTime}ms",
            result.SuccessCount, result.ExecutionTimeMs);

        return Ok(result);
    }

    /// <summary>
    /// Optimizes database indexes
    /// </summary>
    /// <returns>Bulk operation result</returns>
    [HttpPost("optimize-indexes")]
    [ProducesResponseType(typeof(BulkOperationResult), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<BulkOperationResult>> BulkOptimizeIndexes()
    {
        _logger.LogInformation("Starting database index optimization");

        var result = await _bulkOperationsService.BulkOptimizeIndexesAsync();

        _logger.LogInformation("Database index optimization completed in {ExecutionTime}ms", result.ExecutionTimeMs);

        return Ok(result);
    }
}

/// <summary>
/// Request for bulk status update
/// </summary>
public class BulkStatusUpdateRequest
{
    public List<int> KpiIds { get; set; } = new();
    public bool IsActive { get; set; }
}

/// <summary>
/// Request for bulk frequency update
/// </summary>
public class BulkFrequencyUpdateRequest
{
    public List<int> KpiIds { get; set; } = new();
    public int NewFrequency { get; set; }
}

/// <summary>
/// Request for bulk contact assignment
/// </summary>
public class BulkContactAssignmentRequest
{
    public List<int> KpiIds { get; set; } = new();
    public List<int> ContactIds { get; set; } = new();
}

/// <summary>
/// Request for bulk execution
/// </summary>
public class BulkExecutionRequest
{
    public List<int> KpiIds { get; set; } = new();
}

/// <summary>
/// Request for bulk historical data deletion
/// </summary>
public class BulkHistoricalDataDeletionRequest
{
    public DateTime OlderThan { get; set; }
}

/// <summary>
/// Request for bulk historical data archiving
/// </summary>
public class BulkHistoricalDataArchiveRequest
{
    public DateTime OlderThan { get; set; }
    public string ArchiveLocation { get; set; } = string.Empty;
}
