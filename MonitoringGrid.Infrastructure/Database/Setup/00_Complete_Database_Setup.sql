-- =============================================
-- MonitoringGrid Complete Database Setup
-- Modern Indicator-based monitoring system setup
-- =============================================

-- This script sets up the complete MonitoringGrid database with modern Indicator terminology
-- Replaces all legacy KPI scripts with a single, comprehensive setup

USE master;
GO

PRINT '=============================================';
PRINT 'MonitoringGrid Database Setup Starting...';
PRINT '=============================================';

-- =============================================
-- 1. CREATE DATABASE
-- =============================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PopAI')
BEGIN
    CREATE DATABASE [PopAI]
    ON 
    ( NAME = 'PopAI_Data',
      FILENAME = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PopAI.mdf',
      SIZE = 500MB,
      MAXSIZE = 10GB,
      FILEGROWTH = 50MB )
    LOG ON 
    ( NAME = 'PopAI_Log',
      FILENAME = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PopAI.ldf',
      SIZE = 50MB,
      MAXSIZE = 1GB,
      FILEGROWTH = 10MB );
    
    PRINT '✅ PopAI database created successfully';
END
ELSE
BEGIN
    PRINT '✅ PopAI database already exists';
END
GO

USE [PopAI];
GO

-- Set database options for optimal performance
ALTER DATABASE [PopAI] SET RECOVERY SIMPLE;
ALTER DATABASE [PopAI] SET AUTO_CLOSE OFF;
ALTER DATABASE [PopAI] SET AUTO_SHRINK OFF;
ALTER DATABASE [PopAI] SET AUTO_CREATE_STATISTICS ON;
ALTER DATABASE [PopAI] SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE [PopAI] SET AUTO_UPDATE_STATISTICS_ASYNC ON;

PRINT '✅ Database options configured for optimal performance';

-- =============================================
-- 2. CREATE SCHEMAS
-- =============================================

-- Create monitoring schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'monitoring')
BEGIN
    EXEC('CREATE SCHEMA [monitoring]');
    PRINT '✅ Schema [monitoring] created';
END
ELSE
BEGIN
    PRINT '✅ Schema [monitoring] already exists';
END

-- Create auth schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'auth')
BEGIN
    EXEC('CREATE SCHEMA [auth]');
    PRINT '✅ Schema [auth] created';
END
ELSE
BEGIN
    PRINT '✅ Schema [auth] already exists';
END

-- Create stats schema for ProgressPlayDB integration
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'stats')
BEGIN
    EXEC('CREATE SCHEMA [stats]');
    PRINT '✅ Schema [stats] created';
END
ELSE
BEGIN
    PRINT '✅ Schema [stats] already exists';
END

-- =============================================
-- 3. CREATE CORE MONITORING TABLES
-- =============================================

-- Contacts table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.Contacts') AND type = 'U')
BEGIN
    CREATE TABLE monitoring.Contacts (
        ContactId INT IDENTITY(1,1) PRIMARY KEY,
        ContactName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(255) NOT NULL,
        PhoneNumber NVARCHAR(20) NULL,
        Priority INT NOT NULL DEFAULT 2, -- 1=SMS+Email, 2=Email only
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT UK_Contacts_Email UNIQUE (Email),
        CONSTRAINT CK_Contacts_Priority CHECK (Priority IN (1, 2))
    );
    
    PRINT '✅ Table [monitoring.Contacts] created';
END

-- Schedulers table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.Schedulers') AND type = 'U')
BEGIN
    CREATE TABLE monitoring.Schedulers (
        SchedulerID INT IDENTITY(1,1) PRIMARY KEY,
        SchedulerName NVARCHAR(100) NOT NULL,
        SchedulerDescription NVARCHAR(500) NULL,
        ScheduleType NVARCHAR(20) NOT NULL DEFAULT 'interval', -- 'interval', 'cron', 'manual'
        IntervalMinutes INT NULL,
        CronExpression NVARCHAR(100) NULL,
        ExecutionDateTime DATETIME2 NULL,
        StartDate DATE NULL,
        EndDate DATE NULL,
        Timezone NVARCHAR(50) NOT NULL DEFAULT 'UTC',
        IsEnabled BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT UK_Schedulers_Name UNIQUE (SchedulerName),
        CONSTRAINT CK_Schedulers_ScheduleType CHECK (ScheduleType IN ('interval', 'cron', 'manual')),
        CONSTRAINT CK_Schedulers_IntervalMinutes CHECK (IntervalMinutes IS NULL OR IntervalMinutes > 0),
        CONSTRAINT CK_Schedulers_CronExpression CHECK (
            (ScheduleType = 'cron' AND CronExpression IS NOT NULL) OR 
            (ScheduleType != 'cron' AND CronExpression IS NULL)
        ),
        CONSTRAINT CK_Schedulers_IntervalRequired CHECK (
            (ScheduleType = 'interval' AND IntervalMinutes IS NOT NULL) OR 
            (ScheduleType != 'interval')
        )
    );
    
    PRINT '✅ Table [monitoring.Schedulers] created';
END

-- Indicators table (modern replacement for KPIs)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.Indicators') AND type = 'U')
BEGIN
    CREATE TABLE monitoring.Indicators (
        IndicatorID BIGINT IDENTITY(1,1) PRIMARY KEY,
        IndicatorName NVARCHAR(200) NOT NULL,
        IndicatorCode NVARCHAR(50) NOT NULL,
        IndicatorDesc NVARCHAR(1000) NULL,
        OwnerContactId INT NOT NULL,
        Priority NVARCHAR(20) NOT NULL DEFAULT 'medium', -- 'high', 'medium', 'low'
        CollectorID BIGINT NOT NULL,
        CollectorItemName NVARCHAR(200) NOT NULL DEFAULT 'Total',
        LastMinutes INT NOT NULL DEFAULT 1440,
        ThresholdType NVARCHAR(50) NOT NULL DEFAULT 'threshold_value', -- 'threshold_value', 'threshold_percentage'
        ThresholdField NVARCHAR(100) NOT NULL DEFAULT 'Total',
        ThresholdComparison NVARCHAR(10) NOT NULL DEFAULT 'gt', -- 'gt', 'lt', 'eq', 'gte', 'lte'
        ThresholdValue DECIMAL(18,2) NULL,
        AverageLastDays INT NOT NULL DEFAULT 28,
        IsActive BIT NOT NULL DEFAULT 1,
        IsCurrentlyRunning BIT NOT NULL DEFAULT 0,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        LastRun DATETIME2 NULL,
        SchedulerID INT NULL,
        
        CONSTRAINT FK_Indicators_OwnerContact FOREIGN KEY (OwnerContactId) REFERENCES monitoring.Contacts(ContactId),
        CONSTRAINT FK_Indicators_Scheduler FOREIGN KEY (SchedulerID) REFERENCES monitoring.Schedulers(SchedulerID),
        CONSTRAINT UK_Indicators_Code UNIQUE (IndicatorCode),
        CONSTRAINT CK_Indicators_Priority CHECK (Priority IN ('high', 'medium', 'low')),
        CONSTRAINT CK_Indicators_ThresholdType CHECK (ThresholdType IN ('threshold_value', 'threshold_percentage')),
        CONSTRAINT CK_Indicators_ThresholdComparison CHECK (ThresholdComparison IN ('gt', 'lt', 'eq', 'gte', 'lte')),
        CONSTRAINT CK_Indicators_LastMinutes CHECK (LastMinutes > 0),
        CONSTRAINT CK_Indicators_AverageLastDays CHECK (AverageLastDays > 0)
    );
    
    PRINT '✅ Table [monitoring.Indicators] created';
END

-- IndicatorContacts junction table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.IndicatorContacts') AND type = 'U')
BEGIN
    CREATE TABLE monitoring.IndicatorContacts (
        IndicatorContactID BIGINT IDENTITY(1,1) PRIMARY KEY,
        IndicatorID BIGINT NOT NULL,
        ContactId INT NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT FK_IndicatorContacts_Indicator FOREIGN KEY (IndicatorID) REFERENCES monitoring.Indicators(IndicatorID) ON DELETE CASCADE,
        CONSTRAINT FK_IndicatorContacts_Contact FOREIGN KEY (ContactId) REFERENCES monitoring.Contacts(ContactId) ON DELETE CASCADE,
        CONSTRAINT UK_IndicatorContacts_Unique UNIQUE (IndicatorID, ContactId)
    );
    
    PRINT '✅ Table [monitoring.IndicatorContacts] created';
END

-- AlertLogs table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.AlertLogs') AND type = 'U')
BEGIN
    CREATE TABLE monitoring.AlertLogs (
        AlertLogID BIGINT IDENTITY(1,1) PRIMARY KEY,
        IndicatorId BIGINT NOT NULL,
        AlertType NVARCHAR(50) NOT NULL DEFAULT 'threshold_breach',
        TriggerTime DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CurrentValue DECIMAL(18,2) NULL,
        ThresholdValue DECIMAL(18,2) NULL,
        HistoricalValue DECIMAL(18,2) NULL,
        DeviationPercentage DECIMAL(5,2) NULL,
        AlertMessage NVARCHAR(MAX) NULL,
        ContactsNotified INT NOT NULL DEFAULT 0,
        EmailsSent INT NOT NULL DEFAULT 0,
        SmsSent INT NOT NULL DEFAULT 0,
        IsResolved BIT NOT NULL DEFAULT 0,
        ResolvedDate DATETIME2 NULL,
        ResolvedBy NVARCHAR(100) NULL,
        
        CONSTRAINT FK_AlertLogs_Indicator FOREIGN KEY (IndicatorId) REFERENCES monitoring.Indicators(IndicatorID),
        CONSTRAINT CK_AlertLogs_AlertType CHECK (AlertType IN ('threshold_breach', 'execution_failure', 'data_anomaly', 'system_error'))
    );
    
    PRINT '✅ Table [monitoring.AlertLogs] created';
END

-- MonitorStatistics table for ProgressPlayDB integration
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.MonitorStatistics') AND type = 'U')
BEGIN
    CREATE TABLE monitoring.MonitorStatistics (
        StatisticID BIGINT IDENTITY(1,1) PRIMARY KEY,
        CollectorID BIGINT NOT NULL,
        ItemName NVARCHAR(200) NOT NULL,
        Timestamp DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Total DECIMAL(18,2) NOT NULL DEFAULT 0,
        Average DECIMAL(18,2) NOT NULL DEFAULT 0,
        ByHour NVARCHAR(MAX) NULL, -- JSON data for hourly breakdown
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        
        CONSTRAINT UK_MonitorStatistics_Unique UNIQUE (CollectorID, ItemName, Timestamp)
    );
    
    PRINT '✅ Table [monitoring.MonitorStatistics] created';
END

-- SystemStatus table for health monitoring
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.SystemStatus') AND type = 'U')
BEGIN
    CREATE TABLE monitoring.SystemStatus (
        StatusId INT IDENTITY(1,1) PRIMARY KEY,
        ServiceName NVARCHAR(100) NOT NULL,
        LastHeartbeat DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Status NVARCHAR(50) NOT NULL, -- 'Running', 'Stopped', 'Error'
        ErrorMessage NVARCHAR(MAX) NULL,
        ProcessedIndicators INT NOT NULL DEFAULT 0,
        AlertsSent INT NOT NULL DEFAULT 0,
        
        CONSTRAINT UK_SystemStatus_ServiceName UNIQUE (ServiceName),
        CONSTRAINT CK_SystemStatus_Status CHECK (Status IN ('Running', 'Stopped', 'Error'))
    );
    
    PRINT '✅ Table [monitoring.SystemStatus] created';
END

PRINT '✅ All core monitoring tables created successfully';

-- =============================================
-- 4. INSERT DEFAULT DATA
-- =============================================

-- Insert default schedulers
MERGE monitoring.Schedulers AS target
USING (VALUES
    ('Every 5 Minutes', 'Execute every 5 minutes', 'interval', 5, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Every 15 Minutes', 'Execute every 15 minutes', 'interval', 15, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Every 30 Minutes', 'Execute every 30 minutes', 'interval', 30, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Hourly', 'Execute every hour', 'interval', 60, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Every 2 Hours', 'Execute every 2 hours', 'interval', 120, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Every 4 Hours', 'Execute every 4 hours', 'interval', 240, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Daily at Midnight', 'Execute daily at midnight UTC', 'cron', NULL, '0 0 0 * * ?', NULL, NULL, NULL, 'UTC', 1),
    ('Hourly on the Hour', 'Execute at the top of every hour', 'cron', NULL, '0 0 * * * ?', NULL, NULL, NULL, 'UTC', 1),
    ('Manual Execution', 'Manual execution only', 'manual', NULL, NULL, NULL, NULL, NULL, 'UTC', 1)
) AS source (SchedulerName, SchedulerDescription, ScheduleType, IntervalMinutes, CronExpression, ExecutionDateTime, StartDate, EndDate, Timezone, IsEnabled)
ON target.SchedulerName = source.SchedulerName
WHEN NOT MATCHED THEN
    INSERT (SchedulerName, SchedulerDescription, ScheduleType, IntervalMinutes, CronExpression, ExecutionDateTime, StartDate, EndDate, Timezone, IsEnabled)
    VALUES (source.SchedulerName, source.SchedulerDescription, source.ScheduleType, source.IntervalMinutes, source.CronExpression, source.ExecutionDateTime, source.StartDate, source.EndDate, source.Timezone, source.IsEnabled);

PRINT '✅ Default schedulers inserted';

-- Insert default contacts
MERGE monitoring.Contacts AS target
USING (VALUES
    ('System Administrator', 'admin@monitoringgrid.com', '+1-555-0100', 1, 1),
    ('Development Team', 'dev@monitoringgrid.com', '+1-555-0101', 2, 1),
    ('Operations Team', 'ops@monitoringgrid.com', '+1-555-0102', 1, 1)
) AS source (ContactName, Email, PhoneNumber, Priority, IsActive)
ON target.Email = source.Email
WHEN NOT MATCHED THEN
    INSERT (ContactName, Email, PhoneNumber, Priority, IsActive)
    VALUES (source.ContactName, source.Email, source.PhoneNumber, source.Priority, source.IsActive);

PRINT '✅ Default contacts inserted';

PRINT '';
PRINT '=============================================';
PRINT 'MonitoringGrid Database Setup Complete!';
PRINT '=============================================';
PRINT 'Next steps:';
PRINT '1. Run Entity Framework migrations for any additional schema changes';
PRINT '2. Configure authentication tables if needed';
PRINT '3. Set up ProgressPlayDB integration';
PRINT '4. Run performance optimization script';
PRINT '5. Configure monitoring stored procedures';
PRINT '';
PRINT 'Database is ready for MonitoringGrid operations!';
PRINT '=============================================';
