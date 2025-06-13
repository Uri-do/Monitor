using EnterpriseApp.Api.Middleware;
<!--#if (enableMonitoring)-->
using Prometheus;
<!--#endif-->

namespace EnterpriseApp.Api.Extensions;

/// <summary>
/// Extension methods for IApplicationBuilder
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds security headers middleware
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            // Remove server header
            context.Response.Headers.Remove("Server");

            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

            // Add Content Security Policy
            var csp = "default-src 'self'; " +
                     "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                     "style-src 'self' 'unsafe-inline'; " +
                     "img-src 'self' data: https:; " +
                     "font-src 'self' data:; " +
                     "connect-src 'self'; " +
                     "frame-ancestors 'none';";
            context.Response.Headers.Add("Content-Security-Policy", csp);

            await next();
        });
    }

    /// <summary>
    /// Adds request/response logging middleware
    /// </summary>
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetService<Microsoft.Extensions.Options.IOptions<RequestResponseLoggingOptions>>()?.Value
                     ?? new RequestResponseLoggingOptions();

        return app.UseMiddleware<RequestResponseLoggingMiddleware>(options);
    }

    /// <summary>
    /// Adds performance monitoring middleware
    /// </summary>
    public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PerformanceMonitoringMiddleware>();
    }

    /// <summary>
    /// Adds rate limiting middleware
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }

    /// <summary>
    /// Adds API versioning middleware
    /// </summary>
    public static IApplicationBuilder UseApiVersioning(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiVersioningMiddleware>();
    }

<!--#if (enableMonitoring)-->
    /// <summary>
    /// Adds metrics collection middleware
    /// </summary>
    public static IApplicationBuilder UseMetrics(this IApplicationBuilder app)
    {
        // Add Prometheus metrics
        app.UseMetricServer();
        app.UseHttpMetrics();

        return app;
    }

    /// <summary>
    /// Adds health checks endpoints
    /// </summary>
    public static IApplicationBuilder UseHealthChecks(this IApplicationBuilder app)
    {
        app.UseHealthChecks("/health", new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new
                    {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        exception = x.Value.Exception?.Message,
                        duration = x.Value.Duration.ToString()
                    }),
                    duration = report.TotalDuration.ToString()
                };
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            }
        });

        app.UseHealthChecks("/health/ready", new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        app.UseHealthChecks("/health/live", new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => false
        });

        return app;
    }

    /// <summary>
    /// Maps health checks UI
    /// </summary>
    public static IApplicationBuilder MapHealthChecksUI(this IApplicationBuilder app)
    {
        if (app is WebApplication webApp)
        {
            webApp.MapHealthChecksUI(options =>
            {
                options.UIPath = "/health-ui";
                options.ApiPath = "/health-ui-api";
            });
        }

        return app;
    }
<!--#endif-->

    /// <summary>
    /// Adds correlation ID middleware
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ??
                               context.Request.Headers["X-Request-ID"].FirstOrDefault() ??
                               Guid.NewGuid().ToString();

            context.Response.Headers.Add("X-Correlation-ID", correlationId);
            context.Items["CorrelationId"] = correlationId;

            await next();
        });
    }

    /// <summary>
    /// Adds request timeout middleware
    /// </summary>
    public static IApplicationBuilder UseRequestTimeout(this IApplicationBuilder app, TimeSpan timeout)
    {
        return app.Use(async (context, next) =>
        {
            using var cts = new CancellationTokenSource(timeout);
            var originalToken = context.RequestAborted;
            var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(originalToken, cts.Token).Token;

            context.RequestAborted = combinedToken;

            try
            {
                await next();
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
                await context.Response.WriteAsync("Request timeout");
            }
        });
    }

    /// <summary>
    /// Adds request size limit middleware
    /// </summary>
    public static IApplicationBuilder UseRequestSizeLimit(this IApplicationBuilder app, long maxRequestSize)
    {
        return app.Use(async (context, next) =>
        {
            if (context.Request.ContentLength > maxRequestSize)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsync("Request entity too large");
                return;
            }

            await next();
        });
    }

    /// <summary>
    /// Adds custom error pages for different environments
    /// </summary>
    public static IApplicationBuilder UseCustomErrorPages(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        return app;
    }

    /// <summary>
    /// Adds API documentation middleware
    /// </summary>
    public static IApplicationBuilder UseApiDocumentation(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment() || env.IsStaging())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "EnterpriseApp API V1");
                c.RoutePrefix = "swagger";
                c.DisplayRequestDuration();
                c.EnableDeepLinking();
                c.EnableFilter();
                c.ShowExtensions();
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
            });
        }

        return app;
    }

    /// <summary>
    /// Adds request/response caching
    /// </summary>
    public static IApplicationBuilder UseApiCaching(this IApplicationBuilder app)
    {
        app.UseResponseCaching();

        // Add cache headers for static content
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value?.ToLower();
            
            if (IsStaticContent(path))
            {
                context.Response.Headers.Add("Cache-Control", "public, max-age=31536000"); // 1 year
            }
            else if (IsApiEndpoint(path))
            {
                context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                context.Response.Headers.Add("Pragma", "no-cache");
                context.Response.Headers.Add("Expires", "0");
            }

            await next();
        });

        return app;
    }

    /// <summary>
    /// Checks if the path is for static content
    /// </summary>
    private static bool IsStaticContent(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot" };
        return staticExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if the path is for an API endpoint
    /// </summary>
    private static bool IsApiEndpoint(string? path)
    {
        return path?.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Adds comprehensive middleware pipeline for production
    /// </summary>
    public static IApplicationBuilder UseProductionMiddleware(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Security
        app.UseSecurityHeaders();
        app.UseHsts();

        // Performance
        app.UseResponseCompression();
        app.UseApiCaching();

        // Monitoring
        app.UseCorrelationId();
        app.UseRequestResponseLogging();
        app.UsePerformanceMonitoring();

        // Rate limiting
        app.UseRateLimiting();

        // Request limits
        app.UseRequestTimeout(TimeSpan.FromMinutes(5));
        app.UseRequestSizeLimit(10 * 1024 * 1024); // 10MB

        return app;
    }

    /// <summary>
    /// Adds comprehensive middleware pipeline for development
    /// </summary>
    public static IApplicationBuilder UseDevelopmentMiddleware(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Development-specific middleware
        app.UseDeveloperExceptionPage();

        // CORS for development
        app.UseCors("DefaultPolicy");

        // Monitoring (less restrictive)
        app.UseCorrelationId();
        app.UseRequestResponseLogging();

        // API documentation
        app.UseApiDocumentation(env);

        return app;
    }
}
