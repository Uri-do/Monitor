using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for SystemStatus entity
/// </summary>
public class SystemStatusConfiguration : IEntityTypeConfiguration<SystemStatus>
{
    public void Configure(EntityTypeBuilder<SystemStatus> builder)
    {
        builder.ToTable("SystemStatus", "monitoring");

        builder.HasKey(s => s.StatusId);

        builder.Property(s => s.ServiceName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.LastHeartbeat)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.ErrorMessage)
            .HasColumnType("nvarchar(max)");

        builder.Property(s => s.ProcessedKpis)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.AlertsSent)
            .IsRequired()
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(s => s.ServiceName)
            .IsUnique()
            .HasDatabaseName("IX_SystemStatus_ServiceName");

        builder.HasIndex(s => s.LastHeartbeat)
            .HasDatabaseName("IX_SystemStatus_LastHeartbeat");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_SystemStatus_Status");
    }
}
