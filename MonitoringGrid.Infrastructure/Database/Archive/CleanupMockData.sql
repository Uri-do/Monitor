-- Cleanup Mock Data Script
-- This script removes all mock KPIs and keeps only real KPIs with actual stored procedures
USE [PopAI]
GO

PRINT '=== Cleaning up mock KPI data ==='
PRINT 'Removing mock KPIs and keeping only real KPIs with actual stored procedures...'
PRINT ''

-- First, remove KPI-Contact mappings for mock KPIs
DELETE FROM monitoring.KpiContacts 
WHERE KpiId IN (
    SELECT KpiId FROM monitoring.KPIs 
    WHERE Indicator IN ('Deposits', 'Settlement Companies', 'Transaction Volume', 'Country Deposits', 'White Label Performance')
)

PRINT '✅ Removed KPI-Contact mappings for mock KPIs'

-- Remove historical data for mock KPIs
DELETE FROM monitoring.HistoricalData 
WHERE KpiId IN (
    SELECT KpiId FROM monitoring.KPIs 
    WHERE Indicator IN ('Deposits', 'Settlement Companies', 'Transaction Volume', 'Country Deposits', 'White Label Performance')
)

PRINT '✅ Removed historical data for mock KPIs'

-- Remove alert logs for mock KPIs
DELETE FROM monitoring.AlertLogs 
WHERE KpiId IN (
    SELECT KpiId FROM monitoring.KPIs 
    WHERE Indicator IN ('Deposits', 'Settlement Companies', 'Transaction Volume', 'Country Deposits', 'White Label Performance')
)

PRINT '✅ Removed alert logs for mock KPIs'

-- Remove scheduled jobs for mock KPIs
DELETE FROM monitoring.ScheduledJobs 
WHERE KpiId IN (
    SELECT KpiId FROM monitoring.KPIs 
    WHERE Indicator IN ('Deposits', 'Settlement Companies', 'Transaction Volume', 'Country Deposits', 'White Label Performance')
)

PRINT '✅ Removed scheduled jobs for mock KPIs'

-- Finally, remove the mock KPIs themselves
DELETE FROM monitoring.KPIs 
WHERE Indicator IN ('Deposits', 'Settlement Companies', 'Transaction Volume', 'Country Deposits', 'White Label Performance')

PRINT '✅ Removed mock KPIs'

-- Reset the Transaction Success Rate KPI to ensure it's properly configured
UPDATE monitoring.KPIs 
SET 
    Owner = 'Gavriel',
    Priority = 1,
    Frequency = 30,
    Deviation = 5.00,
    SpName = '[stats].[stp_MonitorTransactions]',
    SubjectTemplate = 'Transaction success rate alert: {deviation}% deviation detected',
    DescriptionTemplate = 'Transaction monitoring alert: Current success rate is {current}%, historical average is {historical}%. Deviation of {deviation}% detected.',
    IsActive = 1,
    LastRun = NULL,
    CooldownMinutes = 60,
    MinimumThreshold = 90.00,
    ModifiedDate = GETUTCDATE(),
    -- Reset execution tracking fields
    IsCurrentlyRunning = 0,
    ExecutionStartTime = NULL,
    ExecutionContext = NULL
WHERE Indicator = 'Transaction Success Rate'

PRINT '✅ Reset Transaction Success Rate KPI configuration'

-- Ensure proper contacts are mapped to the real KPI
DECLARE @TransactionKpiId INT = (SELECT KpiId FROM monitoring.KPIs WHERE Indicator = 'Transaction Success Rate')
DECLARE @GavrielId INT = (SELECT ContactId FROM monitoring.Contacts WHERE Name = 'Gavriel')
DECLARE @TechTeamId INT = (SELECT ContactId FROM monitoring.Contacts WHERE Name = 'Tech Team')

-- Remove existing mappings for this KPI
DELETE FROM monitoring.KpiContacts WHERE KpiId = @TransactionKpiId

-- Add correct mappings
IF @TransactionKpiId IS NOT NULL AND @GavrielId IS NOT NULL
BEGIN
    INSERT INTO monitoring.KpiContacts (KpiId, ContactId)
    VALUES (@TransactionKpiId, @GavrielId)
    PRINT '✅ Mapped Transaction Success Rate KPI to Gavriel'
END

IF @TransactionKpiId IS NOT NULL AND @TechTeamId IS NOT NULL
BEGIN
    INSERT INTO monitoring.KpiContacts (KpiId, ContactId)
    VALUES (@TransactionKpiId, @TechTeamId)
    PRINT '✅ Mapped Transaction Success Rate KPI to Tech Team'
END

PRINT ''
PRINT '=== Cleanup Complete! ==='
PRINT 'Summary of changes:'
PRINT '- Removed all mock KPIs (Deposits, Settlement Companies, etc.)'
PRINT '- Removed associated historical data, alerts, and scheduled jobs'
PRINT '- Reset Transaction Success Rate KPI to proper configuration'
PRINT '- Ensured proper contact mappings for the real KPI'
PRINT ''
PRINT 'Your dashboard will now show only real data!'

-- Show remaining KPIs
PRINT ''
PRINT 'Remaining KPIs in the system:'
SELECT 
    KpiId,
    Indicator,
    Owner,
    SpName,
    IsActive,
    LastRun
FROM monitoring.KPIs
ORDER BY Indicator

GO
