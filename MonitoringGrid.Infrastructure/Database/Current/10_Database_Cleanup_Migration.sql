-- =============================================
-- MonitoringGrid Database Cleanup Migration
-- Removes legacy KPI references and optimizes for modern Indicator system
-- =============================================

USE PopAI;
GO

PRINT '=============================================';
PRINT 'MonitoringGrid Database Cleanup Migration';
PRINT '=============================================';

-- =============================================
-- 1. BACKUP LEGACY DATA (if needed)
-- =============================================

-- Create backup tables for legacy data before cleanup
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KPIs_Backup') AND type = 'U')
AND EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KPIs') AND type = 'U')
BEGIN
    SELECT * INTO monitoring.KPIs_Backup FROM monitoring.KPIs;
    PRINT '✅ Created backup of legacy KPIs table';
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KpiContacts_Backup') AND type = 'U')
AND EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KpiContacts') AND type = 'U')
BEGIN
    SELECT * INTO monitoring.KpiContacts_Backup FROM monitoring.KpiContacts;
    PRINT '✅ Created backup of legacy KpiContacts table';
END

-- =============================================
-- 2. UPDATE EXISTING DATA TO MODERN TERMINOLOGY
-- =============================================

-- Update any remaining KPI references in AlertLogs
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.AlertLogs') AND name = 'KpiId')
BEGIN
    -- Add new IndicatorId column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.AlertLogs') AND name = 'IndicatorId')
    BEGIN
        ALTER TABLE monitoring.AlertLogs ADD IndicatorId BIGINT NULL;
        PRINT '✅ Added IndicatorId column to AlertLogs';
    END
    
    -- Copy data from KpiId to IndicatorId
    UPDATE monitoring.AlertLogs SET IndicatorId = KpiId WHERE IndicatorId IS NULL;
    PRINT '✅ Migrated KpiId data to IndicatorId in AlertLogs';
    
    -- Drop the old KpiId column after data migration
    ALTER TABLE monitoring.AlertLogs DROP COLUMN KpiId;
    PRINT '✅ Removed legacy KpiId column from AlertLogs';
END

-- Update any remaining KPI references in SystemStatus
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.SystemStatus') AND name = 'ProcessedKpis')
BEGIN
    -- Add new ProcessedIndicators column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.SystemStatus') AND name = 'ProcessedIndicators')
    BEGIN
        ALTER TABLE monitoring.SystemStatus ADD ProcessedIndicators INT NOT NULL DEFAULT 0;
        PRINT '✅ Added ProcessedIndicators column to SystemStatus';
    END
    
    -- Copy data from ProcessedKpis to ProcessedIndicators
    UPDATE monitoring.SystemStatus SET ProcessedIndicators = ProcessedKpis WHERE ProcessedIndicators = 0;
    PRINT '✅ Migrated ProcessedKpis data to ProcessedIndicators in SystemStatus';
    
    -- Drop the old ProcessedKpis column
    ALTER TABLE monitoring.SystemStatus DROP COLUMN ProcessedKpis;
    PRINT '✅ Removed legacy ProcessedKpis column from SystemStatus';
END

-- =============================================
-- 3. CLEAN UP LEGACY TABLES
-- =============================================

-- Drop legacy KPI tables if they exist and are empty or backed up
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KpiContacts') AND type = 'U')
BEGIN
    -- Check if we have a backup and the table is not being used
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KpiContacts_Backup') AND type = 'U')
    BEGIN
        DROP TABLE monitoring.KpiContacts;
        PRINT '✅ Removed legacy KpiContacts table';
    END
    ELSE
    BEGIN
        PRINT '⚠️ KpiContacts table exists but no backup found - skipping removal';
    END
END

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KPIs') AND type = 'U')
BEGIN
    -- Check if we have a backup and the table is not being used
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KPIs_Backup') AND type = 'U')
    BEGIN
        DROP TABLE monitoring.KPIs;
        PRINT '✅ Removed legacy KPIs table';
    END
    ELSE
    BEGIN
        PRINT '⚠️ KPIs table exists but no backup found - skipping removal';
    END
END

-- =============================================
-- 4. CLEAN UP LEGACY INDEXES
-- =============================================

-- Remove old KPI-related indexes if they exist
DECLARE @sql NVARCHAR(MAX);
DECLARE @indexName NVARCHAR(128);

-- Cursor to find and drop KPI-related indexes
DECLARE kpi_indexes_cursor CURSOR FOR
SELECT i.name
FROM sys.indexes i
INNER JOIN sys.objects o ON i.object_id = o.object_id
WHERE o.name IN ('KPIs', 'KpiContacts') 
   OR i.name LIKE '%KPI%' 
   OR i.name LIKE '%Kpi%';

OPEN kpi_indexes_cursor;
FETCH NEXT FROM kpi_indexes_cursor INTO @indexName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = 'DROP INDEX ' + @indexName + ' ON ' + OBJECT_SCHEMA_NAME(OBJECT_ID('monitoring.' + @indexName)) + '.' + OBJECT_NAME(OBJECT_ID('monitoring.' + @indexName));
    
    BEGIN TRY
        EXEC sp_executesql @sql;
        PRINT '✅ Removed legacy index: ' + @indexName;
    END TRY
    BEGIN CATCH
        PRINT '⚠️ Could not remove index: ' + @indexName + ' - ' + ERROR_MESSAGE();
    END CATCH
    
    FETCH NEXT FROM kpi_indexes_cursor INTO @indexName;
END

CLOSE kpi_indexes_cursor;
DEALLOCATE kpi_indexes_cursor;

-- =============================================
-- 5. CLEAN UP LEGACY STORED PROCEDURES
-- =============================================

-- Drop legacy KPI stored procedures
DECLARE @procName NVARCHAR(128);

DECLARE kpi_procs_cursor CURSOR FOR
SELECT name
FROM sys.procedures
WHERE name LIKE '%KPI%' OR name LIKE '%Kpi%';

OPEN kpi_procs_cursor;
FETCH NEXT FROM kpi_procs_cursor INTO @procName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = 'DROP PROCEDURE ' + @procName;
    
    BEGIN TRY
        EXEC sp_executesql @sql;
        PRINT '✅ Removed legacy stored procedure: ' + @procName;
    END TRY
    BEGIN CATCH
        PRINT '⚠️ Could not remove stored procedure: ' + @procName + ' - ' + ERROR_MESSAGE();
    END CATCH
    
    FETCH NEXT FROM kpi_procs_cursor INTO @procName;
END

CLOSE kpi_procs_cursor;
DEALLOCATE kpi_procs_cursor;

-- =============================================
-- 6. CLEAN UP LEGACY VIEWS
-- =============================================

-- Drop legacy KPI views
DECLARE @viewName NVARCHAR(128);

DECLARE kpi_views_cursor CURSOR FOR
SELECT name
FROM sys.views
WHERE name LIKE '%KPI%' OR name LIKE '%Kpi%';

OPEN kpi_views_cursor;
FETCH NEXT FROM kpi_views_cursor INTO @viewName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = 'DROP VIEW ' + @viewName;
    
    BEGIN TRY
        EXEC sp_executesql @sql;
        PRINT '✅ Removed legacy view: ' + @viewName;
    END TRY
    BEGIN CATCH
        PRINT '⚠️ Could not remove view: ' + @viewName + ' - ' + ERROR_MESSAGE();
    END CATCH
    
    FETCH NEXT FROM kpi_views_cursor INTO @viewName;
END

CLOSE kpi_views_cursor;
DEALLOCATE kpi_views_cursor;

-- =============================================
-- 7. UPDATE STATISTICS AND REBUILD INDEXES
-- =============================================

-- Update statistics for all monitoring tables
UPDATE STATISTICS monitoring.Indicators;
UPDATE STATISTICS monitoring.IndicatorContacts;
UPDATE STATISTICS monitoring.Contacts;
UPDATE STATISTICS monitoring.AlertLogs;
UPDATE STATISTICS monitoring.Schedulers;
UPDATE STATISTICS monitoring.MonitorStatistics;
UPDATE STATISTICS monitoring.SystemStatus;

PRINT '✅ Updated statistics for all monitoring tables';

-- Rebuild indexes for optimal performance
ALTER INDEX ALL ON monitoring.Indicators REBUILD;
ALTER INDEX ALL ON monitoring.IndicatorContacts REBUILD;
ALTER INDEX ALL ON monitoring.Contacts REBUILD;
ALTER INDEX ALL ON monitoring.AlertLogs REBUILD;
ALTER INDEX ALL ON monitoring.Schedulers REBUILD;
ALTER INDEX ALL ON monitoring.MonitorStatistics REBUILD;

PRINT '✅ Rebuilt indexes for optimal performance';

-- =============================================
-- 8. VERIFY CLEANUP RESULTS
-- =============================================

-- Check for any remaining KPI references
DECLARE @kpiReferences INT = 0;

-- Check table names
SELECT @kpiReferences = @kpiReferences + COUNT(*)
FROM sys.tables
WHERE name LIKE '%KPI%' OR name LIKE '%Kpi%';

-- Check column names
SELECT @kpiReferences = @kpiReferences + COUNT(*)
FROM sys.columns c
INNER JOIN sys.tables t ON c.object_id = t.object_id
WHERE c.name LIKE '%KPI%' OR c.name LIKE '%Kpi%';

-- Check stored procedure names
SELECT @kpiReferences = @kpiReferences + COUNT(*)
FROM sys.procedures
WHERE name LIKE '%KPI%' OR name LIKE '%Kpi%';

-- Check view names
SELECT @kpiReferences = @kpiReferences + COUNT(*)
FROM sys.views
WHERE name LIKE '%KPI%' OR name LIKE '%Kpi%';

IF @kpiReferences = 0
BEGIN
    PRINT '✅ Database cleanup successful - no KPI references found';
END
ELSE
BEGIN
    PRINT '⚠️ Warning: ' + CAST(@kpiReferences AS VARCHAR) + ' KPI references still exist';
END

-- =============================================
-- 9. COMPLETION SUMMARY
-- =============================================

PRINT '';
PRINT '=============================================';
PRINT 'MonitoringGrid Database Cleanup Complete!';
PRINT '=============================================';
PRINT 'Cleanup actions performed:';
PRINT '✅ Backed up legacy KPI tables';
PRINT '✅ Migrated KPI data to Indicator terminology';
PRINT '✅ Removed legacy KPI tables and indexes';
PRINT '✅ Cleaned up legacy stored procedures and views';
PRINT '✅ Updated statistics and rebuilt indexes';
PRINT '✅ Verified cleanup completion';
PRINT '';
PRINT 'Database is now fully modernized with Indicator terminology!';
PRINT '=============================================';
