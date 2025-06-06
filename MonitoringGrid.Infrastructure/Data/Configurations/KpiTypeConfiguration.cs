using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for KpiType entity
/// </summary>
public class KpiTypeConfiguration : IEntityTypeConfiguration<KpiType>
{
    public void Configure(EntityTypeBuilder<KpiType> builder)
    {
        builder.ToTable("KpiTypes", "monitoring");

        builder.HasKey(kt => kt.KpiTypeId);

        builder.Property(kt => kt.KpiTypeId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(kt => kt.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(kt => kt.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(kt => kt.RequiredFields)
            .IsRequired()
            .HasColumnType("NVARCHAR(MAX)");

        builder.Property(kt => kt.DefaultStoredProcedure)
            .HasMaxLength(255);

        builder.Property(kt => kt.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(kt => kt.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(kt => kt.ModifiedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(kt => kt.Name)
            .HasDatabaseName("IX_KpiTypes_Name");

        builder.HasIndex(kt => kt.IsActive)
            .HasDatabaseName("IX_KpiTypes_IsActive");

        // Relationships
        builder.HasMany(kt => kt.KPIs)
            .WithOne()
            .HasForeignKey(k => k.KpiType)
            .HasPrincipalKey(kt => kt.KpiTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed data
        builder.HasData(
            new KpiType
            {
                KpiTypeId = "success_rate",
                Name = "Success Rate Monitoring",
                Description = "Monitors success percentages and compares them against historical averages. Ideal for tracking transaction success rates, API response rates, login success rates, and other percentage-based metrics.",
                RequiredFields = "[\"deviation\", \"lastMinutes\"]",
                DefaultStoredProcedure = "monitoring.usp_MonitorTransactions",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new KpiType
            {
                KpiTypeId = "transaction_volume",
                Name = "Transaction Volume Monitoring",
                Description = "Tracks transaction counts and compares them to historical patterns. Perfect for detecting unusual spikes or drops in activity, monitoring daily transactions, API calls, user registrations, and other count-based metrics.",
                RequiredFields = "[\"deviation\", \"minimumThreshold\", \"lastMinutes\"]",
                DefaultStoredProcedure = "monitoring.usp_MonitorTransactionVolume",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new KpiType
            {
                KpiTypeId = "threshold",
                Name = "Threshold Monitoring",
                Description = "Simple threshold-based monitoring that triggers alerts when values cross specified limits. Useful for monitoring system resources, queue lengths, error counts, response times, and other absolute value metrics.",
                RequiredFields = "[\"thresholdValue\", \"comparisonOperator\"]",
                DefaultStoredProcedure = "monitoring.usp_MonitorThreshold",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new KpiType
            {
                KpiTypeId = "trend_analysis",
                Name = "Trend Analysis",
                Description = "Analyzes trends over time to detect gradual changes or patterns. Excellent for capacity planning, performance degradation detection, user behavior analysis, and early warning systems for emerging issues.",
                RequiredFields = "[\"deviation\", \"lastMinutes\"]",
                DefaultStoredProcedure = "monitoring.usp_MonitorTrends",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        );
    }
}
