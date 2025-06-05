using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Config entity
/// </summary>
public class ConfigConfiguration : IEntityTypeConfiguration<Config>
{
    public void Configure(EntityTypeBuilder<Config> builder)
    {
        builder.ToTable("Config", "monitoring");

        builder.HasKey(c => c.ConfigKey);

        builder.Property(c => c.ConfigKey)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.ConfigValue)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.ModifiedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Index
        builder.HasIndex(c => c.ModifiedDate)
            .HasDatabaseName("IX_Config_ModifiedDate");
    }
}
