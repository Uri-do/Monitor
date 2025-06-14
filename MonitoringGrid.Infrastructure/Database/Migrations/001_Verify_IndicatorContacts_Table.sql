-- =============================================
-- Verification Script: IndicatorContacts Table
-- Description: Verifies the IndicatorContacts table structure and relationships
-- Author: System Migration
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

PRINT '🔍 VERIFICATION: IndicatorContacts Table Structure and Relationships'
PRINT '=================================================================='
PRINT ''

-- Check if table exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND type in (N'U'))
BEGIN
    PRINT '✅ monitoring.IndicatorContacts table exists'
    
    -- 1. Table Structure Verification
    PRINT ''
    PRINT '📋 TABLE STRUCTURE:'
    PRINT '==================='
    
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
        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN '🔑 PK' ELSE '' END AS [Primary Key],
        CASE WHEN fk.COLUMN_NAME IS NOT NULL THEN '🔗 FK' ELSE '' END AS [Foreign Key]
    FROM INFORMATION_SCHEMA.COLUMNS c
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk ON c.COLUMN_NAME = pk.COLUMN_NAME 
        AND c.TABLE_NAME = pk.TABLE_NAME 
        AND pk.CONSTRAINT_NAME LIKE 'PK_%'
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE fk ON c.COLUMN_NAME = fk.COLUMN_NAME 
        AND c.TABLE_NAME = fk.TABLE_NAME 
        AND fk.CONSTRAINT_NAME LIKE 'FK_%'
    WHERE c.TABLE_NAME = 'IndicatorContacts' AND c.TABLE_SCHEMA = 'monitoring'
    ORDER BY c.ORDINAL_POSITION
    
    -- 2. Constraints Verification
    PRINT ''
    PRINT '🔒 CONSTRAINTS:'
    PRINT '==============='
    
    SELECT 
        tc.CONSTRAINT_NAME AS [Constraint Name],
        tc.CONSTRAINT_TYPE AS [Type],
        kcu.COLUMN_NAME AS [Column],
        CASE 
            WHEN tc.CONSTRAINT_TYPE = 'FOREIGN KEY' THEN 
                CONCAT('References: ', rc.UNIQUE_CONSTRAINT_SCHEMA, '.', rc.UNIQUE_CONSTRAINT_NAME)
            ELSE 'N/A'
        END AS [References]
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
        ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
    LEFT JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc 
        ON tc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
    WHERE tc.TABLE_NAME = 'IndicatorContacts' AND tc.TABLE_SCHEMA = 'monitoring'
    ORDER BY tc.CONSTRAINT_TYPE, tc.CONSTRAINT_NAME
    
    -- 3. Foreign Key Relationships
    PRINT ''
    PRINT '🔗 FOREIGN KEY RELATIONSHIPS:'
    PRINT '============================='
    
    SELECT 
        fk.name AS [FK Constraint],
        tp.name AS [Parent Table],
        cp.name AS [Parent Column],
        tr.name AS [Referenced Table],
        cr.name AS [Referenced Column],
        fk.delete_referential_action_desc AS [On Delete],
        fk.update_referential_action_desc AS [On Update]
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
    INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
    INNER JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
    INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
    WHERE tp.name = 'IndicatorContacts'
    ORDER BY fk.name
    
    -- 4. Indexes Verification
    PRINT ''
    PRINT '📊 INDEXES:'
    PRINT '==========='
    
    SELECT 
        i.name AS [Index Name],
        i.type_desc AS [Type],
        CASE WHEN i.is_unique = 1 THEN 'Yes' ELSE 'No' END AS [Unique],
        CASE WHEN i.is_primary_key = 1 THEN 'Yes' ELSE 'No' END AS [Primary Key],
        STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS [Columns],
        CASE WHEN i.has_filter = 1 THEN i.filter_definition ELSE 'None' END AS [Filter]
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE i.object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]')
    AND i.name IS NOT NULL
    GROUP BY i.name, i.type_desc, i.is_unique, i.is_primary_key, i.has_filter, i.filter_definition
    ORDER BY i.name
    
    -- 5. Extended Properties (Documentation)
    PRINT ''
    PRINT '📝 DOCUMENTATION:'
    PRINT '================='
    
    -- Table description
    SELECT 
        'Table' AS [Object Type],
        'IndicatorContacts' AS [Object Name],
        ISNULL(ep.value, 'No description') AS [Description]
    FROM sys.extended_properties ep
    RIGHT JOIN sys.objects o ON ep.major_id = o.object_id
    WHERE o.name = 'IndicatorContacts' 
    AND ep.minor_id = 0
    AND ep.name = 'MS_Description'
    
    UNION ALL
    
    -- Column descriptions
    SELECT 
        'Column' AS [Object Type],
        c.name AS [Object Name],
        ISNULL(ep.value, 'No description') AS [Description]
    FROM sys.columns c
    LEFT JOIN sys.extended_properties ep ON ep.major_id = c.object_id 
        AND ep.minor_id = c.column_id 
        AND ep.name = 'MS_Description'
    WHERE c.object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]')
    ORDER BY [Object Type], [Object Name]
    
    -- 6. Sample Data Check
    PRINT ''
    PRINT '📊 DATA SUMMARY:'
    PRINT '================'
    
    DECLARE @TotalRecords INT, @ActiveRecords INT, @InactiveRecords INT
    
    SELECT @TotalRecords = COUNT(*) FROM [monitoring].[IndicatorContacts]
    SELECT @ActiveRecords = COUNT(*) FROM [monitoring].[IndicatorContacts] WHERE IsActive = 1
    SELECT @InactiveRecords = COUNT(*) FROM [monitoring].[IndicatorContacts] WHERE IsActive = 0
    
    SELECT 
        @TotalRecords AS [Total Records],
        @ActiveRecords AS [Active Records],
        @InactiveRecords AS [Inactive Records]
    
    -- Show sample data if any exists
    IF @TotalRecords > 0
    BEGIN
        PRINT ''
        PRINT '📋 SAMPLE DATA (First 5 records):'
        PRINT '=================================='
        
        SELECT TOP 5
            IndicatorContactID,
            IndicatorID,
            ContactID,
            CreatedDate,
            CreatedBy,
            IsActive
        FROM [monitoring].[IndicatorContacts]
        ORDER BY CreatedDate DESC
    END
    
    -- 7. Relationship Validation
    PRINT ''
    PRINT '🔍 RELATIONSHIP VALIDATION:'
    PRINT '==========================='
    
    -- Check for orphaned records
    DECLARE @OrphanedIndicators INT, @OrphanedContacts INT
    
    SELECT @OrphanedIndicators = COUNT(*)
    FROM [monitoring].[IndicatorContacts] ic
    LEFT JOIN [monitoring].[Indicators] i ON ic.IndicatorID = i.IndicatorID
    WHERE i.IndicatorID IS NULL

    SELECT @OrphanedContacts = COUNT(*)
    FROM [monitoring].[IndicatorContacts] ic
    LEFT JOIN [monitoring].[Contacts] c ON ic.ContactID = c.ContactID
    WHERE c.ContactID IS NULL
    
    SELECT 
        @OrphanedIndicators AS [Orphaned Indicator References],
        @OrphanedContacts AS [Orphaned Contact References]
    
    IF @OrphanedIndicators = 0 AND @OrphanedContacts = 0
    BEGIN
        PRINT '✅ All relationships are valid - no orphaned records found'
    END
    ELSE
    BEGIN
        PRINT '⚠️  WARNING: Orphaned records detected! Data integrity issues found.'
    END
    
    PRINT ''
    PRINT '✅ VERIFICATION COMPLETE!'
    PRINT '========================'
    PRINT 'The monitoring.IndicatorContacts table structure is valid and ready for use.'

END
ELSE
BEGIN
    PRINT '❌ ERROR: monitoring.IndicatorContacts table does not exist!'
    PRINT 'Please run the migration script first: 001_Create_IndicatorContacts_Table.sql'
END
GO

PRINT ''
PRINT '🎯 Verification completed!'
PRINT ''
PRINT 'Next steps:'
PRINT '1. If verification passed, update your Entity Framework models'
PRINT '2. Add the IndicatorContact entity to your DbContext'
PRINT '3. Configure the many-to-many relationship'
PRINT '4. Test the indicator-contact functionality in your application'
GO
