-- =============================================
-- Verification Script: Legacy Schedule Configuration Cleanup
-- Description: Verifies that legacy schedule configuration cleanup was successful
-- Author: System Migration - Frontend Deep Cleanup
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

PRINT '🔍 VERIFYING LEGACY SCHEDULE CONFIGURATION CLEANUP'
PRINT '================================================='
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

PRINT '✅ monitoring.Indicators table exists'
PRINT ''

-- 1. Check for legacy columns
PRINT '🔍 1. CHECKING FOR LEGACY COLUMNS'
PRINT '================================='

DECLARE @legacyColumns TABLE (
    ColumnName NVARCHAR(128),
    DataType NVARCHAR(128),
    IsNullable NVARCHAR(3),
    Status NVARCHAR(20)
)

-- Check for AverageOfCurrHour
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'AverageOfCurrHour')
BEGIN
    INSERT INTO @legacyColumns (ColumnName, DataType, IsNullable, Status)
    SELECT 'AverageOfCurrHour', DATA_TYPE, IS_NULLABLE, '❌ STILL EXISTS'
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'monitoring' AND TABLE_NAME = 'Indicators' AND COLUMN_NAME = 'AverageOfCurrHour'
END
ELSE
BEGIN
    INSERT INTO @legacyColumns (ColumnName, DataType, IsNullable, Status)
    VALUES ('AverageOfCurrHour', 'N/A', 'N/A', '✅ REMOVED')
END

-- Check for ScheduleConfiguration
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'ScheduleConfiguration')
BEGIN
    INSERT INTO @legacyColumns (ColumnName, DataType, IsNullable, Status)
    SELECT 'ScheduleConfiguration', DATA_TYPE, IS_NULLABLE, '❌ STILL EXISTS'
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'monitoring' AND TABLE_NAME = 'Indicators' AND COLUMN_NAME = 'ScheduleConfiguration'
END
ELSE
BEGIN
    INSERT INTO @legacyColumns (ColumnName, DataType, IsNullable, Status)
    VALUES ('ScheduleConfiguration', 'N/A', 'N/A', '✅ REMOVED')
END

-- Display results
SELECT 
    ColumnName AS [Legacy Column],
    DataType AS [Data Type],
    IsNullable AS [Nullable],
    Status
FROM @legacyColumns
ORDER BY ColumnName

PRINT ''

-- 2. Check for modern scheduler column
PRINT '🔍 2. CHECKING FOR MODERN SCHEDULER COLUMN'
PRINT '========================================='

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'SchedulerID')
BEGIN
    PRINT '✅ SchedulerID column exists (modern scheduler system)'
    
    SELECT 
        COLUMN_NAME AS [Column Name],
        DATA_TYPE AS [Data Type],
        IS_NULLABLE AS [Nullable],
        COLUMN_DEFAULT AS [Default Value]
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'monitoring' 
    AND TABLE_NAME = 'Indicators' 
    AND COLUMN_NAME = 'SchedulerID'
END
ELSE
BEGIN
    PRINT '❌ SchedulerID column does not exist!'
    PRINT '   This may indicate that the modern scheduler system is not yet implemented.'
END

PRINT ''

-- 3. Check backup table
PRINT '🔍 3. CHECKING BACKUP TABLE'
PRINT '=========================='

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.IndicatorLegacyScheduleBackup') AND type in (N'U'))
BEGIN
    DECLARE @backupCount INT
    SELECT @backupCount = COUNT(*) FROM monitoring.IndicatorLegacyScheduleBackup
    
    PRINT '✅ Backup table exists: monitoring.IndicatorLegacyScheduleBackup'
    PRINT '   Records in backup: ' + CAST(@backupCount AS NVARCHAR(10))
    
    -- Show backup table structure
    PRINT ''
    PRINT '📋 Backup table structure:'
    SELECT 
        COLUMN_NAME AS [Column Name],
        DATA_TYPE AS [Data Type],
        IS_NULLABLE AS [Nullable]
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'monitoring' 
    AND TABLE_NAME = 'IndicatorLegacyScheduleBackup'
    ORDER BY ORDINAL_POSITION
    
    -- Show sample backup data
    IF @backupCount > 0
    BEGIN
        PRINT ''
        PRINT '📋 Sample backup data (first 5 records):'
        SELECT TOP 5
            IndicatorID,
            IndicatorName,
            AverageOfCurrHour,
            CASE 
                WHEN LEN(ISNULL(ScheduleConfiguration, '')) > 50 
                THEN LEFT(ScheduleConfiguration, 47) + '...'
                ELSE ScheduleConfiguration
            END AS [ScheduleConfiguration (truncated)],
            BackupDate
        FROM monitoring.IndicatorLegacyScheduleBackup
        ORDER BY BackupDate DESC
    END
END
ELSE
BEGIN
    PRINT '⚠️ Backup table does not exist'
    PRINT '   This may indicate that:'
    PRINT '   - The migration was not run'
    PRINT '   - No legacy data existed to backup'
    PRINT '   - The backup table was already dropped'
END

PRINT ''

-- 4. Check current Indicators table structure
PRINT '🔍 4. CURRENT INDICATORS TABLE STRUCTURE'
PRINT '======================================='

PRINT '📋 All columns in monitoring.Indicators table:'
SELECT 
    ORDINAL_POSITION AS [#],
    COLUMN_NAME AS [Column Name],
    DATA_TYPE AS [Data Type],
    CHARACTER_MAXIMUM_LENGTH AS [Max Length],
    IS_NULLABLE AS [Nullable],
    COLUMN_DEFAULT AS [Default Value]
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'monitoring' 
AND TABLE_NAME = 'Indicators'
ORDER BY ORDINAL_POSITION

PRINT ''

-- 5. Check for any indicators with scheduler assignments
PRINT '🔍 5. CHECKING SCHEDULER ASSIGNMENTS'
PRINT '==================================='

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'SchedulerID')
BEGIN
    DECLARE @totalIndicators INT, @indicatorsWithScheduler INT, @indicatorsWithoutScheduler INT
    
    SELECT @totalIndicators = COUNT(*) FROM monitoring.Indicators
    SELECT @indicatorsWithScheduler = COUNT(*) FROM monitoring.Indicators WHERE SchedulerID IS NOT NULL
    SELECT @indicatorsWithoutScheduler = COUNT(*) FROM monitoring.Indicators WHERE SchedulerID IS NULL
    
    PRINT '📊 Scheduler assignment statistics:'
    PRINT '   Total indicators: ' + CAST(@totalIndicators AS NVARCHAR(10))
    PRINT '   With scheduler: ' + CAST(@indicatorsWithScheduler AS NVARCHAR(10))
    PRINT '   Without scheduler: ' + CAST(@indicatorsWithoutScheduler AS NVARCHAR(10))
    
    IF @indicatorsWithScheduler > 0
    BEGIN
        PRINT ''
        PRINT '📋 Indicators with scheduler assignments:'
        SELECT 
            IndicatorID,
            IndicatorName,
            SchedulerID,
            IsActive
        FROM monitoring.Indicators
        WHERE SchedulerID IS NOT NULL
        ORDER BY IndicatorName
    END
END
ELSE
BEGIN
    PRINT '⚠️ SchedulerID column not found - modern scheduler system not implemented'
END

PRINT ''

-- 6. Overall migration status
PRINT '🔍 6. OVERALL MIGRATION STATUS'
PRINT '============================='

DECLARE @migrationStatus NVARCHAR(50) = '✅ SUCCESS'
DECLARE @issues TABLE (Issue NVARCHAR(255))

-- Check for issues
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'AverageOfCurrHour')
BEGIN
    INSERT INTO @issues VALUES ('❌ AverageOfCurrHour column still exists')
    SET @migrationStatus = '⚠️ PARTIAL'
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'ScheduleConfiguration')
BEGIN
    INSERT INTO @issues VALUES ('❌ ScheduleConfiguration column still exists')
    SET @migrationStatus = '⚠️ PARTIAL'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'SchedulerID')
BEGIN
    INSERT INTO @issues VALUES ('⚠️ SchedulerID column not found (modern scheduler system not implemented)')
    IF @migrationStatus = '✅ SUCCESS'
        SET @migrationStatus = '⚠️ INCOMPLETE'
END

PRINT 'Migration Status: ' + @migrationStatus
PRINT ''

IF EXISTS (SELECT * FROM @issues)
BEGIN
    PRINT '📋 Issues found:'
    SELECT Issue FROM @issues
    PRINT ''
    PRINT '📝 Recommendations:'
    PRINT '   1. If legacy columns still exist, re-run the cleanup migration'
    PRINT '   2. If SchedulerID is missing, ensure the modern scheduler system is implemented'
    PRINT '   3. Test the application thoroughly after any changes'
END
ELSE
BEGIN
    PRINT '🎉 No issues found! Migration appears to be successful.'
    PRINT ''
    PRINT '📝 Next steps:'
    PRINT '   1. Test the application to ensure scheduler functionality works'
    PRINT '   2. Verify that the scheduler dropdown appears in indicator edit forms'
    PRINT '   3. After successful testing, consider dropping the backup table:'
    PRINT '      DROP TABLE monitoring.IndicatorLegacyScheduleBackup'
END

PRINT ''
PRINT '📋 Verification completed.'
PRINT 'Review the results above to ensure the migration was successful.'

GO
