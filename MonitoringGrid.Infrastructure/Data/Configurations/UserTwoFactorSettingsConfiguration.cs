using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Security;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for UserTwoFactorSettings entity
/// </summary>
public class UserTwoFactorSettingsConfiguration : IEntityTypeConfiguration<UserTwoFactorSettings>
{
    public void Configure(EntityTypeBuilder<UserTwoFactorSettings> builder)
    {
        builder.ToTable("UserTwoFactorSettings", "auth");

        builder.HasKey(utfs => utfs.UserId);

        builder.Property(utfs => utfs.UserId)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(utfs => utfs.IsEnabled)
            .IsRequired();

        builder.Property(utfs => utfs.Secret)
            .HasMaxLength(255);

        builder.Property(utfs => utfs.EnabledAt);

        // Configure RecoveryCodes as JSON
        builder.Property(utfs => utfs.RecoveryCodes)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            )
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(utfs => utfs.IsEnabled);
        builder.HasIndex(utfs => utfs.EnabledAt);
    }
}
