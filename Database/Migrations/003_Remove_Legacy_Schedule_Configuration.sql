-- =============================================
-- Migration Script: Remove Legacy Schedule Configuration
-- Description: Removes legacy schedule configuration columns from Indicators table
-- Author: System Migration - Frontend Deep Cleanup
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

PRINT '🧹 REMOVING LEGACY SCHEDULE CONFIGURATION COLUMNS'
PRINT '================================================='
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
    PRINT 'Please ensure the monitoring schema is created first.'
    RETURN
END

PRINT '✅ monitoring schema exists'
PRINT ''

-- Check if Indicators table exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND type in (N'U'))
BEGIN
    PRINT '❌ ERROR: monitoring.Indicators table does not exist!'
    PRINT 'Please ensure the Indicators table is created first.'
    RETURN
END

PRINT '✅ monitoring.Indicators table exists'
PRINT ''

-- Begin transaction for safety
BEGIN TRANSACTION RemoveLegacyScheduleConfig

BEGIN TRY
    PRINT '🔍 Checking for legacy schedule configuration columns...'
    PRINT ''

    -- Check for AverageOfCurrHour column
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'AverageOfCurrHour')
    BEGIN
        PRINT '📋 Found AverageOfCurrHour column - will be removed'
        
        -- Create backup of data before removal (optional)
        PRINT '💾 Creating backup of AverageOfCurrHour data...'
        
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.IndicatorLegacyScheduleBackup') AND type in (N'U'))
        BEGIN
            CREATE TABLE monitoring.IndicatorLegacyScheduleBackup (
                IndicatorID BIGINT NOT NULL,
                IndicatorName NVARCHAR(255) NOT NULL,
                AverageOfCurrHour BIT NULL,
                BackupDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                BackupReason NVARCHAR(255) NOT NULL DEFAULT 'Legacy schedule configuration cleanup'
            )
            
            PRINT '✅ Created backup table: monitoring.IndicatorLegacyScheduleBackup'
        END
        
        -- Insert backup data
        INSERT INTO monitoring.IndicatorLegacyScheduleBackup (IndicatorID, IndicatorName, AverageOfCurrHour)
        SELECT IndicatorID, IndicatorName, AverageOfCurrHour
        FROM monitoring.Indicators
        WHERE AverageOfCurrHour IS NOT NULL
        
        DECLARE @backupCount INT = @@ROWCOUNT
        PRINT '✅ Backed up ' + CAST(@backupCount AS NVARCHAR(10)) + ' records with AverageOfCurrHour data'
        
        -- Remove the column
        PRINT '🗑️ Removing AverageOfCurrHour column...'
        ALTER TABLE monitoring.Indicators DROP COLUMN AverageOfCurrHour
        PRINT '✅ AverageOfCurrHour column removed successfully'
    END
    ELSE
    BEGIN
        PRINT '✅ AverageOfCurrHour column not found (already removed or never existed)'
    END
    
    PRINT ''

    -- Check for any other legacy schedule configuration columns
    -- (Add more columns here if they exist in your schema)
    
    -- Check for ScheduleConfiguration JSON column (if it exists)
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'ScheduleConfiguration')
    BEGIN
        PRINT '📋 Found ScheduleConfiguration column - will be removed'
        
        -- Backup ScheduleConfiguration data
        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.IndicatorLegacyScheduleBackup') AND name = 'ScheduleConfiguration')
        BEGIN
            ALTER TABLE monitoring.IndicatorLegacyScheduleBackup 
            ADD ScheduleConfiguration NVARCHAR(MAX) NULL
            PRINT '✅ Added ScheduleConfiguration column to backup table'
        END
        
        -- Update backup with ScheduleConfiguration data
        UPDATE backup
        SET ScheduleConfiguration = ind.ScheduleConfiguration
        FROM monitoring.IndicatorLegacyScheduleBackup backup
        INNER JOIN monitoring.Indicators ind ON backup.IndicatorID = ind.IndicatorID
        WHERE ind.ScheduleConfiguration IS NOT NULL
        
        -- Insert new records for indicators not already in backup
        INSERT INTO monitoring.IndicatorLegacyScheduleBackup (IndicatorID, IndicatorName, ScheduleConfiguration)
        SELECT ind.IndicatorID, ind.IndicatorName, ind.ScheduleConfiguration
        FROM monitoring.Indicators ind
        LEFT JOIN monitoring.IndicatorLegacyScheduleBackup backup ON ind.IndicatorID = backup.IndicatorID
        WHERE ind.ScheduleConfiguration IS NOT NULL
        AND backup.IndicatorID IS NULL
        
        DECLARE @scheduleBackupCount INT = @@ROWCOUNT
        PRINT '✅ Backed up ' + CAST(@scheduleBackupCount AS NVARCHAR(10)) + ' additional records with ScheduleConfiguration data'
        
        -- Remove the column
        PRINT '🗑️ Removing ScheduleConfiguration column...'
        ALTER TABLE monitoring.Indicators DROP COLUMN ScheduleConfiguration
        PRINT '✅ ScheduleConfiguration column removed successfully'
    END
    ELSE
    BEGIN
        PRINT '✅ ScheduleConfiguration column not found (already removed or never existed)'
    END
    
    PRINT ''

    -- Verify the cleanup
    PRINT '🔍 Verifying cleanup results...'
    
    -- Check that legacy columns are gone
    DECLARE @legacyColumnCount INT = 0
    
    SELECT @legacyColumnCount = COUNT(*)
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'monitoring.Indicators') 
    AND name IN ('AverageOfCurrHour', 'ScheduleConfiguration')
    
    IF @legacyColumnCount = 0
    BEGIN
        PRINT '✅ All legacy schedule configuration columns have been successfully removed'
    END
    ELSE
    BEGIN
        PRINT '⚠️ WARNING: ' + CAST(@legacyColumnCount AS NVARCHAR(10)) + ' legacy columns still exist'
    END
    
    -- Show current Indicators table structure
    PRINT ''
    PRINT '📋 Current monitoring.Indicators table structure:'
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        IS_NULLABLE,
        COLUMN_DEFAULT
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'monitoring' 
    AND TABLE_NAME = 'Indicators'
    ORDER BY ORDINAL_POSITION
    
    -- Show backup table info if it exists
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.IndicatorLegacyScheduleBackup') AND type in (N'U'))
    BEGIN
        DECLARE @backupRecordCount INT
        SELECT @backupRecordCount = COUNT(*) FROM monitoring.IndicatorLegacyScheduleBackup
        
        PRINT ''
        PRINT '💾 Backup information:'
        PRINT '   Table: monitoring.IndicatorLegacyScheduleBackup'
        PRINT '   Records: ' + CAST(@backupRecordCount AS NVARCHAR(10))
        PRINT '   Purpose: Contains backed up legacy schedule configuration data'
        PRINT '   Note: This table can be dropped after verifying the migration was successful'
    END

    -- Commit the transaction
    COMMIT TRANSACTION RemoveLegacyScheduleConfig
    
    PRINT ''
    PRINT '🎉 MIGRATION COMPLETED SUCCESSFULLY!'
    PRINT '=================================='
    PRINT ''
    PRINT '✅ Legacy schedule configuration columns have been removed'
    PRINT '✅ Data has been safely backed up to monitoring.IndicatorLegacyScheduleBackup'
    PRINT '✅ Modern scheduler system is now ready to use'
    PRINT ''
    PRINT '📝 Next steps:'
    PRINT '   1. Test the application to ensure everything works correctly'
    PRINT '   2. Verify that scheduler dropdown functionality works in the UI'
    PRINT '   3. After successful testing, you can optionally drop the backup table:'
    PRINT '      DROP TABLE monitoring.IndicatorLegacyScheduleBackup'
    PRINT ''

END TRY
BEGIN CATCH
    -- Rollback on error
    ROLLBACK TRANSACTION RemoveLegacyScheduleConfig
    
    PRINT ''
    PRINT '❌ ERROR OCCURRED DURING MIGRATION!'
    PRINT '=================================='
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
    PRINT 'Error Message: ' + ERROR_MESSAGE()
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS NVARCHAR(10))
    PRINT ''
    PRINT 'Transaction has been rolled back. No changes were made to the database.'
    PRINT 'Please review the error and re-run the script after fixing any issues.'
    
END CATCH

GO

PRINT ''
PRINT '📋 Migration script execution completed.'
PRINT 'Check the output above for results and any required next steps.'
