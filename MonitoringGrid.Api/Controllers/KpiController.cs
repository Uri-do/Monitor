using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Services;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API controller for managing KPIs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class KpiController : ControllerBase
{
    private readonly IRepository<KPI> _kpiRepository;
    private readonly IRepository<AlertLog> _alertRepository;
    private readonly IRepository<HistoricalData> _historicalRepository;
    private readonly IMapper _mapper;
    private readonly IKpiExecutionService _kpiExecutionService;
    private readonly KpiDomainService _kpiDomainService;
    private readonly ILogger<KpiController> _logger;

    public KpiController(
        IRepository<KPI> kpiRepository,
        IRepository<AlertLog> alertRepository,
        IRepository<HistoricalData> historicalRepository,
        IMapper mapper,
        IKpiExecutionService kpiExecutionService,
        KpiDomainService kpiDomainService,
        ILogger<KpiController> logger)
    {
        _kpiRepository = kpiRepository;
        _alertRepository = alertRepository;
        _historicalRepository = historicalRepository;
        _mapper = mapper;
        _kpiExecutionService = kpiExecutionService;
        _kpiDomainService = kpiDomainService;
        _logger = logger;
    }

    /// <summary>
    /// Get all KPIs with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<KpiDto>>> GetKpis(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? owner = null,
        [FromQuery] byte? priority = null)
    {
        var allKpis = await _kpiRepository.GetAllAsync();
        var filteredKpis = allKpis.AsQueryable();

        if (isActive.HasValue)
            filteredKpis = filteredKpis.Where(k => k.IsActive == isActive.Value);

        if (!string.IsNullOrEmpty(owner))
            filteredKpis = filteredKpis.Where(k => k.Owner.Contains(owner));

        if (priority.HasValue)
            filteredKpis = filteredKpis.Where(k => k.Priority == priority.Value);

        var kpis = filteredKpis.OrderBy(k => k.Indicator).ToList();
        return Ok(_mapper.Map<List<KpiDto>>(kpis));
    }

    /// <summary>
    /// Get KPI by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<KpiDto>> GetKpi(int id)
    {
        var kpi = await _kpiRepository.GetByIdAsync(id);

        if (kpi == null)
            return NotFound($"KPI with ID {id} not found");

        return Ok(_mapper.Map<KpiDto>(kpi));
    }

    /// <summary>
    /// Create a new KPI
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<KpiDto>> CreateKpi([FromBody] CreateKpiRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Check if indicator name is unique using domain service
        if (!await _kpiDomainService.IsIndicatorUniqueAsync(request.Indicator))
            return BadRequest($"KPI with indicator '{request.Indicator}' already exists");

        var kpi = _mapper.Map<KPI>(request);
        kpi.CreatedDate = DateTime.UtcNow;
        kpi.ModifiedDate = DateTime.UtcNow;

        var createdKpi = await _kpiRepository.AddAsync(kpi);
        await _kpiRepository.SaveChangesAsync();

        // Note: Contact associations would be handled by a separate domain service
        // For now, we'll return the created KPI without contact associations

        _logger.LogInformation("Created KPI {Indicator} with ID {KpiId}", createdKpi.Indicator, createdKpi.KpiId);

        return CreatedAtAction(nameof(GetKpi), new { id = createdKpi.KpiId }, _mapper.Map<KpiDto>(createdKpi));
    }

    /// <summary>
    /// Update an existing KPI
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<KpiDto>> UpdateKpi(int id, [FromBody] UpdateKpiRequest request)
    {
        if (id != request.KpiId)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingKpi = await _kpiRepository.GetByIdAsync(id);

        if (existingKpi == null)
            return NotFound($"KPI with ID {id} not found");

        // Check if indicator name is unique (excluding current KPI)
        if (!await _kpiDomainService.IsIndicatorUniqueAsync(request.Indicator, id))
            return BadRequest($"KPI with indicator '{request.Indicator}' already exists");

        _mapper.Map(request, existingKpi);
        existingKpi.ModifiedDate = DateTime.UtcNow;

        await _kpiRepository.UpdateAsync(existingKpi);
        await _kpiRepository.SaveChangesAsync();

        _logger.LogInformation("Updated KPI {Indicator} with ID {KpiId}", existingKpi.Indicator, id);

        return Ok(_mapper.Map<KpiDto>(existingKpi));
    }

    /// <summary>
    /// Delete a KPI
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKpi(int id)
    {
        var kpi = await _kpiRepository.GetByIdAsync(id);
        if (kpi == null)
            return NotFound($"KPI with ID {id} not found");

        await _kpiRepository.DeleteAsync(kpi);
        await _kpiRepository.SaveChangesAsync();

        _logger.LogInformation("Deleted KPI {Indicator} with ID {KpiId}", kpi.Indicator, id);

        return NoContent();
    }

    /// <summary>
    /// Execute a KPI manually for testing
    /// </summary>
    [HttpPost("{id}/execute")]
    public async Task<ActionResult<KpiExecutionResultDto>> ExecuteKpi(int id, [FromBody] TestKpiRequest? request = null)
    {
        var kpi = await _kpiRepository.GetByIdAsync(id);
        if (kpi == null)
            return NotFound($"KPI with ID {id} not found");

        // Use custom frequency if provided, otherwise use KPI's configured frequency
        if (request?.CustomFrequency.HasValue == true)
        {
            kpi.Frequency = request.CustomFrequency.Value;
        }

        var result = await _kpiExecutionService.ExecuteKpiAsync(kpi);

        var dto = _mapper.Map<KpiExecutionResultDto>(result);
        dto.KpiId = id;
        dto.Indicator = kpi.Indicator;

        _logger.LogInformation("Manually executed KPI {Indicator} with result: {Result}",
            kpi.Indicator, result.GetSummary());

        return Ok(dto);
    }

    /// <summary>
    /// Get KPI status dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<KpiDashboardDto>> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var kpis = (await _kpiRepository.GetAllAsync()).ToList();
        var allAlerts = (await _alertRepository.GetAllAsync()).ToList();
        var alertsToday = allAlerts.Count(a => a.TriggerTime >= today);
        var alertsThisWeek = allAlerts.Count(a => a.TriggerTime >= today.AddDays(-7));

        var recentAlerts = allAlerts
            .Where(a => a.TriggerTime >= now.AddHours(-24))
            .OrderByDescending(a => a.TriggerTime)
            .Take(10)
            .Select(a => new KpiStatusDto
            {
                KpiId = a.KpiId,
                Indicator = kpis.FirstOrDefault(k => k.KpiId == a.KpiId)?.Indicator ?? "Unknown",
                Owner = kpis.FirstOrDefault(k => k.KpiId == a.KpiId)?.Owner ?? "Unknown",
                LastAlert = a.TriggerTime,
                LastDeviation = a.DeviationPercent
            })
            .ToList();

        var dueKpis = kpis
            .Where(k => k.IsActive && (k.LastRun == null || k.LastRun < now.AddMinutes(-k.Frequency)))
            .Select(k => _mapper.Map<KpiStatusDto>(k))
            .ToList();

        var dashboard = new KpiDashboardDto
        {
            TotalKpis = kpis.Count,
            ActiveKpis = kpis.Count(k => k.IsActive),
            InactiveKpis = kpis.Count(k => !k.IsActive),
            KpisInErrorCount = recentAlerts.Count,
            KpisDue = dueKpis.Count,
            AlertsToday = alertsToday,
            AlertsThisWeek = alertsThisWeek,
            LastUpdate = now,
            RecentAlerts = recentAlerts,
            KpisInError = recentAlerts,
            DueKpis = dueKpis
        };

        return Ok(dashboard);
    }

    /// <summary>
    /// Get KPI metrics and trend data
    /// </summary>
    [HttpGet("{id}/metrics")]
    public async Task<ActionResult<KpiMetricsDto>> GetKpiMetrics(int id, [FromQuery] int days = 30)
    {
        var kpi = await _kpiRepository.GetByIdAsync(id);
        if (kpi == null)
            return NotFound($"KPI with ID {id} not found");

        // Use domain service to get statistics
        var statistics = await _kpiDomainService.GetKpiStatisticsAsync(id, days);

        var startDate = DateTime.UtcNow.AddDays(-days);
        var allHistoricalData = (await _historicalRepository.GetAllAsync()).ToList();
        var allAlerts = (await _alertRepository.GetAllAsync()).ToList();

        var historicalData = allHistoricalData
            .Where(h => h.KpiId == id && h.Timestamp >= startDate)
            .OrderBy(h => h.Timestamp)
            .ToList();

        var alerts = allAlerts
            .Where(a => a.KpiId == id && a.TriggerTime >= startDate)
            .ToList();

        var metrics = new KpiMetricsDto
        {
            KpiId = id,
            Indicator = kpi.Indicator,
            TotalExecutions = historicalData.Count,
            SuccessfulExecutions = historicalData.Count, // Assuming all logged data is successful
            FailedExecutions = 0,
            SuccessRate = historicalData.Count > 0 ? 100 : 0,
            TotalAlerts = alerts.Count,
            LastExecution = historicalData.LastOrDefault()?.Timestamp,
            LastAlert = alerts.LastOrDefault()?.TriggerTime,
            TrendData = _mapper.Map<List<KpiTrendDataDto>>(historicalData)
        };

        return Ok(metrics);
    }

    /// <summary>
    /// Bulk operations on KPIs
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkOperation([FromBody] BulkKpiOperationRequest request)
    {
        if (!request.KpiIds.Any())
            return BadRequest("No KPI IDs provided");

        var allKpis = await _kpiRepository.GetAllAsync();
        var kpis = allKpis.Where(k => request.KpiIds.Contains(k.KpiId)).ToList();

        if (!kpis.Any())
            return NotFound("No KPIs found with the provided IDs");

        switch (request.Operation.ToLower())
        {
            case "activate":
                kpis.ForEach(k => k.IsActive = true);
                break;
            case "deactivate":
                kpis.ForEach(k => k.IsActive = false);
                break;
            case "delete":
                await _kpiRepository.DeleteRangeAsync(kpis);
                await _kpiRepository.SaveChangesAsync();
                _logger.LogInformation("Bulk operation {Operation} performed on {Count} KPIs",
                    request.Operation, kpis.Count);
                return Ok(new { Message = $"Operation '{request.Operation}' completed on {kpis.Count} KPIs" });
            default:
                return BadRequest($"Unknown operation: {request.Operation}");
        }

        // For activate/deactivate operations
        foreach (var kpi in kpis)
        {
            await _kpiRepository.UpdateAsync(kpi);
        }
        await _kpiRepository.SaveChangesAsync();

        _logger.LogInformation("Bulk operation {Operation} performed on {Count} KPIs",
            request.Operation, kpis.Count);

        return Ok(new { Message = $"Operation '{request.Operation}' completed on {kpis.Count} KPIs" });
    }
}
