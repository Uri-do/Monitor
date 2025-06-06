using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for KPI entity
/// </summary>
public class KpiConfiguration : IEntityTypeConfiguration<KPI>
{
    public void Configure(EntityTypeBuilder<KPI> builder)
    {
        builder.ToTable("KPIs", "monitoring");

        builder.HasKey(k => k.KpiId);

        builder.Property(k => k.Indicator)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(k => k.Owner)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(k => k.Priority)
            .IsRequired();

        builder.Property(k => k.Frequency)
            .IsRequired();

        builder.Property(k => k.LastMinutes)
            .IsRequired()
            .HasDefaultValue(1440); // Default 24 hours

        builder.Property(k => k.Deviation)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        builder.Property(k => k.SpName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(k => k.SubjectTemplate)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(k => k.DescriptionTemplate)
            .IsRequired();

        builder.Property(k => k.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(k => k.CooldownMinutes)
            .IsRequired()
            .HasDefaultValue(30);

        builder.Property(k => k.MinimumThreshold)
            .HasColumnType("decimal(18,2)");

        builder.Property(k => k.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(k => k.ModifiedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // New properties for enhanced KPI system
        builder.Property(k => k.KpiType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("success_rate");

        builder.Property(k => k.ScheduleConfiguration)
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(k => k.ThresholdValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(k => k.ComparisonOperator)
            .HasMaxLength(10);

        // Add check constraints
        builder.ToTable(t => t.HasCheckConstraint("CK_KPIs_Priority", "Priority IN (1, 2)"));
        builder.ToTable(t => t.HasCheckConstraint("CK_KPIs_KpiType",
            "KpiType IN ('success_rate', 'transaction_volume', 'threshold', 'trend_analysis')"));
        builder.ToTable(t => t.HasCheckConstraint("CK_KPIs_ComparisonOperator",
            "ComparisonOperator IS NULL OR ComparisonOperator IN ('gt', 'gte', 'lt', 'lte', 'eq')"));

        // Indexes
        builder.HasIndex(k => k.Indicator)
            .IsUnique()
            .HasDatabaseName("IX_KPIs_Indicator");

        builder.HasIndex(k => k.Owner)
            .HasDatabaseName("IX_KPIs_Owner");

        builder.HasIndex(k => k.IsActive)
            .HasDatabaseName("IX_KPIs_IsActive");

        builder.HasIndex(k => k.LastRun)
            .HasDatabaseName("IX_KPIs_LastRun");

        builder.HasIndex(k => k.KpiType)
            .HasDatabaseName("IX_KPIs_KpiType")
            .IncludeProperties(k => k.IsActive);

        // Relationships
        builder.HasMany(k => k.KpiContacts)
            .WithOne(kc => kc.KPI)
            .HasForeignKey(kc => kc.KpiId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(k => k.AlertLogs)
            .WithOne(a => a.KPI)
            .HasForeignKey(a => a.KpiId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(k => k.HistoricalData)
            .WithOne(h => h.KPI)
            .HasForeignKey(h => h.KpiId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
