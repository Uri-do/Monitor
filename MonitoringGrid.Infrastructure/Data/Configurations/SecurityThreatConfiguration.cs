using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Security;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for SecurityThreat entity
/// </summary>
public class SecurityThreatConfiguration : IEntityTypeConfiguration<SecurityThreat>
{
    public void Configure(EntityTypeBuilder<SecurityThreat> builder)
    {
        builder.ToTable("SecurityThreats", "auth");

        builder.HasKey(st => st.ThreatId);

        builder.Property(st => st.ThreatId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(st => st.ThreatType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(st => st.Severity)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(st => st.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(st => st.UserId)
            .HasMaxLength(50);

        builder.Property(st => st.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(st => st.DetectedAt)
            .IsRequired();

        builder.Property(st => st.IsResolved)
            .IsRequired();

        builder.Property(st => st.ResolvedAt);

        builder.Property(st => st.Resolution)
            .HasMaxLength(1000);

        // Configure ThreatData as JSON
        builder.Property(st => st.ThreatData)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
            )
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(st => st.ThreatType);
        builder.HasIndex(st => st.Severity);
        builder.HasIndex(st => st.UserId);
        builder.HasIndex(st => st.IpAddress);
        builder.HasIndex(st => st.DetectedAt);
        builder.HasIndex(st => st.IsResolved);
    }
}
