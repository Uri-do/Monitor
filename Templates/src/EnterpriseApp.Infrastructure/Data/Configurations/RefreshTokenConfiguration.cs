<!--#if (enableAuth)-->
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseApp.Core.Security;

namespace EnterpriseApp.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for RefreshToken
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Table configuration
        builder.ToTable("RefreshTokens", "auth");

        // Primary key
        builder.HasKey(rt => rt.Id);

        // Properties
        builder.Property(rt => rt.Id)
            .IsRequired()
            .ValueGeneratedOnAdd()
            .HasComment("Unique identifier for the refresh token");

        builder.Property(rt => rt.UserId)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("User ID this token belongs to");

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("The refresh token value");

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired()
            .HasComment("When the token expires");

        builder.Property(rt => rt.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the token was created");

        builder.Property(rt => rt.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indicates if the token is active");

        builder.Property(rt => rt.RevokedAt)
            .HasComment("When the token was revoked (if revoked)");

        builder.Property(rt => rt.RevokedBy)
            .HasMaxLength(100)
            .HasComment("Who revoked the token");

        builder.Property(rt => rt.RevokedReason)
            .HasMaxLength(500)
            .HasComment("Reason for revocation");

        builder.Property(rt => rt.IpAddress)
            .HasMaxLength(45)
            .HasComment("IP address where the token was created");

        builder.Property(rt => rt.UserAgent)
            .HasMaxLength(500)
            .HasComment("User agent of the client");

        // Indexes
        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

        builder.HasIndex(rt => rt.IsActive)
            .HasDatabaseName("IX_RefreshTokens_IsActive");

        builder.HasIndex(rt => rt.CreatedAt)
            .HasDatabaseName("IX_RefreshTokens_CreatedAt");

        // Composite indexes for common queries
        builder.HasIndex(rt => new { rt.UserId, rt.IsActive })
            .HasDatabaseName("IX_RefreshTokens_UserId_IsActive");

        builder.HasIndex(rt => new { rt.IsActive, rt.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_Active_ExpiresAt");

        // Relationships
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table comment
        builder.HasComment("Refresh tokens for JWT token management");
    }
}

/// <summary>
/// Entity Framework configuration for UserPassword
/// </summary>
public class UserPasswordConfiguration : IEntityTypeConfiguration<UserPassword>
{
    public void Configure(EntityTypeBuilder<UserPassword> builder)
    {
        // Table configuration
        builder.ToTable("UserPasswords", "auth");

        // Primary key
        builder.HasKey(up => up.Id);

        // Properties
        builder.Property(up => up.Id)
            .IsRequired()
            .ValueGeneratedOnAdd()
            .HasComment("Unique identifier");

        builder.Property(up => up.UserId)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("User ID this password belongs to");

        builder.Property(up => up.PasswordHash)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Password hash");

        builder.Property(up => up.PasswordSalt)
            .HasMaxLength(255)
            .HasComment("Password salt");

        builder.Property(up => up.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the password was created");

        builder.Property(up => up.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indicates if this is the current active password");

        builder.Property(up => up.CreatedBy)
            .HasMaxLength(100)
            .HasComment("Who created this password entry");

        // Indexes
        builder.HasIndex(up => up.UserId)
            .HasDatabaseName("IX_UserPasswords_UserId");

        builder.HasIndex(up => up.CreatedAt)
            .HasDatabaseName("IX_UserPasswords_CreatedAt");

        builder.HasIndex(up => up.IsActive)
            .HasDatabaseName("IX_UserPasswords_IsActive");

        // Composite index for password history queries
        builder.HasIndex(up => new { up.UserId, up.CreatedAt })
            .HasDatabaseName("IX_UserPasswords_UserId_CreatedAt");

        // Relationships
        builder.HasOne(up => up.User)
            .WithMany(u => u.PasswordHistory)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Table comment
        builder.HasComment("User password history for preventing password reuse");
    }
}
<!--#endif-->
