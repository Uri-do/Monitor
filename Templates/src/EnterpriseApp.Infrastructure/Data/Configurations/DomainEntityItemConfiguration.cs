using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseApp.Core.Entities;

namespace EnterpriseApp.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for DomainEntityItem
/// </summary>
public class DomainEntityItemConfiguration : IEntityTypeConfiguration<DomainEntityItem>
{
    public void Configure(EntityTypeBuilder<DomainEntityItem> builder)
    {
        // Table configuration
        builder.ToTable("DomainEntityItems");

        // Primary key
        builder.HasKey(i => i.Id);

        // Properties
        builder.Property(i => i.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(i => i.DomainEntityId)
            .IsRequired()
            .HasComment("Foreign key to the parent DomainEntity");

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasComment("Name of the item");

        builder.Property(i => i.Description)
            .HasMaxLength(500)
            .HasComment("Description of the item");

        builder.Property(i => i.Value)
            .HasColumnType("decimal(18,2)")
            .HasComment("Value associated with the item");

        builder.Property(i => i.Quantity)
            .IsRequired()
            .HasDefaultValue(1)
            .HasComment("Quantity of the item");

        builder.Property(i => i.Unit)
            .HasMaxLength(50)
            .HasComment("Unit of measurement");

        builder.Property(i => i.SortOrder)
            .IsRequired()
            .HasDefaultValue(0)
            .HasComment("Order/sequence of the item");

        builder.Property(i => i.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indicates if the item is active");

        builder.Property(i => i.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the item was created");

        builder.Property(i => i.ModifiedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the item was last modified");

        builder.Property(i => i.CreatedBy)
            .HasMaxLength(100)
            .HasComment("Who created the item");

        builder.Property(i => i.ModifiedBy)
            .HasMaxLength(100)
            .HasComment("Who last modified the item");

        builder.Property(i => i.Metadata)
            .HasColumnType("nvarchar(max)")
            .HasComment("Additional metadata in JSON format");

        // Indexes
        builder.HasIndex(i => i.DomainEntityId)
            .HasDatabaseName("IX_DomainEntityItems_DomainEntityId");

        builder.HasIndex(i => i.Name)
            .HasDatabaseName("IX_DomainEntityItems_Name");

        builder.HasIndex(i => i.IsActive)
            .HasDatabaseName("IX_DomainEntityItems_IsActive");

        builder.HasIndex(i => i.SortOrder)
            .HasDatabaseName("IX_DomainEntityItems_SortOrder");

        builder.HasIndex(i => i.CreatedDate)
            .HasDatabaseName("IX_DomainEntityItems_CreatedDate");

        // Composite index for efficient queries
        builder.HasIndex(i => new { i.DomainEntityId, i.IsActive, i.SortOrder })
            .HasDatabaseName("IX_DomainEntityItems_DomainEntity_Active_Sort");

        // Relationships
        builder.HasOne(i => i.DomainEntity)
            .WithMany(e => e.Items)
            .HasForeignKey(i => i.DomainEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Check constraints
        builder.HasCheckConstraint("CK_DomainEntityItems_Quantity", "[Quantity] >= 0");
        builder.HasCheckConstraint("CK_DomainEntityItems_Value", "[Value] IS NULL OR [Value] >= 0");

        // Table comment
        builder.HasComment("Items related to domain entities");
    }
}
