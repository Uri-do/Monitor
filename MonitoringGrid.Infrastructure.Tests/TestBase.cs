using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MonitoringGrid.Core.Interfaces;
using MonitoringGrid.Infrastructure.Data;
using MonitoringGrid.Infrastructure;

namespace MonitoringGrid.Infrastructure.Tests;

/// <summary>
/// Base class for integration tests with common setup
/// </summary>
public abstract class TestBase : IDisposable
{
    protected IServiceProvider ServiceProvider { get; private set; }
    protected MonitoringContext Context { get; private set; }
    protected IConfiguration Configuration { get; private set; }

    protected TestBase()
    {
        var services = new ServiceCollection();
        
        // Configuration
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.test.json")
            .Build();

        // Add logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Add in-memory database with transaction warning suppression
        services.AddDbContext<MonitoringContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                   .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));

        // Add Infrastructure services manually for testing
        services.AddScoped(typeof(IRepository<>), typeof(MonitoringGrid.Infrastructure.Repositories.Repository<>));
        services.AddScoped<MonitoringGrid.Core.Interfaces.IUnitOfWork, MonitoringGrid.Infrastructure.Repositories.UnitOfWork>();
        services.AddScoped<MonitoringGrid.Core.Interfaces.ICacheService, MonitoringGrid.Infrastructure.Services.CacheService>();
        services.AddScoped<MonitoringGrid.Core.Interfaces.IDomainEventPublisher, MonitoringGrid.Infrastructure.Events.DomainEventPublisher>();

        // Add caching services for testing
        services.AddMemoryCache();
        services.AddDistributedMemoryCache(); // In-memory distributed cache for testing

        ServiceProvider = services.BuildServiceProvider();
        Context = ServiceProvider.GetRequiredService<MonitoringContext>();

        // Ensure database is created
        Context.Database.EnsureCreated();
    }

    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    protected T? GetOptionalService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }

    public virtual void Dispose()
    {
        // Don't dispose Context here - let DI container manage it
        // This prevents "Cannot access a disposed context" errors
        if (ServiceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
