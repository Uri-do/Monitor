-- Cleanup Scheduling Migration - Remove Legacy JSON Configuration
-- This script removes the old ScheduleConfiguration JSON field and migrates to clean SchedulerID relationship
USE [PopAI]
GO

PRINT 'Starting cleanup of legacy scheduling configuration...'

-- Step 1: Migrate existing indicators to use default schedulers based on their current configuration
-- This attempts to preserve existing scheduling behavior by mapping to appropriate schedulers

-- Create a temporary mapping of common intervals to scheduler IDs
DECLARE @DefaultSchedulerMapping TABLE (
    IntervalMinutes INT,
    SchedulerID INT
);

-- Populate mapping with existing schedulers
INSERT INTO @DefaultSchedulerMapping (IntervalMinutes, SchedulerID)
SELECT 5, SchedulerID FROM monitoring.Schedulers WHERE SchedulerName = 'Every 5 Minutes'
UNION ALL
SELECT 15, SchedulerID FROM monitoring.Schedulers WHERE SchedulerName = 'Every 15 Minutes'
UNION ALL
SELECT 30, SchedulerID FROM monitoring.Schedulers WHERE SchedulerName = 'Every 30 Minutes'
UNION ALL
SELECT 60, SchedulerID FROM monitoring.Schedulers WHERE SchedulerName = 'Every Hour'
UNION ALL
SELECT 120, SchedulerID FROM monitoring.Schedulers WHERE SchedulerName = 'Every 2 Hours'
UNION ALL
SELECT 360, SchedulerID FROM monitoring.Schedulers WHERE SchedulerName = 'Every 6 Hours';

-- Get the default hourly scheduler for fallback
DECLARE @DefaultSchedulerID INT;
SELECT @DefaultSchedulerID = SchedulerID 
FROM monitoring.Schedulers 
WHERE SchedulerName = 'Every Hour';

-- Step 2: Update indicators that don't have a SchedulerID assigned yet
-- Try to map based on legacy Frequency column if it exists
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'Frequency')
BEGIN
    PRINT 'Migrating indicators based on legacy Frequency column...'
    
    -- Update indicators with matching frequency intervals
    UPDATE i
    SET SchedulerID = COALESCE(m.SchedulerID, @DefaultSchedulerID)
    FROM monitoring.Indicators i
    LEFT JOIN @DefaultSchedulerMapping m ON i.Frequency = m.IntervalMinutes
    WHERE i.SchedulerID IS NULL;
    
    PRINT 'Migrated indicators to use scheduler relationships'
END
ELSE
BEGIN
    PRINT 'No legacy Frequency column found, assigning default scheduler to unassigned indicators...'
    
    -- Assign default scheduler to any indicators without a scheduler
    UPDATE monitoring.Indicators
    SET SchedulerID = @DefaultSchedulerID
    WHERE SchedulerID IS NULL;
END

-- Step 3: Remove the old ScheduleConfiguration JSON column if it exists
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'ScheduleConfiguration')
BEGIN
    PRINT 'Removing legacy ScheduleConfiguration column...'
    ALTER TABLE monitoring.Indicators DROP COLUMN ScheduleConfiguration;
    PRINT 'Removed ScheduleConfiguration column'
END

-- Step 4: Remove the old Frequency column if it exists (since we now use Schedulers)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'Frequency')
BEGIN
    PRINT 'Removing legacy Frequency column...'
    ALTER TABLE monitoring.Indicators DROP COLUMN Frequency;
    PRINT 'Removed Frequency column'
END

-- Step 4.1: Remove the old AverageOfCurrHour column if it exists (replaced with AverageLastDays)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'AverageOfCurrHour')
BEGIN
    PRINT 'Removing legacy AverageOfCurrHour column...'
    ALTER TABLE monitoring.Indicators DROP COLUMN AverageOfCurrHour;
    PRINT 'Removed AverageOfCurrHour column'
END

-- Step 4.2: Update AlertSuppressionRules to use IndicatorID instead of KpiId
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.AlertSuppressionRules') AND name = 'KpiId')
BEGIN
    PRINT 'Updating AlertSuppressionRules to use IndicatorID...'

    -- Add new IndicatorID column
    ALTER TABLE monitoring.AlertSuppressionRules ADD IndicatorID BIGINT NULL;

    -- Copy data from KpiId to IndicatorID (if any data exists)
    UPDATE monitoring.AlertSuppressionRules
    SET IndicatorID = KpiId
    WHERE KpiId IS NOT NULL;

    -- Drop old KpiId column
    ALTER TABLE monitoring.AlertSuppressionRules DROP COLUMN KpiId;

    PRINT 'Updated AlertSuppressionRules to use IndicatorID'
END

-- Step 5: Make SchedulerID required for active indicators
-- Add a check constraint to ensure active indicators have a scheduler
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID('monitoring.CK_Indicators_ActiveScheduler'))
BEGIN
    ALTER TABLE monitoring.Indicators ADD CONSTRAINT CK_Indicators_ActiveScheduler
        CHECK (IsActive = 0 OR SchedulerID IS NOT NULL);
    PRINT 'Added constraint to ensure active indicators have a scheduler'
END

-- Step 6: Update the enhanced view to reflect the clean schema
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID('monitoring.vw_IndicatorsWithSchedulers'))
    DROP VIEW monitoring.vw_IndicatorsWithSchedulers;
GO

CREATE VIEW monitoring.vw_IndicatorsWithSchedulers AS
SELECT 
    i.IndicatorID,
    i.IndicatorName,
    i.IndicatorCode,
    i.IndicatorDesc,
    i.CollectorID,
    i.CollectorItemName,
    i.Priority,
    i.LastMinutes,
    i.ThresholdType,
    i.ThresholdField,
    i.ThresholdComparison,
    i.ThresholdValue,
    i.OwnerContactID,
    i.AverageLastDays,
    i.IsActive,
    i.CreatedDate,
    i.ModifiedDate,
    i.LastRun,
    i.SchedulerID,
    
    -- Scheduler information
    s.SchedulerName,
    s.SchedulerDescription,
    s.ScheduleType,
    s.IntervalMinutes,
    s.CronExpression,
    s.ExecutionDateTime,
    s.StartDate,
    s.EndDate,
    s.Timezone,
    s.IsEnabled AS SchedulerEnabled,
    
    -- Computed schedule status
    CASE 
        WHEN i.IsActive = 1 AND s.IsEnabled = 1 THEN 'Active'
        WHEN i.IsActive = 1 AND s.IsEnabled = 0 THEN 'Indicator Active, Scheduler Disabled'
        WHEN i.IsActive = 0 AND s.IsEnabled = 1 THEN 'Indicator Disabled, Scheduler Active'
        WHEN i.IsActive = 0 AND s.IsEnabled = 0 THEN 'Both Disabled'
        WHEN s.SchedulerID IS NULL THEN 'No Scheduler Assigned'
        ELSE 'Unknown'
    END AS ScheduleStatus,
    
    -- Next execution calculation (simplified - actual calculation should be done in application layer)
    CASE 
        WHEN i.IsActive = 1 AND s.IsEnabled = 1 AND s.ScheduleType = 'interval' AND s.IntervalMinutes IS NOT NULL THEN
            DATEADD(MINUTE, s.IntervalMinutes, COALESCE(i.LastRun, GETUTCDATE()))
        ELSE NULL
    END AS NextExecutionTime
FROM monitoring.Indicators i
    LEFT JOIN monitoring.Schedulers s ON i.SchedulerID = s.SchedulerID;
GO

PRINT 'Updated enhanced Indicators view'

-- Step 7: Create stored procedure to get indicators due for execution
IF EXISTS (SELECT * FROM sys.procedures WHERE object_id = OBJECT_ID('monitoring.usp_GetDueIndicators'))
    DROP PROCEDURE monitoring.usp_GetDueIndicators;
GO

CREATE PROCEDURE monitoring.usp_GetDueIndicators
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        i.IndicatorID,
        i.IndicatorName,
        i.IndicatorCode,
        i.LastRun,
        s.SchedulerName,
        s.ScheduleType,
        s.IntervalMinutes,
        s.CronExpression,
        s.ExecutionDateTime
    FROM monitoring.Indicators i
        INNER JOIN monitoring.Schedulers s ON i.SchedulerID = s.SchedulerID
    WHERE i.IsActive = 1 
        AND s.IsEnabled = 1
        AND (
            -- Interval-based scheduling
            (s.ScheduleType = 'interval' 
             AND s.IntervalMinutes IS NOT NULL 
             AND (i.LastRun IS NULL OR GETUTCDATE() >= DATEADD(MINUTE, s.IntervalMinutes, i.LastRun)))
            OR
            -- One-time scheduling (due now)
            (s.ScheduleType = 'onetime' 
             AND s.ExecutionDateTime IS NOT NULL 
             AND GETUTCDATE() >= s.ExecutionDateTime
             AND (i.LastRun IS NULL OR i.LastRun < s.ExecutionDateTime))
            -- Note: Cron scheduling calculation is complex and should be handled in application layer
        )
    ORDER BY 
        CASE WHEN i.LastRun IS NULL THEN 0 ELSE 1 END, -- Never-run indicators first
        i.LastRun ASC; -- Then oldest last-run first
END
GO

PRINT 'Created usp_GetDueIndicators stored procedure'

-- Step 8: Clean up any orphaned data
-- Remove any scheduled jobs that reference non-existent indicators
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.ScheduledJobs') AND type in (N'U'))
BEGIN
    DELETE sj
    FROM monitoring.ScheduledJobs sj
    LEFT JOIN monitoring.Indicators i ON sj.KpiId = i.IndicatorID
    WHERE i.IndicatorID IS NULL;
    
    PRINT 'Cleaned up orphaned scheduled jobs'
END

-- Step 9: Update statistics
UPDATE STATISTICS monitoring.Indicators;
UPDATE STATISTICS monitoring.Schedulers;

PRINT 'Updated table statistics'

-- Step 10: Verify migration results
DECLARE @IndicatorCount INT, @ScheduledIndicatorCount INT, @SchedulerCount INT;

SELECT @IndicatorCount = COUNT(*) FROM monitoring.Indicators;
SELECT @ScheduledIndicatorCount = COUNT(*) FROM monitoring.Indicators WHERE SchedulerID IS NOT NULL;
SELECT @SchedulerCount = COUNT(*) FROM monitoring.Schedulers;

PRINT 'Migration Summary:'
PRINT '  Total Indicators: ' + CAST(@IndicatorCount AS NVARCHAR(10))
PRINT '  Indicators with Schedulers: ' + CAST(@ScheduledIndicatorCount AS NVARCHAR(10))
PRINT '  Total Schedulers: ' + CAST(@SchedulerCount AS NVARCHAR(10))

IF @IndicatorCount > 0 AND @ScheduledIndicatorCount = 0
BEGIN
    PRINT 'WARNING: No indicators have schedulers assigned. This may indicate a migration issue.'
END
ELSE
BEGIN
    PRINT 'Migration completed successfully!'
END

PRINT 'Cleanup of legacy scheduling configuration completed!'
