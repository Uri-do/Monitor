using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Indicator entity
/// </summary>
public class IndicatorConfiguration : IEntityTypeConfiguration<Indicator>
{
    public void Configure(EntityTypeBuilder<Indicator> builder)
    {
        builder.ToTable("Indicators", "monitoring");

        builder.HasKey(i => i.IndicatorId);

        builder.Property(i => i.IndicatorId)
            .ValueGeneratedOnAdd();

        builder.Property(i => i.IndicatorName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(i => i.IndicatorCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.IndicatorDesc)
            .HasMaxLength(500);

        builder.Property(i => i.CollectorId)
            .IsRequired();

        builder.Property(i => i.CollectorItemName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(i => i.ScheduleConfiguration)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(i => i.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(i => i.LastMinutes)
            .IsRequired()
            .HasDefaultValue(60);

        builder.Property(i => i.ThresholdType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.ThresholdField)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.ThresholdComparison)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(i => i.ThresholdValue)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Priority)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("medium");

        builder.Property(i => i.OwnerContactId)
            .IsRequired();

        builder.Property(i => i.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(i => i.UpdatedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(i => i.LastRun);

        builder.Property(i => i.LastRunResult)
            .HasMaxLength(1000);

        builder.Property(i => i.AverageHour)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.AverageLastDays);

        builder.Property(i => i.AverageOfCurrHour)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.IsCurrentlyRunning)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.ExecutionStartTime);

        builder.Property(i => i.ExecutionContext)
            .HasMaxLength(50);

        // Relationships
        builder.HasOne(i => i.OwnerContact)
            .WithMany()
            .HasForeignKey(i => i.OwnerContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.IndicatorContacts)
            .WithOne(ic => ic.Indicator)
            .HasForeignKey(ic => ic.IndicatorId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: AlertLogs and HistoricalData still reference KpiId for now
        // These relationships will be updated when we migrate the data
        // builder.HasMany(i => i.AlertLogs)
        //     .WithOne(a => a.KPI)
        //     .HasForeignKey(a => a.KpiId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // builder.HasMany(i => i.HistoricalData)
        //     .WithOne(h => h.KPI)
        //     .HasForeignKey(h => h.KpiId)
        //     .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(i => i.IndicatorName)
            .HasDatabaseName("IX_Indicators_IndicatorName");

        builder.HasIndex(i => i.IndicatorCode)
            .IsUnique()
            .HasDatabaseName("IX_Indicators_IndicatorCode");

        builder.HasIndex(i => i.CollectorId)
            .HasDatabaseName("IX_Indicators_CollectorId");

        builder.HasIndex(i => i.OwnerContactId)
            .HasDatabaseName("IX_Indicators_OwnerContactId");

        builder.HasIndex(i => i.IsActive)
            .HasDatabaseName("IX_Indicators_IsActive");

        builder.HasIndex(i => i.Priority)
            .HasDatabaseName("IX_Indicators_Priority");

        builder.HasIndex(i => i.LastRun)
            .HasDatabaseName("IX_Indicators_LastRun");

        builder.HasIndex(i => new { i.CollectorId, i.CollectorItemName })
            .HasDatabaseName("IX_Indicators_Collector_Item");
    }
}
