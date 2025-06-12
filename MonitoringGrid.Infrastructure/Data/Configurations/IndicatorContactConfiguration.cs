using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for IndicatorContact entity
/// </summary>
public class IndicatorContactConfiguration : IEntityTypeConfiguration<IndicatorContact>
{
    public void Configure(EntityTypeBuilder<IndicatorContact> builder)
    {
        builder.ToTable("IndicatorContacts", "monitoring");

        builder.HasKey(ic => new { ic.IndicatorId, ic.ContactId });

        builder.Property(ic => ic.IndicatorId)
            .IsRequired();

        builder.Property(ic => ic.ContactId)
            .IsRequired();

        // Relationships
        builder.HasOne(ic => ic.Indicator)
            .WithMany(i => i.IndicatorContacts)
            .HasForeignKey(ic => ic.IndicatorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ic => ic.Contact)
            .WithMany()
            .HasForeignKey(ic => ic.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ic => ic.IndicatorId)
            .HasDatabaseName("IX_IndicatorContacts_IndicatorId");

        builder.HasIndex(ic => ic.ContactId)
            .HasDatabaseName("IX_IndicatorContacts_ContactId");
    }
}
