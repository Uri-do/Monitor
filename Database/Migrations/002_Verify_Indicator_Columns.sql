-- =============================================
-- Verification Script: Indicator Columns
-- Description: Verifies that all required columns exist in monitoring.Indicators table
-- Author: System Migration
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

PRINT '🔍 VERIFICATION: Indicators Table Columns'
PRINT '=========================================='
PRINT ''

-- Check if table exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND type in (N'U'))
BEGIN
    PRINT '❌ ERROR: monitoring.Indicators table does not exist!'
    RETURN
END

PRINT '✅ monitoring.Indicators table found'
PRINT ''

-- Check all required columns
PRINT '📋 CHECKING REQUIRED COLUMNS:'
PRINT '============================='

DECLARE @MissingColumns TABLE (ColumnName NVARCHAR(128))

-- Check ExecutionContext
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ExecutionContext')
    INSERT INTO @MissingColumns VALUES ('ExecutionContext')
ELSE
    PRINT '✅ ExecutionContext column exists'

-- Check ExecutionStartTime
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ExecutionStartTime')
    INSERT INTO @MissingColumns VALUES ('ExecutionStartTime')
ELSE
    PRINT '✅ ExecutionStartTime column exists'

-- Check IsCurrentlyRunning
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'IsCurrentlyRunning')
    INSERT INTO @MissingColumns VALUES ('IsCurrentlyRunning')
ELSE
    PRINT '✅ IsCurrentlyRunning column exists'

-- Check ThresholdComparison
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdComparison')
    INSERT INTO @MissingColumns VALUES ('ThresholdComparison')
ELSE
    PRINT '✅ ThresholdComparison column exists'

-- Check ThresholdField
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdField')
    INSERT INTO @MissingColumns VALUES ('ThresholdField')
ELSE
    PRINT '✅ ThresholdField column exists'

-- Check ThresholdType
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdType')
    INSERT INTO @MissingColumns VALUES ('ThresholdType')
ELSE
    PRINT '✅ ThresholdType column exists'

-- Check ThresholdValue
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdValue')
    INSERT INTO @MissingColumns VALUES ('ThresholdValue')
ELSE
    PRINT '✅ ThresholdValue column exists'

-- Report missing columns
DECLARE @MissingCount INT = (SELECT COUNT(*) FROM @MissingColumns)

IF @MissingCount > 0
BEGIN
    PRINT ''
    PRINT '❌ MISSING COLUMNS DETECTED:'
    PRINT '============================'
    SELECT ColumnName AS [Missing Column] FROM @MissingColumns
    PRINT ''
    PRINT 'Please run the migration script: 002_Add_Missing_Indicator_Columns.sql'
END
ELSE
BEGIN
    PRINT ''
    PRINT '✅ ALL REQUIRED COLUMNS EXIST!'
END

PRINT ''
PRINT '📊 COMPLETE TABLE STRUCTURE:'
PRINT '============================'

SELECT 
    c.COLUMN_NAME AS [Column Name],
    c.DATA_TYPE AS [Data Type],
    CASE 
        WHEN c.CHARACTER_MAXIMUM_LENGTH IS NOT NULL 
        THEN CONCAT(c.DATA_TYPE, '(', c.CHARACTER_MAXIMUM_LENGTH, ')')
        WHEN c.NUMERIC_PRECISION IS NOT NULL 
        THEN CONCAT(c.DATA_TYPE, '(', c.NUMERIC_PRECISION, ',', c.NUMERIC_SCALE, ')')
        ELSE c.DATA_TYPE
    END AS [Full Type],
    c.IS_NULLABLE AS [Nullable],
    ISNULL(c.COLUMN_DEFAULT, 'None') AS [Default Value],
    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN '🔑 PK' ELSE '' END AS [Primary Key]
FROM INFORMATION_SCHEMA.COLUMNS c
LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk ON c.COLUMN_NAME = pk.COLUMN_NAME 
    AND c.TABLE_NAME = pk.TABLE_NAME 
    AND c.TABLE_SCHEMA = pk.TABLE_SCHEMA
    AND pk.CONSTRAINT_NAME LIKE 'PK_%'
WHERE c.TABLE_NAME = 'Indicators' AND c.TABLE_SCHEMA = 'monitoring'
ORDER BY c.ORDINAL_POSITION

PRINT ''
PRINT '📊 INDEXES:'
PRINT '==========='

SELECT 
    i.name AS [Index Name],
    i.type_desc AS [Type],
    CASE WHEN i.is_unique = 1 THEN 'Yes' ELSE 'No' END AS [Unique],
    CASE WHEN i.is_primary_key = 1 THEN 'Yes' ELSE 'No' END AS [Primary Key],
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS [Columns]
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID(N'[monitoring].[Indicators]')
AND i.name IS NOT NULL
GROUP BY i.name, i.type_desc, i.is_unique, i.is_primary_key
ORDER BY i.name

PRINT ''
PRINT '🎯 ENTITY FRAMEWORK COMPATIBILITY:'
PRINT '=================================='

IF @MissingCount = 0
BEGIN
    PRINT '✅ Table structure is compatible with Entity Framework'
    PRINT '✅ All Indicator entity properties have corresponding columns'
    PRINT '✅ Ready for application queries'
END
ELSE
BEGIN
    PRINT '❌ Table structure is NOT compatible with Entity Framework'
    PRINT '❌ Missing columns will cause SQL errors'
    PRINT '❌ Run migration script before starting application'
END

PRINT ''
PRINT '🎯 Verification completed!'
GO
