using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseApp.Core.Entities;
using EnterpriseApp.Core.Enums;

namespace EnterpriseApp.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for DomainEntity
/// </summary>
public class DomainEntityConfiguration : IEntityTypeConfiguration<DomainEntity>
{
    public void Configure(EntityTypeBuilder<DomainEntity> builder)
    {
        // Table configuration
        builder.ToTable("DomainEntities");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasComment("Name of the DomainEntity");

        builder.Property(e => e.Description)
            .HasMaxLength(1000)
            .HasComment("Description of the DomainEntity");

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment("Indicates if the DomainEntity is active");

        builder.Property(e => e.CreatedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the DomainEntity was created");

        builder.Property(e => e.ModifiedDate)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the DomainEntity was last modified");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100)
            .HasComment("Who created the DomainEntity");

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(100)
            .HasComment("Who last modified the DomainEntity");

        builder.Property(e => e.Category)
            .HasMaxLength(500)
            .HasComment("Category of the DomainEntity");

        builder.Property(e => e.Priority)
            .IsRequired()
            .HasDefaultValue(3)
            .HasComment("Priority level (1-5, where 1 is highest priority)");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasDefaultValue(DomainEntityStatus.Draft)
            .HasConversion<int>()
            .HasComment("Status of the DomainEntity");

        builder.Property(e => e.Tags)
            .HasMaxLength(500)
            .HasComment("Tags associated with this DomainEntity");

        builder.Property(e => e.ExternalId)
            .HasMaxLength(100)
            .HasComment("External reference ID if integrating with other systems");

        builder.Property(e => e.Metadata)
            .HasColumnType("nvarchar(max)")
            .HasComment("JSON metadata for flexible data storage");

        // Indexes
        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_DomainEntities_Name");

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_DomainEntities_Category");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_DomainEntities_Status");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_DomainEntities_IsActive");

        builder.HasIndex(e => e.CreatedDate)
            .HasDatabaseName("IX_DomainEntities_CreatedDate");

        builder.HasIndex(e => e.Priority)
            .HasDatabaseName("IX_DomainEntities_Priority");

        builder.HasIndex(e => e.ExternalId)
            .HasDatabaseName("IX_DomainEntities_ExternalId");

        // Relationships
        builder.HasMany(e => e.Items)
            .WithOne(i => i.DomainEntity)
            .HasForeignKey(i => i.DomainEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.AuditLogs)
            .WithOne(a => a.DomainEntity)
            .HasForeignKey(a => a.DomainEntityId)
            .OnDelete(DeleteBehavior.SetNull);

        // Check constraints
        builder.HasCheckConstraint("CK_DomainEntities_Priority", "[Priority] >= 1 AND [Priority] <= 5");

        // Table comment
        builder.HasComment("Domain entities representing the core business objects");
    }
}
