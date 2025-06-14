-- =============================================
-- MonitoringGrid Database Performance Optimization
-- Replaces legacy KPI optimization scripts with modern Indicator optimizations
-- =============================================

USE PopAI;
GO

-- =============================================
-- 1. INDICATOR TABLE OPTIMIZATIONS
-- =============================================

-- Primary index on IndicatorID (should already exist as PK)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'PK_Indicators')
BEGIN
    ALTER TABLE monitoring.Indicators ADD CONSTRAINT PK_Indicators PRIMARY KEY CLUSTERED (IndicatorID);
    PRINT 'Created primary key index on Indicators.IndicatorID';
END

-- Index for active indicators (most common query)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'IX_Indicators_IsActive_IndicatorName')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Indicators_IsActive_IndicatorName 
    ON monitoring.Indicators (IsActive, IndicatorName)
    INCLUDE (OwnerContactId, Priority, CollectorID, ThresholdType, ThresholdValue, LastRun, IsCurrentlyRunning);
    PRINT 'Created index for active indicators query optimization';
END

-- Index for owner-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'IX_Indicators_OwnerContactId_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Indicators_OwnerContactId_IsActive 
    ON monitoring.Indicators (OwnerContactId, IsActive)
    INCLUDE (IndicatorName, Priority, LastRun);
    PRINT 'Created index for owner-based indicator queries';
END

-- Index for priority-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'IX_Indicators_Priority_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Indicators_Priority_IsActive 
    ON monitoring.Indicators (Priority, IsActive)
    INCLUDE (IndicatorName, OwnerContactId, LastRun);
    PRINT 'Created index for priority-based indicator queries';
END

-- Index for collector-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'IX_Indicators_CollectorID_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Indicators_CollectorID_IsActive 
    ON monitoring.Indicators (CollectorID, IsActive)
    INCLUDE (IndicatorName, CollectorItemName, LastRun);
    PRINT 'Created index for collector-based indicator queries';
END

-- Index for due indicators (LastRun + frequency calculations)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'IX_Indicators_LastRun_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Indicators_LastRun_IsActive 
    ON monitoring.Indicators (LastRun, IsActive)
    INCLUDE (IndicatorName, SchedulerID, IsCurrentlyRunning);
    PRINT 'Created index for due indicators optimization';
END

-- Index for currently running indicators
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'IX_Indicators_IsCurrentlyRunning')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Indicators_IsCurrentlyRunning 
    ON monitoring.Indicators (IsCurrentlyRunning)
    WHERE IsCurrentlyRunning = 1
    INCLUDE (IndicatorName, LastRun, OwnerContactId);
    PRINT 'Created filtered index for currently running indicators';
END

-- =============================================
-- 2. INDICATOR CONTACTS TABLE OPTIMIZATIONS
-- =============================================

-- Composite index for indicator-contact relationships
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.IndicatorContacts') AND name = 'IX_IndicatorContacts_IndicatorID_ContactId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_IndicatorContacts_IndicatorID_ContactId 
    ON monitoring.IndicatorContacts (IndicatorID, ContactId);
    PRINT 'Created index for indicator-contact relationships';
END

-- Reverse index for contact-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.IndicatorContacts') AND name = 'IX_IndicatorContacts_ContactId_IndicatorID')
BEGIN
    CREATE NONCLUSTERED INDEX IX_IndicatorContacts_ContactId_IndicatorID 
    ON monitoring.IndicatorContacts (ContactId, IndicatorID);
    PRINT 'Created reverse index for contact-based indicator queries';
END

-- =============================================
-- 3. CONTACTS TABLE OPTIMIZATIONS
-- =============================================

-- Index for active contacts
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Contacts') AND name = 'IX_Contacts_IsActive_ContactName')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Contacts_IsActive_ContactName 
    ON monitoring.Contacts (IsActive, ContactName)
    INCLUDE (Email, PhoneNumber, Priority);
    PRINT 'Created index for active contacts optimization';
END

-- Index for email-based lookups
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Contacts') AND name = 'IX_Contacts_Email')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Contacts_Email 
    ON monitoring.Contacts (Email)
    WHERE Email IS NOT NULL AND Email != ''
    INCLUDE (ContactName, IsActive);
    PRINT 'Created index for email-based contact lookups';
END

-- =============================================
-- 4. ALERT LOGS TABLE OPTIMIZATIONS
-- =============================================

-- Index for indicator-based alert queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.AlertLogs') AND name = 'IX_AlertLogs_IndicatorId_TriggerTime')
BEGIN
    CREATE NONCLUSTERED INDEX IX_AlertLogs_IndicatorId_TriggerTime 
    ON monitoring.AlertLogs (IndicatorId, TriggerTime DESC)
    INCLUDE (AlertType, CurrentValue, ThresholdValue, IsResolved);
    PRINT 'Created index for indicator-based alert queries';
END

-- Index for time-based alert queries (recent alerts)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.AlertLogs') AND name = 'IX_AlertLogs_TriggerTime_IsResolved')
BEGIN
    CREATE NONCLUSTERED INDEX IX_AlertLogs_TriggerTime_IsResolved 
    ON monitoring.AlertLogs (TriggerTime DESC, IsResolved)
    INCLUDE (IndicatorId, AlertType, CurrentValue);
    PRINT 'Created index for time-based alert queries';
END

-- Index for unresolved alerts
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.AlertLogs') AND name = 'IX_AlertLogs_IsResolved_TriggerTime')
BEGIN
    CREATE NONCLUSTERED INDEX IX_AlertLogs_IsResolved_TriggerTime 
    ON monitoring.AlertLogs (IsResolved, TriggerTime DESC)
    WHERE IsResolved = 0
    INCLUDE (IndicatorId, AlertType, CurrentValue);
    PRINT 'Created filtered index for unresolved alerts';
END

-- =============================================
-- 5. SCHEDULERS TABLE OPTIMIZATIONS
-- =============================================

-- Index for active schedulers
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Schedulers') AND name = 'IX_Schedulers_IsEnabled_SchedulerName')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Schedulers_IsEnabled_SchedulerName 
    ON monitoring.Schedulers (IsEnabled, SchedulerName)
    INCLUDE (ScheduleType, CronExpression, IntervalMinutes);
    PRINT 'Created index for active schedulers optimization';
END

-- Index for schedule type queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Schedulers') AND name = 'IX_Schedulers_ScheduleType_IsEnabled')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Schedulers_ScheduleType_IsEnabled 
    ON monitoring.Schedulers (ScheduleType, IsEnabled)
    INCLUDE (SchedulerName, CronExpression, IntervalMinutes);
    PRINT 'Created index for schedule type queries';
END

-- =============================================
-- 6. MONITOR STATISTICS TABLE OPTIMIZATIONS
-- =============================================

-- Index for collector and time-based queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.MonitorStatistics') AND name = 'IX_MonitorStatistics_CollectorID_Timestamp')
BEGIN
    CREATE NONCLUSTERED INDEX IX_MonitorStatistics_CollectorID_Timestamp 
    ON monitoring.MonitorStatistics (CollectorID, Timestamp DESC)
    INCLUDE (ItemName, Total, Average, ByHour);
    PRINT 'Created index for collector-based statistics queries';
END

-- Index for item-specific queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.MonitorStatistics') AND name = 'IX_MonitorStatistics_CollectorID_ItemName_Timestamp')
BEGIN
    CREATE NONCLUSTERED INDEX IX_MonitorStatistics_CollectorID_ItemName_Timestamp 
    ON monitoring.MonitorStatistics (CollectorID, ItemName, Timestamp DESC)
    INCLUDE (Total, Average, ByHour);
    PRINT 'Created index for item-specific statistics queries';
END

-- Index for time-range queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.MonitorStatistics') AND name = 'IX_MonitorStatistics_Timestamp_CollectorID')
BEGIN
    CREATE NONCLUSTERED INDEX IX_MonitorStatistics_Timestamp_CollectorID 
    ON monitoring.MonitorStatistics (Timestamp DESC, CollectorID)
    INCLUDE (ItemName, Total, Average);
    PRINT 'Created index for time-range statistics queries';
END

-- =============================================
-- 7. UPDATE STATISTICS FOR ALL TABLES
-- =============================================

UPDATE STATISTICS monitoring.Indicators;
UPDATE STATISTICS monitoring.IndicatorContacts;
UPDATE STATISTICS monitoring.Contacts;
UPDATE STATISTICS monitoring.AlertLogs;
UPDATE STATISTICS monitoring.Schedulers;
UPDATE STATISTICS monitoring.MonitorStatistics;

PRINT 'Updated statistics for all monitoring tables';

-- =============================================
-- 8. PERFORMANCE MONITORING VIEWS
-- =============================================

-- View for indicator performance metrics
IF OBJECT_ID('monitoring.vw_IndicatorPerformanceMetrics', 'V') IS NOT NULL
    DROP VIEW monitoring.vw_IndicatorPerformanceMetrics;
GO

CREATE VIEW monitoring.vw_IndicatorPerformanceMetrics
AS
SELECT 
    i.IndicatorID,
    i.IndicatorName,
    i.Priority,
    i.IsActive,
    i.IsCurrentlyRunning,
    i.LastRun,
    CASE 
        WHEN s.ScheduleType = 'cron' THEN 'Cron: ' + s.CronExpression
        WHEN s.ScheduleType = 'interval' THEN 'Every ' + CAST(s.IntervalMinutes AS VARCHAR) + ' minutes'
        ELSE 'Manual'
    END AS ScheduleDescription,
    DATEDIFF(MINUTE, i.LastRun, GETUTCDATE()) AS MinutesSinceLastRun,
    (SELECT COUNT(*) FROM monitoring.AlertLogs al WHERE al.IndicatorId = i.IndicatorID AND al.TriggerTime >= DATEADD(DAY, -7, GETUTCDATE())) AS AlertsLast7Days,
    (SELECT COUNT(*) FROM monitoring.AlertLogs al WHERE al.IndicatorId = i.IndicatorID AND al.TriggerTime >= DATEADD(DAY, -30, GETUTCDATE())) AS AlertsLast30Days,
    c.ContactName AS OwnerName,
    c.Email AS OwnerEmail
FROM monitoring.Indicators i
LEFT JOIN monitoring.Schedulers s ON i.SchedulerID = s.SchedulerID
LEFT JOIN monitoring.Contacts c ON i.OwnerContactId = c.ContactId;
GO

PRINT 'Created indicator performance metrics view';

-- View for system health dashboard
IF OBJECT_ID('monitoring.vw_SystemHealthDashboard', 'V') IS NOT NULL
    DROP VIEW monitoring.vw_SystemHealthDashboard;
GO

CREATE VIEW monitoring.vw_SystemHealthDashboard
AS
SELECT 
    (SELECT COUNT(*) FROM monitoring.Indicators WHERE IsActive = 1) AS ActiveIndicators,
    (SELECT COUNT(*) FROM monitoring.Indicators WHERE IsCurrentlyRunning = 1) AS RunningIndicators,
    (SELECT COUNT(*) FROM monitoring.AlertLogs WHERE TriggerTime >= DATEADD(HOUR, -24, GETUTCDATE()) AND IsResolved = 0) AS UnresolvedAlertsLast24Hours,
    (SELECT COUNT(*) FROM monitoring.AlertLogs WHERE TriggerTime >= DATEADD(HOUR, -1, GETUTCDATE())) AS AlertsLastHour,
    (SELECT COUNT(*) FROM monitoring.Contacts WHERE IsActive = 1) AS ActiveContacts,
    (SELECT COUNT(*) FROM monitoring.Schedulers WHERE IsEnabled = 1) AS EnabledSchedulers,
    (SELECT MAX(Timestamp) FROM monitoring.MonitorStatistics) AS LastStatisticsUpdate;
GO

PRINT 'Created system health dashboard view';

-- =============================================
-- 9. CLEANUP OLD INDEXES (if they exist with old names)
-- =============================================

-- Remove old KPI-related indexes if they exist
IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'IX_KPIs_IsActive')
BEGIN
    DROP INDEX IX_KPIs_IsActive ON monitoring.KPIs;
    PRINT 'Removed old KPI index: IX_KPIs_IsActive';
END

IF EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.KpiContacts') AND name = 'IX_KpiContacts_KpiId')
BEGIN
    DROP INDEX IX_KpiContacts_KpiId ON monitoring.KpiContacts;
    PRINT 'Removed old KPI contacts index: IX_KpiContacts_KpiId';
END

-- =============================================
-- COMPLETION MESSAGE
-- =============================================

PRINT '';
PRINT '=============================================';
PRINT 'MonitoringGrid Performance Optimization Complete';
PRINT '=============================================';
PRINT 'Optimizations applied:';
PRINT '- Indicator table indexes for common query patterns';
PRINT '- Contact and alert log performance indexes';
PRINT '- Scheduler optimization indexes';
PRINT '- Statistics table performance indexes';
PRINT '- Performance monitoring views created';
PRINT '- Statistics updated for all tables';
PRINT '- Legacy KPI indexes cleaned up';
PRINT '';
PRINT 'Database is now optimized for MonitoringGrid operations.';
PRINT '=============================================';
