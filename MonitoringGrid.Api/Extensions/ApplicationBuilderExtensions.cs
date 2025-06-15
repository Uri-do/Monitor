using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Api.Middleware;
using MonitoringGrid.Api.Versioning;
using System.Diagnostics;

namespace MonitoringGrid.Api.Extensions;

/// <summary>
/// Extension methods for IApplicationBuilder to organize middleware configuration
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configure development environment middleware
    /// </summary>
    public static IApplicationBuilder UseDevelopmentEnvironment(this IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MonitoringGrid API v1");
            c.RoutePrefix = string.Empty; // Serve Swagger UI at root
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.EnableFilter();
            c.ShowExtensions();
        });

        return app;
    }

    /// <summary>
    /// Configure production environment middleware
    /// </summary>
    public static IApplicationBuilder UseProductionEnvironment(this IApplicationBuilder app)
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        
        return app;
    }

    /// <summary>
    /// Configure security middleware
    /// </summary>
    public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseHttpsRedirection();
        
        return app;
    }

    /// <summary>
    /// Configure performance middleware
    /// </summary>
    public static IApplicationBuilder UsePerformanceMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<ResponseCachingMiddleware>();
        app.UseResponseCompression();
        
        return app;
    }

    /// <summary>
    /// Configure validation middleware
    /// </summary>
    public static IApplicationBuilder UseValidationMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<ValidationMiddleware>();
        
        return app;
    }

    /// <summary>
    /// Configure exception handling middleware
    /// </summary>
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<EnhancedExceptionHandlingMiddleware>();
        
        return app;
    }

    /// <summary>
    /// Configure rate limiting middleware
    /// </summary>
    public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<RateLimitingMiddleware>();
        
        return app;
    }

    /// <summary>
    /// Configure authentication and authorization
    /// </summary>
    public static IApplicationBuilder UseAuthenticationAndAuthorization(this IApplicationBuilder app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        
        return app;
    }

    /// <summary>
    /// Configure CORS
    /// </summary>
    public static IApplicationBuilder UseCorsConfiguration(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseCors("DevelopmentPolicy");
        }
        else
        {
            app.UseCors("DefaultPolicy");
        }
        
        return app;
    }

    /// <summary>
    /// Configure health checks
    /// </summary>
    public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health");
        app.UseHealthChecks("/health/ready");
        app.UseHealthChecks("/health/live");
        
        return app;
    }

    /// <summary>
    /// Configure database migration and seeding
    /// </summary>
    public static async Task<IApplicationBuilder> UseDatabaseMigrationAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            // Migrate MonitoringGrid database
            var monitoringContext = services.GetRequiredService<MonitoringContext>();
            await monitoringContext.Database.MigrateAsync();
            logger.LogInformation("MonitoringGrid database migration completed successfully");

            // Note: ProgressPlay and PopAI databases are external and should not be migrated
            logger.LogInformation("Database initialization completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }

        return app;
    }

    /// <summary>
    /// Configure request logging
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.Use(async (context, next) =>
            {
                var stopwatch = Stopwatch.StartNew();
                await next();
                stopwatch.Stop();

                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogInformation(
                    "Request {Method} {Path} completed in {ElapsedMilliseconds}ms with status {StatusCode}",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds,
                    context.Response.StatusCode);
            });
        }

        return app;
    }

    /// <summary>
    /// Configure static files
    /// </summary>
    public static IApplicationBuilder UseStaticFilesConfiguration(this IApplicationBuilder app)
    {
        app.UseStaticFiles();
        
        return app;
    }

    /// <summary>
    /// Configure routing
    /// </summary>
    public static IApplicationBuilder UseRoutingConfiguration(this IApplicationBuilder app)
    {
        app.UseRouting();
        
        return app;
    }

    /// <summary>
    /// Configure endpoints
    /// </summary>
    public static IApplicationBuilder UseEndpointsConfiguration(this IApplicationBuilder app)
    {
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health");
            
            // Default route for SPA
            endpoints.MapFallbackToFile("index.html");
        });
        
        return app;
    }

    /// <summary>
    /// Configure all middleware in the correct order
    /// </summary>
    public static async Task<IApplicationBuilder> ConfigureMiddlewarePipelineAsync(
        this IApplicationBuilder app, 
        IWebHostEnvironment env)
    {
        // Exception handling (must be first)
        app.UseExceptionHandlingMiddleware();

        // Environment-specific configuration
        if (env.IsDevelopment())
        {
            app.UseDevelopmentEnvironment();
        }
        else
        {
            app.UseProductionEnvironment();
        }

        // Security middleware
        app.UseSecurityMiddleware();

        // API versioning middleware
        app.UseAdvancedApiVersioning();

        // Performance middleware
        app.UsePerformanceMiddleware();

        // Request logging
        app.UseRequestLogging(env);

        // Static files
        app.UseStaticFilesConfiguration();

        // Routing
        app.UseRoutingConfiguration();

        // CORS
        app.UseCorsConfiguration(env);

        // Rate limiting
        app.UseRateLimitingMiddleware();

        // Authentication and authorization
        app.UseAuthenticationAndAuthorization();

        // Validation
        app.UseValidationMiddleware();

        // Health checks
        app.UseHealthChecks();

        // Endpoints
        app.UseEndpointsConfiguration();

        // Database migration
        await app.UseDatabaseMigrationAsync();

        return app;
    }
}
