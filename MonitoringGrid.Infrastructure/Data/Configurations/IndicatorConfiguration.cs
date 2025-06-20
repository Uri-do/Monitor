using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Advanced Entity Framework configuration for Indicator entity with enterprise features
/// </summary>
public class IndicatorConfiguration : IEntityTypeConfiguration<Indicator>
{
    public void Configure(EntityTypeBuilder<Indicator> builder)
    {
        // Table configuration with schema
        builder.ToTable("Indicators", "monitoring");

        // Primary key configuration
        builder.HasKey(i => i.IndicatorID);

        // Configure all properties
        ConfigureProperties(builder);

        // Configure relationships
        ConfigureRelationships(builder);

        // Configure performance indexes
        ConfigurePerformanceIndexes(builder);

        // Configure constraints and validations
        ConfigureConstraints(builder);
    }

    private void ConfigureProperties(EntityTypeBuilder<Indicator> builder)
    {
        // Primary key
        builder.Property(i => i.IndicatorID)
            .IsRequired()
            .ValueGeneratedOnAdd()
            .HasComment("Unique identifier for the indicator");

        // Required string properties
        builder.Property(i => i.IndicatorName)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Name of the indicator");

        builder.Property(i => i.IndicatorCode)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Unique code for the indicator");

        builder.Property(i => i.IndicatorDesc)
            .HasMaxLength(1000)
            .HasComment("Description of the indicator");

        builder.Property(i => i.CollectorItemName)
            .IsRequired()
            .HasMaxLength(255)
            .HasComment("Name of the collector item");

        // Foreign key properties
        builder.Property(i => i.CollectorID)
            .IsRequired()
            .HasComment("Associated collector identifier");

        builder.Property(i => i.SchedulerID)
            .IsRequired(false)
            .HasComment("Associated scheduler identifier (nullable for manual execution)");

        builder.Property(i => i.OwnerContactId)
            .IsRequired()
            .HasComment("Owner contact identifier");

        // Boolean properties with defaults
        builder.Property(i => i.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indicates if the indicator is active");

        builder.Property(i => i.IsCurrentlyRunning)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment("Indicates if the indicator is currently executing");

        // Numeric properties
        builder.Property(i => i.LastMinutes)
            .IsRequired()
            .HasDefaultValue(60)
            .HasComment("Time window in minutes for the indicator");

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

        builder.HasOne(i => i.Scheduler)
            .WithMany(s => s.Indicators)
            .HasForeignKey(i => i.SchedulerID)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.IndicatorContacts)
            .WithOne(ic => ic.Indicator)
            .HasForeignKey(ic => ic.IndicatorID)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: Relationship with MonitorStatisticsCollector removed since it's in a different database (ProgressPlayDBTest)

        // Legacy KPI references DELETED - Using modern Indicator relationships only

        // Indexes
        builder.HasIndex(i => i.IndicatorName)
            .HasDatabaseName("IX_Indicators_IndicatorName");

        builder.HasIndex(i => i.IndicatorCode)
            .IsUnique()
            .HasDatabaseName("IX_Indicators_IndicatorCode");

        builder.HasIndex(i => i.CollectorID)
            .HasDatabaseName("IX_Indicators_CollectorID");

        builder.HasIndex(i => i.OwnerContactId)
            .HasDatabaseName("IX_Indicators_OwnerContactId");

        builder.HasIndex(i => i.IsActive)
            .HasDatabaseName("IX_Indicators_IsActive");

        builder.HasIndex(i => i.Priority)
            .HasDatabaseName("IX_Indicators_Priority");

        builder.HasIndex(i => i.LastRun)
            .HasDatabaseName("IX_Indicators_LastRun");

        builder.HasIndex(i => i.SchedulerID)
            .HasDatabaseName("IX_Indicators_SchedulerID");

        builder.HasIndex(i => new { i.CollectorID, i.CollectorItemName })
            .HasDatabaseName("IX_Indicators_Collector_Item");
    }

    private void ConfigureRelationships(EntityTypeBuilder<Indicator> builder)
    {
        // Owner contact relationship
        builder.HasOne(i => i.OwnerContact)
            .WithMany()
            .HasForeignKey(i => i.OwnerContactId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Indicators_OwnerContact");

        // Scheduler relationship (optional)
        builder.HasOne(i => i.Scheduler)
            .WithMany(s => s.Indicators)
            .HasForeignKey(i => i.SchedulerID)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_Indicators_Scheduler");

        // Indicator contacts relationship
        builder.HasMany(i => i.IndicatorContacts)
            .WithOne(ic => ic.Indicator)
            .HasForeignKey(ic => ic.IndicatorID)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_IndicatorContacts_Indicator");
    }

    private void ConfigurePerformanceIndexes(EntityTypeBuilder<Indicator> builder)
    {
        // Unique index on indicator code
        builder.HasIndex(i => i.IndicatorCode)
            .IsUnique()
            .HasDatabaseName("IX_Indicators_IndicatorCode_Unique");

        // Performance index for active indicators
        builder.HasIndex(i => new { i.IsActive, i.LastRun })
            .HasDatabaseName("IX_Indicators_Active_LastRun_Performance");

        // Index for execution queries
        builder.HasIndex(i => new { i.IsActive, i.IsCurrentlyRunning, i.Priority })
            .HasDatabaseName("IX_Indicators_Execution_Performance");

        // Index for collector queries
        builder.HasIndex(i => new { i.CollectorID, i.IsActive })
            .HasDatabaseName("IX_Indicators_Collector_Active_Performance");

        // Index for scheduler queries
        builder.HasIndex(i => new { i.SchedulerID, i.IsActive })
            .HasDatabaseName("IX_Indicators_Scheduler_Active_Performance")
            .HasFilter("[SchedulerID] IS NOT NULL");

        // Index for audit and reporting
        builder.HasIndex(i => new { i.CreatedDate, i.UpdatedDate })
            .HasDatabaseName("IX_Indicators_Audit_Performance");
    }

    private void ConfigureConstraints(EntityTypeBuilder<Indicator> builder)
    {
        // Check constraints using modern ToTable syntax
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Indicators_ThresholdValue", "[ThresholdValue] >= 0");
            t.HasCheckConstraint("CK_Indicators_LastMinutes", "[LastMinutes] > 0 AND [LastMinutes] <= 10080"); // Max 1 week
            t.HasCheckConstraint("CK_Indicators_Priority", "[Priority] IN ('low', 'medium', 'high', 'critical')");
            t.HasCheckConstraint("CK_Indicators_ThresholdComparison", "[ThresholdComparison] IN ('>', '<', '>=', '<=', '=', '!=')");
        });
    }
}
