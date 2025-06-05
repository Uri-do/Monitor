using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Infrastructure.Data.Configurations;

namespace MonitoringGrid.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for the monitoring system
/// </summary>
public class MonitoringContext : DbContext
{
    public MonitoringContext(DbContextOptions<MonitoringContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<KPI> KPIs { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<KpiContact> KpiContacts { get; set; }
    public DbSet<AlertLog> AlertLogs { get; set; }
    public DbSet<HistoricalData> HistoricalData { get; set; }
    public DbSet<Config> Config { get; set; }
    public DbSet<SystemStatus> SystemStatus { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new KpiConfiguration());
        modelBuilder.ApplyConfiguration(new ContactConfiguration());
        modelBuilder.ApplyConfiguration(new KpiContactConfiguration());
        modelBuilder.ApplyConfiguration(new AlertLogConfiguration());
        modelBuilder.ApplyConfiguration(new HistoricalDataConfiguration());
        modelBuilder.ApplyConfiguration(new ConfigConfiguration());
        modelBuilder.ApplyConfiguration(new SystemStatusConfiguration());
    }
}
