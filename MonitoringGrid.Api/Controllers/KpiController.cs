using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Api.Observability;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Factories;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Services;
using MonitoringGrid.Core.Specifications;
using MonitoringGrid.Core.ValueObjects;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API controller for managing KPIs
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[PerformanceMonitor(slowThresholdMs: 2000)]
public class KpiController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IKpiExecutionService _kpiExecutionService;
    private readonly KpiDomainService _kpiDomainService;
    private readonly KpiFactory _kpiFactory;
    private readonly MetricsService _metricsService;
    private readonly ILogger<KpiController> _logger;

    public KpiController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IKpiExecutionService kpiExecutionService,
        KpiDomainService kpiDomainService,
        KpiFactory kpiFactory,
        MetricsService metricsService,
        ILogger<KpiController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _kpiExecutionService = kpiExecutionService;
        _kpiDomainService = kpiDomainService;
        _kpiFactory = kpiFactory;
        _metricsService = metricsService;
        _logger = logger;
    }

    /// <summary>
    /// Get all KPIs with optional filtering using specifications
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "isActive", "owner", "priority" })]
    [DatabasePerformanceMonitor]
    public async Task<ActionResult<List<KpiDto>>> GetKpis(
        [FromQuery] bool? isActive = null,
        [FromQuery] string? owner = null,
        [FromQuery] byte? priority = null)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            IEnumerable<KPI> kpis;

            if (!string.IsNullOrEmpty(owner))
            {
                // Use specification for owner filtering
                var specification = new KpisByOwnerSpecification(owner);
                kpis = await kpiRepository.GetAsync(specification);
            }
            else
            {
                kpis = await kpiRepository.GetAllAsync();
            }

            // Apply additional filters
            if (isActive.HasValue)
                kpis = kpis.Where(k => k.IsActive == isActive.Value);

            if (priority.HasValue)
                kpis = kpis.Where(k => k.Priority == priority.Value);

            var orderedKpis = kpis.OrderBy(k => k.Indicator).ToList();
            return Ok(_mapper.Map<List<KpiDto>>(orderedKpis));
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
    public async Task<ActionResult<KpiDto>> GetKpi(int id)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdAsync(id);

            if (kpi == null)
                return NotFound($"KPI with ID {id} not found");

            return Ok(_mapper.Map<KpiDto>(kpi));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI with ID: {KpiId}", id);
            return StatusCode(500, "An error occurred while retrieving the KPI");
        }
    }

    /// <summary>
    /// Create a new KPI using factory pattern and value objects
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<KpiDto>> CreateKpi([FromBody] CreateKpiRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate deviation percentage using value object
            try
            {
                var deviationPercentage = new DeviationPercentage(request.Deviation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            // Check if indicator name is unique using domain service
            if (!await _kpiDomainService.IsIndicatorUniqueAsync(request.Indicator))
                return BadRequest($"KPI with indicator '{request.Indicator}' already exists");

            // Use factory to create KPI
            var kpi = _kpiFactory.CreateKpi(
                request.Indicator,
                request.Owner,
                request.Priority,
                request.Frequency,
                request.Deviation,
                request.SpName,
                request.SubjectTemplate,
                request.DescriptionTemplate);

            // For simple operations, just use SaveChangesAsync without manual transactions
            var kpiRepository = _unitOfWork.Repository<KPI>();
            await kpiRepository.AddAsync(kpi);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Created KPI {Indicator} with ID {KpiId}", kpi.Indicator, kpi.KpiId);

            var kpiDto = _mapper.Map<KpiDto>(kpi);
            return CreatedAtAction(nameof(GetKpi), new { id = kpi.KpiId }, kpiDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid KPI creation request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating KPI: {@Request}", request);
            return StatusCode(500, "An error occurred while creating the KPI");
        }
    }

    /// <summary>
    /// Update an existing KPI with domain validation
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<KpiDto>> UpdateKpi(int id, [FromBody] UpdateKpiRequest request)
    {
        try
        {
            if (id != request.KpiId)
                return BadRequest("ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate using value objects
            try
            {
                var deviationPercentage = new DeviationPercentage(request.Deviation);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            var kpiRepository = _unitOfWork.Repository<KPI>();
            var existingKpi = await kpiRepository.GetByIdAsync(id);

            if (existingKpi == null)
                return NotFound($"KPI with ID {id} not found");

            // Check if indicator name is unique (excluding current KPI)
            if (!await _kpiDomainService.IsIndicatorUniqueAsync(request.Indicator, id))
                return BadRequest($"KPI with indicator '{request.Indicator}' already exists");

            // For simple operations, just use SaveChangesAsync without manual transactions
            _mapper.Map(request, existingKpi);
            existingKpi.ModifiedDate = DateTime.UtcNow;

            await kpiRepository.UpdateAsync(existingKpi);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated KPI {Indicator} with ID {KpiId}", existingKpi.Indicator, id);

            var kpiDto = _mapper.Map<KpiDto>(existingKpi);
            return Ok(kpiDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid KPI update request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating KPI {KpiId}: {@Request}", id, request);
            return StatusCode(500, "An error occurred while updating the KPI");
        }
    }

    /// <summary>
    /// Delete a KPI with proper domain validation
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKpi(int id)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdAsync(id);

            if (kpi == null)
                return NotFound($"KPI with ID {id} not found");

            // Use domain method for deactivation instead of deletion
            kpi.Deactivate("API Request", "Deleted via API");

            await kpiRepository.UpdateAsync(kpi);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Deactivated KPI {Indicator} with ID {KpiId}", kpi.Indicator, id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting KPI {KpiId}", id);
            return StatusCode(500, "An error occurred while deleting the KPI");
        }
    }

    /// <summary>
    /// Execute a KPI manually for testing
    /// </summary>
    [HttpPost("{id}/execute")]
    [KpiPerformanceMonitor]
    public async Task<ActionResult<KpiExecutionResultDto>> ExecuteKpi(int id, [FromBody] TestKpiRequest? request = null)
    {
        using var activity = KpiActivitySource.StartKpiExecution(id, "Manual Execution");
        var startTime = DateTime.UtcNow;

        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdAsync(id);

            if (kpi == null)
            {
                activity?.SetTag("error.type", "NotFound");
                return NotFound($"KPI with ID {id} not found");
            }

            // Add KPI details to activity
            activity?.SetTag("kpi.indicator", kpi.Indicator)
                    ?.SetTag("kpi.owner", kpi.Owner)
                    ?.SetTag("kpi.frequency", kpi.Frequency);

            _logger.LogKpiExecutionStart(id, kpi.Indicator, kpi.Owner);

            // Use custom frequency if provided, otherwise use KPI's configured frequency
            if (request?.CustomFrequency.HasValue == true)
            {
                kpi.Frequency = request.CustomFrequency.Value;
                activity?.SetTag("kpi.custom_frequency", request.CustomFrequency.Value);
            }

            var result = await _kpiExecutionService.ExecuteKpiAsync(kpi);
            var duration = DateTime.UtcNow - startTime;

            // Record metrics
            _metricsService.RecordKpiExecution(kpi.Indicator, kpi.Owner, duration.TotalSeconds, result.IsSuccessful);

            var dto = _mapper.Map<KpiExecutionResultDto>(result);
            dto.KpiId = id;
            dto.Indicator = kpi.Indicator;

            // Log structured completion
            _logger.LogKpiExecutionCompleted(id, kpi.Indicator, duration, result.IsSuccessful, result.GetSummary());

            // Record success in activity
            KpiActivitySource.RecordSuccess(activity, result.GetSummary());
            KpiActivitySource.RecordPerformanceMetrics(activity, (long)duration.TotalMilliseconds);

            return Ok(dto);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;

            // Record error metrics
            _metricsService.RecordKpiExecution("Unknown", "Unknown", duration.TotalSeconds, false);

            // Log structured error
            _logger.LogKpiExecutionError(id, "Unknown", ex, duration);

            // Record error in activity
            KpiActivitySource.RecordError(activity, ex);

            return StatusCode(500, "An error occurred while executing the KPI");
        }
    }

    /// <summary>
    /// Get KPI status dashboard
    /// </summary>
    [HttpGet("dashboard")]
    [ResponseCache(Duration = 60, VaryByQueryKeys = new string[] { })]
    [DatabasePerformanceMonitor]
    public async Task<ActionResult<KpiDashboardDto>> GetDashboard()
    {
        using var activity = ApiActivitySource.StartDashboardAggregation("KpiDashboard");
        var startTime = DateTime.UtcNow;

        try
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            var kpiRepository = _unitOfWork.Repository<KPI>();
            var alertRepository = _unitOfWork.Repository<AlertLog>();

            var kpis = (await kpiRepository.GetAllAsync()).ToList();
            var allAlerts = (await alertRepository.GetAllAsync()).ToList();

            activity?.SetTag("dashboard.kpi_count", kpis.Count)
                    ?.SetTag("dashboard.alert_count", allAlerts.Count);
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

            var duration = DateTime.UtcNow - startTime;

            // Update metrics
            _metricsService.UpdateKpiStatus(dashboard.ActiveKpis, dueKpis.Count);

            // Calculate and update system health score
            var healthScore = CalculateSystemHealthScore(dashboard);
            _metricsService.UpdateSystemHealth(healthScore);

            // Record performance
            KpiActivitySource.RecordSuccess(activity, $"Dashboard generated with {kpis.Count} KPIs");
            KpiActivitySource.RecordPerformanceMetrics(activity, (long)duration.TotalMilliseconds, kpis.Count);

            _logger.LogInformation("Dashboard data retrieved successfully in {Duration}ms " +
                "- {TotalKpis} KPIs, {ActiveKpis} active, {AlertsToday} alerts today",
                duration.TotalMilliseconds, dashboard.TotalKpis, dashboard.ActiveKpis, dashboard.AlertsToday);

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            KpiActivitySource.RecordError(activity, ex);
            _logger.LogError(ex, "Error retrieving dashboard data after {Duration}ms", duration.TotalMilliseconds);
            return StatusCode(500, "An error occurred while retrieving dashboard data");
        }
    }

    /// <summary>
    /// Calculate system health score based on dashboard metrics
    /// </summary>
    private static double CalculateSystemHealthScore(KpiDashboardDto dashboard)
    {
        if (dashboard.TotalKpis == 0) return 100.0;

        var activeRatio = (double)dashboard.ActiveKpis / dashboard.TotalKpis;
        var errorRatio = dashboard.ActiveKpis > 0 ? (double)dashboard.KpisInErrorCount / dashboard.ActiveKpis : 0;
        var dueRatio = dashboard.ActiveKpis > 0 ? (double)dashboard.KpisDue / dashboard.ActiveKpis : 0;

        // Health score calculation (0-100)
        var healthScore = 100.0;
        healthScore -= (1.0 - activeRatio) * 30; // Penalty for inactive KPIs
        healthScore -= errorRatio * 40; // Penalty for KPIs in error
        healthScore -= dueRatio * 30; // Penalty for overdue KPIs

        return Math.Max(0, Math.Min(100, healthScore));
    }

    /// <summary>
    /// Get KPI metrics and trend data
    /// </summary>
    [HttpGet("{id}/metrics")]
    public async Task<ActionResult<KpiMetricsDto>> GetKpiMetrics(int id, [FromQuery] int days = 30)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdAsync(id);

            if (kpi == null)
                return NotFound($"KPI with ID {id} not found");

            // Use domain service to get statistics
            var statistics = await _kpiDomainService.GetKpiStatisticsAsync(id, days);

            var startDate = DateTime.UtcNow.AddDays(-days);
            var historicalRepository = _unitOfWork.Repository<HistoricalData>();
            var alertRepository = _unitOfWork.Repository<AlertLog>();

            var allHistoricalData = (await historicalRepository.GetAllAsync()).ToList();
            var allAlerts = (await alertRepository.GetAllAsync()).ToList();

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI metrics for ID: {KpiId}", id);
            return StatusCode(500, "An error occurred while retrieving KPI metrics");
        }
    }

    /// <summary>
    /// Bulk operations on KPIs
    /// </summary>
    [HttpPost("bulk")]
    [DatabasePerformanceMonitor(slowQueryThresholdMs: 1000)]
    public async Task<IActionResult> BulkOperation([FromBody] BulkKpiOperationRequest request)
    {
        try
        {
            if (!request.KpiIds.Any())
                return BadRequest("No KPI IDs provided");

            var kpiRepository = _unitOfWork.Repository<KPI>();
            var allKpis = await kpiRepository.GetAllAsync();
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
                    await kpiRepository.DeleteRangeAsync(kpis);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Bulk operation {Operation} performed on {Count} KPIs",
                        request.Operation, kpis.Count);
                    return Ok(new { Message = $"Operation '{request.Operation}' completed on {kpis.Count} KPIs" });
                default:
                    return BadRequest($"Unknown operation: {request.Operation}");
            }

            // For activate/deactivate operations
            foreach (var kpi in kpis)
            {
                await kpiRepository.UpdateAsync(kpi);
            }
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Bulk operation {Operation} performed on {Count} KPIs",
                request.Operation, kpis.Count);

            return Ok(new { Message = $"Operation '{request.Operation}' completed on {kpis.Count} KPIs" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk operation: {Operation}", request.Operation);
            return StatusCode(500, "An error occurred while performing the bulk operation");
        }
    }

    /*
    /// <summary>
    /// Update KPI schedule configuration (TODO: Implement after scheduling service is ready)
    /// </summary>
    [HttpPost("{id}/schedule")]
    public async Task<IActionResult> UpdateSchedule(int id, [FromBody] ScheduleConfigurationRequest request)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdAsync(id);

            if (kpi == null)
                return NotFound($"KPI with ID {id} not found");

            // Validate schedule configuration
            var validation = await _kpiSchedulingService.ValidateScheduleConfigurationAsync(
                JsonSerializer.Serialize(request));

            if (!validation.IsValid)
                return BadRequest(new { Errors = validation.Errors, Warnings = validation.Warnings });

            // Update KPI schedule configuration
            kpi.ScheduleConfiguration = JsonSerializer.Serialize(request);
            kpi.ModifiedDate = DateTime.UtcNow;

            await kpiRepository.UpdateAsync(kpi);
            await _unitOfWork.SaveChangesAsync();

            // Update the scheduler
            await _kpiSchedulingService.UpdateKpiScheduleAsync(kpi);

            _logger.LogInformation("Updated schedule for KPI {Indicator} (ID: {KpiId})", kpi.Indicator, id);

            return Ok(new {
                Message = "Schedule updated successfully",
                NextExecution = validation.NextExecutionTime,
                Description = validation.ScheduleDescription
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schedule for KPI {KpiId}", id);
            return StatusCode(500, "An error occurred while updating the KPI schedule");
        }
    }

    /// <summary>
    /// Get scheduled KPIs information
    /// </summary>
    [HttpGet("scheduled")]
    public async Task<ActionResult<List<ScheduledKpiInfoDto>>> GetScheduledKpis()
    {
        try
        {
            var scheduledKpis = await _kpiSchedulingService.GetScheduledKpisAsync();
            var dtos = _mapper.Map<List<ScheduledKpiInfoDto>>(scheduledKpis);
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scheduled KPIs");
            return StatusCode(500, "An error occurred while retrieving scheduled KPIs");
        }
    }

    /// <summary>
    /// Pause KPI scheduling
    /// </summary>
    [HttpPost("{id}/pause")]
    public async Task<IActionResult> PauseKpi(int id)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdAsync(id);

            if (kpi == null)
                return NotFound($"KPI with ID {id} not found");

            await _kpiSchedulingService.PauseKpiAsync(id);

            _logger.LogInformation("Paused KPI {Indicator} (ID: {KpiId})", kpi.Indicator, id);
            return Ok(new { Message = "KPI scheduling paused successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing KPI {KpiId}", id);
            return StatusCode(500, "An error occurred while pausing the KPI");
        }
    }

    /// <summary>
    /// Resume KPI scheduling
    /// </summary>
    [HttpPost("{id}/resume")]
    public async Task<IActionResult> ResumeKpi(int id)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdAsync(id);

            if (kpi == null)
                return NotFound($"KPI with ID {id} not found");

            await _kpiSchedulingService.ResumeKpiAsync(id);

            _logger.LogInformation("Resumed KPI {Indicator} (ID: {KpiId})", kpi.Indicator, id);
            return Ok(new { Message = "KPI scheduling resumed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming KPI {KpiId}", id);
            return StatusCode(500, "An error occurred while resuming the KPI");
        }
    }

    /// <summary>
    /// Trigger immediate KPI execution
    /// </summary>
    [HttpPost("{id}/trigger")]
    public async Task<IActionResult> TriggerKpi(int id)
    {
        try
        {
            var kpiRepository = _unitOfWork.Repository<KPI>();
            var kpi = await kpiRepository.GetByIdAsync(id);

            if (kpi == null)
                return NotFound($"KPI with ID {id} not found");

            await _kpiSchedulingService.TriggerKpiExecutionAsync(id);

            _logger.LogInformation("Triggered immediate execution of KPI {Indicator} (ID: {KpiId})", kpi.Indicator, id);
            return Ok(new { Message = "KPI execution triggered successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering KPI {KpiId}", id);
            return StatusCode(500, "An error occurred while triggering the KPI");
        }
    }
    */
}
