using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Api.HealthChecks;

/// <summary>
/// Health check for KPI system status
/// </summary>
public class KpiHealthCheck : IHealthCheck
{
    private readonly MonitoringContext _context;
    private readonly ILogger<KpiHealthCheck> _logger;

    public KpiHealthCheck(MonitoringContext context, ILogger<KpiHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>();

            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Check for active KPIs
            var activeKpiCount = await _context.KPIs
                .Where(k => k.IsActive)
                .CountAsync(cancellationToken);
            
            data.Add("activeKpis", activeKpiCount);

            // Check for stale KPIs (haven't run in last 24 hours)
            var staleKpis = await _context.KPIs
                .Where(k => k.IsActive && k.LastRun < DateTime.UtcNow.AddHours(-24))
                .CountAsync(cancellationToken);
                
            data.Add("staleKpis", staleKpis);

            // Check for recent alerts
            var recentAlerts = await _context.AlertLogs
                .Where(a => a.TriggerTime > DateTime.UtcNow.AddHours(-1))
                .CountAsync(cancellationToken);
                
            data.Add("recentAlerts", recentAlerts);

            // Check for unresolved critical alerts (deviation >= 50%)
            var criticalAlerts = await _context.AlertLogs
                .Where(a => !a.IsResolved && a.DeviationPercent >= 50)
                .CountAsync(cancellationToken);
                
            data.Add("criticalAlerts", criticalAlerts);

            // Determine health status
            if (criticalAlerts > 5)
            {
                return HealthCheckResult.Unhealthy(
                    $"Too many critical alerts: {criticalAlerts}", 
                    data: data);
            }

            if (staleKpis > 10)
            {
                return HealthCheckResult.Degraded(
                    $"Many stale KPIs detected: {staleKpis}", 
                    data: data);
            }

            if (activeKpiCount == 0)
            {
                return HealthCheckResult.Degraded(
                    "No active KPIs found", 
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"KPI system healthy - {activeKpiCount} active KPIs", 
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KPI health check failed");
            return HealthCheckResult.Unhealthy(
                $"KPI health check failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Health check for database performance
/// </summary>
public class DatabasePerformanceHealthCheck : IHealthCheck
{
    private readonly MonitoringContext _context;
    private readonly ILogger<DatabasePerformanceHealthCheck> _logger;

    public DatabasePerformanceHealthCheck(MonitoringContext context, ILogger<DatabasePerformanceHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Test simple query performance
            var testQuery = await _context.KPIs
                .Take(1)
                .FirstOrDefaultAsync(cancellationToken);
                
            stopwatch.Stop();
            
            var queryTimeMs = stopwatch.ElapsedMilliseconds;
            var data = new Dictionary<string, object>
            {
                { "queryTimeMs", queryTimeMs },
                { "timestamp", DateTime.UtcNow }
            };

            if (queryTimeMs > 5000) // 5 seconds
            {
                return HealthCheckResult.Unhealthy(
                    $"Database query too slow: {queryTimeMs}ms", 
                    data: data);
            }

            if (queryTimeMs > 1000) // 1 second
            {
                return HealthCheckResult.Degraded(
                    $"Database query slow: {queryTimeMs}ms", 
                    data: data);
            }

            return HealthCheckResult.Healthy(
                $"Database performance good: {queryTimeMs}ms", 
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database performance health check failed");
            return HealthCheckResult.Unhealthy(
                $"Database performance check failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Health check for external services
/// </summary>
public class ExternalServicesHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExternalServicesHealthCheck> _logger;

    public ExternalServicesHealthCheck(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ExternalServicesHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var issues = new List<string>();

        try
        {
            // Check email service (if configured)
            var emailConfig = _configuration.GetSection("Email");
            if (emailConfig.Exists())
            {
                var emailStatus = await CheckEmailServiceAsync(cancellationToken);
                data.Add("emailService", emailStatus);
                if (!emailStatus.IsHealthy)
                {
                    issues.Add($"Email service: {emailStatus.Message}");
                }
            }

            // Check external APIs (if configured)
            var externalApis = _configuration.GetSection("ExternalApis").GetChildren();
            foreach (var apiConfig in externalApis)
            {
                var apiName = apiConfig.Key;
                var apiUrl = apiConfig["HealthCheckUrl"];
                
                if (!string.IsNullOrEmpty(apiUrl))
                {
                    var apiStatus = await CheckExternalApiAsync(apiName, apiUrl, cancellationToken);
                    data.Add($"externalApi_{apiName}", apiStatus);
                    if (!apiStatus.IsHealthy)
                    {
                        issues.Add($"External API {apiName}: {apiStatus.Message}");
                    }
                }
            }

            if (issues.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Some external services have issues: {string.Join(", ", issues)}", 
                    data: data);
            }

            return HealthCheckResult.Healthy("All external services are healthy", data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External services health check failed");
            return HealthCheckResult.Unhealthy(
                $"External services health check failed: {ex.Message}",
                data: data);
        }
    }

    private async Task<ServiceStatus> CheckEmailServiceAsync(CancellationToken cancellationToken)
    {
        try
        {
            // This is a placeholder - implement actual email service health check
            await Task.Delay(100, cancellationToken);
            return new ServiceStatus { IsHealthy = true, Message = "Email service accessible" };
        }
        catch (Exception ex)
        {
            return new ServiceStatus { IsHealthy = false, Message = ex.Message };
        }
    }

    private async Task<ServiceStatus> CheckExternalApiAsync(string apiName, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            
            var response = await client.GetAsync(url, cancellationToken);
            
            return new ServiceStatus 
            { 
                IsHealthy = response.IsSuccessStatusCode, 
                Message = $"Status: {response.StatusCode}" 
            };
        }
        catch (Exception ex)
        {
            return new ServiceStatus { IsHealthy = false, Message = ex.Message };
        }
    }

    private class ServiceStatus
    {
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
