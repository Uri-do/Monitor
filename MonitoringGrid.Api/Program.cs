using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Options;
using Quartz;
using MonitoringGrid.Api.Mapping;
using MonitoringGrid.Api.Hubs;
using MonitoringGrid.Api.Middleware;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Api.HealthChecks;
using MonitoringGrid.Api.Observability;
using MonitoringGrid.Api.Authentication;
using MonitoringGrid.Api.BackgroundServices;
using MonitoringGrid.Api.Validators;
using MonitoringGrid.Core.EventSourcing;
using MonitoringGrid.Core.Events;
using FluentValidation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Models;
using MonitoringGrid.Core.Services;
using MonitoringGrid.Core.Security;
using MonitoringGrid.Core.Factories;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Repositories;
using MonitoringGrid.Infrastructure.Services;
using MonitoringGrid.Api.Services;
using Serilog;
using Serilog.Events;
using System.Text.Json.Serialization;
using System.Text;
using FluentValidation.AspNetCore;
using MonitoringGrid.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog - simplified for testing
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/monitoring-api-.log")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers(options =>
    {
        // Add performance monitoring filter globally
        options.Filters.Add<PerformanceMonitoringFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("version"),
        new HeaderApiVersionReader("X-Version"),
        new MediaTypeApiVersionReader("ver")
    );
})
.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// Add configuration sections
builder.Services.Configure<MonitoringConfiguration>(
    builder.Configuration.GetSection("Monitoring"));
builder.Services.Configure<EmailConfiguration>(
    builder.Configuration.GetSection("Email"));
builder.Services.Configure<SecurityConfiguration>(
    builder.Configuration.GetSection("Security"));

// Get connection string - use MonitoringGrid connection for the API
var connectionString = builder.Configuration.GetConnectionString("MonitoringGrid");

// Add Entity Framework - Use real database now that VPN is enabled
builder.Services.AddDbContext<MonitoringContext>(options =>
{
    options.UseSqlServer(connectionString);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Database seeding not needed for real database
// builder.Services.AddScoped<IDbSeeder, DbSeeder>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile), typeof(AuthMappingProfile));

// Add MediatR for CQRS and Domain Events
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add domain services
builder.Services.AddScoped<KpiDomainService>();

// Add factories
builder.Services.AddScoped<KpiFactory>();

// Add domain event publisher (MediatR-based)
builder.Services.AddScoped<IDomainEventPublisher, MonitoringGrid.Api.Events.MediatRDomainEventPublisher>();

// Add domain event integration service
builder.Services.AddScoped<MonitoringGrid.Api.Events.DomainEventIntegrationService>();



// Add repository services
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IProjectionRepository<>), typeof(ProjectionRepository<>));
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Add application services
builder.Services.AddScoped<IKpiExecutionService, KpiExecutionService>(); // Keep original for now
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();

// Add performance optimization services
builder.Services.AddSingleton<IPerformanceMetricsService, PerformanceMetricsService>();
builder.Services.AddSingleton<ICacheInvalidationService, CacheInvalidationService>();
builder.Services.AddSingleton<MonitoringGrid.Api.Middleware.IRateLimitingService, MonitoringGrid.Api.Middleware.RateLimitingService>();

// Configure response caching options
builder.Services.AddSingleton(new ResponseCachingOptions
{
    Enabled = true,
    DefaultDuration = TimeSpan.FromMinutes(5),
    MaxCacheSize = 100,
    EnableCompression = true
});

// Configure rate limiting options
builder.Services.AddSingleton(new RateLimitingOptions
{
    Rules = new List<RateLimitRule>
    {
        new() { Name = "Default", Limit = 1000, Window = TimeSpan.FromHours(1), IsDefault = true },
        new() { Name = "API_Authenticated", Limit = 5000, Window = TimeSpan.FromHours(1), UserRoles = new[] { "User", "Admin" } },
        new() { Name = "API_Admin", Limit = 10000, Window = TimeSpan.FromHours(1), UserRoles = new[] { "Admin" } },
        new() { Name = "KPI_Execute", Limit = 100, Window = TimeSpan.FromMinutes(10), PathPattern = "/execute" },
        new() { Name = "Bulk_Operations", Limit = 10, Window = TimeSpan.FromMinutes(10), PathPattern = "/bulk" }
    },
    EnableLogging = true,
    CleanupInterval = TimeSpan.FromMinutes(5)
});
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IRealtimeNotificationService, RealtimeNotificationService>();

// Add Quartz.NET scheduling services (temporarily disabled for compilation)
/*
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjection();
    q.UseSimpleTypeLoader();
    q.UseInMemoryStore();
    q.UseDefaultThreadPool(tp =>
    {
        tp.MaxConcurrency = 10;
    });
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
*/

// Add KPI scheduling service (will add after fixing interface issues)
// builder.Services.AddScoped<IKpiSchedulingService, KpiSchedulingService>();

// Add Quartz job and listener (will add after creating them)
// builder.Services.AddScoped<KpiExecutionJob>();
// builder.Services.AddScoped<KpiJobListener>();

// Add authentication services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();

// Add unified security service (replaces 6 separate services)
builder.Services.AddScoped<ISecurityService, SecurityService>();

// Maintain backward compatibility with adapters for existing controllers
builder.Services.AddScoped<IAuthenticationService>(provider =>
    new AuthenticationServiceAdapter(provider.GetRequiredService<ISecurityService>()));
builder.Services.AddScoped<IJwtTokenService>(provider =>
    new JwtTokenServiceAdapter(provider.GetRequiredService<ISecurityService>()));
builder.Services.AddScoped<IEncryptionService>(provider =>
    new EncryptionServiceAdapter(provider.GetRequiredService<ISecurityService>()));
builder.Services.AddScoped<ISecurityAuditService>(provider =>
    new SecurityAuditServiceAdapter(provider.GetRequiredService<ISecurityService>()));
builder.Services.AddScoped<IThreatDetectionService>(provider =>
    new ThreatDetectionServiceAdapter(provider.GetRequiredService<ISecurityService>()));
builder.Services.AddScoped<ITwoFactorService>(provider =>
    new TwoFactorServiceAdapter(provider.GetRequiredService<ISecurityService>()));

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:4173",
                "http://localhost:8080") // React dev servers
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Single API version for now to avoid conflicts
    c.SwaggerDoc("v1", new()
    {
        Title = "Monitoring Grid API",
        Version = "v1.0",
        Description = "API for managing KPI monitoring and alerting system with enhanced performance and reliability",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "MonitoringGrid Support",
            Email = "support@monitoringgrid.com",
            Url = new Uri("https://github.com/monitoringgrid/support")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add examples and enhanced documentation
    c.EnableAnnotations();
    c.DescribeAllParametersInCamelCase();
    c.UseInlineDefinitionsForEnums();

    // Add custom schema and operation filters for examples
    c.SchemaFilter<MonitoringGrid.Api.Documentation.SwaggerExampleSchemaFilter>();
    c.OperationFilter<MonitoringGrid.Api.Documentation.SwaggerExampleOperationFilter>();

    // Add custom operation filters for better documentation
    c.OperationFilter<MonitoringGrid.Api.Documentation.ApiVersionOperationFilter>();
    c.DocumentFilter<MonitoringGrid.Api.Documentation.ApiDocumentationFilter>();
});

// Add JWT Authentication
var securityConfig = builder.Configuration.GetSection("Security").Get<SecurityConfiguration>();
if (securityConfig?.Jwt != null)
{
    var key = Encoding.UTF8.GetBytes(securityConfig.Jwt.SecretKey);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = securityConfig.Jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = securityConfig.Jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RequireExpirationTime = true
        };

        // Add SignalR support
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/monitoring-hub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationOptions.DefaultScheme,
        options => { });
}

// Add Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireUserReadPermission", policy =>
        policy.RequireClaim("permission", "User:Read"));
    options.AddPolicy("RequireUserWritePermission", policy =>
        policy.RequireClaim("permission", "User:Write"));
    options.AddPolicy("RequireUserDeletePermission", policy =>
        policy.RequireClaim("permission", "User:Delete"));
    options.AddPolicy("RequireRoleReadPermission", policy =>
        policy.RequireClaim("permission", "Role:Read"));
    options.AddPolicy("RequireRoleWritePermission", policy =>
        policy.RequireClaim("permission", "Role:Write"));
    options.AddPolicy("RequireSystemAdminPermission", policy =>
        policy.RequireClaim("permission", "System:Admin"));
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MonitoringContext>("database")
    .AddCheck("api", () => HealthCheckResult.Healthy("API is running"))
    .AddCheck<KpiHealthCheck>("kpi-system")
    .AddCheck<DatabasePerformanceHealthCheck>("database-performance")
    .AddCheck<ExternalServicesHealthCheck>("external-services");

// Register health check services
builder.Services.AddScoped<KpiHealthCheck>();
builder.Services.AddScoped<DatabasePerformanceHealthCheck>();
builder.Services.AddScoped<ExternalServicesHealthCheck>();

// Add HTTP client factory for health checks
builder.Services.AddHttpClient();

// Add response caching
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 1024 * 1024; // 1MB
    options.UseCaseSensitivePaths = false;
});

// Add memory cache for custom caching scenarios
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 100 * 1024 * 1024; // 100MB
});

// Add rate limiting configuration
builder.Services.Configure<RateLimitOptions>(options =>
{
    // Configure different limits for different endpoint categories
    options.GeneralLimit = new RateLimit { RequestsPerWindow = 100, WindowSizeMinutes = 1 };
    options.AuthenticationLimit = new RateLimit { RequestsPerWindow = 10, WindowSizeMinutes = 1 };
    options.KpiExecutionLimit = new RateLimit { RequestsPerWindow = 20, WindowSizeMinutes = 1 };
    options.KpiManagementLimit = new RateLimit { RequestsPerWindow = 50, WindowSizeMinutes = 1 };
    options.AlertManagementLimit = new RateLimit { RequestsPerWindow = 30, WindowSizeMinutes = 1 };
    options.AnalyticsLimit = new RateLimit { RequestsPerWindow = 25, WindowSizeMinutes = 1 };
});

builder.Services.AddSingleton(provider =>
    provider.GetRequiredService<IOptions<RateLimitOptions>>().Value);

// Add observability services
builder.Services.AddSingleton<MetricsService>();

// Add event sourcing services
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();
builder.Services.AddScoped<IEventSourcingService, EventSourcingService>();
builder.Services.AddScoped<MonitoringGrid.Core.Events.IDomainEventPublisher, MonitoringGrid.Infrastructure.Events.DomainEventPublisher>();

// Add API key authentication services
builder.Services.AddSingleton<MonitoringGrid.Api.Authentication.IApiKeyService, InMemoryApiKeyService>();

// Add background services based on configuration
var enableWorkerServices = builder.Configuration.GetValue<bool>("Monitoring:EnableWorkerServices", false);
if (enableWorkerServices)
{
    // Add Worker services directly in API (for single-process deployment)
    builder.Services.AddScoped<MonitoringGrid.Core.Interfaces.IKpiService, MonitoringGrid.Infrastructure.Services.KpiService>();

    // Add Worker configuration
    builder.Services.Configure<MonitoringGrid.Worker.Configuration.WorkerConfiguration>(
        builder.Configuration.GetSection("Worker"));

    // Add Worker services
    builder.Services.AddHostedService<MonitoringGrid.Worker.Services.KpiMonitoringWorker>();
    builder.Services.AddHostedService<MonitoringGrid.Worker.Services.ScheduledTaskWorker>();
    builder.Services.AddHostedService<MonitoringGrid.Worker.Services.HealthCheckWorker>();
    builder.Services.AddHostedService<MonitoringGrid.Worker.Services.AlertProcessingWorker>();
    builder.Services.AddHostedService<MonitoringGrid.Worker.Worker>();

    // Add Quartz for Worker services
    builder.Services.AddQuartz(q =>
    {
        q.UseSimpleTypeLoader();
        q.UseInMemoryStore();
        q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 10);
    });
    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

    // Add Worker health checks
    builder.Services.AddHealthChecks()
        .AddCheck<MonitoringGrid.Worker.Services.KpiExecutionHealthCheck>("worker-kpi-execution")
        .AddCheck<MonitoringGrid.Worker.Services.AlertProcessingHealthCheck>("worker-alert-processing");
}
else
{
    // Use legacy enhanced scheduler (deprecated - use Worker service instead)
    // builder.Services.AddHostedService<EnhancedKpiSchedulerService>();
}

// Add real-time update service (always enabled for SignalR updates)
builder.Services.AddHostedService<RealtimeUpdateService>();

// Add graceful shutdown service for proper process cleanup
builder.Services.AddHostedService<MonitoringGrid.Api.Services.GracefulShutdownService>();

// Configure security headers
builder.Services.ConfigureSecurityHeaders(builder.Configuration);

// Add bulk operations service
builder.Services.AddScoped<MonitoringGrid.Api.Services.IBulkOperationsService, MonitoringGrid.Api.Services.BulkOperationsService>();

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("MonitoringGrid.Api", "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName,
                    ["service.instance.id"] = Environment.MachineName
                }))
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    activity.SetTag("http.request.body.size", request.ContentLength);
                    activity.SetTag("http.request.client_ip", request.HttpContext.Connection.RemoteIpAddress?.ToString());
                };
                options.EnrichWithHttpResponse = (activity, response) =>
                {
                    activity.SetTag("http.response.body.size", response.ContentLength);
                };
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddSqlClientInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.RecordException = true;
            })
            .AddSource(KpiActivitySource.Source.Name)
            .AddSource(ApiActivitySource.Source.Name);
    });

var app = builder.Build();

// Database seeding not needed for real database
// using (var scope = app.Services.CreateScope())
// {
//     var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder>();
//     await seeder.SeedAsync();
// }

// Configure the HTTP request pipeline

// Add security headers early in the pipeline
if (app.Environment.IsProduction())
{
    app.UseSecurityHeaders();
}
else
{
    app.UseSecurityHeaders(options =>
    {
        var devOptions = SecurityHeadersProfiles.Development;
        options.ContentSecurityPolicy = devOptions.ContentSecurityPolicy;
        options.XFrameOptions = devOptions.XFrameOptions;
        options.EnableHSTS = devOptions.EnableHSTS;
        options.LogSecurityViolations = devOptions.LogSecurityViolations;
        options.LogSuspiciousRequests = devOptions.LogSuspiciousRequests;
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Monitoring Grid API v1.0");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
        c.DefaultModelsExpandDepth(-1); // Hide models section by default
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // Collapse all operations by default
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.EnableValidator();
        c.SupportedSubmitMethods(Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Get,
                                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Post,
                                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Put,
                                Swashbuckle.AspNetCore.SwaggerUI.SubmitMethod.Delete);
    });
}

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowReactApp");

// Add performance optimization middleware
app.UseMiddleware<ResponseCachingMiddleware>();

// Add advanced rate limiting middleware
app.UseMiddleware<AdvancedRateLimitingMiddleware>();

// Add response caching middleware
app.UseResponseCaching();

// Add rate limiting middleware (fallback)
app.UseMiddleware<RateLimitingMiddleware>();

// Add global exception handling middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<MonitoringHub>("/monitoring-hub");

// Configure worker cleanup on application shutdown
app.ConfigureWorkerCleanup();

// Map Prometheus metrics endpoint
app.MapMetrics();

// Add health check endpoint
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
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
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

// Add API info endpoint
app.MapGet("/api/info", () => new
{
    Name = "Monitoring Grid API",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow
});

// Temporarily disable database connection check for testing
/*
// Ensure database connections are accessible
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MonitoringContext>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    Log.Information("Checking database connections...");

    // Test MonitoringGrid database connection
    Log.Information("Testing MonitoringGrid database connection...");
    await context.Database.CanConnectAsync();
    Log.Information("✅ MonitoringGrid database connection successful");

    // Test MainDatabase connection (optional)
    var mainConnectionString = configuration.GetConnectionString("MainDatabase");
    if (!string.IsNullOrEmpty(mainConnectionString))
    {
        try
        {
            Log.Information("Testing MainDatabase connection...");
            using var mainConnection = new Microsoft.Data.SqlClient.SqlConnection(mainConnectionString);
            await mainConnection.OpenAsync();
            Log.Information("✅ MainDatabase connection successful");
            await mainConnection.CloseAsync();
        }
        catch (Exception ex)
        {
            Log.Warning("⚠️ MainDatabase connection failed (continuing anyway): {Message}", ex.Message);
        }
    }
    else
    {
        Log.Warning("⚠️ MainDatabase connection string not configured");
    }

    Log.Information("All database connections verified successfully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Failed to connect to database: {Message}", ex.Message);
    return 1;
}
*/

Log.Information("Monitoring Grid API starting...");
Log.Information("Swagger UI available at: {BaseUrl}", app.Environment.IsDevelopment() ? "https://localhost:7000" : "API base URL");

try
{
    await app.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "API terminated unexpectedly: {Message}", ex.Message);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for testing
public partial class Program { }
