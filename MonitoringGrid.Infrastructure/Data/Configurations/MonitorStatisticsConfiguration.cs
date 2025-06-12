using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for MonitorStatistics entity
/// </summary>
public class MonitorStatisticsConfiguration : IEntityTypeConfiguration<MonitorStatistics>
{
    public void Configure(EntityTypeBuilder<MonitorStatistics> builder)
    {
        builder.ToTable("tbl_Monitor_Statistics", "stats");

        // Composite primary key
        builder.HasKey(ms => new { ms.Day, ms.Hour, ms.CollectorID, ms.ItemName });

        builder.Property(ms => ms.Day)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(ms => ms.Hour)
            .IsRequired()
            .HasColumnType("tinyint");

        builder.Property(ms => ms.CollectorID)
            .IsRequired()
            .HasColumnType("bigint");

        builder.Property(ms => ms.ItemName)
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.Property(ms => ms.Total)
            .HasColumnType("decimal(18,2)");

        builder.Property(ms => ms.Marked)
            .HasColumnType("decimal(18,2)");

        builder.Property(ms => ms.MarkedPercent)
            .HasColumnType("decimal(18,2)");

        builder.Property(ms => ms.UpdatedDate)
            .HasColumnType("datetime");

        // Relationships
        builder.HasOne(ms => ms.Collector)
            .WithMany(c => c.Statistics)
            .HasForeignKey(ms => ms.CollectorID)
            .HasPrincipalKey(c => c.CollectorID)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(ms => ms.CollectorID)
            .HasDatabaseName("IX_MonitorStatistics_CollectorID");

        builder.HasIndex(ms => new { ms.Day, ms.Hour })
            .HasDatabaseName("IX_MonitorStatistics_Day_Hour");

        builder.HasIndex(ms => ms.UpdatedDate)
            .HasDatabaseName("IX_MonitorStatistics_UpdatedDate");
    }
}
