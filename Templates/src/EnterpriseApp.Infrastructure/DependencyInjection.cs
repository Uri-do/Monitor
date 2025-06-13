using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EnterpriseApp.Core.Interfaces;
using EnterpriseApp.Infrastructure.Data;
using EnterpriseApp.Infrastructure.Repositories;
using EnterpriseApp.Infrastructure.Services;
<!--#if (enableAuth)-->
using EnterpriseApp.Infrastructure.Security;
<!--#endif-->

namespace EnterpriseApp.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Infrastructure services to the service collection
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add database context
        services.AddDbContext(configuration);

        // Add repositories
        services.AddRepositories();

        // Add services
        services.AddDomainServices();

<!--#if (enableAuth)-->
        // Add authentication services
        services.AddAuthenticationServices(configuration);
<!--#endif-->

        // Add caching
        services.AddCaching(configuration);

        // Add background services
        services.AddBackgroundServices();

        return services;
    }

    /// <summary>
    /// Adds database context configuration
    /// </summary>
    private static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string is not configured");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                
                sqlOptions.CommandTimeout(30);
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });

            // Enable sensitive data logging in development
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            // Add query logging
            options.LogTo(Console.WriteLine, LogLevel.Information);
        });

        return services;
    }

    /// <summary>
    /// Adds repository services
    /// </summary>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register generic repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Register specific repositories
        services.AddScoped<IDomainEntityRepository, DomainEntityRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        return services;
    }

    /// <summary>
    /// Adds domain services
    /// </summary>
    private static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Register domain services
        services.AddScoped<IDomainEntityService, DomainEntityService>();
        services.AddScoped<IAuditService, AuditService>();

        // Register utility services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFileStorageService, FileStorageService>();

        return services;
    }

<!--#if (enableAuth)-->
    /// <summary>
    /// Adds authentication and authorization services
    /// </summary>
    private static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register authentication services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ISecurityEventService, SecurityEventService>();

        // Register JWT services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Configure JWT settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        return services;
    }
<!--#endif-->

    /// <summary>
    /// Adds caching services
    /// </summary>
    private static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        // Add memory cache
        services.AddMemoryCache();

        // Add Redis cache if configured
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "EnterpriseApp";
            });

            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddScoped<ICacheService, MemoryCacheService>();
        }

        return services;
    }

    /// <summary>
    /// Adds background services
    /// </summary>
    private static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        // Register background job service
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();

        // Add hosted services
        services.AddHostedService<AuditCleanupService>();
        services.AddHostedService<CacheWarmupService>();

        return services;
    }

    /// <summary>
    /// Adds health checks
    /// </summary>
    public static IServiceCollection AddInfrastructureHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database")
            .AddCheck<CacheHealthCheck>("cache")
            .AddCheck<FileStorageHealthCheck>("file-storage");

        // Add Redis health check if configured
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddHealthChecks()
                .AddRedis(redisConnectionString, "redis");
        }

        return services;
    }

    /// <summary>
    /// Configures AutoMapper
    /// </summary>
    public static IServiceCollection AddAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        return services;
    }

    /// <summary>
    /// Adds monitoring and observability
    /// </summary>
    public static IServiceCollection AddMonitoring(this IServiceCollection services)
    {
<!--#if (enableMonitoring)-->
        // Add Prometheus metrics
        services.AddSingleton<IMetricsService, PrometheusMetricsService>();
        
        // Add custom metrics collectors
        services.AddSingleton<DatabaseMetricsCollector>();
        services.AddSingleton<ApplicationMetricsCollector>();
<!--#endif-->

        return services;
    }

    /// <summary>
    /// Ensures database is created and migrated
    /// </summary>
    public static async Task EnsureDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Ensuring database exists and is up to date...");
            
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            // Apply any pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations", pendingMigrations.Count());
                await context.Database.MigrateAsync();
            }

            logger.LogInformation("Database is ready");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure database");
            throw;
        }
    }

    /// <summary>
    /// Seeds initial data
    /// </summary>
    public static async Task SeedDataAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            logger.LogInformation("Seeding initial data...");
            
            // Seed data is handled in the DbContext OnModelCreating method
            // Additional seeding can be done here if needed
            
            await context.SaveChangesAsync();
            logger.LogInformation("Initial data seeded successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed initial data");
            throw;
        }
    }
}

/// <summary>
/// Extension methods for service configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a scoped service with interface and implementation
    /// </summary>
    public static IServiceCollection AddScopedService<TInterface, TImplementation>(this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return services.AddScoped<TInterface, TImplementation>();
    }

    /// <summary>
    /// Adds a singleton service with interface and implementation
    /// </summary>
    public static IServiceCollection AddSingletonService<TInterface, TImplementation>(this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return services.AddSingleton<TInterface, TImplementation>();
    }

    /// <summary>
    /// Adds a transient service with interface and implementation
    /// </summary>
    public static IServiceCollection AddTransientService<TInterface, TImplementation>(this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return services.AddTransient<TInterface, TImplementation>();
    }
}

/// <summary>
/// Configuration validation
/// </summary>
public static class ConfigurationValidation
{
    /// <summary>
    /// Validates required configuration settings
    /// </summary>
    public static void ValidateConfiguration(IConfiguration configuration)
    {
        var requiredSettings = new[]
        {
            "ConnectionStrings:DefaultConnection"
        };

        foreach (var setting in requiredSettings)
        {
            var value = configuration[setting];
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"Required configuration setting '{setting}' is missing or empty");
            }
        }

<!--#if (enableAuth)-->
        // Validate JWT settings
        var jwtSettings = configuration.GetSection("JwtSettings");
        if (string.IsNullOrEmpty(jwtSettings["SecretKey"]))
        {
            throw new InvalidOperationException("JWT SecretKey is required");
        }

        if (string.IsNullOrEmpty(jwtSettings["Issuer"]))
        {
            throw new InvalidOperationException("JWT Issuer is required");
        }

        if (string.IsNullOrEmpty(jwtSettings["Audience"]))
        {
            throw new InvalidOperationException("JWT Audience is required");
        }
<!--#endif-->
    }
}
