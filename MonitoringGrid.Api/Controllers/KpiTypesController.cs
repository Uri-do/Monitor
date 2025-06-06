using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Api.DTOs;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API controller for managing KPI types
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class KpiTypesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<KpiTypesController> _logger;

    public KpiTypesController(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<KpiTypesController> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get all available KPI types
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<KpiTypeDto>>> GetKpiTypes()
    {
        try
        {
            var kpiTypeRepository = _unitOfWork.Repository<KpiType>();
            var kpiTypes = await kpiTypeRepository.GetAllAsync();
            
            var activeKpiTypes = kpiTypes.Where(kt => kt.IsActive).OrderBy(kt => kt.Name).ToList();
            var dtos = _mapper.Map<List<KpiTypeDto>>(activeKpiTypes);
            
            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI types");
            return StatusCode(500, "An error occurred while retrieving KPI types");
        }
    }

    /// <summary>
    /// Get KPI type by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<KpiTypeDto>> GetKpiType(string id)
    {
        try
        {
            var kpiTypeRepository = _unitOfWork.Repository<KpiType>();
            var kpiType = await kpiTypeRepository.GetByIdAsync(id);

            if (kpiType == null || !kpiType.IsActive)
                return NotFound($"KPI type with ID '{id}' not found");

            var dto = _mapper.Map<KpiTypeDto>(kpiType);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving KPI type with ID: {KpiTypeId}", id);
            return StatusCode(500, "An error occurred while retrieving the KPI type");
        }
    }

    /// <summary>
    /// Validate KPI configuration for a specific type
    /// </summary>
    [HttpPost("{id}/validate")]
    public async Task<ActionResult<KpiValidationResultDto>> ValidateKpiConfiguration(
        string id, 
        [FromBody] KpiConfigurationValidationRequest request)
    {
        try
        {
            var kpiTypeRepository = _unitOfWork.Repository<KpiType>();
            var kpiType = await kpiTypeRepository.GetByIdAsync(id);

            if (kpiType == null || !kpiType.IsActive)
                return NotFound($"KPI type with ID '{id}' not found");

            var validationResult = new KpiValidationResultDto
            {
                KpiTypeId = id,
                IsValid = true,
                Errors = new List<string>(),
                Warnings = new List<string>()
            };

            // Parse required fields
            var requiredFields = System.Text.Json.JsonSerializer.Deserialize<string[]>(kpiType.RequiredFields) ?? Array.Empty<string>();

            // Validate based on KPI type requirements
            foreach (var field in requiredFields)
            {
                switch (field.ToLower())
                {
                    case "deviation":
                        if (!request.Deviation.HasValue || request.Deviation < 0 || request.Deviation > 100)
                            validationResult.Errors.Add("Deviation must be between 0 and 100 percent");
                        break;

                    case "thresholdvalue":
                        if (!request.ThresholdValue.HasValue)
                            validationResult.Errors.Add("Threshold value is required for this KPI type");
                        else if (request.ThresholdValue < 0)
                            validationResult.Errors.Add("Threshold value must be positive");
                        break;

                    case "comparisonoperator":
                        var validOperators = new[] { "gt", "gte", "lt", "lte", "eq" };
                        if (string.IsNullOrEmpty(request.ComparisonOperator) || 
                            !validOperators.Contains(request.ComparisonOperator.ToLower()))
                            validationResult.Errors.Add("Valid comparison operator is required");
                        break;

                    case "minimumthreshold":
                        if (!request.MinimumThreshold.HasValue || request.MinimumThreshold < 0)
                            validationResult.Errors.Add("Minimum threshold must be a positive number");
                        break;

                    case "lastminutes":
                        if (!request.LastMinutes.HasValue || request.LastMinutes < 1)
                            validationResult.Errors.Add("Data window must be at least 1 minute");
                        break;
                }
            }

            // Type-specific validations
            switch (id.ToLower())
            {
                case "transaction_volume":
                    if (request.MinimumThreshold.HasValue && request.MinimumThreshold < 1)
                        validationResult.Warnings.Add("Transaction volume monitoring typically requires a minimum threshold of at least 1");
                    break;

                case "trend_analysis":
                    if (request.LastMinutes.HasValue && request.LastMinutes < 60)
                        validationResult.Warnings.Add("Trend analysis works best with at least 60 minutes of data");
                    break;

                case "threshold":
                    if (request.ThresholdValue == 0 && request.ComparisonOperator?.ToLower() == "gt")
                        validationResult.Warnings.Add("Threshold value of 0 with 'greater than' may cause frequent alerts");
                    break;
            }

            validationResult.IsValid = validationResult.Errors.Count == 0;

            return Ok(validationResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating KPI configuration for type: {KpiTypeId}", id);
            return StatusCode(500, "An error occurred while validating the KPI configuration");
        }
    }

    /// <summary>
    /// Get recommended configuration for a KPI type
    /// </summary>
    [HttpGet("{id}/recommendations")]
    public async Task<ActionResult<KpiRecommendationsDto>> GetRecommendations(string id)
    {
        try
        {
            var kpiTypeRepository = _unitOfWork.Repository<KpiType>();
            var kpiType = await kpiTypeRepository.GetByIdAsync(id);

            if (kpiType == null || !kpiType.IsActive)
                return NotFound($"KPI type with ID '{id}' not found");

            var recommendations = new KpiRecommendationsDto
            {
                KpiTypeId = id,
                KpiTypeName = kpiType.Name,
                Description = kpiType.Description,
                DefaultStoredProcedure = kpiType.DefaultStoredProcedure
            };

            // Provide type-specific recommendations
            switch (id.ToLower())
            {
                case "success_rate":
                    recommendations.RecommendedSettings = new Dictionary<string, object>
                    {
                        { "deviation", 10 },
                        { "lastMinutes", 1440 },
                        { "minimumThreshold", 10 }
                    };
                    recommendations.UseCases = new[]
                    {
                        "Payment processing success rate",
                        "API response success rate",
                        "User login success rate",
                        "Email delivery success rate"
                    };
                    recommendations.BestPractices = new[]
                    {
                        "Set deviation to 10-20% for most use cases",
                        "Use 24-hour window for daily patterns",
                        "Set minimum threshold to avoid alerts during low-volume periods"
                    };
                    break;

                case "transaction_volume":
                    recommendations.RecommendedSettings = new Dictionary<string, object>
                    {
                        { "deviation", 20 },
                        { "lastMinutes", 1440 },
                        { "minimumThreshold", 100 }
                    };
                    recommendations.UseCases = new[]
                    {
                        "Daily transaction count monitoring",
                        "API call volume tracking",
                        "User registration monitoring",
                        "Order volume analysis"
                    };
                    recommendations.BestPractices = new[]
                    {
                        "Allow higher deviation (20-30%) for volume metrics",
                        "Set appropriate minimum threshold based on expected volume",
                        "Consider seasonal patterns in your thresholds"
                    };
                    break;

                case "threshold":
                    recommendations.RecommendedSettings = new Dictionary<string, object>
                    {
                        { "thresholdValue", 100 },
                        { "comparisonOperator", "gt" }
                    };
                    recommendations.UseCases = new[]
                    {
                        "CPU usage monitoring (>80%)",
                        "Memory usage alerts (>90%)",
                        "Queue length monitoring (>1000)",
                        "Error count alerts (>10)"
                    };
                    recommendations.BestPractices = new[]
                    {
                        "Set thresholds based on historical data",
                        "Use 'greater than' for resource usage alerts",
                        "Use 'less than' for availability metrics"
                    };
                    break;

                case "trend_analysis":
                    recommendations.RecommendedSettings = new Dictionary<string, object>
                    {
                        { "deviation", 15 },
                        { "lastMinutes", 2880 }
                    };
                    recommendations.UseCases = new[]
                    {
                        "Performance degradation detection",
                        "Capacity planning metrics",
                        "User behavior trend analysis",
                        "Resource usage growth tracking"
                    };
                    recommendations.BestPractices = new[]
                    {
                        "Use longer time windows (48+ hours) for trend analysis",
                        "Set moderate deviation thresholds (15-25%)",
                        "Consider weekly and monthly patterns"
                    };
                    break;
            }

            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recommendations for KPI type: {KpiTypeId}", id);
            return StatusCode(500, "An error occurred while retrieving KPI type recommendations");
        }
    }

    /// <summary>
    /// Get KPI execution statistics by type
    /// </summary>
    [HttpGet("{id}/statistics")]
    public async Task<ActionResult<KpiTypeStatisticsDto>> GetKpiTypeStatistics(string id, [FromQuery] int days = 30)
    {
        try
        {
            var kpiTypeRepository = _unitOfWork.Repository<KpiType>();
            var kpiType = await kpiTypeRepository.GetByIdAsync(id);

            if (kpiType == null || !kpiType.IsActive)
                return NotFound($"KPI type with ID '{id}' not found");

            var kpiRepository = _unitOfWork.Repository<KPI>();
            var alertRepository = _unitOfWork.Repository<AlertLog>();
            var historicalRepository = _unitOfWork.Repository<HistoricalData>();

            var startDate = DateTime.UtcNow.AddDays(-days);

            var allKpis = await kpiRepository.GetAllAsync();
            var allAlerts = await alertRepository.GetAllAsync();
            var allHistoricalData = await historicalRepository.GetAllAsync();

            var kpisOfType = allKpis.Where(k => k.KpiType == id).ToList();
            var kpiIds = kpisOfType.Select(k => k.KpiId).ToList();

            var alerts = allAlerts.Where(a => kpiIds.Contains(a.KpiId) && a.TriggerTime >= startDate).ToList();
            var executions = allHistoricalData.Where(h => kpiIds.Contains(h.KpiId) && h.Timestamp >= startDate).ToList();

            var statistics = new KpiTypeStatisticsDto
            {
                KpiTypeId = id,
                KpiTypeName = kpiType.Name,
                TotalKpis = kpisOfType.Count,
                ActiveKpis = kpisOfType.Count(k => k.IsActive),
                TotalExecutions = executions.Count,
                TotalAlerts = alerts.Count,
                AverageExecutionsPerKpi = kpisOfType.Count > 0 ? (double)executions.Count / kpisOfType.Count : 0,
                AlertRate = executions.Count > 0 ? (double)alerts.Count / executions.Count * 100 : 0,
                LastExecution = executions.OrderByDescending(e => e.Timestamp).FirstOrDefault()?.Timestamp,
                LastAlert = alerts.OrderByDescending(a => a.TriggerTime).FirstOrDefault()?.TriggerTime
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving statistics for KPI type: {KpiTypeId}", id);
            return StatusCode(500, "An error occurred while retrieving KPI type statistics");
        }
    }
}
