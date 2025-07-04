using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Core.Interfaces.Security;
using MonitoringGrid.Core.Events;
using MonitoringGrid.Infrastructure.Configuration;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure.Repositories;
using MonitoringGrid.Infrastructure.Services;
using MonitoringGrid.Infrastructure.Events;
using MonitoringGrid.Core.Models;

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
        services.Configure<MonitoringGrid.Core.Security.SecurityConfiguration>(configuration.GetSection("Security"));
        services.Configure<MonitoringGrid.Core.Security.JwtSettings>(configuration.GetSection("Security:Jwt"));
        services.Configure<MonitoringGrid.Core.Security.EncryptionSettings>(configuration.GetSection("Security:Encryption"));

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
            var connectionString = configuration.GetConnectionStringOrThrow("SourceDatabase");
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

        // Performance Monitoring - Removed redundant services, keeping only what's needed

        // Repository Pattern with enterprise features
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

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
        services.AddScoped<IAlertService, AlertService>();

        // Notification Services - Optimized lifetimes
        services.AddSingleton<IEmailService, EmailService>(); // Singleton for better performance
        services.AddSingleton<ISmsService, SmsService>(); // Singleton for better performance
        services.AddScoped<INotificationService, NotificationService>();

        // Integration Services - Removed enterprise services (Slack, Teams, Webhooks)

        // External Services
        services.AddScoped<IProgressPlayDbService, ProgressPlayDbService>();

        // Security Services - Consolidated without adapters
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();
        // Removed IKeyVaultService - enterprise feature not needed

        // Direct service registrations (no adapters needed)
        services.AddScoped<IAuthenticationService, SecurityService>();
        services.AddScoped<ISecurityAuditService, SecurityService>();
        services.AddScoped<IThreatDetectionService, SecurityService>();


        return services;
    }




}
