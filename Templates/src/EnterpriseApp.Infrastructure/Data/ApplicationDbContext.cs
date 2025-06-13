using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using EnterpriseApp.Core.Entities;
using EnterpriseApp.Infrastructure.Data.Configurations;
using System.Reflection;
<!--#if (enableAuth)-->
using EnterpriseApp.Core.Security;
<!--#endif-->

namespace EnterpriseApp.Infrastructure.Data;

/// <summary>
/// Main application database context
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the ApplicationDbContext
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Domain entities
    /// <summary>
    /// DomainEntities table
    /// </summary>
    public DbSet<DomainEntity> DomainEntities { get; set; } = null!;

    /// <summary>
    /// DomainEntity items table
    /// </summary>
    public DbSet<DomainEntityItem> DomainEntityItems { get; set; } = null!;

    /// <summary>
    /// Audit logs table
    /// </summary>
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

<!--#if (enableAuth)-->
    // Authentication entities
    /// <summary>
    /// Users table
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Roles table
    /// </summary>
    public DbSet<Role> Roles { get; set; } = null!;

    /// <summary>
    /// Permissions table
    /// </summary>
    public DbSet<Permission> Permissions { get; set; } = null!;

    /// <summary>
    /// User roles junction table
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; } = null!;

    /// <summary>
    /// Role permissions junction table
    /// </summary>
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;

    /// <summary>
    /// Refresh tokens table
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// User password history table
    /// </summary>
    public DbSet<UserPassword> UserPasswords { get; set; } = null!;
<!--#endif-->

    /// <summary>
    /// Configures the model
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure entity relationships and constraints
        ConfigureEntityRelationships(modelBuilder);

        // Configure indexes
        ConfigureIndexes(modelBuilder);

        // Configure default values
        ConfigureDefaults(modelBuilder);

        // Seed initial data
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Configures entity relationships
    /// </summary>
    private static void ConfigureEntityRelationships(ModelBuilder modelBuilder)
    {
        // DomainEntity -> DomainEntityItems (One-to-Many)
        modelBuilder.Entity<DomainEntity>()
            .HasMany(e => e.Items)
            .WithOne(i => i.DomainEntity)
            .HasForeignKey(i => i.DomainEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        // DomainEntity -> AuditLogs (One-to-Many, optional)
        modelBuilder.Entity<DomainEntity>()
            .HasMany(e => e.AuditLogs)
            .WithOne(a => a.DomainEntity)
            .HasForeignKey(a => a.DomainEntityId)
            .OnDelete(DeleteBehavior.SetNull);

<!--#if (enableAuth)-->
        // User -> UserRoles (One-to-Many)
        modelBuilder.Entity<User>()
            .HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Role -> UserRoles (One-to-Many)
        modelBuilder.Entity<Role>()
            .HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Role -> RolePermissions (One-to-Many)
        modelBuilder.Entity<Role>()
            .HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Permission -> RolePermissions (One-to-Many)
        modelBuilder.Entity<Permission>()
            .HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> RefreshTokens (One-to-Many)
        modelBuilder.Entity<User>()
            .HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> UserPasswords (One-to-Many)
        modelBuilder.Entity<User>()
            .HasMany(u => u.PasswordHistory)
            .WithOne(up => up.User)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure composite keys
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });
<!--#endif-->
    }

    /// <summary>
    /// Configures database indexes
    /// </summary>
    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // DomainEntity indexes
        modelBuilder.Entity<DomainEntity>()
            .HasIndex(e => e.Name)
            .HasDatabaseName("IX_DomainEntities_Name");

        modelBuilder.Entity<DomainEntity>()
            .HasIndex(e => e.Category)
            .HasDatabaseName("IX_DomainEntities_Category");

        modelBuilder.Entity<DomainEntity>()
            .HasIndex(e => e.Status)
            .HasDatabaseName("IX_DomainEntities_Status");

        modelBuilder.Entity<DomainEntity>()
            .HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_DomainEntities_IsActive");

        modelBuilder.Entity<DomainEntity>()
            .HasIndex(e => e.CreatedDate)
            .HasDatabaseName("IX_DomainEntities_CreatedDate");

        // DomainEntityItem indexes
        modelBuilder.Entity<DomainEntityItem>()
            .HasIndex(i => i.DomainEntityId)
            .HasDatabaseName("IX_DomainEntityItems_DomainEntityId");

        modelBuilder.Entity<DomainEntityItem>()
            .HasIndex(i => i.Name)
            .HasDatabaseName("IX_DomainEntityItems_Name");

        // AuditLog indexes
        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => new { a.EntityName, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        modelBuilder.Entity<AuditLog>()
            .HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

<!--#if (enableAuth)-->
        // User indexes
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        // Role indexes
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name");

        // Permission indexes
        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Name");

        modelBuilder.Entity<Permission>()
            .HasIndex(p => new { p.Resource, p.Action })
            .HasDatabaseName("IX_Permissions_ResourceAction");

        // RefreshToken indexes
        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");
<!--#endif-->
    }

    /// <summary>
    /// Configures default values
    /// </summary>
    private static void ConfigureDefaults(ModelBuilder modelBuilder)
    {
        // DomainEntity defaults
        modelBuilder.Entity<DomainEntity>()
            .Property(e => e.CreatedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<DomainEntity>()
            .Property(e => e.ModifiedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<DomainEntity>()
            .Property(e => e.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<DomainEntity>()
            .Property(e => e.Priority)
            .HasDefaultValue(3);

        // DomainEntityItem defaults
        modelBuilder.Entity<DomainEntityItem>()
            .Property(i => i.CreatedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<DomainEntityItem>()
            .Property(i => i.ModifiedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<DomainEntityItem>()
            .Property(i => i.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<DomainEntityItem>()
            .Property(i => i.Quantity)
            .HasDefaultValue(1);

        // AuditLog defaults
        modelBuilder.Entity<AuditLog>()
            .Property(a => a.Timestamp)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<AuditLog>()
            .Property(a => a.Severity)
            .HasDefaultValue("Information");

<!--#if (enableAuth)-->
        // User defaults
        modelBuilder.Entity<User>()
            .Property(u => u.CreatedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<User>()
            .Property(u => u.ModifiedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<User>()
            .Property(u => u.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<User>()
            .Property(u => u.EmailConfirmed)
            .HasDefaultValue(false);

        modelBuilder.Entity<User>()
            .Property(u => u.TwoFactorEnabled)
            .HasDefaultValue(false);

        modelBuilder.Entity<User>()
            .Property(u => u.FailedLoginAttempts)
            .HasDefaultValue(0);

        // Role defaults
        modelBuilder.Entity<Role>()
            .Property(r => r.CreatedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<Role>()
            .Property(r => r.ModifiedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<Role>()
            .Property(r => r.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<Role>()
            .Property(r => r.IsSystemRole)
            .HasDefaultValue(false);

        // Permission defaults
        modelBuilder.Entity<Permission>()
            .Property(p => p.CreatedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<Permission>()
            .Property(p => p.ModifiedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<Permission>()
            .Property(p => p.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<Permission>()
            .Property(p => p.IsSystemPermission)
            .HasDefaultValue(false);

        // RefreshToken defaults
        modelBuilder.Entity<RefreshToken>()
            .Property(rt => rt.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<RefreshToken>()
            .Property(rt => rt.IsActive)
            .HasDefaultValue(true);

        // UserPassword defaults
        modelBuilder.Entity<UserPassword>()
            .Property(up => up.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        modelBuilder.Entity<UserPassword>()
            .Property(up => up.IsActive)
            .HasDefaultValue(true);

        // UserRole defaults
        modelBuilder.Entity<UserRole>()
            .Property(ur => ur.AssignedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // RolePermission defaults
        modelBuilder.Entity<RolePermission>()
            .Property(rp => rp.AssignedDate)
            .HasDefaultValueSql("SYSUTCDATETIME()");
<!--#endif-->
    }

    /// <summary>
    /// Seeds initial data
    /// </summary>
    private static void SeedData(ModelBuilder modelBuilder)
    {
<!--#if (enableAuth)-->
        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role
            {
                RoleId = "role-admin",
                Name = "Admin",
                Description = "System Administrator with full access",
                IsSystemRole = true,
                CreatedBy = "SYSTEM"
            },
            new Role
            {
                RoleId = "role-manager",
                Name = "Manager",
                Description = "Manager with read/write access to most features",
                IsSystemRole = true,
                CreatedBy = "SYSTEM"
            },
            new Role
            {
                RoleId = "role-user",
                Name = "User",
                Description = "Standard user with limited access",
                IsSystemRole = true,
                CreatedBy = "SYSTEM"
            }
        );

        // Seed default permissions
        modelBuilder.Entity<Permission>().HasData(
            new Permission
            {
                PermissionId = "perm-system-admin",
                Name = "System:Admin",
                Description = "Full system administration access",
                Resource = "System",
                Action = "Admin",
                IsSystemPermission = true
            },
            new Permission
            {
                PermissionId = "perm-domainentity-read",
                Name = "DomainEntity:Read",
                Description = "Read DomainEntity information",
                Resource = "DomainEntity",
                Action = "Read",
                IsSystemPermission = true
            },
            new Permission
            {
                PermissionId = "perm-domainentity-write",
                Name = "DomainEntity:Write",
                Description = "Create and update DomainEntities",
                Resource = "DomainEntity",
                Action = "Write",
                IsSystemPermission = true
            },
            new Permission
            {
                PermissionId = "perm-domainentity-delete",
                Name = "DomainEntity:Delete",
                Description = "Delete DomainEntities",
                Resource = "DomainEntity",
                Action = "Delete",
                IsSystemPermission = true
            }
        );

        // Assign all permissions to admin role
        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = "role-admin", PermissionId = "perm-system-admin", AssignedBy = "SYSTEM" },
            new RolePermission { RoleId = "role-admin", PermissionId = "perm-domainentity-read", AssignedBy = "SYSTEM" },
            new RolePermission { RoleId = "role-admin", PermissionId = "perm-domainentity-write", AssignedBy = "SYSTEM" },
            new RolePermission { RoleId = "role-admin", PermissionId = "perm-domainentity-delete", AssignedBy = "SYSTEM" }
        );

        // Assign read/write permissions to manager role
        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = "role-manager", PermissionId = "perm-domainentity-read", AssignedBy = "SYSTEM" },
            new RolePermission { RoleId = "role-manager", PermissionId = "perm-domainentity-write", AssignedBy = "SYSTEM" }
        );

        // Assign read permission to user role
        modelBuilder.Entity<RolePermission>().HasData(
            new RolePermission { RoleId = "role-user", PermissionId = "perm-domainentity-read", AssignedBy = "SYSTEM" }
        );
<!--#endif-->
    }

    /// <summary>
    /// Saves changes with automatic audit logging
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps before saving
        UpdateTimestamps();

        // Generate audit logs
        var auditEntries = GenerateAuditEntries();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Save audit logs after main changes are saved
        if (auditEntries.Any())
        {
            AuditLogs.AddRange(auditEntries);
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Updates timestamps for entities
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is DomainEntity entity)
            {
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedDate = DateTime.UtcNow;
                }
                entity.ModifiedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is DomainEntityItem item)
            {
                if (entry.State == EntityState.Added)
                {
                    item.CreatedDate = DateTime.UtcNow;
                }
                item.ModifiedDate = DateTime.UtcNow;
            }
<!--#if (enableAuth)-->
            else if (entry.Entity is User user)
            {
                if (entry.State == EntityState.Added)
                {
                    user.CreatedDate = DateTime.UtcNow;
                }
                user.ModifiedDate = DateTime.UtcNow;
            }
            else if (entry.Entity is Role role)
            {
                if (entry.State == EntityState.Added)
                {
                    role.CreatedDate = DateTime.UtcNow;
                }
                role.ModifiedDate = DateTime.UtcNow;
            }
<!--#endif-->
        }
    }

    /// <summary>
    /// Generates audit log entries for tracked changes
    /// </summary>
    private List<AuditLog> GenerateAuditEntries()
    {
        var auditEntries = new List<AuditLog>();

        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .Where(e => !(e.Entity is AuditLog)); // Don't audit the audit logs themselves

        foreach (var entry in entries)
        {
            var entityName = entry.Entity.GetType().Name;
            var entityId = GetEntityId(entry);

            if (string.IsNullOrEmpty(entityId))
                continue;

            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = entry.State switch
                {
                    EntityState.Added => Core.Enums.AuditAction.Created,
                    EntityState.Modified => Core.Enums.AuditAction.Updated,
                    EntityState.Deleted => Core.Enums.AuditAction.Deleted,
                    _ => Core.Enums.AuditAction.Custom
                },
                ActionDescription = $"{entityName} {entry.State.ToString().ToLower()}",
                UserId = "SYSTEM", // This should be set from the current user context
                Timestamp = DateTime.UtcNow
            };

            if (entry.State == EntityState.Modified)
            {
                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();

                foreach (var property in entry.Properties)
                {
                    if (property.IsModified)
                    {
                        oldValues[property.Metadata.Name] = property.OriginalValue;
                        newValues[property.Metadata.Name] = property.CurrentValue;
                    }
                }

                auditLog.OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues);
                auditLog.NewValues = System.Text.Json.JsonSerializer.Serialize(newValues);
            }

            auditEntries.Add(auditLog);
        }

        return auditEntries;
    }

    /// <summary>
    /// Gets the entity ID for audit logging
    /// </summary>
    private static string GetEntityId(EntityEntry entry)
    {
        var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return keyProperty?.CurrentValue?.ToString() ?? string.Empty;
    }
}
