using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitoringGrid.Core.Entities;

namespace MonitoringGrid.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Contact entity
/// </summary>
public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts", "monitoring");

        builder.HasKey(c => c.ContactId);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Email)
            .HasMaxLength(255);

        builder.Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(c => c.ModifiedDate)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("IX_Contacts_Name");

        builder.HasIndex(c => c.Email)
            .HasDatabaseName("IX_Contacts_Email");

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("IX_Contacts_IsActive");

        // Relationships
        builder.HasMany(c => c.KpiContacts)
            .WithOne(kc => kc.Contact)
            .HasForeignKey(kc => kc.ContactId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
