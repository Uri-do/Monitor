-- =============================================
-- MonitoringGrid Performance Optimization Scripts
-- Phase 6: Performance Optimizations
-- =============================================

-- =============================================
-- 1. PERFORMANCE INDEXES
-- =============================================

-- Index for KPI queries by status and last run
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KPIs_IsActive_LastRun_Performance')
BEGIN
    CREATE NONCLUSTERED INDEX IX_KPIs_IsActive_LastRun_Performance
    ON monitoring.KPIs (IsActive, LastRun DESC)
    INCLUDE (KpiId, Indicator, Owner, Priority, Frequency, Deviation)
    WHERE IsActive = 1;
    PRINT 'Created index: IX_KPIs_IsActive_LastRun_Performance';
END

-- Index for KPI queries by owner and priority
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KPIs_Owner_Priority_Performance')
BEGIN
    CREATE NONCLUSTERED INDEX IX_KPIs_Owner_Priority_Performance
    ON monitoring.KPIs (Owner, Priority, IsActive)
    INCLUDE (KpiId, Indicator, Frequency, LastRun);
    PRINT 'Created index: IX_KPIs_Owner_Priority_Performance';
END

-- Index for historical data queries by KPI and timestamp
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HistoricalData_KpiId_Timestamp_Performance')
BEGIN
    CREATE NONCLUSTERED INDEX IX_HistoricalData_KpiId_Timestamp_Performance
    ON monitoring.HistoricalData (KpiId, Timestamp DESC)
    INCLUDE (Value, HistoricalValue, DeviationPercent, IsSuccessful, ExecutionTimeMs);
    PRINT 'Created index: IX_HistoricalData_KpiId_Timestamp_Performance';
END

-- Index for historical data queries by timestamp only (for dashboard)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HistoricalData_Timestamp_Performance')
BEGIN
    CREATE NONCLUSTERED INDEX IX_HistoricalData_Timestamp_Performance
    ON monitoring.HistoricalData (Timestamp DESC)
    INCLUDE (KpiId, Value, IsSuccessful, ExecutionTimeMs, ShouldAlert);
    PRINT 'Created index: IX_HistoricalData_Timestamp_Performance';
END

-- Index for alert logs by trigger time and resolution status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AlertLogs_TriggerTime_IsResolved_Performance')
BEGIN
    CREATE NONCLUSTERED INDEX IX_AlertLogs_TriggerTime_IsResolved_Performance
    ON monitoring.AlertLogs (TriggerTime DESC, IsResolved)
    INCLUDE (AlertId, KpiId, DeviationPercent, CurrentValue, HistoricalValue);
    PRINT 'Created index: IX_AlertLogs_TriggerTime_IsResolved_Performance';
END

-- Index for alert logs by KPI
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AlertLogs_KpiId_TriggerTime_Performance')
BEGIN
    CREATE NONCLUSTERED INDEX IX_AlertLogs_KpiId_TriggerTime_Performance
    ON monitoring.AlertLogs (KpiId, TriggerTime DESC)
    INCLUDE (AlertId, DeviationPercent, IsResolved, CurrentValue);
    PRINT 'Created index: IX_AlertLogs_KpiId_TriggerTime_Performance';
END

-- Index for KPI contacts for efficient joins
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KpiContacts_KpiId_ContactId_Performance')
BEGIN
    CREATE NONCLUSTERED INDEX IX_KpiContacts_KpiId_ContactId_Performance
    ON monitoring.KpiContacts (KpiId, ContactId);
    PRINT 'Created index: IX_KpiContacts_KpiId_ContactId_Performance';
END

-- =============================================
-- 2. COLUMNSTORE INDEXES FOR ANALYTICS
-- =============================================

-- Columnstore index for historical data analytics (if table is large enough)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HistoricalData_Columnstore_Analytics')
    AND (SELECT COUNT(*) FROM monitoring.HistoricalData) > 100000
BEGIN
    CREATE NONCLUSTERED COLUMNSTORE INDEX IX_HistoricalData_Columnstore_Analytics
    ON monitoring.HistoricalData (KpiId, Timestamp, Value, HistoricalValue, DeviationPercent, IsSuccessful, ExecutionTimeMs);
    PRINT 'Created columnstore index: IX_HistoricalData_Columnstore_Analytics';
END

-- =============================================
-- 3. STATISTICS UPDATES
-- =============================================

-- Update statistics on key tables
UPDATE STATISTICS monitoring.KPIs;
UPDATE STATISTICS monitoring.HistoricalData;
UPDATE STATISTICS monitoring.AlertLogs;
UPDATE STATISTICS monitoring.KpiContacts;
UPDATE STATISTICS monitoring.Contacts;
PRINT 'Updated statistics on all monitoring tables';

-- =============================================
-- 4. QUERY OPTIMIZATION VIEWS
-- =============================================

-- View for KPI summary with performance optimization
IF OBJECT_ID('monitoring.vw_KpiSummary_Optimized', 'V') IS NOT NULL
    DROP VIEW monitoring.vw_KpiSummary_Optimized;
GO

CREATE VIEW monitoring.vw_KpiSummary_Optimized
AS
SELECT 
    k.KpiId,
    k.Indicator,
    k.Owner,
    k.Priority,
    k.Frequency,
    k.IsActive,
    k.LastRun,
    k.KpiType,
    k.Deviation,
    k.CooldownMinutes,
    -- Performance metrics from recent executions
    recent.AvgExecutionTime,
    recent.LastExecutionSuccess,
    recent.RecentAlertCount
FROM monitoring.KPIs k
OUTER APPLY (
    SELECT TOP 1
        AVG(CAST(h.ExecutionTimeMs AS FLOAT)) OVER (PARTITION BY h.KpiId ORDER BY h.Timestamp DESC ROWS BETWEEN 9 PRECEDING AND CURRENT ROW) AS AvgExecutionTime,
        h.IsSuccessful AS LastExecutionSuccess,
        COUNT(a.AlertId) OVER (PARTITION BY h.KpiId ORDER BY h.Timestamp DESC ROWS BETWEEN 29 PRECEDING AND CURRENT ROW) AS RecentAlertCount
    FROM monitoring.HistoricalData h
    LEFT JOIN monitoring.AlertLogs a ON h.KpiId = a.KpiId AND a.TriggerTime >= DATEADD(day, -7, GETUTCDATE())
    WHERE h.KpiId = k.KpiId
    ORDER BY h.Timestamp DESC
) recent;
GO

-- View for dashboard metrics with performance optimization
IF OBJECT_ID('monitoring.vw_DashboardMetrics_Optimized', 'V') IS NOT NULL
    DROP VIEW monitoring.vw_DashboardMetrics_Optimized;
GO

CREATE VIEW monitoring.vw_DashboardMetrics_Optimized
AS
SELECT 
    -- KPI counts
    (SELECT COUNT(*) FROM monitoring.KPIs) AS TotalKpis,
    (SELECT COUNT(*) FROM monitoring.KPIs WHERE IsActive = 1) AS ActiveKpis,
    (SELECT COUNT(*) FROM monitoring.KPIs WHERE IsActive = 0) AS InactiveKpis,
    
    -- Recent execution metrics
    (SELECT COUNT(*) FROM monitoring.HistoricalData WHERE Timestamp >= CAST(GETUTCDATE() AS DATE)) AS ExecutionsToday,
    (SELECT COUNT(*) FROM monitoring.HistoricalData WHERE Timestamp >= CAST(GETUTCDATE() AS DATE) AND IsSuccessful = 1) AS SuccessfulExecutionsToday,
    (SELECT COUNT(*) FROM monitoring.HistoricalData WHERE Timestamp >= CAST(GETUTCDATE() AS DATE) AND IsSuccessful = 0) AS FailedExecutionsToday,
    
    -- Alert metrics
    (SELECT COUNT(*) FROM monitoring.AlertLogs WHERE TriggerTime >= CAST(GETUTCDATE() AS DATE)) AS AlertsToday,
    (SELECT COUNT(*) FROM monitoring.AlertLogs WHERE IsResolved = 0) AS UnresolvedAlerts,
    (SELECT COUNT(*) FROM monitoring.AlertLogs WHERE IsResolved = 0 AND DeviationPercent >= 50) AS CriticalAlerts,
    
    -- Performance metrics
    (SELECT AVG(CAST(ExecutionTimeMs AS FLOAT)) FROM monitoring.HistoricalData WHERE Timestamp >= DATEADD(hour, -24, GETUTCDATE()) AND ExecutionTimeMs IS NOT NULL) AS AvgExecutionTimeMs24h,
    (SELECT MAX(Timestamp) FROM monitoring.HistoricalData) AS LastExecutionTime;
GO

-- =============================================
-- 5. STORED PROCEDURES FOR OPTIMIZED QUERIES
-- =============================================

-- Optimized procedure for getting KPI list with pagination
IF OBJECT_ID('monitoring.usp_GetKpisOptimized', 'P') IS NOT NULL
    DROP PROCEDURE monitoring.usp_GetKpisOptimized;
GO

CREATE PROCEDURE monitoring.usp_GetKpisOptimized
    @IsActive BIT = NULL,
    @Owner NVARCHAR(100) = NULL,
    @Priority TINYINT = NULL,
    @SearchTerm NVARCHAR(255) = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 20,
    @SortBy NVARCHAR(50) = 'Indicator',
    @SortDescending BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @WhereClause NVARCHAR(MAX) = '';
    DECLARE @OrderClause NVARCHAR(MAX);
    
    -- Build WHERE clause dynamically
    IF @IsActive IS NOT NULL
        SET @WhereClause = @WhereClause + ' AND k.IsActive = @IsActive';
    
    IF @Owner IS NOT NULL
        SET @WhereClause = @WhereClause + ' AND k.Owner LIKE ''%'' + @Owner + ''%''';
    
    IF @Priority IS NOT NULL
        SET @WhereClause = @WhereClause + ' AND k.Priority = @Priority';
    
    IF @SearchTerm IS NOT NULL
        SET @WhereClause = @WhereClause + ' AND (k.Indicator LIKE ''%'' + @SearchTerm + ''%'' OR k.Owner LIKE ''%'' + @SearchTerm + ''%'' OR k.DescriptionTemplate LIKE ''%'' + @SearchTerm + ''%'')';
    
    -- Remove leading AND
    IF LEN(@WhereClause) > 0
        SET @WhereClause = 'WHERE 1=1' + @WhereClause;
    ELSE
        SET @WhereClause = 'WHERE 1=1';
    
    -- Build ORDER clause
    SET @OrderClause = CASE @SortBy
        WHEN 'Indicator' THEN 'k.Indicator'
        WHEN 'Owner' THEN 'k.Owner'
        WHEN 'Priority' THEN 'k.Priority'
        WHEN 'Frequency' THEN 'k.Frequency'
        WHEN 'LastRun' THEN 'k.LastRun'
        WHEN 'IsActive' THEN 'k.IsActive'
        ELSE 'k.Indicator'
    END;
    
    IF @SortDescending = 1
        SET @OrderClause = @OrderClause + ' DESC';
    
    -- Build and execute query
    SET @SQL = '
    SELECT 
        k.KpiId,
        k.Indicator,
        k.Owner,
        k.Priority,
        k.Frequency,
        k.IsActive,
        k.LastRun,
        k.KpiType,
        k.Deviation,
        k.CooldownMinutes,
        COUNT(*) OVER() AS TotalCount
    FROM monitoring.KPIs k
    ' + @WhereClause + '
    ORDER BY ' + @OrderClause + '
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY';
    
    EXEC sp_executesql @SQL, 
        N'@IsActive BIT, @Owner NVARCHAR(100), @Priority TINYINT, @SearchTerm NVARCHAR(255), @Offset INT, @PageSize INT',
        @IsActive, @Owner, @Priority, @SearchTerm, @Offset, @PageSize;
END;
GO

-- =============================================
-- 6. MAINTENANCE PROCEDURES
-- =============================================

-- Procedure for index maintenance
IF OBJECT_ID('monitoring.usp_OptimizeIndexes', 'P') IS NOT NULL
    DROP PROCEDURE monitoring.usp_OptimizeIndexes;
GO

CREATE PROCEDURE monitoring.usp_OptimizeIndexes
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SQL NVARCHAR(MAX);
    DECLARE @TableName NVARCHAR(128);
    DECLARE @IndexName NVARCHAR(128);
    DECLARE @FragmentationPercent FLOAT;
    
    DECLARE index_cursor CURSOR FOR
    SELECT 
        OBJECT_NAME(ips.object_id) AS TableName,
        i.name AS IndexName,
        ips.avg_fragmentation_in_percent
    FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
    INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
    WHERE ips.avg_fragmentation_in_percent > 10
        AND i.name IS NOT NULL
        AND OBJECT_SCHEMA_NAME(ips.object_id) = 'monitoring';
    
    OPEN index_cursor;
    FETCH NEXT FROM index_cursor INTO @TableName, @IndexName, @FragmentationPercent;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @FragmentationPercent > 30
        BEGIN
            SET @SQL = 'ALTER INDEX ' + QUOTENAME(@IndexName) + ' ON monitoring.' + QUOTENAME(@TableName) + ' REBUILD';
            PRINT 'Rebuilding index: ' + @IndexName + ' on table: ' + @TableName + ' (Fragmentation: ' + CAST(@FragmentationPercent AS VARCHAR(10)) + '%)';
        END
        ELSE
        BEGIN
            SET @SQL = 'ALTER INDEX ' + QUOTENAME(@IndexName) + ' ON monitoring.' + QUOTENAME(@TableName) + ' REORGANIZE';
            PRINT 'Reorganizing index: ' + @IndexName + ' on table: ' + @TableName + ' (Fragmentation: ' + CAST(@FragmentationPercent AS VARCHAR(10)) + '%)';
        END
        
        EXEC sp_executesql @SQL;
        
        FETCH NEXT FROM index_cursor INTO @TableName, @IndexName, @FragmentationPercent;
    END;
    
    CLOSE index_cursor;
    DEALLOCATE index_cursor;
    
    -- Update statistics
    EXEC sp_updatestats;
    PRINT 'Index optimization completed';
END;
GO

PRINT 'Performance optimization scripts completed successfully';
