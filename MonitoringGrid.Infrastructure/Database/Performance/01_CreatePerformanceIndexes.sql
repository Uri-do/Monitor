-- =============================================
-- Performance Indexes for MonitoringGrid Database
-- Created: 2024-01-07
-- Purpose: Essential indexes for optimal query performance
-- =============================================

USE [PopAI]
GO

PRINT 'Creating performance indexes for MonitoringGrid...'

-- =============================================
-- 1. AlertLogs Performance Indexes
-- =============================================

-- Index for alert log queries by trigger time and resolution status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AlertLogs_TriggerTime_IsResolved')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AlertLogs_TriggerTime_IsResolved] 
    ON [monitoring].[AlertLogs] ([TriggerTime] DESC, [IsResolved])
    INCLUDE ([KpiId], [DeviationPercent], [AlertMessage], [Severity])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created index: IX_AlertLogs_TriggerTime_IsResolved'
END
ELSE
    PRINT '⚠️ Index already exists: IX_AlertLogs_TriggerTime_IsResolved'

-- Index for alert log queries by KPI and status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AlertLogs_KpiId_IsResolved_TriggerTime')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AlertLogs_KpiId_IsResolved_TriggerTime] 
    ON [monitoring].[AlertLogs] ([KpiId], [IsResolved], [TriggerTime] DESC)
    INCLUDE ([DeviationPercent], [AlertMessage], [Severity], [ResolvedAt])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created index: IX_AlertLogs_KpiId_IsResolved_TriggerTime'
END
ELSE
    PRINT '⚠️ Index already exists: IX_AlertLogs_KpiId_IsResolved_TriggerTime'

-- Index for alert severity and recent alerts
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AlertLogs_Severity_TriggerTime')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_AlertLogs_Severity_TriggerTime] 
    ON [monitoring].[AlertLogs] ([Severity], [TriggerTime] DESC)
    WHERE [IsResolved] = 0  -- Filtered index for active alerts only
    INCLUDE ([KpiId], [DeviationPercent], [AlertMessage])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created filtered index: IX_AlertLogs_Severity_TriggerTime'
END
ELSE
    PRINT '⚠️ Index already exists: IX_AlertLogs_Severity_TriggerTime'

-- =============================================
-- 2. HistoricalData Performance Indexes
-- =============================================

-- Primary index for historical data queries by KPI and time
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HistoricalData_KpiId_Timestamp')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HistoricalData_KpiId_Timestamp] 
    ON [monitoring].[HistoricalData] ([KpiId], [Timestamp] DESC)
    INCLUDE ([Value], [DeviationPercent], [IsSuccessful], [ExecutionTimeMs], [HistoricalValue])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created index: IX_HistoricalData_KpiId_Timestamp'
END
ELSE
    PRINT '⚠️ Index already exists: IX_HistoricalData_KpiId_Timestamp'

-- Index for successful executions and performance analysis
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HistoricalData_IsSuccessful_Timestamp')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HistoricalData_IsSuccessful_Timestamp] 
    ON [monitoring].[HistoricalData] ([IsSuccessful], [Timestamp] DESC)
    INCLUDE ([KpiId], [ExecutionTimeMs], [Value], [DeviationPercent])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created index: IX_HistoricalData_IsSuccessful_Timestamp'
END
ELSE
    PRINT '⚠️ Index already exists: IX_HistoricalData_IsSuccessful_Timestamp'

-- Index for deviation analysis
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_HistoricalData_DeviationPercent_Timestamp')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_HistoricalData_DeviationPercent_Timestamp] 
    ON [monitoring].[HistoricalData] ([DeviationPercent] DESC, [Timestamp] DESC)
    WHERE [DeviationPercent] > 0  -- Filtered index for deviations only
    INCLUDE ([KpiId], [Value], [HistoricalValue], [IsSuccessful])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created filtered index: IX_HistoricalData_DeviationPercent_Timestamp'
END
ELSE
    PRINT '⚠️ Index already exists: IX_HistoricalData_DeviationPercent_Timestamp'

-- =============================================
-- 3. KPIs Performance Indexes
-- =============================================

-- Index for active KPIs and scheduling
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KPIs_IsActive_LastRun')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_KPIs_IsActive_LastRun] 
    ON [monitoring].[KPIs] ([IsActive], [LastRun])
    WHERE [IsActive] = 1  -- Filtered index for active KPIs only
    INCLUDE ([Frequency], [Indicator], [Owner], [Priority], [Deviation])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created filtered index: IX_KPIs_IsActive_LastRun'
END
ELSE
    PRINT '⚠️ Index already exists: IX_KPIs_IsActive_LastRun'

-- Index for KPI ownership and management
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KPIs_Owner_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_KPIs_Owner_IsActive] 
    ON [monitoring].[KPIs] ([Owner], [IsActive])
    INCLUDE ([Indicator], [Priority], [LastRun], [Frequency])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created index: IX_KPIs_Owner_IsActive'
END
ELSE
    PRINT '⚠️ Index already exists: IX_KPIs_Owner_IsActive'

-- Index for priority-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KPIs_Priority_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_KPIs_Priority_IsActive] 
    ON [monitoring].[KPIs] ([Priority], [IsActive])
    INCLUDE ([Indicator], [Owner], [LastRun], [Frequency])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created index: IX_KPIs_Priority_IsActive'
END
ELSE
    PRINT '⚠️ Index already exists: IX_KPIs_Priority_IsActive'

-- Unique index for indicator names (business rule enforcement)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KPIs_Indicator_Unique')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_KPIs_Indicator_Unique] 
    ON [monitoring].[KPIs] ([Indicator])
    WHERE [IsActive] = 1  -- Only enforce uniqueness for active KPIs
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created unique index: IX_KPIs_Indicator_Unique'
END
ELSE
    PRINT '⚠️ Index already exists: IX_KPIs_Indicator_Unique'

-- =============================================
-- 4. Alerts Performance Indexes
-- =============================================

-- Index for alert management by KPI
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Alerts_KpiId_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Alerts_KpiId_IsActive] 
    ON [monitoring].[Alerts] ([KpiId], [IsActive])
    INCLUDE ([AlertName], [Threshold], [Operator], [Severity], [Recipients])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created index: IX_Alerts_KpiId_IsActive'
END
ELSE
    PRINT '⚠️ Index already exists: IX_Alerts_KpiId_IsActive'

-- Index for alert severity management
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Alerts_Severity_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Alerts_Severity_IsActive] 
    ON [monitoring].[Alerts] ([Severity], [IsActive])
    INCLUDE ([AlertName], [KpiId], [Threshold], [Recipients])
    WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
          DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    
    PRINT '✅ Created index: IX_Alerts_Severity_IsActive'
END
ELSE
    PRINT '⚠️ Index already exists: IX_Alerts_Severity_IsActive'

-- =============================================
-- 5. Update Statistics
-- =============================================

PRINT 'Updating statistics for all tables...'

UPDATE STATISTICS [monitoring].[AlertLogs] WITH FULLSCAN
UPDATE STATISTICS [monitoring].[HistoricalData] WITH FULLSCAN  
UPDATE STATISTICS [monitoring].[KPIs] WITH FULLSCAN
UPDATE STATISTICS [monitoring].[Alerts] WITH FULLSCAN

PRINT '✅ Statistics updated for all tables'

-- =============================================
-- 6. Index Usage Report
-- =============================================

PRINT 'Performance indexes creation completed!'
PRINT 'Created indexes:'
PRINT '  - AlertLogs: 3 indexes (including 1 filtered)'
PRINT '  - HistoricalData: 3 indexes (including 1 filtered)'  
PRINT '  - KPIs: 4 indexes (including 2 filtered, 1 unique)'
PRINT '  - Alerts: 2 indexes'
PRINT ''
PRINT 'Total: 12 performance indexes created'
PRINT 'Recommendation: Monitor index usage with sys.dm_db_index_usage_stats'

GO
