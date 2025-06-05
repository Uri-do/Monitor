using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for HistoricalData entity
/// </summary>
public class HistoricalDataConfiguration : IEntityTypeConfiguration<HistoricalData>
{
    public void Configure(EntityTypeBuilder<HistoricalData> builder)
    {
        builder.ToTable("HistoricalData", "monitoring");

        builder.HasKey(h => h.HistoricalId);

        builder.Property(h => h.KpiId)
            .IsRequired();

        builder.Property(h => h.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(h => h.Value)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(h => h.Period)
            .IsRequired();

        builder.Property(h => h.MetricKey)
            .IsRequired()
            .HasMaxLength(255);

        // Indexes
        builder.HasIndex(h => h.KpiId)
            .HasDatabaseName("IX_HistoricalData_KpiId");

        builder.HasIndex(h => h.Timestamp)
            .HasDatabaseName("IX_HistoricalData_Timestamp");

        builder.HasIndex(h => new { h.KpiId, h.Timestamp })
            .HasDatabaseName("IX_HistoricalData_KpiId_Timestamp");

        builder.HasIndex(h => new { h.KpiId, h.MetricKey, h.Period })
            .HasDatabaseName("IX_HistoricalData_KpiId_MetricKey_Period");

        // Relationship is configured in KPI configuration
    }
}
