using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for WorkerTestExecutionHistory entity
/// </summary>
public class WorkerTestExecutionHistoryConfiguration : IEntityTypeConfiguration<WorkerTestExecutionHistory>
{
    public void Configure(EntityTypeBuilder<WorkerTestExecutionHistory> builder)
    {
        builder.ToTable("WorkerTestExecutionHistory", "monitoring");

        // Primary key
        builder.HasKey(e => e.TestExecutionHistoryID);
        builder.Property(e => e.TestExecutionHistoryID)
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(e => e.TestId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.TestType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.StartedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.CompletedAt);

        builder.Property(e => e.DurationMs)
            .IsRequired();

        builder.Property(e => e.Success)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(4000);

        builder.Property(e => e.IndicatorsProcessed)
            .IsRequired();

        builder.Property(e => e.SuccessfulExecutions)
            .IsRequired();

        builder.Property(e => e.FailedExecutions)
            .IsRequired();

        builder.Property(e => e.AverageExecutionTimeMs)
            .IsRequired();

        builder.Property(e => e.MemoryUsageBytes)
            .IsRequired();

        builder.Property(e => e.CpuUsagePercent)
            .IsRequired();

        builder.Property(e => e.AlertsTriggered)
            .IsRequired();

        builder.Property(e => e.WorkerCount)
            .IsRequired();

        builder.Property(e => e.ConcurrentWorkers)
            .IsRequired();

        builder.Property(e => e.TestConfiguration)
            .HasMaxLength(4000);

        builder.Property(e => e.PerformanceMetrics)
            .HasMaxLength(4000);

        builder.Property(e => e.DetailedResults);

        builder.Property(e => e.ExecutedBy)
            .HasMaxLength(100);

        builder.Property(e => e.ExecutionContext)
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("Manual");

        builder.Property(e => e.Metadata)
            .HasMaxLength(4000);

        // Indexes for performance
        builder.HasIndex(e => e.TestId)
            .HasDatabaseName("IX_WorkerTestExecutionHistory_TestId");

        builder.HasIndex(e => e.TestType)
            .HasDatabaseName("IX_WorkerTestExecutionHistory_TestType");

        builder.HasIndex(e => e.StartedAt)
            .HasDatabaseName("IX_WorkerTestExecutionHistory_StartedAt");

        builder.HasIndex(e => new { e.TestType, e.StartedAt })
            .HasDatabaseName("IX_WorkerTestExecutionHistory_TestType_StartedAt");

        builder.HasIndex(e => e.Success)
            .HasDatabaseName("IX_WorkerTestExecutionHistory_Success");
    }
}
