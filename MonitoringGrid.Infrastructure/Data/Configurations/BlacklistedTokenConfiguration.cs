using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for BlacklistedToken entity
/// </summary>
public class BlacklistedTokenConfiguration : IEntityTypeConfiguration<BlacklistedToken>
{
    public void Configure(EntityTypeBuilder<BlacklistedToken> builder)
    {
        builder.ToTable("BlacklistedTokens", "auth");

        builder.HasKey(bt => bt.Id);

        builder.Property(bt => bt.Id)
            .ValueGeneratedOnAdd();

        builder.Property(bt => bt.TokenHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(bt => bt.BlacklistedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(bt => bt.ExpiresAt)
            .IsRequired();

        builder.Property(bt => bt.Reason)
            .HasMaxLength(500);

        builder.Property(bt => bt.BlacklistedBy)
            .HasMaxLength(50);

        builder.Property(bt => bt.IpAddress)
            .HasMaxLength(45);

        // Indexes
        builder.HasIndex(bt => bt.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_BlacklistedTokens_TokenHash");

        builder.HasIndex(bt => bt.ExpiresAt)
            .HasDatabaseName("IX_BlacklistedTokens_ExpiresAt");

        builder.HasIndex(bt => bt.BlacklistedAt)
            .HasDatabaseName("IX_BlacklistedTokens_BlacklistedAt");

        // Composite index for cleanup queries
        builder.HasIndex(bt => new { bt.ExpiresAt, bt.BlacklistedAt })
            .HasDatabaseName("IX_BlacklistedTokens_Expires_Blacklisted");
    }
}
