-- =============================================
-- Rollback Script: Restore Legacy Schedule Configuration
-- Description: Restores legacy schedule configuration columns if needed
-- Author: System Migration - Frontend Deep Cleanup
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

PRINT '🔄 ROLLING BACK LEGACY SCHEDULE CONFIGURATION REMOVAL'
PRINT '===================================================='
PRINT ''

-- Check if we're connected to the correct database
IF DB_NAME() != 'PopAI'
BEGIN
    PRINT '❌ ERROR: Not connected to PopAI database!'
    PRINT 'Current database: ' + DB_NAME()
    PRINT 'Please connect to PopAI database and re-run this script.'
    RETURN
END

PRINT '✅ Connected to PopAI database'
PRINT ''

-- Check if monitoring schema exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'monitoring')
BEGIN
    PRINT '❌ ERROR: monitoring schema does not exist!'
    RETURN
END

-- Check if Indicators table exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND type in (N'U'))
BEGIN
    PRINT '❌ ERROR: monitoring.Indicators table does not exist!'
    RETURN
END

-- Check if backup table exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.IndicatorLegacyScheduleBackup') AND type in (N'U'))
BEGIN
    PRINT '❌ ERROR: Backup table monitoring.IndicatorLegacyScheduleBackup does not exist!'
    PRINT 'Cannot perform rollback without backup data.'
    PRINT 'The original migration may not have been run, or the backup table was already dropped.'
    RETURN
END

PRINT '✅ Backup table found: monitoring.IndicatorLegacyScheduleBackup'
PRINT ''

-- Begin transaction for safety
BEGIN TRANSACTION RollbackLegacyScheduleConfig

BEGIN TRY
    PRINT '🔍 Checking what needs to be restored...'
    
    -- Check backup table contents
    DECLARE @backupRecordCount INT
    SELECT @backupRecordCount = COUNT(*) FROM monitoring.IndicatorLegacyScheduleBackup
    PRINT '📋 Found ' + CAST(@backupRecordCount AS NVARCHAR(10)) + ' records in backup table'
    
    -- Check if AverageOfCurrHour column needs to be restored
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'AverageOfCurrHour')
    BEGIN
        -- Check if we have AverageOfCurrHour data in backup
        DECLARE @avgCurrHourCount INT
        SELECT @avgCurrHourCount = COUNT(*) 
        FROM monitoring.IndicatorLegacyScheduleBackup 
        WHERE AverageOfCurrHour IS NOT NULL
        
        IF @avgCurrHourCount > 0
        BEGIN
            PRINT '🔄 Restoring AverageOfCurrHour column...'
            
            -- Add the column back
            ALTER TABLE monitoring.Indicators 
            ADD AverageOfCurrHour BIT NULL
            
            PRINT '✅ AverageOfCurrHour column added back to monitoring.Indicators'
            
            -- Restore the data
            UPDATE ind
            SET AverageOfCurrHour = backup.AverageOfCurrHour
            FROM monitoring.Indicators ind
            INNER JOIN monitoring.IndicatorLegacyScheduleBackup backup ON ind.IndicatorID = backup.IndicatorID
            WHERE backup.AverageOfCurrHour IS NOT NULL
            
            DECLARE @restoredAvgCount INT = @@ROWCOUNT
            PRINT '✅ Restored AverageOfCurrHour data for ' + CAST(@restoredAvgCount AS NVARCHAR(10)) + ' indicators'
        END
        ELSE
        BEGIN
            PRINT '✅ No AverageOfCurrHour data to restore'
        END
    END
    ELSE
    BEGIN
        PRINT '✅ AverageOfCurrHour column already exists'
    END
    
    -- Check if ScheduleConfiguration column needs to be restored
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'ScheduleConfiguration')
    BEGIN
        -- Check if we have ScheduleConfiguration data in backup
        DECLARE @scheduleConfigCount INT
        SELECT @scheduleConfigCount = COUNT(*) 
        FROM monitoring.IndicatorLegacyScheduleBackup 
        WHERE ScheduleConfiguration IS NOT NULL
        
        IF @scheduleConfigCount > 0
        BEGIN
            PRINT '🔄 Restoring ScheduleConfiguration column...'
            
            -- Add the column back
            ALTER TABLE monitoring.Indicators 
            ADD ScheduleConfiguration NVARCHAR(MAX) NULL
            
            PRINT '✅ ScheduleConfiguration column added back to monitoring.Indicators'
            
            -- Restore the data
            UPDATE ind
            SET ScheduleConfiguration = backup.ScheduleConfiguration
            FROM monitoring.Indicators ind
            INNER JOIN monitoring.IndicatorLegacyScheduleBackup backup ON ind.IndicatorID = backup.IndicatorID
            WHERE backup.ScheduleConfiguration IS NOT NULL
            
            DECLARE @restoredScheduleCount INT = @@ROWCOUNT
            PRINT '✅ Restored ScheduleConfiguration data for ' + CAST(@restoredScheduleCount AS NVARCHAR(10)) + ' indicators'
        END
        ELSE
        BEGIN
            PRINT '✅ No ScheduleConfiguration data to restore'
        END
    END
    ELSE
    BEGIN
        PRINT '✅ ScheduleConfiguration column already exists'
    END
    
    -- Verify the rollback
    PRINT ''
    PRINT '🔍 Verifying rollback results...'
    
    -- Show current table structure
    PRINT '📋 Current monitoring.Indicators table structure:'
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        IS_NULLABLE,
        COLUMN_DEFAULT
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'monitoring' 
    AND TABLE_NAME = 'Indicators'
    AND COLUMN_NAME IN ('AverageOfCurrHour', 'ScheduleConfiguration', 'SchedulerID')
    ORDER BY ORDINAL_POSITION
    
    -- Commit the transaction
    COMMIT TRANSACTION RollbackLegacyScheduleConfig
    
    PRINT ''
    PRINT '🎉 ROLLBACK COMPLETED SUCCESSFULLY!'
    PRINT '================================='
    PRINT ''
    PRINT '✅ Legacy schedule configuration columns have been restored'
    PRINT '✅ Data has been restored from backup table'
    PRINT ''
    PRINT '⚠️ IMPORTANT NOTES:'
    PRINT '   1. The modern scheduler system (SchedulerID) is still in place'
    PRINT '   2. You now have both legacy and modern scheduling columns'
    PRINT '   3. You may need to update your application code to handle both systems'
    PRINT '   4. Consider which system you want to use going forward'
    PRINT ''
    PRINT '📝 Next steps:'
    PRINT '   1. Test the application to ensure everything works correctly'
    PRINT '   2. Decide whether to keep legacy columns or re-run the cleanup migration'
    PRINT '   3. Update application logic if needed to handle both scheduling systems'
    PRINT ''

END TRY
BEGIN CATCH
    -- Rollback on error
    ROLLBACK TRANSACTION RollbackLegacyScheduleConfig
    
    PRINT ''
    PRINT '❌ ERROR OCCURRED DURING ROLLBACK!'
    PRINT '================================='
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
    PRINT 'Error Message: ' + ERROR_MESSAGE()
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS NVARCHAR(10))
    PRINT ''
    PRINT 'Transaction has been rolled back. No changes were made to the database.'
    PRINT 'Please review the error and re-run the script after fixing any issues.'
    
END CATCH

GO

PRINT ''
PRINT '📋 Rollback script execution completed.'
PRINT 'Check the output above for results and any required next steps.'
