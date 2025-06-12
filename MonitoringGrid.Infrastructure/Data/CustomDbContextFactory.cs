using Microsoft.EntityFrameworkCore;

namespace MonitoringGrid.Infrastructure.Data;

/// <summary>
/// Custom DbContext factory to resolve singleton/scoped service conflicts
/// Provides thread-safe creation of MonitoringContext instances
/// </summary>
public class CustomDbContextFactory : IDbContextFactory<MonitoringContext>
{
    private readonly DbContextOptions<MonitoringContext> _options;

    public CustomDbContextFactory(DbContextOptions<MonitoringContext> options)
    {
        _options = options;
    }

    public MonitoringContext CreateDbContext()
    {
        return new MonitoringContext(_options);
    }
}
