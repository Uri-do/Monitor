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

        // Primary Key
        builder.HasKey(ic => ic.IndicatorContactId);

        builder.Property(ic => ic.IndicatorContactId)
            .HasColumnName("IndicatorContactID")
            .ValueGeneratedOnAdd();

        // Required properties
        builder.Property(ic => ic.IndicatorID)
            .IsRequired();

        builder.Property(ic => ic.ContactId)
            .HasColumnName("ContactID")
            .IsRequired();

        builder.Property(ic => ic.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(ic => ic.CreatedBy)
            .HasMaxLength(100);

        builder.Property(ic => ic.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Foreign Key Relationships
        builder.HasOne(ic => ic.Indicator)
            .WithMany(i => i.IndicatorContacts)
            .HasForeignKey(ic => ic.IndicatorID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ic => ic.Contact)
            .WithMany(c => c.IndicatorContacts)
            .HasForeignKey(ic => ic.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint to prevent duplicate indicator-contact relationships
        builder.HasIndex(ic => new { ic.IndicatorID, ic.ContactId })
            .IsUnique()
            .HasDatabaseName("UQ_IndicatorContacts_IndicatorID_ContactId");

        // Performance indexes
        builder.HasIndex(ic => ic.IndicatorID)
            .HasDatabaseName("IX_IndicatorContacts_IndicatorID");

        builder.HasIndex(ic => ic.ContactId)
            .HasDatabaseName("IX_IndicatorContacts_ContactId");

        builder.HasIndex(ic => ic.IsActive)
            .HasDatabaseName("IX_IndicatorContacts_IsActive");
    }
}
