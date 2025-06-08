-- Add KPI Execution Tracking Migration
-- This script adds all missing columns and tables from the AddKpiExecutionTracking migration
USE [PopAI]
GO

PRINT '=== Starting KPI Execution Tracking Migration ==='
PRINT 'This will add missing columns and tables to support real-time KPI monitoring'
PRINT ''

-- Create auth schema if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'auth')
BEGIN
    EXEC('CREATE SCHEMA [auth]')
    PRINT '✅ Created auth schema'
END
ELSE
BEGIN
    PRINT '⚠️ Auth schema already exists'
END

-- Add missing columns to KPIs table
PRINT ''
PRINT 'Adding missing columns to monitoring.KPIs table...'

-- ExecutionContext
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'ExecutionContext')
BEGIN
    ALTER TABLE monitoring.KPIs ADD ExecutionContext NVARCHAR(MAX) NULL;
    PRINT '✅ Added ExecutionContext column'
END
ELSE
BEGIN
    PRINT '⚠️ ExecutionContext column already exists'
END

-- ExecutionStartTime
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'ExecutionStartTime')
BEGIN
    ALTER TABLE monitoring.KPIs ADD ExecutionStartTime DATETIME2 NULL;
    PRINT '✅ Added ExecutionStartTime column'
END
ELSE
BEGIN
    PRINT '⚠️ ExecutionStartTime column already exists'
END

-- IsCurrentlyRunning
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'IsCurrentlyRunning')
BEGIN
    ALTER TABLE monitoring.KPIs ADD IsCurrentlyRunning BIT NOT NULL DEFAULT 0;
    PRINT '✅ Added IsCurrentlyRunning column'
END
ELSE
BEGIN
    PRINT '⚠️ IsCurrentlyRunning column already exists'
END

-- ComparisonOperator
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'ComparisonOperator')
BEGIN
    ALTER TABLE monitoring.KPIs ADD ComparisonOperator NVARCHAR(10) NULL;
    PRINT '✅ Added ComparisonOperator column'
END
ELSE
BEGIN
    PRINT '⚠️ ComparisonOperator column already exists'
END

-- KpiType
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'KpiType')
BEGIN
    ALTER TABLE monitoring.KPIs ADD KpiType NVARCHAR(50) NOT NULL DEFAULT 'success_rate';
    PRINT '✅ Added KpiType column'
END
ELSE
BEGIN
    PRINT '⚠️ KpiType column already exists'
END

-- LastMinutes
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'LastMinutes')
BEGIN
    ALTER TABLE monitoring.KPIs ADD LastMinutes INT NOT NULL DEFAULT 1440;
    PRINT '✅ Added LastMinutes column'
END
ELSE
BEGIN
    PRINT '⚠️ LastMinutes column already exists'
END

-- ScheduleConfiguration
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'ScheduleConfiguration')
BEGIN
    ALTER TABLE monitoring.KPIs ADD ScheduleConfiguration NVARCHAR(MAX) NULL;
    PRINT '✅ Added ScheduleConfiguration column'
END
ELSE
BEGIN
    PRINT '⚠️ ScheduleConfiguration column already exists'
END

-- ThresholdValue
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'ThresholdValue')
BEGIN
    ALTER TABLE monitoring.KPIs ADD ThresholdValue DECIMAL(18,2) NULL;
    PRINT '✅ Added ThresholdValue column'
END
ELSE
BEGIN
    PRINT '⚠️ ThresholdValue column already exists'
END

PRINT ''
PRINT 'Adding enhanced audit columns to monitoring.HistoricalData table...'

-- Enhanced HistoricalData columns for comprehensive audit trail
DECLARE @columns TABLE (ColumnName NVARCHAR(100), DataType NVARCHAR(100), DefaultValue NVARCHAR(100))
INSERT INTO @columns VALUES 
    ('AlertSent', 'BIT NOT NULL DEFAULT 0', '0'),
    ('ConnectionString', 'NVARCHAR(500) NULL', 'NULL'),
    ('DatabaseName', 'NVARCHAR(100) NULL', 'NULL'),
    ('DeviationPercent', 'DECIMAL(18,2) NULL', 'NULL'),
    ('ErrorMessage', 'NVARCHAR(MAX) NULL', 'NULL'),
    ('ExecutedBy', 'NVARCHAR(100) NULL', 'NULL'),
    ('ExecutionContext', 'NVARCHAR(MAX) NULL', 'NULL'),
    ('ExecutionMethod', 'NVARCHAR(50) NULL', 'NULL'),
    ('ExecutionTimeMs', 'INT NULL', 'NULL'),
    ('HistoricalValue', 'DECIMAL(18,2) NULL', 'NULL'),
    ('IpAddress', 'NVARCHAR(50) NULL', 'NULL'),
    ('IsSuccessful', 'BIT NOT NULL DEFAULT 0', '0'),
    ('RawResponse', 'NVARCHAR(MAX) NULL', 'NULL'),
    ('ServerName', 'NVARCHAR(100) NULL', 'NULL'),
    ('SessionId', 'NVARCHAR(100) NULL', 'NULL'),
    ('ShouldAlert', 'BIT NOT NULL DEFAULT 0', '0'),
    ('SqlCommand', 'NVARCHAR(MAX) NULL', 'NULL'),
    ('SqlParameters', 'NVARCHAR(MAX) NULL', 'NULL'),
    ('UserAgent', 'NVARCHAR(500) NULL', 'NULL')

DECLARE @columnName NVARCHAR(100), @dataType NVARCHAR(100), @defaultValue NVARCHAR(100)
DECLARE column_cursor CURSOR FOR SELECT ColumnName, DataType, DefaultValue FROM @columns

OPEN column_cursor
FETCH NEXT FROM column_cursor INTO @columnName, @dataType, @defaultValue

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.HistoricalData') AND name = @columnName)
    BEGIN
        DECLARE @sql NVARCHAR(MAX) = 'ALTER TABLE monitoring.HistoricalData ADD ' + @columnName + ' ' + @dataType
        EXEC sp_executesql @sql
        PRINT '✅ Added ' + @columnName + ' column to HistoricalData'
    END
    ELSE
    BEGIN
        PRINT '⚠️ ' + @columnName + ' column already exists in HistoricalData'
    END
    
    FETCH NEXT FROM column_cursor INTO @columnName, @dataType, @defaultValue
END

CLOSE column_cursor
DEALLOCATE column_cursor

PRINT ''
PRINT 'Adding enhanced columns to monitoring.Config table...'

-- Enhanced Config columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.Config') AND name = 'Category')
BEGIN
    ALTER TABLE monitoring.Config ADD Category NVARCHAR(50) NULL;
    PRINT '✅ Added Category column to Config'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.Config') AND name = 'CreatedDate')
BEGIN
    ALTER TABLE monitoring.Config ADD CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE();
    PRINT '✅ Added CreatedDate column to Config'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.Config') AND name = 'IsEncrypted')
BEGIN
    ALTER TABLE monitoring.Config ADD IsEncrypted BIT NOT NULL DEFAULT 0;
    PRINT '✅ Added IsEncrypted column to Config'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.Config') AND name = 'IsReadOnly')
BEGIN
    ALTER TABLE monitoring.Config ADD IsReadOnly BIT NOT NULL DEFAULT 0;
    PRINT '✅ Added IsReadOnly column to Config'
END

PRINT ''
PRINT '=== Phase 1 Complete: Basic columns added ==='
PRINT 'Run the next script (08_AddKpiExecutionTracking_Part2.sql) to create tables and constraints'
GO
