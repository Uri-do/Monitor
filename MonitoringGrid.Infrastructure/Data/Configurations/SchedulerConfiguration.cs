using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Entity Framework configuration for Scheduler entity
    /// </summary>
    public class SchedulerConfiguration : IEntityTypeConfiguration<Scheduler>
    {
        public void Configure(EntityTypeBuilder<Scheduler> builder)
        {
            // Table configuration
            builder.ToTable("Schedulers", "monitoring");

            // Primary key
            builder.HasKey(s => s.SchedulerID);

            // Properties
            builder.Property(s => s.SchedulerID)
                .ValueGeneratedOnAdd();

            builder.Property(s => s.SchedulerName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.SchedulerDescription)
                .HasMaxLength(500);

            builder.Property(s => s.ScheduleType)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(s => s.CronExpression)
                .HasMaxLength(255);

            builder.Property(s => s.Timezone)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("UTC");

            builder.Property(s => s.IsEnabled)
                .HasDefaultValue(true);

            builder.Property(s => s.CreatedDate)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(s => s.CreatedBy)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("system");

            builder.Property(s => s.ModifiedDate)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(s => s.ModifiedBy)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("system");

            // Constraints using modern ToTable syntax
            builder.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Schedulers_IntervalMinutes",
                    "(ScheduleType = 'interval' AND IntervalMinutes IS NOT NULL AND IntervalMinutes > 0) OR (ScheduleType != 'interval')");
                t.HasCheckConstraint("CK_Schedulers_CronExpression",
                    "(ScheduleType = 'cron' AND CronExpression IS NOT NULL) OR (ScheduleType != 'cron')");
                t.HasCheckConstraint("CK_Schedulers_ExecutionDateTime",
                    "(ScheduleType = 'onetime' AND ExecutionDateTime IS NOT NULL) OR (ScheduleType != 'onetime')");
                t.HasCheckConstraint("CK_Schedulers_ScheduleType",
                    "ScheduleType IN ('interval', 'cron', 'onetime')");
            });

            // Indexes
            builder.HasIndex(s => new { s.ScheduleType, s.IsEnabled })
                .HasDatabaseName("IX_Schedulers_ScheduleType_IsEnabled")
                .IncludeProperties(s => new { s.SchedulerName, s.IntervalMinutes, s.CronExpression });

            builder.HasIndex(s => s.SchedulerName)
                .HasDatabaseName("IX_Schedulers_SchedulerName");

            builder.HasIndex(s => s.IsEnabled)
                .HasDatabaseName("IX_Schedulers_IsEnabled");

            // Relationships
            builder.HasMany(s => s.Indicators)
                .WithOne(i => i.Scheduler)
                .HasForeignKey(i => i.SchedulerID)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
