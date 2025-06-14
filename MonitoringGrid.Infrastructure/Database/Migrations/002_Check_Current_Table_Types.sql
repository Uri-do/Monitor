-- =============================================
-- Diagnostic Script: Check Current Data Types
-- Description: Checks the actual data types of ID columns in Indicators and Contacts tables
-- Author: System Migration
-- Date: 2024-12-19
-- Version: 1.0
-- =============================================

USE [PopAI]
GO

PRINT '🔍 CHECKING CURRENT TABLE DATA TYPES'
PRINT '===================================='
PRINT ''

-- Check Indicators table primary key data type
PRINT '📋 INDICATORS TABLE - Primary Key Data Type:'
PRINT '============================================'

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[Indicators]') AND type in (N'U'))
BEGIN
    SELECT 
        c.COLUMN_NAME AS [Column Name],
        c.DATA_TYPE AS [Data Type],
        CASE 
            WHEN c.NUMERIC_PRECISION IS NOT NULL 
            THEN CONCAT(c.DATA_TYPE, '(', c.NUMERIC_PRECISION, ',', c.NUMERIC_SCALE, ')')
            ELSE c.DATA_TYPE
        END AS [Full Type],
        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN '🔑 PRIMARY KEY' ELSE '' END AS [Key Type]
    FROM INFORMATION_SCHEMA.COLUMNS c
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk ON c.COLUMN_NAME = pk.COLUMN_NAME 
        AND c.TABLE_NAME = pk.TABLE_NAME 
        AND c.TABLE_SCHEMA = pk.TABLE_SCHEMA
        AND pk.CONSTRAINT_NAME LIKE 'PK_%'
    WHERE c.TABLE_NAME = 'Indicators' 
        AND c.TABLE_SCHEMA = 'monitoring'
        AND c.COLUMN_NAME LIKE '%ID'
    ORDER BY c.ORDINAL_POSITION
END
ELSE
BEGIN
    PRINT '❌ monitoring.Indicators table does not exist!'
END

-- Check Contacts table primary key data type
PRINT ''
PRINT '📋 CONTACTS TABLE - Primary Key Data Type:'
PRINT '=========================================='

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[Contacts]') AND type in (N'U'))
BEGIN
    SELECT 
        c.COLUMN_NAME AS [Column Name],
        c.DATA_TYPE AS [Data Type],
        CASE 
            WHEN c.NUMERIC_PRECISION IS NOT NULL 
            THEN CONCAT(c.DATA_TYPE, '(', c.NUMERIC_PRECISION, ',', c.NUMERIC_SCALE, ')')
            ELSE c.DATA_TYPE
        END AS [Full Type],
        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN '🔑 PRIMARY KEY' ELSE '' END AS [Key Type]
    FROM INFORMATION_SCHEMA.COLUMNS c
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk ON c.COLUMN_NAME = pk.COLUMN_NAME 
        AND c.TABLE_NAME = pk.TABLE_NAME 
        AND c.TABLE_SCHEMA = pk.TABLE_SCHEMA
        AND pk.CONSTRAINT_NAME LIKE 'PK_%'
    WHERE c.TABLE_NAME = 'Contacts' 
        AND c.TABLE_SCHEMA = 'monitoring'
        AND c.COLUMN_NAME LIKE '%ID'
    ORDER BY c.ORDINAL_POSITION
END
ELSE
BEGIN
    PRINT '❌ monitoring.Contacts table does not exist!'
END

-- Check if IndicatorContacts table exists and its data types
PRINT ''
PRINT '📋 INDICATORCONTACTS TABLE - Current Data Types:'
PRINT '==============================================='

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[monitoring].[IndicatorContacts]') AND type in (N'U'))
BEGIN
    PRINT '✅ monitoring.IndicatorContacts table exists'
    PRINT ''
    SELECT 
        c.COLUMN_NAME AS [Column Name],
        c.DATA_TYPE AS [Data Type],
        CASE 
            WHEN c.NUMERIC_PRECISION IS NOT NULL 
            THEN CONCAT(c.DATA_TYPE, '(', c.NUMERIC_PRECISION, ',', c.NUMERIC_SCALE, ')')
            ELSE c.DATA_TYPE
        END AS [Full Type],
        c.IS_NULLABLE AS [Nullable],
        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN '🔑 PK' 
             WHEN fk.COLUMN_NAME IS NOT NULL THEN '🔗 FK' 
             ELSE '' END AS [Key Type]
    FROM INFORMATION_SCHEMA.COLUMNS c
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk ON c.COLUMN_NAME = pk.COLUMN_NAME 
        AND c.TABLE_NAME = pk.TABLE_NAME 
        AND c.TABLE_SCHEMA = pk.TABLE_SCHEMA
        AND pk.CONSTRAINT_NAME LIKE 'PK_%'
    LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE fk ON c.COLUMN_NAME = fk.COLUMN_NAME 
        AND c.TABLE_NAME = fk.TABLE_NAME 
        AND c.TABLE_SCHEMA = fk.TABLE_SCHEMA
        AND fk.CONSTRAINT_NAME LIKE 'FK_%'
    WHERE c.TABLE_NAME = 'IndicatorContacts' 
        AND c.TABLE_SCHEMA = 'monitoring'
    ORDER BY c.ORDINAL_POSITION
END
ELSE
BEGIN
    PRINT '❌ monitoring.IndicatorContacts table does not exist!'
END

-- Summary and recommendations
PRINT ''
PRINT '🎯 SUMMARY AND RECOMMENDATIONS:'
PRINT '==============================='
PRINT ''
PRINT 'Based on the data types shown above:'
PRINT '1. If Indicators.IndicatorID is INT, use INT in IndicatorContacts.IndicatorID'
PRINT '2. If Indicators.IndicatorID is BIGINT, use BIGINT in IndicatorContacts.IndicatorID'
PRINT '3. If Contacts.ContactID is INT, use INT in IndicatorContacts.ContactID'
PRINT '4. If Contacts.ContactID is BIGINT, use BIGINT in IndicatorContacts.ContactID'
PRINT ''
PRINT 'Foreign key data types MUST match the referenced primary key data types!'

GO
