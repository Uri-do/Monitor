-- Initial Configuration Data for Monitoring Grid
USE [PopAI]
GO

-- Insert initial configuration values
MERGE monitoring.Config AS target
USING (VALUES 
    ('SmsGateway', 'sms-gateway@example.com', 'Email address for SMS gateway service'),
    ('AdminEmail', 'admin@example.com', 'Administrator email for system notifications'),
    ('MaxParallelExecutions', '5', 'Maximum number of KPIs to process in parallel'),
    ('AlertRetryCount', '3', 'Number of retry attempts for failed alerts'),
    ('DefaultCooldownMinutes', '30', 'Default cooldown period between alerts'),
    ('EnableSms', 'true', 'Enable SMS notifications'),
    ('EnableEmail', 'true', 'Enable email notifications'),
    ('EnableHistoricalComparison', 'true', 'Enable historical data comparison'),
    ('HistoricalWeeksBack', '4', 'Number of weeks to look back for historical comparison'),
    ('ServiceHeartbeatMinutes', '5', 'Service heartbeat interval in minutes'),
    ('MaxAlertHistoryDays', '90', 'Maximum days to keep alert history'),
    ('BatchSize', '10', 'Batch size for processing KPIs'),
    ('DatabaseTimeoutSeconds', '30', 'Database command timeout in seconds')
) AS source (ConfigKey, ConfigValue, Description)
ON target.ConfigKey = source.ConfigKey
WHEN MATCHED THEN
    UPDATE SET 
        ConfigValue = source.ConfigValue,
        Description = source.Description,
        ModifiedDate = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (ConfigKey, ConfigValue, Description)
    VALUES (source.ConfigKey, source.ConfigValue, source.Description);
GO

-- Insert sample contacts
MERGE monitoring.Contacts AS target
USING (VALUES 
    ('Amnon', 'amnon@example.com', '+1234567890', 1),
    ('Itai', 'itai@example.com', '+1234567891', 1),
    ('Mike', 'mike@example.com', '+1234567892', 1),
    ('Gavriel', 'gavriel@example.com', '+1234567893', 1),
    ('Tech Team', 'tech-team@example.com', '+1234567894', 1)
) AS source (Name, Email, Phone, IsActive)
ON target.Name = source.Name
WHEN MATCHED THEN
    UPDATE SET 
        Email = source.Email,
        Phone = source.Phone,
        IsActive = source.IsActive,
        ModifiedDate = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (Name, Email, Phone, IsActive)
    VALUES (source.Name, source.Email, source.Phone, source.IsActive);
GO

-- Insert only real KPI configuration with actual stored procedure
MERGE monitoring.KPIs AS target
USING (VALUES
    ('Transaction Success Rate', 'Gavriel', 1, 30, 5.00, '[stats].[stp_MonitorTransactions]',
     'Transaction success rate alert: {deviation}% deviation detected',
     'Transaction monitoring alert: Current success rate is {current}%, historical average is {historical}%. Deviation of {deviation}% detected.',
     1, NULL, 60, 90.00)
) AS source (Indicator, Owner, Priority, Frequency, Deviation, SpName, SubjectTemplate, DescriptionTemplate, IsActive, LastRun, CooldownMinutes, MinimumThreshold)
ON target.Indicator = source.Indicator
WHEN MATCHED THEN
    UPDATE SET 
        Owner = source.Owner,
        Priority = source.Priority,
        Frequency = source.Frequency,
        Deviation = source.Deviation,
        SpName = source.SpName,
        SubjectTemplate = source.SubjectTemplate,
        DescriptionTemplate = source.DescriptionTemplate,
        IsActive = source.IsActive,
        CooldownMinutes = source.CooldownMinutes,
        MinimumThreshold = source.MinimumThreshold,
        ModifiedDate = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (Indicator, Owner, Priority, Frequency, Deviation, SpName, SubjectTemplate, DescriptionTemplate, IsActive, LastRun, CooldownMinutes, MinimumThreshold)
    VALUES (source.Indicator, source.Owner, source.Priority, source.Frequency, source.Deviation, source.SpName, source.SubjectTemplate, source.DescriptionTemplate, source.IsActive, source.LastRun, source.CooldownMinutes, source.MinimumThreshold);
GO

-- Map KPIs to contacts
DECLARE @TransactionKpiId INT = (SELECT KpiId FROM monitoring.KPIs WHERE Indicator = 'Transaction Success Rate')

DECLARE @AmnonId INT = (SELECT ContactId FROM monitoring.Contacts WHERE Name = 'Amnon')
DECLARE @ItaiId INT = (SELECT ContactId FROM monitoring.Contacts WHERE Name = 'Itai')
DECLARE @MikeId INT = (SELECT ContactId FROM monitoring.Contacts WHERE Name = 'Mike')
DECLARE @GavrielId INT = (SELECT ContactId FROM monitoring.Contacts WHERE Name = 'Gavriel')
DECLARE @TechTeamId INT = (SELECT ContactId FROM monitoring.Contacts WHERE Name = 'Tech Team')

-- Insert KPI-Contact mappings for the real KPI only
MERGE monitoring.KpiContacts AS target
USING (VALUES
    (@TransactionKpiId, @GavrielId),
    (@TransactionKpiId, @TechTeamId)
) AS source (KpiId, ContactId)
ON target.KpiId = source.KpiId AND target.ContactId = source.ContactId
WHEN NOT MATCHED THEN
    INSERT (KpiId, ContactId)
    VALUES (source.KpiId, source.ContactId);
GO

-- Initialize system status
MERGE monitoring.SystemStatus AS target
USING (VALUES 
    ('MonitoringWorker', SYSUTCDATETIME(), 'Initialized', NULL, 0, 0)
) AS source (ServiceName, LastHeartbeat, Status, ErrorMessage, ProcessedKpis, AlertsSent)
ON target.ServiceName = source.ServiceName
WHEN MATCHED THEN
    UPDATE SET 
        LastHeartbeat = source.LastHeartbeat,
        Status = source.Status,
        ErrorMessage = source.ErrorMessage
WHEN NOT MATCHED THEN
    INSERT (ServiceName, LastHeartbeat, Status, ErrorMessage, ProcessedKpis, AlertsSent)
    VALUES (source.ServiceName, source.LastHeartbeat, source.Status, source.ErrorMessage, source.ProcessedKpis, source.AlertsSent);
GO

PRINT 'Initial configuration data inserted successfully!'
PRINT 'KPIs configured: ' + CAST((SELECT COUNT(*) FROM monitoring.KPIs) AS VARCHAR(10))
PRINT 'Contacts configured: ' + CAST((SELECT COUNT(*) FROM monitoring.Contacts) AS VARCHAR(10))
PRINT 'Configuration entries: ' + CAST((SELECT COUNT(*) FROM monitoring.Config) AS VARCHAR(10))
