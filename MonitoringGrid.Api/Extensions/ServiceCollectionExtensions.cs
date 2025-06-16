using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using FluentValidation;
using MediatR;
using AutoMapper;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Repositories;
using MonitoringGrid.Api.Services;
using MonitoringGrid.Api.Middleware;
using MonitoringGrid.Infrastructure.Services;
using MonitoringGrid.Api.CQRS.Behaviors;
using MonitoringGrid.Api.Filters;
using MonitoringGrid.Api.Configuration;
using MonitoringGrid.Api.Versioning;
using MonitoringGrid.Api.Monitoring;
using MonitoringGrid.Api.Security;
using MonitoringGrid.Api.Caching;
using MonitoringGrid.Api.Events;
using Quartz;

namespace MonitoringGrid.Api.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to organize service registration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add database services
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Note: Database contexts are now handled by Infrastructure layer
        // via services.AddInfrastructure(configuration) which registers:
        // - MonitoringContext (main database)
        // - ProgressPlayContext (monitored database)
        // No need to register them here as they're already configured in Infrastructure

        return services;
    }

    /// <summary>
    /// Add authentication and authorization services
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("MonitoringGrid:Security:Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
        });

        return services;
    }

    /// <summary>
    /// Add MediatR and CQRS services
    /// </summary>
    public static IServiceCollection AddMediatRServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        });

        // Domain event publisher
        services.AddScoped<MonitoringGrid.Core.Interfaces.IDomainEventPublisher, MediatRDomainEventPublisher>();

        return services;
    }

    /// <summary>
    /// Add validation services
    /// </summary>
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        // Note: ValidationMiddleware is not registered as a service - it's added to the pipeline directly

        return services;
    }

    /// <summary>
    /// Add AutoMapper services
    /// </summary>
    public static IServiceCollection AddMappingServices(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        return services;
    }

    /// <summary>
    /// Add Swagger/OpenAPI services
    /// </summary>
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MonitoringGrid API",
                Version = "v1",
                Description = "Enterprise monitoring and indicator management API",
                Contact = new OpenApiContact
                {
                    Name = "MonitoringGrid Team",
                    Email = "support@monitoringgrid.com"
                }
            });

            // JWT Authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // Custom operation filters
            c.OperationFilter<PerformanceMonitorOperationFilter>();
        });

        return services;
    }

    /// <summary>
    /// Add application services
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Core services
        services.AddScoped<IIndicatorService, IndicatorService>();
        services.AddScoped<ISchedulerService, SchedulerService>();
        // services.AddScoped<IExecutionHistoryService, ExecutionHistoryService>(); // TODO: Implement ExecutionHistoryService
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IUserService, UserService>();

        // Missing core services
        services.AddCorrelationId(); // Adds ICorrelationIdService and HttpContextAccessor
        services.AddScoped<MonitoringGrid.Api.Security.ISecurityEventService, SecurityEventService>();

        // SignalR and real-time services
        services.AddSignalR();
        services.AddScoped<MonitoringGrid.Api.Hubs.IRealtimeNotificationService, MonitoringGrid.Api.Hubs.RealtimeNotificationService>();

        // HTTP Client for external services
        services.AddHttpClient();

        // Response compression services
        services.AddResponseCompression();

        // Response caching services
        services.Configure<MonitoringGrid.Api.Middleware.ResponseCachingOptions>(configuration.GetSection("ResponseCaching"));
        services.AddSingleton<MonitoringGrid.Api.Middleware.ResponseCachingOptions>(provider =>
        {
            var options = new MonitoringGrid.Api.Middleware.ResponseCachingOptions();
            configuration.GetSection("ResponseCaching").Bind(options);
            return options;
        });
        services.AddScoped<MonitoringGrid.Api.Middleware.ICacheInvalidationService, MonitoringGrid.Api.Middleware.CacheInvalidationService>();

        // Advanced services
        services.AddScoped<AdvancedCachingService>();
        services.AddScoped<AdvancedRateLimitingService>();
        services.AddScoped<DatabaseOptimizationService>();
        services.AddScoped<ResponseOptimizationService>();
        services.AddScoped<SecurityEventService>();
        services.AddScoped<IDataManagementService, SimpleDataManagementService>();
        services.AddScoped<ILifecycleManagementService, LifecycleManagementService>();
        services.AddScoped<IApiDocumentationService, ApiDocumentationService>();

        // Round 3 Enterprise Services
        services.AddSingleton<IAdvancedPerformanceMonitoring, AdvancedPerformanceMonitoring>();
        services.AddScoped<IAdvancedSecurityService, AdvancedSecurityService>();
        services.AddScoped<IIntelligentCachingService, IntelligentCachingService>();

        // API Key Service
        services.AddScoped<MonitoringGrid.Api.Authentication.IApiKeyService, MonitoringGrid.Api.Authentication.InMemoryApiKeyService>();

        // Performance monitoring (using Core interface and Infrastructure implementation)
        services.AddSingleton<MonitoringGrid.Core.Interfaces.IPerformanceMetricsService, MonitoringGrid.Infrastructure.Services.PerformanceMetricsService>();

        // Add Worker services if integrated mode is enabled
        var enableWorkerServices = configuration.GetValue<bool>("MonitoringGrid:Monitoring:EnableWorkerServices", false);
        if (enableWorkerServices)
        {
            services.AddWorkerServices(configuration);
        }

        return services;
    }

    /// <summary>
    /// Add caching services
    /// </summary>
    public static IServiceCollection AddCachingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        
        // Redis cache if configured
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
            });
        }

        return services;
    }

    /// <summary>
    /// Add CORS services
    /// </summary>
    public static IServiceCollection AddCorsServices(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            options.AddPolicy("DevelopmentPolicy", builder =>
            {
                builder
                    .WithOrigins("http://localhost:3000", "https://localhost:3000",
                               "http://localhost:5173", "https://localhost:5173",
                               "http://localhost:5174", "https://localhost:5174")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Add health check services
    /// </summary>
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("database", () => HealthCheckResult.Healthy("Database is healthy"))
            .AddCheck("progressplay", () => HealthCheckResult.Healthy("ProgressPlay database is healthy"))
            .AddCheck("popai", () => HealthCheckResult.Healthy("PopAI database is healthy"));

        return services;
    }

    /// <summary>
    /// Add rate limiting services
    /// </summary>
    public static IServiceCollection AddRateLimitingServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitingOptions>(configuration.GetSection("RateLimiting"));
        // Note: RateLimitingMiddleware is not registered as a service - it's added to the pipeline directly

        return services;
    }

    /// <summary>
    /// Adds enterprise-grade API versioning
    /// </summary>
    public static IServiceCollection AddEnterpriseApiVersioning(this IServiceCollection services)
    {
        return services.AddAdvancedApiVersioning();
    }

    /// <summary>
    /// Add Worker services for integrated mode
    /// </summary>
    public static IServiceCollection AddWorkerServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Quartz.NET for scheduling
        services.AddQuartz(q =>
        {
            q.UseSimpleTypeLoader();
            q.UseInMemoryStore();
            q.UseDefaultThreadPool(tp =>
            {
                tp.MaxConcurrency = 10;
            });
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        // Add Worker hosted services
        services.AddHostedService<MonitoringGrid.Worker.Services.IndicatorMonitoringWorker>();
        services.AddHostedService<MonitoringGrid.Worker.Services.ScheduledTaskWorker>();
        services.AddHostedService<MonitoringGrid.Worker.Services.HealthCheckWorker>();

        return services;
    }
}
