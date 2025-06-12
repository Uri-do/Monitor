using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Infrastructure.Data.Configurations;

namespace MonitoringGrid.Infrastructure.Data;

/// <summary>
/// Database context for ProgressPlayDBTest database
/// Contains monitor statistics tables and other monitored data
/// </summary>
public class ProgressPlayContext : DbContext
{
    public ProgressPlayContext(DbContextOptions<ProgressPlayContext> options) : base(options)
    {
    }

    // Monitor Statistics DbSets (from ProgressPlayDBTest)
    public DbSet<MonitorStatistics> MonitorStatistics { get; set; }
    public DbSet<MonitorStatisticsCollector> MonitorStatisticsCollectors { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply statistics configurations
        modelBuilder.ApplyConfiguration(new MonitorStatisticsConfiguration());
        modelBuilder.ApplyConfiguration(new MonitorStatisticsCollectorConfiguration());
    }
}
