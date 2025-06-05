using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for KpiContact entity
/// </summary>
public class KpiContactConfiguration : IEntityTypeConfiguration<KpiContact>
{
    public void Configure(EntityTypeBuilder<KpiContact> builder)
    {
        builder.ToTable("KpiContacts", "monitoring");

        // Composite primary key
        builder.HasKey(kc => new { kc.KpiId, kc.ContactId });

        // Relationships are configured in KPI and Contact configurations
        // This ensures proper foreign key constraints
    }
}
