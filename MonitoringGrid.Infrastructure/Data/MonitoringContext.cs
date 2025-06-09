using Microsoft.EntityFrameworkCore;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.Security;
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
    public DbSet<KpiType> KpiTypes { get; set; }
    public DbSet<ScheduledJob> ScheduledJobs { get; set; }

    // Authentication DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<Core.Entities.Role> Roles { get; set; }
    public DbSet<Core.Entities.Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<BlacklistedToken> BlacklistedTokens { get; set; }
    public DbSet<UserPassword> UserPasswords { get; set; }
    public DbSet<Core.Security.SecurityAuditEvent> SecurityAuditEvents { get; set; }

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
        modelBuilder.ApplyConfiguration(new KpiTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ScheduledJobConfiguration());

        // Apply authentication configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new BlacklistedTokenConfiguration());
        modelBuilder.ApplyConfiguration(new UserPasswordConfiguration());
        modelBuilder.ApplyConfiguration(new SecurityAuditEventConfiguration());
    }
}
