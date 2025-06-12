using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ScheduledJob entity
/// </summary>
public class ScheduledJobConfiguration : IEntityTypeConfiguration<ScheduledJob>
{
    public void Configure(EntityTypeBuilder<ScheduledJob> builder)
    {
        builder.ToTable("ScheduledJobs", "monitoring");

        builder.HasKey(sj => sj.JobId);

        builder.Property(sj => sj.JobId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sj => sj.IndicatorID)
            .IsRequired();

        builder.Property(sj => sj.JobName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(sj => sj.JobGroup)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue("KPI_JOBS");

        builder.Property(sj => sj.TriggerName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(sj => sj.TriggerGroup)
            .IsRequired()
            .HasMaxLength(255)
            .HasDefaultValue("KPI_TRIGGERS");

        builder.Property(sj => sj.CronExpression)
            .HasMaxLength(255);

        builder.Property(sj => sj.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(sj => sj.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(sj => sj.ModifiedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(sj => sj.IndicatorID)
            .HasDatabaseName("IX_ScheduledJobs_IndicatorID")
            .IncludeProperties(sj => sj.IsActive);

        builder.HasIndex(sj => sj.NextFireTime)
            .HasDatabaseName("IX_ScheduledJobs_NextFireTime")
            .HasFilter("IsActive = 1");

        builder.HasIndex(sj => new { sj.JobName, sj.JobGroup })
            .IsUnique()
            .HasDatabaseName("IX_ScheduledJobs_JobName_JobGroup");

        builder.HasIndex(sj => new { sj.TriggerName, sj.TriggerGroup })
            .IsUnique()
            .HasDatabaseName("IX_ScheduledJobs_TriggerName_TriggerGroup");

        // Relationships
        builder.HasOne(sj => sj.Indicator)
            .WithMany()
            .HasForeignKey(sj => sj.IndicatorID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
