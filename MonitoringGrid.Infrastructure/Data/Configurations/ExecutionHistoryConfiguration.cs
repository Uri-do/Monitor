using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ExecutionHistory entity
/// </summary>
public class ExecutionHistoryConfiguration : IEntityTypeConfiguration<ExecutionHistory>
{
    public void Configure(EntityTypeBuilder<ExecutionHistory> builder)
    {
        builder.ToTable("ExecutionHistory", "monitoring");

        // Primary key
        builder.HasKey(e => e.ExecutionHistoryID);
        builder.Property(e => e.ExecutionHistoryID)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(e => e.IndicatorID)
            .IsRequired();

        builder.Property(e => e.ExecutedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.DurationMs)
            .IsRequired();

        builder.Property(e => e.Success)
            .IsRequired();

        builder.Property(e => e.Result)
            .HasMaxLength(4000);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(4000);

        builder.Property(e => e.RecordCount);

        builder.Property(e => e.ExecutionContext)
            .HasMaxLength(100);

        builder.Property(e => e.ExecutedBy)
            .HasMaxLength(100);

        builder.Property(e => e.Metadata)
            .HasMaxLength(4000);

        // Relationships
        builder.HasOne(e => e.Indicator)
            .WithMany()
            .HasForeignKey(e => e.IndicatorID)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.IndicatorID)
            .HasDatabaseName("IX_ExecutionHistory_IndicatorID");

        builder.HasIndex(e => e.ExecutedAt)
            .HasDatabaseName("IX_ExecutionHistory_ExecutedAt");

        builder.HasIndex(e => new { e.IndicatorID, e.ExecutedAt })
            .HasDatabaseName("IX_ExecutionHistory_IndicatorID_ExecutedAt");

        builder.HasIndex(e => e.Success)
            .HasDatabaseName("IX_ExecutionHistory_Success");
    }
}
