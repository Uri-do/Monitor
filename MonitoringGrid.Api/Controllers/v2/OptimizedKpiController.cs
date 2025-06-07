using MediatR;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.CQRS.Queries.Kpi;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Core.Interfaces;

namespace MonitoringGrid.Api.Controllers.v2;

/// <summary>
/// Optimized KPI controller with performance enhancements
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/kpi/optimized")]
[Produces("application/json")]
public class OptimizedKpiController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OptimizedKpiController> _logger;
    private readonly IPerformanceMetricsService _performanceMetrics;

    public OptimizedKpiController(
        IMediator mediator,
        ILogger<OptimizedKpiController> logger,
        IPerformanceMetricsService performanceMetrics)
    {
        _mediator = mediator;
        _logger = logger;
        _performanceMetrics = performanceMetrics;
    }

    /// <summary>
    /// Gets KPIs with optimized projections and pagination
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
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MonitoringGrid.Api.CQRS.Queries.Kpi.KpiSummaryDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PagedResult<MonitoringGrid.Api.CQRS.Queries.Kpi.KpiSummaryDto>>> GetKpisOptimized(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? owner = null,
        [FromQuery] byte? priority = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "Indicator",
        [FromQuery] bool sortDescending = false)
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

        var result = await _mediator.Send(query);

        _logger.LogDebug("Retrieved {Count} of {Total} KPIs in page {Page}",
            result.Items.Count(), result.TotalCount, result.PageNumber);

        return Ok(result);
    }

    /// <summary>
    /// Gets optimized dashboard data with aggregations
    /// </summary>
    /// <param name="days">Number of days to include in metrics (default: 30)</param>
    /// <returns>Dashboard data with performance metrics</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(KpiDashboardOptimizedDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<KpiDashboardOptimizedDto>> GetDashboardOptimized(
        [FromQuery] int days = 30)
    {
        _logger.LogDebug("Getting optimized dashboard data for {Days} days", days);

        var query = new GetKpiDashboardOptimizedQuery { Days = days };
        var result = await _mediator.Send(query);

        _logger.LogDebug("Retrieved dashboard data: {TotalKpis} KPIs, {ActiveKpis} active, {RecentExecutions} recent executions",
            result.TotalKpis, result.ActiveKpis, result.RecentExecutions.Count);

        return Ok(result);
    }

    /// <summary>
    /// Gets KPI execution history with optimized projections
    /// </summary>
    /// <param name="kpiId">KPI identifier</param>
    /// <param name="startDate">Start date for history</param>
    /// <param name="endDate">End date for history</param>
    /// <param name="successOnly">Filter for successful executions only</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <returns>Paginated execution history</returns>
    [HttpGet("{kpiId:int}/history")]
    [ProducesResponseType(typeof(PagedResult<KpiExecutionHistoryDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PagedResult<KpiExecutionHistoryDto>>> GetExecutionHistoryOptimized(
        int kpiId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool? successOnly = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        _logger.LogDebug("Getting optimized execution history for KPI {KpiId}", kpiId);

        var query = new GetKpiExecutionHistoryOptimizedQuery
        {
            KpiId = kpiId,
            StartDate = startDate,
            EndDate = endDate,
            SuccessOnly = successOnly,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);

        _logger.LogDebug("Retrieved {Count} of {Total} execution records for KPI {KpiId}",
            result.Items.Count(), result.TotalCount, kpiId);

        return Ok(result);
    }

    /// <summary>
    /// Gets KPI analytics and statistics
    /// </summary>
    /// <param name="kpiId">KPI identifier</param>
    /// <param name="days">Number of days for analytics (default: 30)</param>
    /// <returns>KPI analytics data</returns>
    [HttpGet("{kpiId:int}/analytics")]
    [ProducesResponseType(typeof(KpiAnalyticsDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<KpiAnalyticsDto>> GetKpiAnalytics(
        int kpiId,
        [FromQuery] int days = 30)
    {
        _logger.LogDebug("Getting analytics for KPI {KpiId} for {Days} days", kpiId, days);

        var query = new GetKpiAnalyticsQuery
        {
            KpiId = kpiId,
            Days = days
        };

        var result = await _mediator.Send(query);

        _logger.LogDebug("Retrieved analytics for KPI {KpiId}: {TotalExecutions} executions, {SuccessRate}% success rate",
            kpiId, result.TotalExecutions, result.SuccessRate);

        return Ok(result);
    }

    /// <summary>
    /// Gets top performing or problematic KPIs
    /// </summary>
    /// <param name="type">Type of top KPIs to retrieve</param>
    /// <param name="count">Number of KPIs to return (default: 10)</param>
    /// <param name="days">Number of days for analysis (default: 7)</param>
    /// <returns>Top KPIs data</returns>
    [HttpGet("top")]
    [ProducesResponseType(typeof(TopKpisDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<TopKpisDto>> GetTopKpis(
        [FromQuery] TopKpiType type = TopKpiType.MostProblematic,
        [FromQuery] int count = 10,
        [FromQuery] int days = 7)
    {
        _logger.LogDebug("Getting top {Count} {Type} KPIs for {Days} days", count, type, days);

        var query = new GetTopKpisQuery
        {
            Type = type,
            Count = count,
            Days = days
        };

        var result = await _mediator.Send(query);

        _logger.LogDebug("Retrieved {Count} top {Type} KPIs", result.Items.Count, type);

        return Ok(result);
    }

    /// <summary>
    /// Gets performance metrics for the optimized endpoints
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
        var fromDate = from ?? DateTime.UtcNow.AddDays(-7);
        var toDate = to ?? DateTime.UtcNow;

        _logger.LogDebug("Getting performance metrics from {From} to {To}", fromDate, toDate);

        var report = await _performanceMetrics.GetPerformanceReportAsync(fromDate, toDate);

        _logger.LogDebug("Retrieved performance report: {TotalRequests} requests, {AverageResponseTime}ms avg response time",
            report.TotalRequests, report.AverageResponseTime);

        return Ok(report);
    }

    /// <summary>
    /// Gets slow requests for performance analysis
    /// </summary>
    /// <param name="thresholdMs">Threshold in milliseconds for slow requests (default: 1000)</param>
    /// <param name="count">Number of slow requests to return (default: 50)</param>
    /// <returns>List of slow requests</returns>
    [HttpGet("performance/slow-requests")]
    [ProducesResponseType(typeof(List<SlowRequestInfo>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<List<SlowRequestInfo>>> GetSlowRequests(
        [FromQuery] int thresholdMs = 1000,
        [FromQuery] int count = 50)
    {
        _logger.LogDebug("Getting slow requests with threshold {ThresholdMs}ms, count {Count}", thresholdMs, count);

        var slowRequests = await _performanceMetrics.GetSlowRequestsAsync(thresholdMs, count);

        _logger.LogDebug("Retrieved {Count} slow requests", slowRequests.Count);

        return Ok(slowRequests);
    }

    /// <summary>
    /// Gets system performance metrics
    /// </summary>
    /// <returns>Current system performance metrics</returns>
    [HttpGet("performance/system")]
    [ProducesResponseType(typeof(SystemPerformanceMetrics), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SystemPerformanceMetrics>> GetSystemMetrics()
    {
        _logger.LogDebug("Getting system performance metrics");

        var metrics = await _performanceMetrics.GetSystemMetricsAsync();

        _logger.LogDebug("Retrieved system metrics: {MemoryUsage}MB memory, {ThreadCount} threads",
            metrics.MemoryUsageMB, metrics.ThreadCount);

        return Ok(metrics);
    }
}
