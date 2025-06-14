using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Configuration;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Repositories;
using MonitoringGrid.Infrastructure.Services;

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

        // Validate configuration on startup
        configuration.ValidateConfiguration();

        // Database Context
        services.AddDbContext<MonitoringContext>(options =>
        {
            var connectionString = configuration.GetConnectionStringOrThrow("DefaultConnection");
            options.UseSqlServer(connectionString, b =>
            {
                b.MigrationsAssembly(typeof(MonitoringContext).Assembly.FullName);
                b.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        });

        // Caching Infrastructure
        services.AddMemoryCache();

        // Add distributed cache (in-memory for now, can be replaced with Redis in production)
        services.AddDistributedMemoryCache();

        services.AddSingleton<ICacheService, CacheService>();

        // Performance Monitoring
        services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();

        // Repository Pattern
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(typeof(IProjectionRepository<>), typeof(ProjectionRepository<>));
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Core Services
        services.AddScoped<IIndicatorService, IndicatorService>();
        services.AddScoped<IIndicatorExecutionService, IndicatorExecutionService>();
        services.AddScoped<IMonitorStatisticsService, MonitorStatisticsService>();
        services.AddScoped<IConfigurationService, ConfigurationService>();

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

        return services;
    }

}
