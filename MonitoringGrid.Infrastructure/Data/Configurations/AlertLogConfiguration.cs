using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for AlertLog entity
/// </summary>
public class AlertLogConfiguration : IEntityTypeConfiguration<AlertLog>
{
    public void Configure(EntityTypeBuilder<AlertLog> builder)
    {
        builder.ToTable("AlertLogs", "monitoring");

        builder.HasKey(a => a.AlertId);

        builder.Property(a => a.IndicatorId)
            .IsRequired();

        builder.Property(a => a.TriggerTime)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.Details)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.SentVia)
            .IsRequired();

        builder.Property(a => a.SentTo)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.CurrentValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.HistoricalValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.DeviationPercent)
            .HasColumnType("decimal(5,2)");

        builder.Property(a => a.IsResolved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.ResolvedBy)
            .HasMaxLength(100);

        builder.Property(a => a.ResolvedTime);

        builder.Property(a => a.Subject)
            .HasMaxLength(500);

        builder.Property(a => a.Description)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.ResolutionNotes)
            .HasMaxLength(1000);

        // Ignore computed/alias properties
        builder.Ignore(a => a.DeviationPercentage);
        builder.Ignore(a => a.ResolvedAt);
        builder.Ignore(a => a.AlertLogId);

        // Indexes
        builder.HasIndex(a => a.IndicatorId)
            .HasDatabaseName("IX_AlertLogs_IndicatorId");

        builder.HasIndex(a => a.TriggerTime)
            .HasDatabaseName("IX_AlertLogs_TriggerTime");

        builder.HasIndex(a => a.IsResolved)
            .HasDatabaseName("IX_AlertLogs_IsResolved");

        builder.HasIndex(a => new { a.IndicatorId, a.TriggerTime })
            .HasDatabaseName("IX_AlertLogs_IndicatorId_TriggerTime");

        // Check constraint for SentVia
        builder.ToTable(t => t.HasCheckConstraint("CK_AlertLogs_SentVia", "SentVia IN (1, 2, 3)"));

        // Relationship with Indicator
        builder.HasOne(a => a.Indicator)
            .WithMany()
            .HasForeignKey(a => a.IndicatorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
