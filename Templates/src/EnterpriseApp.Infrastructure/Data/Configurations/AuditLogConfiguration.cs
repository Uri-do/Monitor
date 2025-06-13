using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EnterpriseApp.Core.Entities;
using EnterpriseApp.Core.Enums;

namespace EnterpriseApp.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for AuditLog
/// </summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Table configuration
        builder.ToTable("AuditLogs");

        // Primary key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("Name of the entity that was changed");

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment("ID of the entity that was changed");

        builder.Property(a => a.Action)
            .IsRequired()
            .HasConversion<int>()
            .HasComment("Action that was performed");

        builder.Property(a => a.ActionDescription)
            .HasMaxLength(500)
            .HasComment("Description of the action");

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(100)
            .HasComment("User who performed the action");

        builder.Property(a => a.Username)
            .HasMaxLength(100)
            .HasComment("Username of the user who performed the action");

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45)
            .HasComment("IP address from which the action was performed");

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500)
            .HasComment("User agent of the client");

        builder.Property(a => a.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .HasComment("When the action was performed");

        builder.Property(a => a.OldValues)
            .HasColumnType("nvarchar(max)")
            .HasComment("Old values before the change (JSON format)");

        builder.Property(a => a.NewValues)
            .HasColumnType("nvarchar(max)")
            .HasComment("New values after the change (JSON format)");

        builder.Property(a => a.Metadata)
            .HasColumnType("nvarchar(max)")
            .HasComment("Additional metadata about the change");

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(100)
            .HasComment("Correlation ID for tracking related operations");

        builder.Property(a => a.SessionId)
            .HasMaxLength(100)
            .HasComment("Session ID");

        builder.Property(a => a.Source)
            .HasMaxLength(100)
            .HasComment("Application or module that made the change");

        builder.Property(a => a.Severity)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Information")
            .HasComment("Severity level of the audit event");

        builder.Property(a => a.DomainEntityId)
            .HasComment("Foreign key to DomainEntity (if applicable)");

        // Indexes
        builder.HasIndex(a => new { a.EntityName, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");

        builder.HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

        builder.HasIndex(a => a.CorrelationId)
            .HasDatabaseName("IX_AuditLogs_CorrelationId");

        builder.HasIndex(a => a.SessionId)
            .HasDatabaseName("IX_AuditLogs_SessionId");

        builder.HasIndex(a => a.Severity)
            .HasDatabaseName("IX_AuditLogs_Severity");

        // Composite indexes for common queries
        builder.HasIndex(a => new { a.EntityName, a.Timestamp })
            .HasDatabaseName("IX_AuditLogs_EntityName_Timestamp");

        builder.HasIndex(a => new { a.UserId, a.Timestamp })
            .HasDatabaseName("IX_AuditLogs_UserId_Timestamp");

        builder.HasIndex(a => new { a.Action, a.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Action_Timestamp");

        // Relationships
        builder.HasOne(a => a.DomainEntity)
            .WithMany(e => e.AuditLogs)
            .HasForeignKey(a => a.DomainEntityId)
            .OnDelete(DeleteBehavior.SetNull);

        // Table comment
        builder.HasComment("Audit log entries for tracking changes to entities");
    }
}
