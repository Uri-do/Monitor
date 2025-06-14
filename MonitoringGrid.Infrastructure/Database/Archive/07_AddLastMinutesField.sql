-- Add LastMinutes field to KPIs table
-- This field will control how far back in time to look for data when executing KPIs

USE [PopAI]
GO

PRINT 'Adding LastMinutes field to monitoring.KPIs table...'

-- Add the LastMinutes column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'LastMinutes')
BEGIN
    ALTER TABLE monitoring.KPIs 
    ADD LastMinutes INT NOT NULL DEFAULT 1440 -- Default to 24 hours (1440 minutes)
    
    PRINT '✅ LastMinutes column added successfully'
END
ELSE
BEGIN
    PRINT '⚠️  LastMinutes column already exists'
END

GO

-- Update existing KPIs with appropriate LastMinutes values based on their type
PRINT 'Updating existing KPIs with appropriate LastMinutes values...'

-- Transaction Success Rate KPI should use a longer window (100,000 minutes as we configured)
UPDATE monitoring.KPIs
SET LastMinutes = 100000, ModifiedDate = GETUTCDATE()
WHERE Indicator = 'Transaction Success Rate'
    AND SpName = '[stats].[stp_MonitorTransactions]'

-- Other KPIs can use shorter windows based on their frequency
UPDATE monitoring.KPIs
SET LastMinutes = CASE
    WHEN Frequency <= 30 THEN 1440      -- For frequent checks, look back 24 hours
    WHEN Frequency <= 120 THEN 2880     -- For hourly checks, look back 48 hours
    WHEN Frequency <= 1440 THEN 10080   -- For daily checks, look back 1 week
    ELSE 43200                          -- For longer intervals, look back 30 days
END,
ModifiedDate = GETUTCDATE()
WHERE Indicator != 'Transaction Success Rate'
    AND LastMinutes = 1440  -- Only update if still at default

PRINT 'Updated existing KPIs with appropriate LastMinutes values'

-- Display the updated KPI configuration
SELECT
    k.KpiId,
    k.Indicator,
    k.Owner,
    k.Frequency,
    k.LastMinutes,
    k.Deviation,
    k.SpName,
    k.IsActive,
    k.ModifiedDate
FROM monitoring.KPIs k
ORDER BY k.KpiId

PRINT ''
PRINT '=== LastMinutes Field Added Successfully ==='
PRINT 'KPIs now have configurable time windows for data analysis:'
PRINT '- LastMinutes: Controls how far back to look for current data'
PRINT '- Frequency: Controls how often the KPI runs'
PRINT '- Transaction Success Rate KPI: 100,000 minutes (69 days)'
PRINT '- Other KPIs: Scaled based on their frequency'
PRINT ''
