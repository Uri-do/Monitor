using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MonitoringGrid.Api.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace MonitoringGrid.Api.Documentation;

/// <summary>
/// Provides comprehensive examples for Swagger documentation
/// </summary>
public static class SwaggerExamples
{
    /// <summary>
    /// Example for CreateKpiRequest
    /// </summary>
    public static readonly CreateKpiRequest CreateKpiExample = new()
    {
        Indicator = "Transaction_Success_Rate",
        Owner = "tech.team@company.com",
        Priority = 1, // SMS + Email alerts
        Frequency = 15, // Every 15 minutes
        LastMinutes = 60, // Look at last 60 minutes of data
        Deviation = 5.0m, // Alert if deviation > 5%
        SpName = "monitoring.usp_MonitorTransactionSuccessRate",
        SubjectTemplate = "🚨 Transaction Success Rate Alert: {current}% vs {historical}% (deviation: {deviation}%)",
        DescriptionTemplate = "Current success rate: {current}%, Historical average: {historical}%, Deviation: {deviation}%. Please investigate immediately.",
        CooldownMinutes = 30, // Wait 30 minutes between alerts
        ContactIds = new List<int> { 1, 2, 3 }
    };

    /// <summary>
    /// Example for UpdateKpiRequest
    /// </summary>
    public static readonly UpdateKpiRequest UpdateKpiExample = new()
    {
        Indicator = "Transaction_Success_Rate_Updated",
        Owner = "tech.team@company.com",
        Priority = 2, // Email only alerts
        Frequency = 30, // Every 30 minutes
        LastMinutes = 120, // Look at last 2 hours of data
        Deviation = 3.0m, // More sensitive - alert if deviation > 3%
        SpName = "monitoring.usp_MonitorTransactionSuccessRate_v2",
        SubjectTemplate = "📊 Transaction Success Rate Update: {current}% vs {historical}% (deviation: {deviation}%)",
        DescriptionTemplate = "Updated monitoring: Current success rate: {current}%, Historical average: {historical}%, Deviation: {deviation}%.",
        CooldownMinutes = 60, // Wait 1 hour between alerts
        ContactIds = new List<int> { 1, 2 }
    };

    /// <summary>
    /// Example for successful KpiDto response
    /// </summary>
    public static readonly KpiDto KpiResponseExample = new()
    {
        KpiId = 1,
        Indicator = "Transaction_Success_Rate",
        Owner = "tech.team@company.com",
        Priority = 1,
        Frequency = 15,
        LastMinutes = 60,
        Deviation = 5.0m,
        SpName = "monitoring.usp_MonitorTransactionSuccessRate",
        SubjectTemplate = "🚨 Transaction Success Rate Alert: {current}% vs {historical}% (deviation: {deviation}%)",
        DescriptionTemplate = "Current success rate: {current}%, Historical average: {historical}%, Deviation: {deviation}%. Please investigate immediately.",
        CooldownMinutes = 30,
        IsActive = true,
        CreatedDate = DateTime.UtcNow.AddDays(-7),
        ModifiedDate = DateTime.UtcNow.AddHours(-2),
        LastRun = DateTime.UtcNow.AddMinutes(-15),
        // ContactIds removed as it's not part of KpiDto response
    };

    /// <summary>
    /// Example for KpiExecutionResultDto
    /// </summary>
    public static readonly KpiExecutionResultDto ExecutionResultExample = new()
    {
        KpiId = 1,
        Indicator = "Transaction_Success_Rate",
        Key = "transaction_success_rate_2024",
        ExecutionTime = DateTime.UtcNow,
        CurrentValue = 94.5m,
        HistoricalValue = 98.2m,
        DeviationPercent = -3.77m,
        ShouldAlert = false,
        ErrorMessage = null,
        ExecutionTimeMs = 1250,
        ExecutionDetails = "KPI executed successfully. Current success rate: 94.5%, Historical average: 98.2%, Deviation: -3.77%",
        Metadata = new Dictionary<string, object>
        {
            { "DatabaseQueries", 3 },
            { "CacheHit", true },
            { "ProcessingSteps", 4 }
        },
        TimingInfo = new ExecutionTimingInfoDto
        {
            StartTime = DateTime.UtcNow.AddSeconds(-2),
            EndTime = DateTime.UtcNow,
            TotalExecutionMs = 1250,
            DatabaseConnectionMs = 45,
            StoredProcedureExecutionMs = 1150,
            ResultProcessingMs = 55,
            HistoricalDataSaveMs = 25
        },
        ExecutionSteps = new List<ExecutionStepInfoDto>
        {
            new() { StepName = "Database Connection", StartTime = DateTime.UtcNow.AddSeconds(-2), EndTime = DateTime.UtcNow.AddSeconds(-1.9), DurationMs = 45, Status = "Success", Details = "Connected to monitoring database" },
            new() { StepName = "Stored Procedure Execution", StartTime = DateTime.UtcNow.AddSeconds(-1.9), EndTime = DateTime.UtcNow.AddSeconds(-0.75), DurationMs = 1150, Status = "Success", Details = "Executed monitoring.usp_MonitorTransactionSuccessRate" },
            new() { StepName = "Result Processing", StartTime = DateTime.UtcNow.AddSeconds(-0.75), EndTime = DateTime.UtcNow, DurationMs = 55, Status = "Success", Details = "Processed results and calculated deviation" }
        }
    };

    /// <summary>
    /// Example for AlertLogDto
    /// </summary>
    public static readonly AlertLogDto AlertExample = new()
    {
        AlertId = 1,
        KpiId = 1,
        KpiIndicator = "Transaction_Success_Rate",
        KpiOwner = "tech.team@company.com",
        TriggerTime = DateTime.UtcNow.AddMinutes(-30),
        Message = "🚨 Transaction Success Rate Alert: 89.2% vs 98.5% (deviation: -9.44%)",
        Details = "Current success rate: 89.2%, Historical average: 98.5%, Deviation: -9.44%. Please investigate immediately.",
        SentVia = 3, // SMS + Email
        SentTo = "tech.team@company.com, +1234567890",
        CurrentValue = 89.2m,
        HistoricalValue = 98.5m,
        DeviationPercent = -9.44m,
        IsResolved = false,
        ResolvedTime = null,
        ResolvedBy = null
    };

    /// <summary>
    /// Example for error response
    /// </summary>
    public static readonly object ErrorResponseExample = new
    {
        type = "ValidationError",
        title = "One or more validation errors occurred",
        status = 400,
        detail = "The request contains invalid data",
        instance = "/api/kpi",
        errors = new
        {
            Frequency = new[] { "High priority KPIs (SMS alerts) should not run more frequently than every 5 minutes to avoid spam" },
            SpName = new[] { "Stored procedure name must be in 'monitoring' or 'stats' schema and contain no dangerous patterns" },
            SubjectTemplate = new[] { "Subject template must contain {current}, {historical}, and {deviation} placeholders and be safe" }
        },
        traceId = "0HN7GLLP5N1J7:00000001"
    };

    /// <summary>
    /// Example for bulk operations request
    /// </summary>
    public static readonly BulkKpiOperationRequest BulkOperationExample = new()
    {
        Operation = "activate",
        KpiIds = new List<int> { 1, 2, 3, 4, 5 }
    };

    /// <summary>
    /// Example for dashboard data
    /// </summary>
    public static readonly object DashboardExample = new
    {
        totalKpis = 25,
        activeKpis = 22,
        inactiveKpis = 3,
        kpisWithAlerts = 4,
        lastExecutionTime = DateTime.UtcNow.AddMinutes(-5),
        systemHealth = "Healthy",
        recentExecutions = new[]
        {
            new { kpiId = 1, indicator = "Transaction_Success_Rate", executionTime = DateTime.UtcNow.AddMinutes(-2), success = true },
            new { kpiId = 2, indicator = "Payment_Processing_Time", executionTime = DateTime.UtcNow.AddMinutes(-5), success = true },
            new { kpiId = 3, indicator = "User_Registration_Rate", executionTime = DateTime.UtcNow.AddMinutes(-8), success = false }
        },
        alertSummary = new
        {
            totalAlerts = 12,
            unresolvedAlerts = 3,
            criticalAlerts = 1,
            alertsLast24Hours = 8
        }
    };
}

/// <summary>
/// Swagger schema filter to add examples to request/response schemas
/// </summary>
public class SwaggerExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(CreateKpiRequest))
        {
            schema.Example = CreateOpenApiExample(SwaggerExamples.CreateKpiExample);
        }
        else if (context.Type == typeof(UpdateKpiRequest))
        {
            schema.Example = CreateOpenApiExample(SwaggerExamples.UpdateKpiExample);
        }
        else if (context.Type == typeof(KpiDto))
        {
            schema.Example = CreateOpenApiExample(SwaggerExamples.KpiResponseExample);
        }
        else if (context.Type == typeof(KpiExecutionResultDto))
        {
            schema.Example = CreateOpenApiExample(SwaggerExamples.ExecutionResultExample);
        }
        else if (context.Type == typeof(AlertLogDto))
        {
            schema.Example = CreateOpenApiExample(SwaggerExamples.AlertExample);
        }
        else if (context.Type == typeof(BulkKpiOperationRequest))
        {
            schema.Example = CreateOpenApiExample(SwaggerExamples.BulkOperationExample);
        }
    }

    private static IOpenApiAny CreateOpenApiExample(object example)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(example, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        return new OpenApiString(json);
    }
}

/// <summary>
/// Operation filter to add examples to specific operations
/// </summary>
public class SwaggerExampleOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add examples for error responses
        if (operation.Responses.ContainsKey("400"))
        {
            operation.Responses["400"].Content["application/json"].Example = 
                CreateOpenApiExample(SwaggerExamples.ErrorResponseExample);
        }

        // Add specific examples based on operation
        if (context.MethodInfo.Name == "GetDashboard")
        {
            if (operation.Responses.ContainsKey("200"))
            {
                operation.Responses["200"].Content["application/json"].Example = 
                    CreateOpenApiExample(SwaggerExamples.DashboardExample);
            }
        }
    }

    private static IOpenApiAny CreateOpenApiExample(object example)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(example, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
        return new OpenApiString(json);
    }
}

// ApiVersionOperationFilter removed - API versioning has been removed from the system

/// <summary>
/// Document filter to add comprehensive API documentation
/// </summary>
public class ApiDocumentationFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Add common response schemas
        swaggerDoc.Components.Schemas.Add("ErrorResponse", new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["type"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("ValidationError") },
                ["title"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("One or more validation errors occurred") },
                ["status"] = new OpenApiSchema { Type = "integer", Example = new OpenApiInteger(400) },
                ["detail"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("The request contains invalid data") },
                ["instance"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("/api/kpi") },
                ["traceId"] = new OpenApiSchema { Type = "string", Example = new OpenApiString("0HN7GLLP5N1J7:00000001") },
                ["errors"] = new OpenApiSchema
                {
                    Type = "object",
                    AdditionalProperties = new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema { Type = "string" }
                    }
                }
            }
        });

        // Add API information (only if not already present)
        if (!swaggerDoc.Info.Extensions.ContainsKey("x-api-features"))
        {
            swaggerDoc.Info.Extensions.Add("x-api-features", new OpenApiObject
            {
                ["cqrs"] = new OpenApiBoolean(true),
                ["resultPattern"] = new OpenApiBoolean(true),
                ["domainEvents"] = new OpenApiBoolean(true),
                ["validation"] = new OpenApiBoolean(true),
                ["authentication"] = new OpenApiBoolean(true),
                ["realtime"] = new OpenApiBoolean(true),
                ["monitoring"] = new OpenApiBoolean(true)
            });
        }

        // Add server information (only if not already present)
        if (!swaggerDoc.Servers.Any())
        {
            swaggerDoc.Servers.Add(new OpenApiServer
            {
                Url = "https://localhost:7000",
                Description = "Development server"
            });
            swaggerDoc.Servers.Add(new OpenApiServer
            {
                Url = "https://api.monitoringgrid.com",
                Description = "Production server"
            });
        }

        // Add tags for better organization (only if not already present)
        if (swaggerDoc.Tags == null || !swaggerDoc.Tags.Any())
        {
            swaggerDoc.Tags = new List<OpenApiTag>
            {
                new() { Name = "KPI Management", Description = "Operations for managing Key Performance Indicators" },
                new() { Name = "Alert Management", Description = "Operations for managing alerts and notifications" },
                new() { Name = "Analytics", Description = "Analytics and reporting endpoints" },
                new() { Name = "Real-time", Description = "Real-time monitoring and SignalR operations" },
                new() { Name = "Authentication", Description = "Authentication and authorization operations" },
                new() { Name = "System", Description = "System health and monitoring operations" }
            };
        }
    }
}
