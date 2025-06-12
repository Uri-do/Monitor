using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for MonitorStatisticsCollector entity
/// </summary>
public class MonitorStatisticsCollectorConfiguration : IEntityTypeConfiguration<MonitorStatisticsCollector>
{
    public void Configure(EntityTypeBuilder<MonitorStatisticsCollector> builder)
    {
        builder.ToTable("tbl_Monitor_StatisticsCollectors", "stats");

        builder.HasKey(c => c.ID);

        builder.Property(c => c.ID)
            .ValueGeneratedOnAdd()
            .HasColumnType("bigint");

        builder.Property(c => c.CollectorID)
            .IsRequired()
            .HasColumnType("bigint");

        builder.Property(c => c.CollectorCode)
            .HasMaxLength(500)
            .HasColumnType("varchar(500)");

        builder.Property(c => c.CollectorDesc)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.Property(c => c.FrequencyMinutes)
            .IsRequired()
            .HasColumnType("int");

        builder.Property(c => c.LastMinutes)
            .HasColumnType("int");

        builder.Property(c => c.StoreProcedure)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.Property(c => c.IsActive)
            .HasColumnType("bit");

        builder.Property(c => c.UpdatedDate)
            .HasColumnType("datetime");

        builder.Property(c => c.LastRun)
            .HasColumnType("datetime");

        builder.Property(c => c.LastRunResult)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        // Relationships
        builder.HasMany(c => c.Statistics)
            .WithOne(s => s.Collector)
            .HasForeignKey(s => s.CollectorID)
            .HasPrincipalKey(c => c.CollectorID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Indicators)
            .WithOne()
            .HasForeignKey(i => i.CollectorID)
            .HasPrincipalKey(c => c.CollectorID)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(c => c.CollectorID)
            .IsUnique()
            .HasDatabaseName("IX_MonitorStatisticsCollectors_CollectorID");

        builder.HasIndex(c => c.CollectorCode)
            .HasDatabaseName("IX_MonitorStatisticsCollectors_CollectorCode");

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("IX_MonitorStatisticsCollectors_IsActive");

        builder.HasIndex(c => c.LastRun)
            .HasDatabaseName("IX_MonitorStatisticsCollectors_LastRun");
    }
}
