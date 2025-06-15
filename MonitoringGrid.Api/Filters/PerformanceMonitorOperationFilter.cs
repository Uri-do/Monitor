using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MonitoringGrid.Api.Filters;

/// <summary>
/// Swagger operation filter for performance monitoring documentation
/// </summary>
public class PerformanceMonitorOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Add performance monitoring headers to documentation
        operation.Parameters ??= new List<OpenApiParameter>();

        // Add correlation ID header
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-ID",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Correlation ID for request tracking",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid"
            }
        });

        // Add performance budget header
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Performance-Budget",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Expected response time budget in milliseconds",
            Schema = new OpenApiSchema
            {
                Type = "integer",
                Minimum = 1,
                Maximum = 30000
            }
        });

        // Add response headers documentation
        operation.Responses ??= new OpenApiResponses();

        foreach (var response in operation.Responses.Values)
        {
            response.Headers ??= new Dictionary<string, OpenApiHeader>();

            // Add performance timing header
            response.Headers["X-Response-Time"] = new OpenApiHeader
            {
                Description = "Response time in milliseconds",
                Schema = new OpenApiSchema
                {
                    Type = "integer"
                }
            };

            // Add cache status header
            response.Headers["X-Cache-Status"] = new OpenApiHeader
            {
                Description = "Cache hit/miss status",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Enum = new List<object> { "HIT", "MISS", "BYPASS" }
                }
            };

            // Add rate limit headers
            response.Headers["X-RateLimit-Limit"] = new OpenApiHeader
            {
                Description = "Request limit per time window",
                Schema = new OpenApiSchema
                {
                    Type = "integer"
                }
            };

            response.Headers["X-RateLimit-Remaining"] = new OpenApiHeader
            {
                Description = "Remaining requests in current window",
                Schema = new OpenApiSchema
                {
                    Type = "integer"
                }
            };

            response.Headers["X-RateLimit-Reset"] = new OpenApiHeader
            {
                Description = "Time when rate limit resets (Unix timestamp)",
                Schema = new OpenApiSchema
                {
                    Type = "integer"
                }
            };
        }

        // Add performance-related tags
        operation.Tags ??= new List<OpenApiTag>();
        
        // Add performance considerations to description
        if (!string.IsNullOrEmpty(operation.Description))
        {
            operation.Description += "\n\n**Performance Considerations:**\n" +
                                   "- This endpoint is monitored for performance\n" +
                                   "- Response times are tracked and optimized\n" +
                                   "- Rate limiting may apply based on usage patterns";
        }
    }
}
