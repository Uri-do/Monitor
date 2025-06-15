using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Infrastructure.Configuration;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Repositories;
using MonitoringGrid.Infrastructure.Services;
using MonitoringGrid.Infrastructure.Events;

namespace MonitoringGrid.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add Infrastructure services to the service collection
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add standardized configuration
        services.AddMonitoringGridConfiguration(configuration);

        // Add security-specific configuration for SecurityService
        services.Configure<MonitoringGrid.Core.Security.SecurityConfiguration>(configuration.GetSection("MonitoringGrid:Security"));
        services.Configure<MonitoringGrid.Core.Security.JwtSettings>(configuration.GetSection("MonitoringGrid:Security:Jwt"));
        services.Configure<MonitoringGrid.Core.Security.EncryptionSettings>(configuration.GetSection("MonitoringGrid:Security:Encryption"));

        // Validate configuration on startup
        configuration.ValidateConfiguration();

        // Enhanced Database Context with performance optimizations
        services.AddDbContext<MonitoringContext>(options =>
        {
            var connectionString = configuration.GetConnectionStringOrThrow("DefaultConnection");
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(MonitoringContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });

            // Performance optimizations
            options.EnableServiceProviderCaching();
            options.EnableSensitiveDataLogging(false); // Disable in production
        });

        // ProgressPlay Database Context for monitored database
        services.AddDbContext<ProgressPlayContext>(options =>
        {
            var connectionString = configuration.GetConnectionStringOrThrow("ProgressPlayConnection");
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });

            // Performance optimizations
            options.EnableServiceProviderCaching();
            options.EnableSensitiveDataLogging(false); // Disable in production
        });

        // Database Context Factory for concurrent operations - Fix scoping issue
        services.AddSingleton<IDbContextFactory<MonitoringContext>>(provider =>
        {
            var connectionString = configuration.GetConnectionStringOrThrow("DefaultConnection");
            var optionsBuilder = new DbContextOptionsBuilder<MonitoringContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new CustomDbContextFactory(optionsBuilder.Options);
        });

        // Caching Infrastructure
        services.AddMemoryCache();

        // Add distributed cache (in-memory for now, can be replaced with Redis in production)
        services.AddDistributedMemoryCache();

        services.AddSingleton<ICacheService, CacheService>();

        // Performance Monitoring
        services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();

        // Advanced Repository Pattern with enterprise features
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IAdvancedRepository<>), typeof(AdvancedRepository<>));
        services.AddScoped(typeof(IProjectionRepository<>), typeof(ProjectionRepository<>));
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAdvancedUnitOfWork, AdvancedUnitOfWork>();

        // Domain Events
        services.AddScoped<MonitoringGrid.Core.Interfaces.IDomainEventPublisher, DomainEventPublisher>();

        // Database Connection Management
        services.AddSingleton<IDatabaseConnectionManager, DatabaseConnectionManager>();

        // Core Services
        services.AddScoped<IIndicatorService, IndicatorService>();
        services.AddScoped<IIndicatorExecutionService, IndicatorExecutionService>();
        services.AddScoped<IMonitorStatisticsService, MonitorStatisticsService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddScoped<ISchedulerService, SchedulerService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IPerformanceMetricsService, PerformanceMetricsService>();
        services.AddScoped<IAlertService, AlertService>();

        // Notification Services - Optimized lifetimes
        services.AddSingleton<IEmailService, EmailService>(); // Singleton for better performance
        services.AddSingleton<ISmsService, SmsService>(); // Singleton for better performance
        services.AddScoped<INotificationService, NotificationService>();

        // Integration Services - Fixed lifetimes for database dependencies
        services.AddSingleton<ISlackService, SlackService>(); // Singleton - no database dependencies
        services.AddSingleton<ITeamsService, TeamsService>(); // Singleton - no database dependencies
        services.AddScoped<IWebhookService, WebhookService>(); // Scoped - depends on MonitoringContext

        // External Services
        services.AddScoped<IProgressPlayDbService, ProgressPlayDbService>();

        // Security Services - Fixed lifetimes for database dependencies
        services.AddScoped<ISecurityService, SecurityService>(); // Scoped - depends on MonitoringContext
        services.AddScoped<IRoleManagementService, RoleManagementService>();

        // Security Service Adapters for backward compatibility
        services.AddScoped<ISecurityAuditService>(provider =>
            new SecurityAuditServiceAdapter(provider.GetRequiredService<ISecurityService>()));
        services.AddScoped<IAuthenticationService>(provider =>
            new AuthenticationServiceAdapter(provider.GetRequiredService<ISecurityService>()));

        services.AddScoped<IThreatDetectionService>(provider =>
            new ThreatDetectionServiceAdapter(provider.GetRequiredService<ISecurityService>()));


        return services;
    }

    /// <summary>
    /// Add enterprise database services with advanced features
    /// </summary>
    public static IServiceCollection AddEnterpriseDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Connection pooling and management
        services.Configure<DatabaseConnectionConfig>(configuration.GetSection("Database"));

        // Health checks for all database connections
        // TODO: Add health checks package reference and uncomment
        // services.AddHealthChecks()
        //     .AddDbContextCheck<MonitoringContext>("MonitoringGrid Database")
        //     .AddSqlServer(
        //         configuration.GetConnectionString("ProgressPlayConnection")!,
        //         name: "ProgressPlay Database")
        //     .AddSqlServer(
        //         configuration.GetConnectionString("PopAIConnection")!,
        //         name: "PopAI Database");

        return services;
    }

    /// <summary>
    /// Add performance monitoring services
    /// </summary>
    public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services)
    {
        services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();

        // Add performance counters and metrics
        services.AddSingleton<IPerformanceMetricsCollector, PerformanceMetricsCollector>();

        return services;
    }
}
