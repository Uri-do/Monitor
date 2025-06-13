-- =============================================
-- Verification Script: Check Current Database State
-- Description: Checks the current state of the database before running migrations
-- Author: System Migration - Frontend Deep Cleanup
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

PRINT '🔍 CHECKING CURRENT DATABASE STATE'
PRINT '================================='
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

-- 1. Check Indicators table structure
PRINT '📋 1. INDICATORS TABLE STRUCTURE'
PRINT '==============================='

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND type in (N'U'))
BEGIN
    PRINT '✅ monitoring.Indicators table exists'
    
    -- Show all columns
    SELECT 
        COLUMN_NAME AS [Column Name],
        DATA_TYPE AS [Data Type],
        IS_NULLABLE AS [Nullable],
        COLUMN_DEFAULT AS [Default]
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'monitoring' 
    AND TABLE_NAME = 'Indicators'
    ORDER BY ORDINAL_POSITION
    
    -- Check for specific columns
    DECLARE @hasSchedulerID BIT = 0
    DECLARE @hasModifiedDate BIT = 0
    DECLARE @hasCreatedDate BIT = 0
    DECLARE @hasAverageOfCurrHour BIT = 0
    DECLARE @hasScheduleConfiguration BIT = 0
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'SchedulerID')
        SET @hasSchedulerID = 1
    
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'ModifiedDate')
        SET @hasModifiedDate = 1
        
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'CreatedDate')
        SET @hasCreatedDate = 1
        
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'AverageOfCurrHour')
        SET @hasAverageOfCurrHour = 1
        
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'ScheduleConfiguration')
        SET @hasScheduleConfiguration = 1
    
    PRINT ''
    PRINT '📊 Column Status:'
    PRINT '   SchedulerID: ' + CASE WHEN @hasSchedulerID = 1 THEN '✅ EXISTS' ELSE '❌ MISSING' END
    PRINT '   ModifiedDate: ' + CASE WHEN @hasModifiedDate = 1 THEN '✅ EXISTS' ELSE '❌ MISSING' END
    PRINT '   CreatedDate: ' + CASE WHEN @hasCreatedDate = 1 THEN '✅ EXISTS' ELSE '❌ MISSING' END
    PRINT '   AverageOfCurrHour (legacy): ' + CASE WHEN @hasAverageOfCurrHour = 1 THEN '⚠️ EXISTS (should be removed)' ELSE '✅ NOT FOUND' END
    PRINT '   ScheduleConfiguration (legacy): ' + CASE WHEN @hasScheduleConfiguration = 1 THEN '⚠️ EXISTS (should be removed)' ELSE '✅ NOT FOUND' END
END
ELSE
BEGIN
    PRINT '❌ monitoring.Indicators table does not exist!'
END

PRINT ''

-- 2. Check Schedulers table
PRINT '📋 2. SCHEDULERS TABLE'
PRINT '===================='

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.Schedulers') AND type in (N'U'))
BEGIN
    PRINT '✅ monitoring.Schedulers table exists'
    
    DECLARE @schedulerCount INT
    SELECT @schedulerCount = COUNT(*) FROM monitoring.Schedulers
    PRINT '   Records: ' + CAST(@schedulerCount AS NVARCHAR(10))
    
    IF @schedulerCount > 0
    BEGIN
        PRINT ''
        PRINT '📋 Sample schedulers:'
        SELECT TOP 5
            SchedulerID,
            SchedulerName,
            ScheduleType,
            CASE ScheduleType
                WHEN 'interval' THEN 'Every ' + CAST(IntervalMinutes AS NVARCHAR(10)) + ' minutes'
                WHEN 'cron' THEN 'Cron: ' + CronExpression
                WHEN 'onetime' THEN 'One-time: ' + FORMAT(ExecutionDateTime, 'yyyy-MM-dd HH:mm')
                ELSE 'Unknown'
            END AS [Schedule Display],
            IsEnabled
        FROM monitoring.Schedulers
        ORDER BY ScheduleType, SchedulerName
    END
END
ELSE
BEGIN
    PRINT '❌ monitoring.Schedulers table does not exist!'
END

PRINT ''

-- 3. Check foreign key relationship
PRINT '📋 3. FOREIGN KEY RELATIONSHIPS'
PRINT '==============================='

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID('monitoring.FK_Indicators_Schedulers'))
BEGIN
    PRINT '✅ FK_Indicators_Schedulers foreign key exists'
END
ELSE
BEGIN
    PRINT '❌ FK_Indicators_Schedulers foreign key missing'
END

PRINT ''

-- 4. Check views
PRINT '📋 4. DATABASE VIEWS'
PRINT '=================='

IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID('monitoring.vw_IndicatorsWithSchedulers'))
BEGIN
    PRINT '✅ vw_IndicatorsWithSchedulers view exists'
    
    -- Try to query the view to see if it works
    BEGIN TRY
        DECLARE @viewRecordCount INT
        SELECT @viewRecordCount = COUNT(*) FROM monitoring.vw_IndicatorsWithSchedulers
        PRINT '✅ View is functional - ' + CAST(@viewRecordCount AS NVARCHAR(10)) + ' records'
    END TRY
    BEGIN CATCH
        PRINT '❌ View exists but has errors: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '❌ vw_IndicatorsWithSchedulers view does not exist'
END

PRINT ''

-- 5. Check stored procedures
PRINT '📋 5. STORED PROCEDURES'
PRINT '======================'

DECLARE @procCount INT = 0

IF EXISTS (SELECT * FROM sys.procedures WHERE object_id = OBJECT_ID('monitoring.usp_GetSchedulers'))
BEGIN
    PRINT '✅ usp_GetSchedulers procedure exists'
    SET @procCount = @procCount + 1
END
ELSE
BEGIN
    PRINT '❌ usp_GetSchedulers procedure missing'
END

IF EXISTS (SELECT * FROM sys.procedures WHERE object_id = OBJECT_ID('monitoring.usp_UpsertScheduler'))
BEGIN
    PRINT '✅ usp_UpsertScheduler procedure exists'
    SET @procCount = @procCount + 1
END
ELSE
BEGIN
    PRINT '❌ usp_UpsertScheduler procedure missing'
END

PRINT ''

-- 6. Check backup table
PRINT '📋 6. BACKUP TABLE'
PRINT '================='

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.IndicatorLegacyScheduleBackup') AND type in (N'U'))
BEGIN
    DECLARE @backupCount INT
    SELECT @backupCount = COUNT(*) FROM monitoring.IndicatorLegacyScheduleBackup
    PRINT '✅ IndicatorLegacyScheduleBackup table exists'
    PRINT '   Records: ' + CAST(@backupCount AS NVARCHAR(10))
END
ELSE
BEGIN
    PRINT '✅ No backup table found (normal if migration hasn''t been run)'
END

PRINT ''

-- 7. Overall assessment
PRINT '📋 7. OVERALL ASSESSMENT'
PRINT '======================='

DECLARE @needsMigration BIT = 0
DECLARE @issues TABLE (Issue NVARCHAR(255))

-- Check what needs to be done
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'ModifiedDate')
BEGIN
    INSERT INTO @issues VALUES ('❌ ModifiedDate column missing from Indicators table')
    SET @needsMigration = 1
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'CreatedDate')
BEGIN
    INSERT INTO @issues VALUES ('❌ CreatedDate column missing from Indicators table')
    SET @needsMigration = 1
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'AverageOfCurrHour')
BEGIN
    INSERT INTO @issues VALUES ('⚠️ Legacy AverageOfCurrHour column still exists')
    SET @needsMigration = 1
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND name = 'ScheduleConfiguration')
BEGIN
    INSERT INTO @issues VALUES ('⚠️ Legacy ScheduleConfiguration column still exists')
    SET @needsMigration = 1
END

IF NOT EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID('monitoring.vw_IndicatorsWithSchedulers'))
BEGIN
    INSERT INTO @issues VALUES ('❌ vw_IndicatorsWithSchedulers view missing')
    SET @needsMigration = 1
END

IF EXISTS (SELECT * FROM @issues)
BEGIN
    PRINT '⚠️ Issues found that require migration:'
    SELECT Issue FROM @issues
    PRINT ''
    PRINT '📝 Recommended action:'
    PRINT '   Run the migration script: 004_Fix_Schedulers_And_Cleanup_Legacy.sql'
END
ELSE
BEGIN
    PRINT '🎉 Database appears to be in good state!'
    PRINT '✅ All required columns exist'
    PRINT '✅ No legacy columns found'
    PRINT '✅ Views and procedures are in place'
    PRINT ''
    PRINT '📝 The scheduler system should be ready to use.'
END

PRINT ''
PRINT '📋 Verification completed.'

GO
