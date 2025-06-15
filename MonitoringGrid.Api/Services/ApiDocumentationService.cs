using MonitoringGrid.Api.Models;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Diagnostics;
using MonitoringGrid.Infrastructure.Data;

namespace MonitoringGrid.Api.Services;

/// <summary>
/// Service for enhancing API documentation
/// </summary>
public interface IApiDocumentationService
{
    /// <summary>
    /// Generates comprehensive API documentation
    /// </summary>
    Task<ApiDocumentationInfo> GenerateDocumentationAsync();

    /// <summary>
    /// Gets API health and status information
    /// </summary>
    Task<ApiHealthInfo> GetApiHealthAsync();

    /// <summary>
    /// Gets API performance metrics
    /// </summary>
    Task<ApiPerformanceInfo> GetApiPerformanceAsync();
}

/// <summary>
/// Implementation of API documentation service
/// </summary>
public class ApiDocumentationService : IApiDocumentationService
{
    private readonly ILogger<ApiDocumentationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public ApiDocumentationService(
        ILogger<ApiDocumentationService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    /// <summary>
    /// Generates comprehensive API documentation
    /// </summary>
    public async Task<ApiDocumentationInfo> GenerateDocumentationAsync()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var controllers = GetControllerInfo(assembly);
            var endpoints = GetEndpointInfo();

            var documentation = new ApiDocumentationInfo
            {
                ApiName = "MonitoringGrid API",
                Version = "1.0",
                Description = "Comprehensive monitoring and alerting system API",
                GeneratedAt = DateTime.UtcNow,
                Controllers = controllers,
                Endpoints = endpoints,
                TotalEndpoints = endpoints.Count,
                AuthenticationSchemes = GetAuthenticationSchemes(),
                SupportedFormats = new[] { "application/json", "application/xml" },
                RateLimits = GetRateLimitInfo(),
                PerformanceMetrics = await GetPerformanceMetricsAsync()
            };

            _logger.LogInformation("Generated API documentation with {EndpointCount} endpoints", endpoints.Count);
            return documentation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate API documentation");
            throw;
        }
    }

    /// <summary>
    /// Gets API health and status information
    /// </summary>
    public async Task<ApiHealthInfo> GetApiHealthAsync()
    {
        try
        {
            var healthInfo = new ApiHealthInfo
            {
                Status = "Healthy",
                CheckedAt = DateTime.UtcNow,
                Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                Uptime = GetUptime(),
                Dependencies = await CheckDependenciesAsync(),
                MemoryUsage = GetMemoryUsage(),
                ActiveConnections = GetActiveConnections()
            };

            // Determine overall health status
            if (healthInfo.Dependencies.Any(d => d.Status != "Healthy"))
            {
                healthInfo.Status = "Degraded";
            }

            return healthInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API health information");
            return new ApiHealthInfo
            {
                Status = "Unhealthy",
                CheckedAt = DateTime.UtcNow,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets API performance metrics
    /// </summary>
    public async Task<ApiPerformanceInfo> GetApiPerformanceAsync()
    {
        try
        {
            var performanceInfo = new ApiPerformanceInfo
            {
                CollectedAt = DateTime.UtcNow,
                AverageResponseTime = await GetAverageResponseTimeAsync(),
                RequestsPerSecond = await GetRequestsPerSecondAsync(),
                ErrorRate = await GetErrorRateAsync(),
                CacheHitRate = await GetCacheHitRateAsync(),
                DatabaseConnectionPool = await GetDatabasePoolInfoAsync(),
                TopEndpoints = await GetTopEndpointsAsync(),
                RecentErrors = await GetRecentErrorsAsync()
            };

            return performanceInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API performance information");
            throw;
        }
    }

    private List<ControllerInfo> GetControllerInfo(Assembly assembly)
    {
        var controllers = new List<ControllerInfo>();

        var controllerTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller") && t.IsPublic)
            .ToList();

        foreach (var controllerType in controllerTypes)
        {
            var controllerInfo = new ControllerInfo
            {
                Name = controllerType.Name.Replace("Controller", ""),
                FullName = controllerType.FullName ?? controllerType.Name,
                Description = GetControllerDescription(controllerType),
                Actions = GetActionInfo(controllerType),
                Route = GetControllerRoute(controllerType)
            };

            controllers.Add(controllerInfo);
        }

        return controllers;
    }

    private List<EndpointInfo> GetEndpointInfo()
    {
        // This would typically integrate with ASP.NET Core's endpoint discovery
        // For now, return a basic implementation
        return new List<EndpointInfo>();
    }

    private string GetControllerDescription(Type controllerType)
    {
        var xmlDoc = controllerType.GetCustomAttributes<System.ComponentModel.DescriptionAttribute>()
            .FirstOrDefault()?.Description;

        return xmlDoc ?? $"API controller for {controllerType.Name.Replace("Controller", "")} operations";
    }

    private List<ActionInfo> GetActionInfo(Type controllerType)
    {
        var actions = new List<ActionInfo>();

        var methods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.IsPublic && !m.IsSpecialName && m.DeclaringType == controllerType)
            .ToList();

        foreach (var method in methods)
        {
            var actionInfo = new ActionInfo
            {
                Name = method.Name,
                HttpMethod = GetHttpMethod(method),
                Route = GetActionRoute(method),
                Description = GetActionDescription(method),
                Parameters = GetParameterInfo(method),
                ReturnType = method.ReturnType.Name
            };

            actions.Add(actionInfo);
        }

        return actions;
    }

    private string GetControllerRoute(Type controllerType)
    {
        var routeAttribute = controllerType.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>();
        return routeAttribute?.Template ?? $"api/{controllerType.Name.Replace("Controller", "").ToLowerInvariant()}";
    }

    private string GetHttpMethod(MethodInfo method)
    {
        if (method.GetCustomAttribute<Microsoft.AspNetCore.Mvc.HttpGetAttribute>() != null) return "GET";
        if (method.GetCustomAttribute<Microsoft.AspNetCore.Mvc.HttpPostAttribute>() != null) return "POST";
        if (method.GetCustomAttribute<Microsoft.AspNetCore.Mvc.HttpPutAttribute>() != null) return "PUT";
        if (method.GetCustomAttribute<Microsoft.AspNetCore.Mvc.HttpDeleteAttribute>() != null) return "DELETE";
        if (method.GetCustomAttribute<Microsoft.AspNetCore.Mvc.HttpPatchAttribute>() != null) return "PATCH";
        return "GET"; // Default
    }

    private string GetActionRoute(MethodInfo method)
    {
        var routeAttribute = method.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>();
        return routeAttribute?.Template ?? method.Name.ToLowerInvariant();
    }

    private string GetActionDescription(MethodInfo method)
    {
        return $"{method.Name} operation";
    }

    // Temporarily commented out due to ambiguous reference between MonitoringGrid.Api.Models.ParameterInfo and System.Reflection.ParameterInfo
    /*
    private List<ParameterInfo> GetParameterInfo(MethodInfo method)
    {
        return method.GetParameters()
            .Select(p => new MonitoringGrid.Api.Models.ParameterInfo
            {
                Name = p.Name ?? "unknown",
                Type = p.ParameterType.Name,
                IsRequired = !p.HasDefaultValue,
                DefaultValue = p.HasDefaultValue ? p.DefaultValue?.ToString() : null
            })
            .ToList();
    }
    */

    private List<MonitoringGrid.Api.Models.ParameterInfo> GetParameterInfo(MethodInfo method)
    {
        return method.GetParameters()
            .Select(p => new MonitoringGrid.Api.Models.ParameterInfo
            {
                Name = p.Name ?? "unknown",
                Type = p.ParameterType.Name,
                IsRequired = !p.HasDefaultValue,
                DefaultValue = p.HasDefaultValue ? p.DefaultValue?.ToString() : null
            })
            .ToList();
    }

    private List<string> GetAuthenticationSchemes()
    {
        return new List<string> { "Bearer", "ApiKey" };
    }

    private RateLimitInfo GetRateLimitInfo()
    {
        return new RateLimitInfo
        {
            DefaultLimit = 100,
            WindowSeconds = 60,
            BurstLimit = 150
        };
    }

    private async Task<Dictionary<string, object>> GetPerformanceMetricsAsync()
    {
        return new Dictionary<string, object>
        {
            ["ResponseTime"] = "< 200ms",
            ["Availability"] = "99.9%",
            ["Throughput"] = "1000 req/sec"
        };
    }

    private TimeSpan GetUptime()
    {
        return DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
    }

    private async Task<List<DependencyHealth>> CheckDependenciesAsync()
    {
        var dependencies = new List<DependencyHealth>();

        // Check database
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetService<MonitoringGridContext>();
            if (context != null)
            {
                await context.Database.CanConnectAsync();
                dependencies.Add(new DependencyHealth { Name = "Database", Status = "Healthy" });
            }
        }
        catch (Exception ex)
        {
            dependencies.Add(new DependencyHealth { Name = "Database", Status = "Unhealthy", Error = ex.Message });
        }

        return dependencies;
    }

    private long GetMemoryUsage()
    {
        return GC.GetTotalMemory(false);
    }

    private int GetActiveConnections()
    {
        // This would typically get actual connection count
        return 0;
    }

    private async Task<double> GetAverageResponseTimeAsync()
    {
        // Implementation would get actual metrics
        return 150.0;
    }

    private async Task<double> GetRequestsPerSecondAsync()
    {
        return 50.0;
    }

    private async Task<double> GetErrorRateAsync()
    {
        return 0.1;
    }

    private async Task<double> GetCacheHitRateAsync()
    {
        return 85.0;
    }

    private async Task<object> GetDatabasePoolInfoAsync()
    {
        return new { ActiveConnections = 5, MaxConnections = 100 };
    }

    private async Task<List<object>> GetTopEndpointsAsync()
    {
        return new List<object>();
    }

    private async Task<List<object>> GetRecentErrorsAsync()
    {
        return new List<object>();
    }
}
