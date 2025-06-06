-- Add Transaction Monitoring KPI to the monitoring system (FIXED VERSION)
-- This script adds the [stats].[stp_MonitorTransactions] stored procedure as a KPI

USE [PopAI]
GO

-- Insert the Transaction Monitoring KPI
MERGE monitoring.KPIs AS target
USING (VALUES 
    ('Transaction Success Rate', 'Gavriel', 1, 30, 5.00, '[stats].[stp_MonitorTransactions]', 
     'Transaction success rate alert: {deviation}% deviation detected', 
     'Transaction monitoring alert: Current success rate is {current}%, historical average is {historical}%. Deviation of {deviation}% detected. Total transactions processed: {metadata}', 
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
        ModifiedDate = GETUTCDATE()
WHEN NOT MATCHED THEN
    INSERT (Indicator, Owner, Priority, Frequency, Deviation, SpName, SubjectTemplate, DescriptionTemplate, IsActive, LastRun, CooldownMinutes, MinimumThreshold, CreatedDate, ModifiedDate)
    VALUES (source.Indicator, source.Owner, source.Priority, source.Frequency, source.Deviation, source.SpName, source.SubjectTemplate, source.DescriptionTemplate, source.IsActive, source.LastRun, source.CooldownMinutes, source.MinimumThreshold, GETUTCDATE(), GETUTCDATE());

-- Get the KPI ID for the transaction monitoring KPI
DECLARE @KpiId INT
SELECT @KpiId = KpiId FROM monitoring.KPIs WHERE Indicator = 'Transaction Success Rate'

PRINT 'Transaction Monitoring KPI added/updated with ID: ' + CAST(@KpiId AS NVARCHAR(10))

-- Add some sample contacts for this KPI (optional)
-- You can modify these or add your own contacts
MERGE monitoring.Contacts AS target
USING (VALUES 
    ('Gavriel', 'gavriel@example.com', '+1234567890', 1),
    ('Tech Team', 'tech-team@example.com', '+1234567891', 1)
) AS source (Name, Email, Phone, IsActive)
ON target.Email = source.Email
WHEN NOT MATCHED THEN
    INSERT (Name, Email, Phone, IsActive, CreatedDate, ModifiedDate)
    VALUES (source.Name, source.Email, source.Phone, source.IsActive, GETUTCDATE(), GETUTCDATE());

-- Link contacts to the Transaction Monitoring KPI
DECLARE @GavrielContactId INT, @TechTeamContactId INT

SELECT @GavrielContactId = ContactId FROM monitoring.Contacts WHERE Email = 'gavriel@example.com'
SELECT @TechTeamContactId = ContactId FROM monitoring.Contacts WHERE Email = 'tech-team@example.com'

-- Add KPI-Contact relationships (FIXED - removed CreatedDate column)
IF @GavrielContactId IS NOT NULL
BEGIN
    MERGE monitoring.KpiContacts AS target
    USING (VALUES (@KpiId, @GavrielContactId)) AS source (KpiId, ContactId)
    ON target.KpiId = source.KpiId AND target.ContactId = source.ContactId
    WHEN NOT MATCHED THEN
        INSERT (KpiId, ContactId)
        VALUES (source.KpiId, source.ContactId);
    
    PRINT 'Linked Gavriel to Transaction Monitoring KPI'
END

IF @TechTeamContactId IS NOT NULL
BEGIN
    MERGE monitoring.KpiContacts AS target
    USING (VALUES (@KpiId, @TechTeamContactId)) AS source (KpiId, ContactId)
    ON target.KpiId = source.KpiId AND target.ContactId = source.ContactId
    WHEN NOT MATCHED THEN
        INSERT (KpiId, ContactId)
        VALUES (source.KpiId, source.ContactId);
    
    PRINT 'Linked Tech Team to Transaction Monitoring KPI'
END

-- Display the final KPI configuration
SELECT 
    k.KpiId,
    k.Indicator,
    k.Owner,
    k.Priority,
    k.Frequency,
    k.Deviation,
    k.SpName,
    k.SubjectTemplate,
    k.DescriptionTemplate,
    k.IsActive,
    k.LastRun,
    k.CooldownMinutes,
    k.MinimumThreshold,
    k.CreatedDate,
    k.ModifiedDate
FROM monitoring.KPIs k
WHERE k.Indicator = 'Transaction Success Rate'

-- Display linked contacts
SELECT 
    c.Name,
    c.Email,
    c.Phone,
    c.IsActive
FROM monitoring.KPIs k
INNER JOIN monitoring.KpiContacts kc ON k.KpiId = kc.KpiId
INNER JOIN monitoring.Contacts c ON kc.ContactId = c.ContactId
WHERE k.Indicator = 'Transaction Success Rate'

PRINT 'Transaction Monitoring KPI setup completed successfully!'
