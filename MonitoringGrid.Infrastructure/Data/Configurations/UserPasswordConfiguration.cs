using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for UserPassword entity
/// </summary>
public class UserPasswordConfiguration : IEntityTypeConfiguration<UserPassword>
{
    public void Configure(EntityTypeBuilder<UserPassword> builder)
    {
        builder.ToTable("UserPasswords", "auth");

        builder.HasKey(up => up.Id);

        builder.Property(up => up.Id)
            .ValueGeneratedOnAdd();

        builder.Property(up => up.UserId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(up => up.PasswordHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(up => up.PasswordSalt)
            .HasMaxLength(255);

        builder.Property(up => up.CreatedBy)
            .HasMaxLength(100);

        builder.Property(up => up.CreatedAt)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Indexes
        builder.HasIndex(up => up.UserId)
            .HasDatabaseName("IX_UserPasswords_UserId");

        builder.HasIndex(up => new { up.UserId, up.IsActive })
            .HasDatabaseName("IX_UserPasswords_UserId_Active");

        // Relationships are configured in User configuration
    }
}
