-- =============================================
-- Migration Script: Add Missing Indicator Columns
-- Description: Adds missing columns to monitoring.Indicators table for Entity Framework compatibility
-- Author: System Migration
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

PRINT '🔧 ADDING MISSING COLUMNS TO INDICATORS TABLE'
PRINT '============================================='
PRINT ''

-- Check if table exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND type in (N'U'))
BEGIN
    PRINT '❌ ERROR: monitoring.Indicators table does not exist!'
    PRINT 'Please create the Indicators table first.'
    RETURN
END

PRINT '✅ monitoring.Indicators table found'
PRINT ''

-- Add ExecutionContext column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ExecutionContext')
BEGIN
    PRINT 'Adding ExecutionContext column...'
    ALTER TABLE [monitoring].[Indicators] 
    ADD [ExecutionContext] [nvarchar](50) NULL
    PRINT '✅ ExecutionContext column added'
END
ELSE
BEGIN
    PRINT '⚠️  ExecutionContext column already exists'
END

-- Add ExecutionStartTime column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ExecutionStartTime')
BEGIN
    PRINT 'Adding ExecutionStartTime column...'
    ALTER TABLE [monitoring].[Indicators] 
    ADD [ExecutionStartTime] [datetime2](7) NULL
    PRINT '✅ ExecutionStartTime column added'
END
ELSE
BEGIN
    PRINT '⚠️  ExecutionStartTime column already exists'
END

-- Add IsCurrentlyRunning column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'IsCurrentlyRunning')
BEGIN
    PRINT 'Adding IsCurrentlyRunning column...'
    ALTER TABLE [monitoring].[Indicators] 
    ADD [IsCurrentlyRunning] [bit] NOT NULL DEFAULT (0)
    PRINT '✅ IsCurrentlyRunning column added'
END
ELSE
BEGIN
    PRINT '⚠️  IsCurrentlyRunning column already exists'
END

-- Add ThresholdComparison column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdComparison')
BEGIN
    PRINT 'Adding ThresholdComparison column...'
    ALTER TABLE [monitoring].[Indicators] 
    ADD [ThresholdComparison] [nvarchar](10) NOT NULL DEFAULT ('gt')
    PRINT '✅ ThresholdComparison column added'
END
ELSE
BEGIN
    PRINT '⚠️  ThresholdComparison column already exists'
END

-- Add ThresholdField column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdField')
BEGIN
    PRINT 'Adding ThresholdField column...'
    ALTER TABLE [monitoring].[Indicators] 
    ADD [ThresholdField] [nvarchar](50) NOT NULL DEFAULT ('Total')
    PRINT '✅ ThresholdField column added'
END
ELSE
BEGIN
    PRINT '⚠️  ThresholdField column already exists'
END

-- Add ThresholdType column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdType')
BEGIN
    PRINT 'Adding ThresholdType column...'
    ALTER TABLE [monitoring].[Indicators] 
    ADD [ThresholdType] [nvarchar](50) NOT NULL DEFAULT ('threshold_value')
    PRINT '✅ ThresholdType column added'
END
ELSE
BEGIN
    PRINT '⚠️  ThresholdType column already exists'
END

-- Add ThresholdValue column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'ThresholdValue')
BEGIN
    PRINT 'Adding ThresholdValue column...'
    ALTER TABLE [monitoring].[Indicators] 
    ADD [ThresholdValue] [decimal](18,2) NOT NULL DEFAULT (0.00)
    PRINT '✅ ThresholdValue column added'
END
ELSE
BEGIN
    PRINT '⚠️  ThresholdValue column already exists'
END

PRINT ''
PRINT '📊 ADDING INDEXES FOR PERFORMANCE'
PRINT '=================================='

-- Add index for execution state queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'IX_Indicators_IsCurrentlyRunning')
BEGIN
    PRINT 'Creating index IX_Indicators_IsCurrentlyRunning...'
    CREATE NONCLUSTERED INDEX [IX_Indicators_IsCurrentlyRunning] ON [monitoring].[Indicators]
    (
        [IsCurrentlyRunning] ASC
    )
    INCLUDE ([IndicatorName], [ExecutionStartTime], [ExecutionContext])
    PRINT '✅ Index IX_Indicators_IsCurrentlyRunning created'
END
ELSE
BEGIN
    PRINT '⚠️  Index IX_Indicators_IsCurrentlyRunning already exists'
END

-- Add index for threshold queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND name = 'IX_Indicators_ThresholdType')
BEGIN
    PRINT 'Creating index IX_Indicators_ThresholdType...'
    CREATE NONCLUSTERED INDEX [IX_Indicators_ThresholdType] ON [monitoring].[Indicators]
    (
        [ThresholdType] ASC,
        [IsActive] ASC
    )
    INCLUDE ([ThresholdField], [ThresholdComparison], [ThresholdValue])
    PRINT '✅ Index IX_Indicators_ThresholdType created'
END
ELSE
BEGIN
    PRINT '⚠️  Index IX_Indicators_ThresholdType already exists'
END

PRINT ''
PRINT '📝 ADDING COLUMN DOCUMENTATION'
PRINT '=============================='

-- Add extended properties for documentation
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Current execution context: Manual, Scheduled, Test',
    @level0type = N'SCHEMA', @level0name = N'monitoring', 
    @level1type = N'TABLE', @level1name = N'Indicators', 
    @level2type = N'COLUMN', @level2name = N'ExecutionContext'

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Timestamp when indicator execution started',
    @level0type = N'SCHEMA', @level0name = N'monitoring', 
    @level1type = N'TABLE', @level1name = N'Indicators', 
    @level2type = N'COLUMN', @level2name = N'ExecutionStartTime'

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Flag indicating if indicator is currently executing',
    @level0type = N'SCHEMA', @level0name = N'monitoring', 
    @level1type = N'TABLE', @level1name = N'Indicators', 
    @level2type = N'COLUMN', @level2name = N'IsCurrentlyRunning'

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Comparison operator: gt, gte, lt, lte, eq',
    @level0type = N'SCHEMA', @level0name = N'monitoring', 
    @level1type = N'TABLE', @level1name = N'Indicators', 
    @level2type = N'COLUMN', @level2name = N'ThresholdComparison'

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Field to evaluate: Total, Marked, MarkedPercent',
    @level0type = N'SCHEMA', @level0name = N'monitoring', 
    @level1type = N'TABLE', @level1name = N'Indicators', 
    @level2type = N'COLUMN', @level2name = N'ThresholdField'

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Threshold type: volume_average, threshold_value, etc.',
    @level0type = N'SCHEMA', @level0name = N'monitoring', 
    @level1type = N'TABLE', @level1name = N'Indicators', 
    @level2type = N'COLUMN', @level2name = N'ThresholdType'

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description', @value = N'Threshold value for comparison',
    @level0type = N'SCHEMA', @level0name = N'monitoring', 
    @level1type = N'TABLE', @level1name = N'Indicators', 
    @level2type = N'COLUMN', @level2name = N'ThresholdValue'

PRINT ''
PRINT '✅ MIGRATION COMPLETED SUCCESSFULLY!'
PRINT '===================================='
PRINT ''
PRINT 'Added columns:'
PRINT '- ExecutionContext (nvarchar(50), nullable)'
PRINT '- ExecutionStartTime (datetime2(7), nullable)'
PRINT '- IsCurrentlyRunning (bit, default 0)'
PRINT '- ThresholdComparison (nvarchar(10), default ''gt'')'
PRINT '- ThresholdField (nvarchar(50), default ''Total'')'
PRINT '- ThresholdType (nvarchar(50), default ''threshold_value'')'
PRINT '- ThresholdValue (decimal(18,2), default 0.00)'
PRINT ''
PRINT 'Added indexes:'
PRINT '- IX_Indicators_IsCurrentlyRunning'
PRINT '- IX_Indicators_ThresholdType'
PRINT ''
PRINT 'Next steps:'
PRINT '1. Test Entity Framework queries'
PRINT '2. Verify indicator functionality'
PRINT '3. Update any existing indicators with proper threshold values'

GO
