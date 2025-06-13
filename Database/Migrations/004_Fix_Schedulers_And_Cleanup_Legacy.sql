-- =============================================
-- Migration Script: Fix Schedulers System and Clean Legacy Schedule Configuration
-- Description: Fixes the Schedulers view issue and removes legacy schedule columns
-- Author: System Migration - Frontend Deep Cleanup
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

PRINT '🔧 FIXING SCHEDULERS SYSTEM AND CLEANING LEGACY CONFIGURATION'
PRINT '============================================================='
PRINT ''

-- Check if we're connected to the correct database
IF DB_NAME() != 'PopAI'
BEGIN
    PRINT '❌ ERROR: Not connected to PopAI database!'
    PRINT 'Current database: ' + DB_NAME()
    RETURN
END

PRINT '✅ Connected to PopAI database'
PRINT ''

-- Begin transaction for safety
BEGIN TRANSACTION FixSchedulersAndCleanup

BEGIN TRY
    -- 1. Fix missing ModifiedDate column in Indicators table
    PRINT '🔧 1. FIXING INDICATORS TABLE STRUCTURE'
    PRINT '======================================'
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'ModifiedDate')
    BEGIN
        PRINT '📋 Adding missing ModifiedDate column to Indicators table...'
        ALTER TABLE monitoring.Indicators ADD ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
        PRINT '✅ ModifiedDate column added successfully'
    END
    ELSE
    BEGIN
        PRINT '✅ ModifiedDate column already exists'
    END
    
    -- Also ensure CreatedDate exists
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'CreatedDate')
    BEGIN
        PRINT '📋 Adding missing CreatedDate column to Indicators table...'
        ALTER TABLE monitoring.Indicators ADD CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
        PRINT '✅ CreatedDate column added successfully'
    END
    ELSE
    BEGIN
        PRINT '✅ CreatedDate column already exists'
    END
    
    PRINT ''

    -- 2. Recreate the Indicators view with proper column references
    PRINT '🔧 2. RECREATING INDICATORS VIEW'
    PRINT '==============================='
    
    -- Drop existing view if it exists
    IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID('monitoring.vw_IndicatorsWithSchedulers'))
    BEGIN
        DROP VIEW monitoring.vw_IndicatorsWithSchedulers
        PRINT '🗑️ Dropped existing view'
    END
    
    -- Create the corrected view
    EXEC('
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

        -- Computed schedule display text
        CASE s.ScheduleType
            WHEN ''interval'' THEN ''Every '' + CAST(s.IntervalMinutes AS NVARCHAR(10)) + '' minutes''
            WHEN ''cron'' THEN ''Cron: '' + s.CronExpression
            WHEN ''onetime'' THEN ''One-time: '' + FORMAT(s.ExecutionDateTime, ''yyyy-MM-dd HH:mm'')
            ELSE ''Unknown''
        END AS ScheduleDisplayText,

        -- Computed schedule status
        CASE
            WHEN i.IsActive = 1 AND s.IsEnabled = 1 THEN ''Active''
            WHEN i.IsActive = 1 AND s.IsEnabled = 0 THEN ''Indicator Active, Scheduler Disabled''
            WHEN i.IsActive = 0 AND s.IsEnabled = 1 THEN ''Indicator Disabled, Scheduler Active''
            WHEN i.IsActive = 0 AND s.IsEnabled = 0 THEN ''Both Disabled''
            WHEN s.SchedulerID IS NULL THEN ''No Scheduler Assigned''
            ELSE ''Unknown''
        END AS ScheduleStatus
    FROM monitoring.Indicators i
        LEFT JOIN monitoring.Schedulers s ON i.SchedulerID = s.SchedulerID
    ')
    
    PRINT '✅ Recreated vw_IndicatorsWithSchedulers view successfully'
    PRINT ''

    -- 3. Clean up legacy schedule configuration columns
    PRINT '🧹 3. CLEANING LEGACY SCHEDULE CONFIGURATION'
    PRINT '==========================================='
    
    -- Create backup table for legacy data
    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.IndicatorLegacyScheduleBackup') AND type in (N'U'))
    BEGIN
        CREATE TABLE monitoring.IndicatorLegacyScheduleBackup (
            IndicatorID BIGINT NOT NULL,
            IndicatorName NVARCHAR(255) NOT NULL,
            AverageOfCurrHour BIT NULL,
            ScheduleConfiguration NVARCHAR(MAX) NULL,
            BackupDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            BackupReason NVARCHAR(255) NOT NULL DEFAULT 'Legacy schedule configuration cleanup'
        )
        PRINT '✅ Created backup table: monitoring.IndicatorLegacyScheduleBackup'
    END
    
    -- Backup and remove AverageOfCurrHour column
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'AverageOfCurrHour')
    BEGIN
        PRINT '📋 Backing up and removing AverageOfCurrHour column...'
        
        -- Backup data
        INSERT INTO monitoring.IndicatorLegacyScheduleBackup (IndicatorID, IndicatorName, AverageOfCurrHour)
        SELECT IndicatorID, IndicatorName, AverageOfCurrHour
        FROM monitoring.Indicators
        WHERE AverageOfCurrHour IS NOT NULL
        
        DECLARE @avgBackupCount INT = @@ROWCOUNT
        PRINT '💾 Backed up ' + CAST(@avgBackupCount AS NVARCHAR(10)) + ' records with AverageOfCurrHour data'
        
        -- Remove the column
        ALTER TABLE monitoring.Indicators DROP COLUMN AverageOfCurrHour
        PRINT '✅ AverageOfCurrHour column removed successfully'
    END
    ELSE
    BEGIN
        PRINT '✅ AverageOfCurrHour column not found (already removed or never existed)'
    END
    
    -- Backup and remove ScheduleConfiguration column
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'ScheduleConfiguration')
    BEGIN
        PRINT '📋 Backing up and removing ScheduleConfiguration column...'
        
        -- Add column to backup table if needed
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.IndicatorLegacyScheduleBackup') AND name = 'ScheduleConfiguration')
        BEGIN
            ALTER TABLE monitoring.IndicatorLegacyScheduleBackup ADD ScheduleConfiguration NVARCHAR(MAX) NULL
        END
        
        -- Backup data
        MERGE monitoring.IndicatorLegacyScheduleBackup AS target
        USING (
            SELECT IndicatorID, IndicatorName, ScheduleConfiguration
            FROM monitoring.Indicators
            WHERE ScheduleConfiguration IS NOT NULL
        ) AS source ON target.IndicatorID = source.IndicatorID
        WHEN MATCHED THEN
            UPDATE SET ScheduleConfiguration = source.ScheduleConfiguration
        WHEN NOT MATCHED THEN
            INSERT (IndicatorID, IndicatorName, ScheduleConfiguration)
            VALUES (source.IndicatorID, source.IndicatorName, source.ScheduleConfiguration);
        
        DECLARE @scheduleBackupCount INT = @@ROWCOUNT
        PRINT '💾 Backed up ' + CAST(@scheduleBackupCount AS NVARCHAR(10)) + ' records with ScheduleConfiguration data'
        
        -- Remove the column
        ALTER TABLE monitoring.Indicators DROP COLUMN ScheduleConfiguration
        PRINT '✅ ScheduleConfiguration column removed successfully'
    END
    ELSE
    BEGIN
        PRINT '✅ ScheduleConfiguration column not found (already removed or never existed)'
    END
    
    PRINT ''

    -- 4. Verify the final state
    PRINT '🔍 4. VERIFYING FINAL STATE'
    PRINT '=========================='
    
    -- Check that legacy columns are gone
    DECLARE @legacyColumnCount INT = 0
    SELECT @legacyColumnCount = COUNT(*)
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'monitoring.Indicators') 
    AND name IN ('AverageOfCurrHour', 'ScheduleConfiguration')
    
    IF @legacyColumnCount = 0
    BEGIN
        PRINT '✅ All legacy schedule configuration columns removed'
    END
    ELSE
    BEGIN
        PRINT '⚠️ WARNING: ' + CAST(@legacyColumnCount AS NVARCHAR(10)) + ' legacy columns still exist'
    END
    
    -- Check that SchedulerID column exists
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'SchedulerID')
    BEGIN
        PRINT '✅ SchedulerID column exists (modern scheduler system ready)'
    END
    ELSE
    BEGIN
        PRINT '❌ SchedulerID column missing!'
    END
    
    -- Check that view exists and works
    IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID('monitoring.vw_IndicatorsWithSchedulers'))
    BEGIN
        PRINT '✅ vw_IndicatorsWithSchedulers view exists'
        
        -- Test the view
        DECLARE @viewTestCount INT
        SELECT @viewTestCount = COUNT(*) FROM monitoring.vw_IndicatorsWithSchedulers
        PRINT '✅ View test successful - ' + CAST(@viewTestCount AS NVARCHAR(10)) + ' indicators found'
    END
    ELSE
    BEGIN
        PRINT '❌ vw_IndicatorsWithSchedulers view missing!'
    END
    
    -- Show backup table info
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.IndicatorLegacyScheduleBackup') AND type in (N'U'))
    BEGIN
        DECLARE @backupRecordCount INT
        SELECT @backupRecordCount = COUNT(*) FROM monitoring.IndicatorLegacyScheduleBackup
        PRINT '💾 Backup table contains ' + CAST(@backupRecordCount AS NVARCHAR(10)) + ' records'
    END

    -- Commit the transaction
    COMMIT TRANSACTION FixSchedulersAndCleanup
    
    PRINT ''
    PRINT '🎉 MIGRATION COMPLETED SUCCESSFULLY!'
    PRINT '=================================='
    PRINT ''
    PRINT '✅ Schedulers system is now properly configured'
    PRINT '✅ Legacy schedule configuration columns have been removed'
    PRINT '✅ Data has been safely backed up'
    PRINT '✅ Modern scheduler system is ready to use'
    PRINT ''
    PRINT '📝 Next steps:'
    PRINT '   1. Test the application scheduler functionality'
    PRINT '   2. Verify scheduler dropdown works in indicator edit forms'
    PRINT '   3. Test creating and editing schedulers'
    PRINT '   4. After successful testing, optionally drop backup table'
    PRINT ''

END TRY
BEGIN CATCH
    -- Rollback on error
    ROLLBACK TRANSACTION FixSchedulersAndCleanup
    
    PRINT ''
    PRINT '❌ ERROR OCCURRED DURING MIGRATION!'
    PRINT '=================================='
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
    PRINT 'Error Message: ' + ERROR_MESSAGE()
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS NVARCHAR(10))
    PRINT ''
    PRINT 'Transaction has been rolled back. No changes were made.'
    
END CATCH

GO
