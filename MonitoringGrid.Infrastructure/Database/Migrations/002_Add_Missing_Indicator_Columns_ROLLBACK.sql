-- =============================================
-- Rollback Script: Remove Added Indicator Columns
-- Description: Removes the columns added by 002_Add_Missing_Indicator_Columns.sql
-- Author: System Migration
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

PRINT '🔄 ROLLING BACK INDICATOR COLUMNS MIGRATION'
PRINT '==========================================='
PRINT ''

-- Check if table exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND type in (N'U'))
BEGIN
    PRINT '❌ ERROR: monitoring.Indicators table does not exist!'
    RETURN
END

PRINT '✅ monitoring.Indicators table found'
PRINT ''

-- Drop indexes first
PRINT '📊 REMOVING INDEXES'
PRINT '=================='

IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'IX_Indicators_IsCurrentlyRunning')
BEGIN
    PRINT 'Dropping index IX_Indicators_IsCurrentlyRunning...'
    DROP INDEX [IX_Indicators_IsCurrentlyRunning] ON [monitoring].[Indicators]
    PRINT '✅ Index IX_Indicators_IsCurrentlyRunning dropped'
END

IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'IX_Indicators_ThresholdType')
BEGIN
    PRINT 'Dropping index IX_Indicators_ThresholdType...'
    DROP INDEX [IX_Indicators_ThresholdType] ON [monitoring].[Indicators]
    PRINT '✅ Index IX_Indicators_ThresholdType dropped'
END

PRINT ''
PRINT '🗑️  REMOVING COLUMNS'
PRINT '==================='

-- Remove ExecutionContext column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ExecutionContext')
BEGIN
    PRINT 'Removing ExecutionContext column...'
    ALTER TABLE [monitoring].[Indicators] 
    DROP COLUMN [ExecutionContext]
    PRINT '✅ ExecutionContext column removed'
END

-- Remove ExecutionStartTime column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ExecutionStartTime')
BEGIN
    PRINT 'Removing ExecutionStartTime column...'
    ALTER TABLE [monitoring].[Indicators] 
    DROP COLUMN [ExecutionStartTime]
    PRINT '✅ ExecutionStartTime column removed'
END

-- Remove IsCurrentlyRunning column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'IsCurrentlyRunning')
BEGIN
    PRINT 'Removing IsCurrentlyRunning column...'
    ALTER TABLE [monitoring].[Indicators] 
    DROP COLUMN [IsCurrentlyRunning]
    PRINT '✅ IsCurrentlyRunning column removed'
END

-- Remove ThresholdComparison column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdComparison')
BEGIN
    PRINT 'Removing ThresholdComparison column...'
    ALTER TABLE [monitoring].[Indicators] 
    DROP COLUMN [ThresholdComparison]
    PRINT '✅ ThresholdComparison column removed'
END

-- Remove ThresholdField column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdField')
BEGIN
    PRINT 'Removing ThresholdField column...'
    ALTER TABLE [monitoring].[Indicators] 
    DROP COLUMN [ThresholdField]
    PRINT '✅ ThresholdField column removed'
END

-- Remove ThresholdType column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdType')
BEGIN
    PRINT 'Removing ThresholdType column...'
    ALTER TABLE [monitoring].[Indicators] 
    DROP COLUMN [ThresholdType]
    PRINT '✅ ThresholdType column removed'
END

-- Remove ThresholdValue column
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdValue')
BEGIN
    PRINT 'Removing ThresholdValue column...'
    ALTER TABLE [monitoring].[Indicators] 
    DROP COLUMN [ThresholdValue]
    PRINT '✅ ThresholdValue column removed'
END

PRINT ''
PRINT '✅ ROLLBACK COMPLETED SUCCESSFULLY!'
PRINT '=================================='
PRINT ''
PRINT 'Removed columns:'
PRINT '- ExecutionContext'
PRINT '- ExecutionStartTime'
PRINT '- IsCurrentlyRunning'
PRINT '- ThresholdComparison'
PRINT '- ThresholdField'
PRINT '- ThresholdType'
PRINT '- ThresholdValue'
PRINT ''
PRINT 'Removed indexes:'
PRINT '- IX_Indicators_IsCurrentlyRunning'
PRINT '- IX_Indicators_ThresholdType'
PRINT ''
PRINT 'WARNING: This rollback will cause Entity Framework errors'
PRINT 'if the application tries to access these properties!'

GO
