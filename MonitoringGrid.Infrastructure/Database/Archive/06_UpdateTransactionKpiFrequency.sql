-- Update Transaction Success Rate KPI to use 100,000 minutes frequency
-- This will capture much more historical data to show actual transaction results

USE [PopAI]
GO

PRINT 'Updating Transaction Success Rate KPI frequency to 100,000 minutes...'

-- Update the KPI frequency to capture more historical data
UPDATE monitoring.KPIs 
SET 
    Frequency = 100000,  -- 100,000 minutes (approximately 69 days)
    ModifiedDate = GETUTCDATE()
WHERE Indicator = 'Transaction Success Rate'
    AND SpName = '[stats].[stp_MonitorTransactions]'

-- Verify the update
IF @@ROWCOUNT > 0
BEGIN
    PRINT 'Successfully updated Transaction Success Rate KPI frequency to 100,000 minutes'
    
    -- Display the updated KPI configuration
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
    
    PRINT ''
    PRINT '=== KPI Configuration Updated ==='
    PRINT 'The Transaction Success Rate KPI will now look back 100,000 minutes (approximately 69 days)'
    PRINT 'This should capture much more transaction data and show actual results instead of empty data.'
    PRINT ''
END
ELSE
BEGIN
    PRINT 'No KPI was updated. Please check if the Transaction Success Rate KPI exists.'
    
    -- Show existing KPIs for debugging
    PRINT 'Existing KPIs in the system:'
    SELECT 
        k.KpiId,
        k.Indicator,
        k.SpName,
        k.Frequency
    FROM monitoring.KPIs k
    ORDER BY k.KpiId
END

PRINT 'Update completed!'
