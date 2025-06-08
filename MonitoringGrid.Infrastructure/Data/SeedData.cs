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

        // Seed KPIs with realistic LastRun values to show countdown
        var now = DateTime.UtcNow;
        var kpis = new List<KPI>
        {
            new KPI
            {
                Indicator = "Daily Sales Revenue",
                Owner = "John Smith",
                Priority = 1, // Critical
                Frequency = 60, // Every hour
                ScheduleConfiguration = JsonSerializer.Serialize(ScheduleConfiguration.CreateInterval(60)), // Every hour
                Deviation = 15.0m,
                SpName = "sp_GetDailySalesRevenue",
                SubjectTemplate = "Sales Alert: {{indicator}} deviation detected",
                DescriptionTemplate = "Current: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%",
                IsActive = true,
                CooldownMinutes = 30,
                MinimumThreshold = 1000,
                LastRun = now.AddMinutes(-50), // Last ran 50 minutes ago, due in 10 minutes
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new KPI
            {
                Indicator = "Customer Satisfaction Score",
                Owner = "Sarah Johnson",
                Priority = 2, // High
                Frequency = 120, // Every 2 hours
                ScheduleConfiguration = JsonSerializer.Serialize(ScheduleConfiguration.CreateInterval(120)), // Every 2 hours
                Deviation = 10.0m,
                SpName = "sp_GetCustomerSatisfaction",
                SubjectTemplate = "Customer Alert: {{indicator}} deviation detected",
                DescriptionTemplate = "Current: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%",
                IsActive = true,
                CooldownMinutes = 60,
                MinimumThreshold = 80,
                LastRun = now.AddMinutes(-110), // Last ran 1.83 hours ago, due in 10 minutes
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new KPI
            {
                Indicator = "System Response Time",
                Owner = "Mike Wilson",
                Priority = 1, // Critical
                Frequency = 30, // Every 30 minutes
                ScheduleConfiguration = JsonSerializer.Serialize(ScheduleConfiguration.CreateInterval(30)), // Every 30 minutes
                Deviation = 20.0m,
                SpName = "sp_GetSystemResponseTime",
                SubjectTemplate = "Performance Alert: {{indicator}} deviation detected",
                DescriptionTemplate = "Current: {{current}}ms, Historical: {{historical}}ms, Deviation: {{deviation}}%",
                IsActive = true,
                CooldownMinutes = 15,
                MinimumThreshold = 500,
                LastRun = now.AddMinutes(-27), // Last ran 27 minutes ago, due in 3 minutes
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new KPI
            {
                Indicator = "Order Processing Rate",
                Owner = "Lisa Davis",
                Priority = 2, // High
                Frequency = 90, // Every 1.5 hours
                ScheduleConfiguration = JsonSerializer.Serialize(ScheduleConfiguration.CreateInterval(90)), // Every 1.5 hours
                Deviation = 12.0m,
                SpName = "sp_GetOrderProcessingRate",
                SubjectTemplate = "Operations Alert: {{indicator}} deviation detected",
                DescriptionTemplate = "Current: {{current}}, Historical: {{historical}}, Deviation: {{deviation}}%",
                IsActive = true,
                CooldownMinutes = 45,
                MinimumThreshold = 100,
                LastRun = now.AddMinutes(-85), // Last ran 1.42 hours ago, due in 5 minutes
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
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
                LastRun = now.AddMinutes(-29), // Last ran 29 minutes ago, due in 1 minute
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            },
            new KPI
            {
                Indicator = "API Error Rate",
                Owner = "Tech Team",
                Priority = 1, // Critical
                Frequency = 10, // Every 10 minutes
                ScheduleConfiguration = JsonSerializer.Serialize(ScheduleConfiguration.CreateInterval(10)), // Every 10 minutes
                Deviation = 8.0m,
                SpName = "sp_GetApiErrorRate",
                SubjectTemplate = "API Alert: {{indicator}} threshold exceeded",
                DescriptionTemplate = "API error rate: {{current}}%, threshold: {{threshold}}%, deviation: {{deviation}}%",
                IsActive = false, // Inactive to show variety
                CooldownMinutes = 20,
                MinimumThreshold = 2,
                LastRun = now.AddMinutes(-120), // Last ran 2 hours ago
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }
        };

        await context.KPIs.AddRangeAsync(kpis);
        await context.SaveChangesAsync();

        // Assign contacts to KPIs
        var kpiContacts = new List<KpiContact>
        {
            new KpiContact { KpiId = kpis[0].KpiId, ContactId = contacts[0].ContactId }, // John -> Sales
            new KpiContact { KpiId = kpis[1].KpiId, ContactId = contacts[1].ContactId }, // Sarah -> Customer Satisfaction
            new KpiContact { KpiId = kpis[2].KpiId, ContactId = contacts[2].ContactId }, // Mike -> System Response
            new KpiContact { KpiId = kpis[3].KpiId, ContactId = contacts[3].ContactId }, // Lisa -> Order Processing
            new KpiContact { KpiId = kpis[4].KpiId, ContactId = contacts[0].ContactId }, // John -> Transaction Success Rate
            new KpiContact { KpiId = kpis[5].KpiId, ContactId = contacts[2].ContactId }, // Mike -> API Error Rate
            new KpiContact { KpiId = kpis[0].KpiId, ContactId = contacts[3].ContactId }, // Lisa also gets Sales alerts
            new KpiContact { KpiId = kpis[2].KpiId, ContactId = contacts[0].ContactId }, // John also gets System alerts
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
