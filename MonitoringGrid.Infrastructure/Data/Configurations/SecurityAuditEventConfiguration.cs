using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Security;
using System.Text.Json;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

public class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEvent>
{
    public void Configure(EntityTypeBuilder<SecurityAuditEvent> builder)
    {
        builder.ToTable("SecurityAuditEvents");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasMaxLength(50);

        builder.Property(e => e.Username)
            .HasMaxLength(100);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.Resource)
            .HasMaxLength(200);

        builder.Property(e => e.Action)
            .HasMaxLength(100);

        builder.Property(e => e.IsSuccess)
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(1000);

        // Configure AdditionalData as JSON column
        builder.Property(e => e.AdditionalData)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.Property(e => e.Severity)
            .HasMaxLength(50)
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => new { e.EventType, e.Timestamp });
        builder.HasIndex(e => new { e.UserId, e.Timestamp });
    }
}
