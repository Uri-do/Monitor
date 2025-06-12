-- =============================================
-- Migration Script: Create IndicatorContacts Table
-- Description: Creates the junction table for many-to-many relationship between Indicators and Contacts
-- Author: System Migration
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

-- Check if the table already exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND type in (N'U'))
BEGIN
    PRINT 'Creating monitoring.IndicatorContacts table...'

    -- Create the IndicatorContacts junction table
    CREATE TABLE [monitoring].[IndicatorContacts](
        [IndicatorContactID] [int] IDENTITY(1,1) NOT NULL,
        [IndicatorID] [bigint] NOT NULL,
        [ContactID] [int] NOT NULL,
        [CreatedDate] [datetime2](7) NOT NULL CONSTRAINT [DF_IndicatorContacts_CreatedDate] DEFAULT (GETUTCDATE()),
        [CreatedBy] [nvarchar](100) NULL,
        [IsActive] [bit] NOT NULL CONSTRAINT [DF_IndicatorContacts_IsActive] DEFAULT (1),

        -- Primary Key
        CONSTRAINT [PK_IndicatorContacts] PRIMARY KEY CLUSTERED ([IndicatorContactID] ASC),

        -- Foreign Key to Indicators table
        CONSTRAINT [FK_IndicatorContacts_Indicators] FOREIGN KEY([IndicatorID])
            REFERENCES [monitoring].[Indicators] ([IndicatorID])
            ON DELETE CASCADE,

        -- Foreign Key to Contacts table
        CONSTRAINT [FK_IndicatorContacts_Contacts] FOREIGN KEY([ContactID])
            REFERENCES [monitoring].[Contacts] ([ContactID])
            ON DELETE CASCADE,

        -- Unique constraint to prevent duplicate indicator-contact relationships
        CONSTRAINT [UQ_IndicatorContacts_IndicatorId_ContactId] UNIQUE ([IndicatorID], [ContactID])
    )
    
    PRINT 'monitoring.IndicatorContacts table created successfully.'
END
ELSE
BEGIN
    PRINT 'monitoring.IndicatorContacts table already exists. Skipping creation.'
END
GO

-- Create indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND name = N'IX_IndicatorContacts_IndicatorId')
BEGIN
    PRINT 'Creating index IX_IndicatorContacts_IndicatorId...'
    CREATE NONCLUSTERED INDEX [IX_IndicatorContacts_IndicatorId] ON [monitoring].[IndicatorContacts]
    (
        [IndicatorID] ASC
    )
    INCLUDE ([ContactID], [IsActive])
    PRINT 'Index IX_IndicatorContacts_IndicatorId created successfully.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND name = N'IX_IndicatorContacts_ContactId')
BEGIN
    PRINT 'Creating index IX_IndicatorContacts_ContactId...'
    CREATE NONCLUSTERED INDEX [IX_IndicatorContacts_ContactId] ON [monitoring].[IndicatorContacts]
    (
        [ContactID] ASC
    )
    INCLUDE ([IndicatorID], [IsActive])
    PRINT 'Index IX_IndicatorContacts_ContactId created successfully.'
END
GO

-- Create index for active relationships
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND name = N'IX_IndicatorContacts_IsActive')
BEGIN
    PRINT 'Creating index IX_IndicatorContacts_IsActive...'
    CREATE NONCLUSTERED INDEX [IX_IndicatorContacts_IsActive] ON [monitoring].[IndicatorContacts]
    (
        [IsActive] ASC
    )
    INCLUDE ([IndicatorID], [ContactID])
    PRINT 'Index IX_IndicatorContacts_IsActive created successfully.'
END
GO

-- Add extended properties for documentation
IF NOT EXISTS (SELECT * FROM sys.extended_properties WHERE major_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND minor_id = 0)
BEGIN
    EXEC sys.sp_addextendedproperty
        @name = N'MS_Description',
        @value = N'Junction table for many-to-many relationship between Indicators and Contacts. Manages notification contacts for each indicator.',
        @level0type = N'SCHEMA', @level0name = N'monitoring',
        @level1type = N'TABLE', @level1name = N'IndicatorContacts'
END
GO

-- Add column descriptions
EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'Primary key for the indicator-contact relationship',
    @level0type = N'SCHEMA', @level0name = N'monitoring',
    @level1type = N'TABLE', @level1name = N'IndicatorContacts',
    @level2type = N'COLUMN', @level2name = N'IndicatorContactID'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'Foreign key reference to the Indicators table',
    @level0type = N'SCHEMA', @level0name = N'monitoring',
    @level1type = N'TABLE', @level1name = N'IndicatorContacts',
    @level2type = N'COLUMN', @level2name = N'IndicatorID'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'Foreign key reference to the Contacts table',
    @level0type = N'SCHEMA', @level0name = N'monitoring',
    @level1type = N'TABLE', @level1name = N'IndicatorContacts',
    @level2type = N'COLUMN', @level2name = N'ContactID'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'Timestamp when the relationship was created (UTC)',
    @level0type = N'SCHEMA', @level0name = N'monitoring',
    @level1type = N'TABLE', @level1name = N'IndicatorContacts',
    @level2type = N'COLUMN', @level2name = N'CreatedDate'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'User who created the relationship',
    @level0type = N'SCHEMA', @level0name = N'monitoring',
    @level1type = N'TABLE', @level1name = N'IndicatorContacts',
    @level2type = N'COLUMN', @level2name = N'CreatedBy'
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'Indicates if the relationship is active (soft delete support)',
    @level0type = N'SCHEMA', @level0name = N'monitoring',
    @level1type = N'TABLE', @level1name = N'IndicatorContacts',
    @level2type = N'COLUMN', @level2name = N'IsActive'
GO

-- Verify the table was created successfully
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND type in (N'U'))
BEGIN
    PRINT '✅ SUCCESS: monitoring.IndicatorContacts table and indexes created successfully!'
    
    -- Display table structure
    PRINT ''
    PRINT 'Table Structure:'
    SELECT 
        c.COLUMN_NAME,
        c.DATA_TYPE,
        c.IS_NULLABLE,
        c.COLUMN_DEFAULT,
        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 'YES' ELSE 'NO' END AS IS_PRIMARY_KEY,
        CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN 'YES' ELSE 'NO' END AS IS_FOREIGN_KEY
    FROM INFORMATION_SCHEMA.COLUMNS c
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk ON c.COLUMN_NAME = pk.COLUMN_NAME 
        AND c.TABLE_NAME = pk.TABLE_NAME 
        AND pk.CONSTRAINT_NAME LIKE 'PK_%'
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE fk ON c.COLUMN_NAME = fk.COLUMN_NAME 
        AND c.TABLE_NAME = fk.TABLE_NAME 
        AND fk.CONSTRAINT_NAME LIKE 'FK_%'
    WHERE c.TABLE_NAME = 'IndicatorContacts'
    ORDER BY c.ORDINAL_POSITION
    
    PRINT ''
    PRINT 'Indexes:'
    SELECT 
        i.name AS IndexName,
        i.type_desc AS IndexType,
        i.is_unique AS IsUnique
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'[dbo].[IndicatorContacts]')
    AND i.name IS NOT NULL
    ORDER BY i.name
END
ELSE
BEGIN
    PRINT '❌ ERROR: Failed to create monitoring.IndicatorContacts table!'
END
GO

PRINT ''
PRINT '🎯 Migration completed successfully!'
PRINT 'Next steps:'
PRINT '1. Update your Entity Framework models to include the IndicatorContacts entity'
PRINT '2. Add the DbSet<IndicatorContact> to your DbContext'
PRINT '3. Configure the many-to-many relationship in your entity configurations'
PRINT '4. Test the indicator-contact relationship functionality'
GO
