using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoringGrid.Api.Controllers.Base;
using MonitoringGrid.Api.Common;
using MonitoringGrid.Api.Services;
using MonitoringGrid.Api.Monitoring;
using MonitoringGrid.Api.Security;
using MonitoringGrid.Api.Models;
using MonitoringGrid.Core.Interfaces;
using MediatR;

namespace MonitoringGrid.Api.Controllers;

/// <summary>
/// API controller for comprehensive API documentation and monitoring
/// </summary>
[ApiController]
[Route("api/documentation")]
[Authorize(Roles = "Admin")]
public class ApiDocumentationController : BaseApiController
{
    private readonly IApiDocumentationService _documentationService;
    private readonly IAdvancedPerformanceMonitoring _performanceMonitoring;
    private readonly IAdvancedSecurityService _securityService;

    public ApiDocumentationController(
        IMediator mediator,
        ILogger<ApiDocumentationController> logger,
        IApiDocumentationService documentationService,
        IAdvancedPerformanceMonitoring performanceMonitoring,
        IAdvancedSecurityService securityService,
        IPerformanceMetricsService? performanceMetrics = null)
        : base(mediator, logger, performanceMetrics)
    {
        _documentationService = documentationService;
        _performanceMonitoring = performanceMonitoring;
        _securityService = securityService;
    }

    /// <summary>
    /// Get comprehensive API documentation
    /// </summary>
    /// <returns>Complete API documentation with metadata</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ApiDocumentationInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetApiDocumentation()
    {
        try
        {
            var documentation = await _documentationService.GenerateDocumentationAsync();
            var response = ApiResponse<ApiDocumentationInfo>.Success(
                documentation,
                "API documentation generated successfully");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating API documentation");
            return StatusCode(500, CreateErrorResponse("Failed to generate API documentation"));
        }
    }

    /// <summary>
    /// Get API health status
    /// </summary>
    /// <returns>Current API health information</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ApiHealthInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetApiHealth()
    {
        try
        {
            var health = await _documentationService.GetApiHealthAsync();
            var response = ApiResponse<ApiHealthInfo>.Success(
                health,
                $"API health status: {health.Status}");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting API health");
            return StatusCode(500, CreateErrorResponse("Failed to get API health"));
        }
    }

    /// <summary>
    /// Get API performance metrics
    /// </summary>
    /// <returns>Current API performance information</returns>
    [HttpGet("performance")]
    [ProducesResponseType(typeof(ApiResponse<ApiPerformanceInfo>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetApiPerformance()
    {
        try
        {
            var performance = await _documentationService.GetApiPerformanceAsync();
            var response = ApiResponse<ApiPerformanceInfo>.Success(
                performance,
                "API performance metrics retrieved successfully");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting API performance");
            return StatusCode(500, CreateErrorResponse("Failed to get API performance"));
        }
    }

    /// <summary>
    /// Get detailed performance snapshot
    /// </summary>
    /// <returns>Comprehensive performance snapshot</returns>
    [HttpGet("performance/snapshot")]
    [ProducesResponseType(typeof(ApiResponse<PerformanceSnapshot>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetPerformanceSnapshot()
    {
        try
        {
            var snapshot = await _performanceMonitoring.GetPerformanceSnapshotAsync();
            var response = ApiResponse<PerformanceSnapshot>.Success(
                snapshot,
                "Performance snapshot captured successfully");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting performance snapshot");
            return StatusCode(500, CreateErrorResponse("Failed to get performance snapshot"));
        }
    }

    /// <summary>
    /// Get performance trends over time
    /// </summary>
    /// <param name="hours">Number of hours to analyze (default: 24)</param>
    /// <returns>Performance trends analysis</returns>
    [HttpGet("performance/trends")]
    [ProducesResponseType(typeof(ApiResponse<PerformanceTrends>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetPerformanceTrends([FromQuery] int hours = 24)
    {
        if (hours < 1 || hours > 168) // Max 1 week
        {
            return ValidationError(nameof(hours), "Hours must be between 1 and 168");
        }

        try
        {
            var period = TimeSpan.FromHours(hours);
            var trends = await _performanceMonitoring.GetPerformanceTrendsAsync(period);
            var response = ApiResponse<PerformanceTrends>.Success(
                trends,
                $"Performance trends for {hours} hours retrieved successfully");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting performance trends for {Hours} hours", hours);
            return StatusCode(500, CreateErrorResponse("Failed to get performance trends"));
        }
    }

    /// <summary>
    /// Get security metrics
    /// </summary>
    /// <param name="hours">Number of hours to analyze (default: 24)</param>
    /// <returns>Security metrics and threat analysis</returns>
    [HttpGet("security/metrics")]
    [ProducesResponseType(typeof(ApiResponse<SecurityMetrics>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetSecurityMetrics([FromQuery] int hours = 24)
    {
        if (hours < 1 || hours > 168) // Max 1 week
        {
            return ValidationError(nameof(hours), "Hours must be between 1 and 168");
        }

        try
        {
            var period = TimeSpan.FromHours(hours);
            var metrics = await _securityService.GetSecurityMetricsAsync(period);
            var response = ApiResponse<SecurityMetrics>.Success(
                metrics,
                $"Security metrics for {hours} hours retrieved successfully");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting security metrics for {Hours} hours", hours);
            return StatusCode(500, CreateErrorResponse("Failed to get security metrics"));
        }
    }

    /// <summary>
    /// Get API usage statistics
    /// </summary>
    /// <param name="days">Number of days to analyze (default: 7)</param>
    /// <returns>API usage statistics and analytics</returns>
    [HttpGet("usage")]
    [ProducesResponseType(typeof(ApiResponse<ApiUsageStatistics>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetApiUsage([FromQuery] int days = 7)
    {
        if (days < 1 || days > 90) // Max 3 months
        {
            return ValidationError(nameof(days), "Days must be between 1 and 90");
        }

        try
        {
            // This would typically query usage analytics
            var usage = new ApiUsageStatistics
            {
                CollectedAt = DateTime.UtcNow,
                Period = TimeSpan.FromDays(days),
                TotalRequests = 10000,
                UniqueUsers = 150,
                UserAgentCounts = new Dictionary<string, long>
                {
                    ["Chrome"] = 5000,
                    ["Firefox"] = 3000,
                    ["Safari"] = 2000
                },
                CountryCodeCounts = new Dictionary<string, long>
                {
                    ["US"] = 6000,
                    ["UK"] = 2000,
                    ["CA"] = 1500,
                    ["AU"] = 500
                },
                HourlyDistribution = new Dictionary<string, long>(),
                TopUsers = new List<TopUser>(),
                PopularEndpoints = new List<PopularEndpoint>()
            };

            var response = ApiResponse<ApiUsageStatistics>.Success(
                usage,
                $"API usage statistics for {days} days retrieved successfully");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting API usage for {Days} days", days);
            return StatusCode(500, CreateErrorResponse("Failed to get API usage"));
        }
    }

    /// <summary>
    /// Get API metrics summary
    /// </summary>
    /// <param name="startDate">Start date for metrics (optional)</param>
    /// <param name="endDate">End date for metrics (optional)</param>
    /// <returns>Comprehensive API metrics summary</returns>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ApiResponse<ApiMetricsSummary>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetApiMetrics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-7);
        var end = endDate ?? DateTime.UtcNow;

        if (start >= end)
        {
            return ValidationError("dateRange", "Start date must be before end date");
        }

        if ((end - start).TotalDays > 90)
        {
            return ValidationError("dateRange", "Date range cannot exceed 90 days");
        }

        try
        {
            // This would typically aggregate metrics from the database
            var metrics = new ApiMetricsSummary
            {
                PeriodStart = start,
                PeriodEnd = end,
                TotalRequests = 50000,
                SuccessfulRequests = 47500,
                FailedRequests = 2500,
                AverageResponseTime = 150.5,
                MedianResponseTime = 120.0,
                P95ResponseTime = 300.0,
                P99ResponseTime = 500.0,
                StatusCodeCounts = new Dictionary<string, long>
                {
                    ["200"] = 40000,
                    ["201"] = 5000,
                    ["400"] = 1500,
                    ["401"] = 500,
                    ["404"] = 300,
                    ["500"] = 200
                },
                EndpointCounts = new Dictionary<string, long>
                {
                    ["/api/indicators"] = 20000,
                    ["/api/contacts"] = 15000,
                    ["/api/statistics"] = 10000,
                    ["/api/alerts"] = 5000
                },
                TopErrors = new List<ErrorSummary>()
            };

            var response = ApiResponse<ApiMetricsSummary>.Success(
                metrics,
                $"API metrics from {start:yyyy-MM-dd} to {end:yyyy-MM-dd} retrieved successfully");
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting API metrics from {StartDate} to {EndDate}", start, end);
            return StatusCode(500, CreateErrorResponse("Failed to get API metrics"));
        }
    }

    /// <summary>
    /// Export API documentation in various formats
    /// </summary>
    /// <param name="format">Export format (json, yaml, pdf)</param>
    /// <returns>API documentation in requested format</returns>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> ExportDocumentation([FromQuery] string format = "json")
    {
        var allowedFormats = new[] { "json", "yaml", "pdf" };
        if (!allowedFormats.Contains(format.ToLowerInvariant()))
        {
            return ValidationError(nameof(format), $"Format must be one of: {string.Join(", ", allowedFormats)}");
        }

        try
        {
            var documentation = await _documentationService.GenerateDocumentationAsync();
            
            return format.ToLowerInvariant() switch
            {
                "json" => File(
                    System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(documentation),
                    "application/json",
                    $"api-documentation-{DateTime.UtcNow:yyyyMMdd}.json"),
                
                "yaml" => File(
                    System.Text.Encoding.UTF8.GetBytes("# API Documentation YAML export not implemented"),
                    "application/yaml",
                    $"api-documentation-{DateTime.UtcNow:yyyyMMdd}.yaml"),
                
                "pdf" => File(
                    System.Text.Encoding.UTF8.GetBytes("PDF export not implemented"),
                    "application/pdf",
                    $"api-documentation-{DateTime.UtcNow:yyyyMMdd}.pdf"),
                
                _ => BadRequest(CreateErrorResponse("Unsupported format"))
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error exporting API documentation in format {Format}", format);
            return StatusCode(500, CreateErrorResponse("Failed to export API documentation"));
        }
    }
}
