<!--#if (enableAuth)-->
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseApp.Core.Security;

namespace EnterpriseApp.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for User
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table configuration
        builder.ToTable("Users", "auth");

        // Primary key
        builder.HasKey(u => u.UserId);

        // Properties
        builder.Property(u => u.UserId)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Unique identifier for the user");

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Unique username for login");

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Email address (also used for login)");

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Display name for the user");

        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .HasComment("First name");

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .HasComment("Last name");

        builder.Property(u => u.Department)
            .HasMaxLength(100)
            .HasComment("Department or organizational unit");

        builder.Property(u => u.Title)
            .HasMaxLength(100)
            .HasComment("Job title");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Hashed password");

        builder.Property(u => u.PasswordSalt)
            .HasMaxLength(255)
            .HasComment("Password salt");

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indicates if the user account is active");

        builder.Property(u => u.EmailConfirmed)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indicates if the email has been confirmed");

        builder.Property(u => u.TwoFactorEnabled)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indicates if two-factor authentication is enabled");

        builder.Property(u => u.FailedLoginAttempts)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Number of failed login attempts");

        builder.Property(u => u.LockoutEnd)
            .HasComment("When the account lockout ends (if locked)");

        builder.Property(u => u.LastLogin)
            .HasComment("Last successful login timestamp");

        builder.Property(u => u.LastPasswordChange)
            .HasComment("When the password was last changed");

        builder.Property(u => u.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the user account was created");

        builder.Property(u => u.ModifiedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the user account was last modified");

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100)
            .HasComment("Who created the user account");

        builder.Property(u => u.ModifiedBy)
            .HasMaxLength(100)
            .HasComment("Who last modified the user account");

        // Indexes
        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        builder.HasIndex(u => u.LastLogin)
            .HasDatabaseName("IX_Users_LastLogin");

        builder.HasIndex(u => u.CreatedDate)
            .HasDatabaseName("IX_Users_CreatedDate");

        builder.HasIndex(u => u.Department)
            .HasDatabaseName("IX_Users_Department");

        // Composite indexes
        builder.HasIndex(u => new { u.IsActive, u.EmailConfirmed })
            .HasDatabaseName("IX_Users_Active_EmailConfirmed");

        // Relationships
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.PasswordHistory)
            .WithOne(up => up.User)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Check constraints
        builder.HasCheckConstraint("CK_Users_FailedLoginAttempts", "[FailedLoginAttempts] >= 0");

        // Table comment
        builder.HasComment("User accounts for authentication and authorization");
    }
}

/// <summary>
/// Entity Framework configuration for Role
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Table configuration
        builder.ToTable("Roles", "auth");

        // Primary key
        builder.HasKey(r => r.RoleId);

        // Properties
        builder.Property(r => r.RoleId)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Unique identifier for the role");

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Name of the role");

        builder.Property(r => r.Description)
            .HasMaxLength(500)
            .HasDefaultValue("")
            .HasComment("Description of the role");

        builder.Property(r => r.IsSystemRole)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indicates if this is a system role (cannot be deleted)");

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indicates if the role is active");

        builder.Property(r => r.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the role was created");

        builder.Property(r => r.ModifiedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the role was last modified");

        builder.Property(r => r.CreatedBy)
            .HasMaxLength(100)
            .HasComment("Who created the role");

        builder.Property(r => r.ModifiedBy)
            .HasMaxLength(100)
            .HasComment("Who last modified the role");

        // Indexes
        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("IX_Roles_IsActive");

        builder.HasIndex(r => r.IsSystemRole)
            .HasDatabaseName("IX_Roles_IsSystemRole");

        // Relationships
        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.RolePermissions)
            .WithOne(rp => rp.Role)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table comment
        builder.HasComment("Roles for authorization");
    }
}

/// <summary>
/// Entity Framework configuration for Permission
/// </summary>
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        // Table configuration
        builder.ToTable("Permissions", "auth");

        // Primary key
        builder.HasKey(p => p.PermissionId);

        // Properties
        builder.Property(p => p.PermissionId)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Unique identifier for the permission");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Name of the permission");

        builder.Property(p => p.Description)
            .HasMaxLength(500)
            .HasDefaultValue("")
            .HasComment("Description of the permission");

        builder.Property(p => p.Resource)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Resource this permission applies to");

        builder.Property(p => p.Action)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Action this permission allows");

        builder.Property(p => p.IsSystemPermission)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indicates if this is a system permission");

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indicates if the permission is active");

        builder.Property(p => p.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the permission was created");

        builder.Property(p => p.ModifiedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the permission was last modified");

        // Indexes
        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("IX_Permissions_Name");

        builder.HasIndex(p => new { p.Resource, p.Action })
            .HasDatabaseName("IX_Permissions_ResourceAction");

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Permissions_IsActive");

        // Relationships
        builder.HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table comment
        builder.HasComment("Permissions for fine-grained authorization");
    }
}

/// <summary>
/// Entity Framework configuration for UserRole
/// </summary>
public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        // Table configuration
        builder.ToTable("UserRoles", "auth");

        // Composite primary key
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        // Properties
        builder.Property(ur => ur.UserId)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("User ID");

        builder.Property(ur => ur.RoleId)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Role ID");

        builder.Property(ur => ur.AssignedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the role was assigned");

        builder.Property(ur => ur.AssignedBy)
            .HasMaxLength(100)
            .HasComment("Who assigned the role");

        // Indexes
        builder.HasIndex(ur => ur.UserId)
            .HasDatabaseName("IX_UserRoles_UserId");

        builder.HasIndex(ur => ur.RoleId)
            .HasDatabaseName("IX_UserRoles_RoleId");

        builder.HasIndex(ur => ur.AssignedDate)
            .HasDatabaseName("IX_UserRoles_AssignedDate");

        // Relationships
        builder.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table comment
        builder.HasComment("Junction table for User-Role many-to-many relationship");
    }
}

/// <summary>
/// Entity Framework configuration for RolePermission
/// </summary>
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        // Table configuration
        builder.ToTable("RolePermissions", "auth");

        // Composite primary key
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Properties
        builder.Property(rp => rp.RoleId)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Role ID");

        builder.Property(rp => rp.PermissionId)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("Permission ID");

        builder.Property(rp => rp.AssignedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the permission was assigned");

        builder.Property(rp => rp.AssignedBy)
            .HasMaxLength(100)
            .HasComment("Who assigned the permission");

        // Indexes
        builder.HasIndex(rp => rp.RoleId)
            .HasDatabaseName("IX_RolePermissions_RoleId");

        builder.HasIndex(rp => rp.PermissionId)
            .HasDatabaseName("IX_RolePermissions_PermissionId");

        // Relationships
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table comment
        builder.HasComment("Junction table for Role-Permission many-to-many relationship");
    }
}
<!--#endif-->
