using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MonitoringGrid.Core.Interfaces;
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
        // Database Context
        services.AddDbContext<MonitoringContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionString, b =>
            {
                b.MigrationsAssembly(typeof(MonitoringContext).Assembly.FullName);
                b.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });
        });

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

        // Notification Services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Integration Services
        services.AddScoped<ISlackService, SlackService>();
        services.AddScoped<ITeamsService, TeamsService>();
        services.AddScoped<IWebhookService, WebhookService>();

        // External Services
        services.AddScoped<IProgressPlayDbService, ProgressPlayDbService>();

        // Security Services
        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IRoleManagementService, RoleManagementService>();

        return services;
    }

}
