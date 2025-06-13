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
    public DbSet<Indicator> Indicators { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<IndicatorContact> IndicatorContacts { get; set; }
    public DbSet<AlertLog> AlertLogs { get; set; }
    public DbSet<Config> Config { get; set; }
    public DbSet<SystemStatus> SystemStatus { get; set; }
    public DbSet<Scheduler> Schedulers { get; set; }

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
    public DbSet<Core.Security.SecurityThreat> SecurityThreats { get; set; }
    public DbSet<Core.Security.UserTwoFactorSettings> UserTwoFactorSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new IndicatorConfiguration());
        modelBuilder.ApplyConfiguration(new ContactConfiguration());
        modelBuilder.ApplyConfiguration(new IndicatorContactConfiguration());
        modelBuilder.ApplyConfiguration(new AlertLogConfiguration());
        modelBuilder.ApplyConfiguration(new ConfigConfiguration());
        modelBuilder.ApplyConfiguration(new SystemStatusConfiguration());
        modelBuilder.ApplyConfiguration(new SchedulerConfiguration());

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
        modelBuilder.ApplyConfiguration(new SecurityThreatConfiguration());
        modelBuilder.ApplyConfiguration(new UserTwoFactorSettingsConfiguration());
    }
}
