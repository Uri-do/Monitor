using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MonitoringGrid.Core.Entities;
using MonitoringGrid.Core.ValueObjects;

namespace MonitoringGrid.Infrastructure.Data;

/// <summary>
/// Provides seed data for the monitoring system
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Seeds the database with initial data
    /// </summary>
    public static async Task SeedAsync(MonitoringContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await context.Contacts.AnyAsync() || await context.KPIs.AnyAsync())
        {
            return; // Database has been seeded
        }

        // Seed Contacts
        var contacts = new List<Contact>
        {
            new Contact
            {
                Name = "John Smith",
                Email = "john.smith@company.com",
                Phone = "+1-555-0101",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new Contact
            {
                Name = "Sarah Johnson",
                Email = "sarah.johnson@company.com",
                Phone = "+1-555-0102",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new Contact
            {
                Name = "Mike Wilson",
                Email = "mike.wilson@company.com",
                Phone = "+1-555-0103",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new Contact
            {
                Name = "Lisa Davis",
                Email = "lisa.davis@company.com",
                Phone = "+1-555-0104",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        };

        await context.Contacts.AddRangeAsync(contacts);
        await context.SaveChangesAsync();

        // Seed only real KPIs with actual stored procedures that exist in the database
        var now = DateTime.UtcNow;
        var kpis = new List<KPI>
        {
            new KPI
            {
                Indicator = "Transaction Success Rate",
                Owner = "Gavriel",
                Priority = 1, // Critical
                Frequency = 30, // Every 30 minutes
                ScheduleConfiguration = JsonSerializer.Serialize(ScheduleConfiguration.CreateInterval(30)), // Every 30 minutes
                Deviation = 5.0m,
                SpName = "[stats].[stp_MonitorTransactions]",
                SubjectTemplate = "Transaction success rate alert: {{deviation}}% deviation detected",
                DescriptionTemplate = "Transaction monitoring alert: Current success rate is {{current}}%, historical average is {{historical}}%. Deviation of {{deviation}}% detected.",
                IsActive = true,
                CooldownMinutes = 60,
                MinimumThreshold = 90,
                LastRun = null, // No previous runs - will be set when first executed
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        };

        await context.KPIs.AddRangeAsync(kpis);
        await context.SaveChangesAsync();

        // Assign contacts to the real KPI
        var kpiContacts = new List<KpiContact>
        {
            new KpiContact { KpiId = kpis[0].KpiId, ContactId = contacts[0].ContactId }, // Gavriel -> Transaction Success Rate
            new KpiContact { KpiId = kpis[0].KpiId, ContactId = contacts[1].ContactId }, // Sarah also gets Transaction alerts
        };

        await context.KpiContacts.AddRangeAsync(kpiContacts);
        await context.SaveChangesAsync();

        // Seed Configuration
        var configs = new List<Config>
        {
            new Config
            {
                ConfigKey = "EmailEnabled",
                ConfigValue = "true",
                Description = "Enable email notifications",
                ModifiedDate = DateTime.UtcNow
            },
            new Config
            {
                ConfigKey = "SmsEnabled",
                ConfigValue = "false",
                Description = "Enable SMS notifications",
                ModifiedDate = DateTime.UtcNow
            },
            new Config
            {
                ConfigKey = "MaxRetryAttempts",
                ConfigValue = "3",
                Description = "Maximum retry attempts for failed operations",
                ModifiedDate = DateTime.UtcNow
            },
            new Config
            {
                ConfigKey = "DefaultCooldownMinutes",
                ConfigValue = "30",
                Description = "Default cooldown period between alerts",
                ModifiedDate = DateTime.UtcNow
            }
        };

        await context.Config.AddRangeAsync(configs);
        await context.SaveChangesAsync();

        // Seed System Status
        var systemStatus = new SystemStatus
        {
            ServiceName = "MonitoringWorker",
            LastHeartbeat = DateTime.UtcNow,
            Status = "Running",
            ProcessedKpis = 0,
            AlertsSent = 0
        };

        await context.SystemStatus.AddAsync(systemStatus);
        await context.SaveChangesAsync();
    }
}
