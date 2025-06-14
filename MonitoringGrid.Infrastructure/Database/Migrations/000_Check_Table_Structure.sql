-- =============================================
-- Diagnostic Script: Check Existing Table Structures
-- Description: Verifies the actual column names and primary keys in Indicators and Contacts tables
-- Author: System Migration
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

PRINT '🔍 CHECKING EXISTING TABLE STRUCTURES'
PRINT '====================================='
PRINT ''

-- Check if monitoring schema exists
IF EXISTS (SELECT * FROM sys.schemas WHERE name = 'monitoring')
BEGIN
    PRINT '✅ monitoring schema exists'
END
ELSE
BEGIN
    PRINT '❌ monitoring schema does not exist!'
    PRINT 'Please create the monitoring schema first.'
    RETURN
END

-- Check Indicators table structure
PRINT ''
PRINT '📋 INDICATORS TABLE STRUCTURE:'
PRINT '=============================='

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND type in (N'U'))
BEGIN
    PRINT '✅ monitoring.Indicators table exists'
    PRINT ''
    PRINT 'Columns:'
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
        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN '🔑 PRIMARY KEY' ELSE '' END AS [Key Type]
    FROM INFORMATION_SCHEMA.COLUMNS c
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk ON c.COLUMN_NAME = pk.COLUMN_NAME 
        AND c.TABLE_NAME = pk.TABLE_NAME 
        AND c.TABLE_SCHEMA = pk.TABLE_SCHEMA
        AND pk.CONSTRAINT_NAME LIKE 'PK_%'
    WHERE c.TABLE_NAME = 'Indicators' AND c.TABLE_SCHEMA = 'monitoring'
    ORDER BY c.ORDINAL_POSITION
    
    PRINT ''
    PRINT 'Primary Key:'
    SELECT 
        kcu.COLUMN_NAME AS [Primary Key Column],
        tc.CONSTRAINT_NAME AS [Constraint Name]
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
        ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
    WHERE tc.TABLE_NAME = 'Indicators' 
        AND tc.TABLE_SCHEMA = 'monitoring'
        AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
END
ELSE
BEGIN
    PRINT '❌ monitoring.Indicators table does not exist!'
END

-- Check Contacts table structure
PRINT ''
PRINT '📋 CONTACTS TABLE STRUCTURE:'
PRINT '============================'

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[Contacts]') AND type in (N'U'))
BEGIN
    PRINT '✅ monitoring.Contacts table exists'
    PRINT ''
    PRINT 'Columns:'
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
        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN '🔑 PRIMARY KEY' ELSE '' END AS [Key Type]
    FROM INFORMATION_SCHEMA.COLUMNS c
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk ON c.COLUMN_NAME = pk.COLUMN_NAME 
        AND c.TABLE_NAME = pk.TABLE_NAME 
        AND c.TABLE_SCHEMA = pk.TABLE_SCHEMA
        AND pk.CONSTRAINT_NAME LIKE 'PK_%'
    WHERE c.TABLE_NAME = 'Contacts' AND c.TABLE_SCHEMA = 'monitoring'
    ORDER BY c.ORDINAL_POSITION
    
    PRINT ''
    PRINT 'Primary Key:'
    SELECT 
        kcu.COLUMN_NAME AS [Primary Key Column],
        tc.CONSTRAINT_NAME AS [Constraint Name]
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
        ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
    WHERE tc.TABLE_NAME = 'Contacts' 
        AND tc.TABLE_SCHEMA = 'monitoring'
        AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
END
ELSE
BEGIN
    PRINT '❌ monitoring.Contacts table does not exist!'
END

-- Check if IndicatorContacts table already exists
PRINT ''
PRINT '📋 INDICATORCONTACTS TABLE CHECK:'
PRINT '================================='

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND type in (N'U'))
BEGIN
    PRINT '⚠️  monitoring.IndicatorContacts table already exists!'
    PRINT 'You may need to drop it first or use a different approach.'
    
    PRINT ''
    PRINT 'Existing table structure:'
    SELECT 
        c.COLUMN_NAME AS [Column Name],
        c.DATA_TYPE AS [Data Type],
        c.IS_NULLABLE AS [Nullable]
    FROM INFORMATION_SCHEMA.COLUMNS c
    WHERE c.TABLE_NAME = 'IndicatorContacts' AND c.TABLE_SCHEMA = 'monitoring'
    ORDER BY c.ORDINAL_POSITION
END
ELSE
BEGIN
    PRINT '✅ monitoring.IndicatorContacts table does not exist - ready for creation'
END

PRINT ''
PRINT '🎯 SUMMARY:'
PRINT '==========='
PRINT 'Use the column names shown above in your foreign key references.'
PRINT 'Update the migration script with the correct primary key column names.'
PRINT ''
PRINT 'Example:'
PRINT 'If Indicators primary key is "IndicatorID", use:'
PRINT 'REFERENCES [monitoring].[Indicators] ([IndicatorID])'
PRINT ''
PRINT 'If Contacts primary key is "ContactID", use:'
PRINT 'REFERENCES [monitoring].[Contacts] ([ContactID])'
GO
