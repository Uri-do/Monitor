-- =============================================
-- Rollback Script: Drop IndicatorContacts Table
-- Description: Safely removes the IndicatorContacts table and related objects
-- Author: System Migration
-- Date: 2024-12-19
-- Version: 1.0
-- WARNING: This will permanently delete all indicator-contact relationships!
-- =============================================

USE [PopAI]
GO

PRINT '⚠️  WARNING: This script will permanently delete the IndicatorContacts table and all data!'
PRINT 'Make sure you have a backup before proceeding.'
PRINT ''

-- Check if the table exists before attempting to drop
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND type in (N'U'))
BEGIN
    PRINT 'monitoring.IndicatorContacts table found. Beginning rollback...'

    -- First, check if there are any records in the table
    DECLARE @RecordCount INT
    SELECT @RecordCount = COUNT(*) FROM [monitoring].[IndicatorContacts]
    
    IF @RecordCount > 0
    BEGIN
        PRINT CONCAT('⚠️  WARNING: Table contains ', @RecordCount, ' records that will be permanently deleted!')
        
        -- Optional: Create a backup of the data before deletion
        PRINT 'Creating backup of existing data...'
        
        IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts_Backup_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss') + ']') AND type in (N'U'))
        BEGIN
            DECLARE @BackupTableName NVARCHAR(128) = 'IndicatorContacts_Backup_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss')
            DECLARE @BackupSQL NVARCHAR(MAX) = 'SELECT * INTO [monitoring].[' + @BackupTableName + '] FROM [monitoring].[IndicatorContacts]'
            
            EXEC sp_executesql @BackupSQL
            PRINT CONCAT('✅ Backup created: ', @BackupTableName)
        END
    END
    ELSE
    BEGIN
        PRINT 'Table is empty. Safe to proceed with deletion.'
    END
    
    -- Drop foreign key constraints first
    PRINT 'Dropping foreign key constraints...'
    
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[monitoring].[FK_IndicatorContacts_Indicators]'))
    BEGIN
        ALTER TABLE [monitoring].[IndicatorContacts] DROP CONSTRAINT [FK_IndicatorContacts_Indicators]
        PRINT '✅ Dropped FK_IndicatorContacts_Indicators'
    END

    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[monitoring].[FK_IndicatorContacts_Contacts]'))
    BEGIN
        ALTER TABLE [monitoring].[IndicatorContacts] DROP CONSTRAINT [FK_IndicatorContacts_Contacts]
        PRINT '✅ Dropped FK_IndicatorContacts_Contacts'
    END
    
    -- Drop unique constraints
    PRINT 'Dropping unique constraints...'
    
    IF EXISTS (SELECT * FROM sys.key_constraints WHERE object_id = OBJECT_ID(N'[monitoring].[UQ_IndicatorContacts_IndicatorId_ContactId]'))
    BEGIN
        ALTER TABLE [monitoring].[IndicatorContacts] DROP CONSTRAINT [UQ_IndicatorContacts_IndicatorId_ContactId]
        PRINT '✅ Dropped UQ_IndicatorContacts_IndicatorId_ContactId'
    END
    
    -- Drop indexes
    PRINT 'Dropping indexes...'
    
    IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND name = N'IX_IndicatorContacts_IndicatorId')
    BEGIN
        DROP INDEX [IX_IndicatorContacts_IndicatorId] ON [monitoring].[IndicatorContacts]
        PRINT '✅ Dropped IX_IndicatorContacts_IndicatorId'
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND name = N'IX_IndicatorContacts_ContactId')
    BEGIN
        DROP INDEX [IX_IndicatorContacts_ContactId] ON [monitoring].[IndicatorContacts]
        PRINT '✅ Dropped IX_IndicatorContacts_ContactId'
    END

    IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND name = N'IX_IndicatorContacts_IsActive')
    BEGIN
        DROP INDEX [IX_IndicatorContacts_IsActive] ON [monitoring].[IndicatorContacts]
        PRINT '✅ Dropped IX_IndicatorContacts_IsActive'
    END
    
    -- Remove extended properties
    PRINT 'Removing extended properties...'
    
    -- Remove table description
    IF EXISTS (SELECT * FROM sys.extended_properties WHERE major_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND minor_id = 0)
    BEGIN
        EXEC sys.sp_dropextendedproperty
            @name = N'MS_Description',
            @level0type = N'SCHEMA', @level0name = N'monitoring',
            @level1type = N'TABLE', @level1name = N'IndicatorContacts'
        PRINT '✅ Removed table description'
    END

    -- Remove column descriptions (they will be removed automatically when table is dropped)

    -- Finally, drop the table
    PRINT 'Dropping monitoring.IndicatorContacts table...'
    DROP TABLE [monitoring].[IndicatorContacts]
    PRINT '✅ monitoring.IndicatorContacts table dropped successfully!'
    
END
ELSE
BEGIN
    PRINT 'monitoring.IndicatorContacts table does not exist. Nothing to rollback.'
END
GO

-- Verify the table was dropped
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND type in (N'U'))
BEGIN
    PRINT ''
    PRINT '✅ SUCCESS: monitoring.IndicatorContacts table rollback completed successfully!'
    PRINT ''
    PRINT '📋 Rollback Summary:'
    PRINT '- IndicatorContacts table: DROPPED'
    PRINT '- Foreign key constraints: REMOVED'
    PRINT '- Indexes: REMOVED'
    PRINT '- Extended properties: REMOVED'
    
    -- Check for backup tables
    IF EXISTS (SELECT * FROM sys.objects WHERE name LIKE 'IndicatorContacts_Backup_%' AND type in (N'U'))
    BEGIN
        PRINT ''
        PRINT '💾 Backup tables created:'
        SELECT 
            name AS BackupTableName,
            create_date AS CreatedDate
        FROM sys.objects 
        WHERE name LIKE 'IndicatorContacts_Backup_%' 
        AND type in (N'U')
        ORDER BY create_date DESC
        
        PRINT ''
        PRINT 'ℹ️  Note: Backup tables can be dropped manually when no longer needed.'
    END
END
ELSE
BEGIN
    PRINT ''
    PRINT '❌ ERROR: Failed to drop monitoring.IndicatorContacts table!'
    PRINT 'Please check for dependencies and try again.'
END
GO

PRINT ''
PRINT '🎯 Rollback completed!'
PRINT ''
PRINT 'Next steps after rollback:'
PRINT '1. Remove IndicatorContact entity from your Entity Framework models'
PRINT '2. Remove DbSet<IndicatorContact> from your DbContext'
PRINT '3. Remove many-to-many relationship configurations'
PRINT '4. Update your application code to handle the missing relationship'
PRINT '5. Consider alternative approaches for indicator-contact relationships'
GO
