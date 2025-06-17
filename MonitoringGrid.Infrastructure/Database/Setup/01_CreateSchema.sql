-- Monitoring Grid Database Schema
-- This script creates the complete database schema for the monitoring system

USE [PopAI]
GO

-- Create monitoring schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'monitoring')
BEGIN
    EXEC('CREATE SCHEMA monitoring')
END
GO

-- Legacy KPI Configuration Table (deprecated - use Indicators table instead)
-- This table is maintained for backward compatibility only
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KPIs') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.KPIs (
        KpiId INT IDENTITY(1,1) PRIMARY KEY,
        Indicator NVARCHAR(255) NOT NULL,
        Owner NVARCHAR(100) NOT NULL,
        Priority TINYINT NOT NULL CHECK (Priority IN (1, 2)), -- 1=SMS, 2=Email
        Frequency INT NOT NULL, -- Minutes
        Deviation DECIMAL(5,2) NOT NULL, -- Percentage
        SpName NVARCHAR(255) NOT NULL,
        SubjectTemplate NVARCHAR(500) NOT NULL,
        DescriptionTemplate NVARCHAR(MAX) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        LastRun DATETIME2 NULL,
        CooldownMinutes INT NOT NULL DEFAULT 30, -- Prevent alert flooding
        MinimumThreshold DECIMAL(18,2) NULL, -- Absolute value threshold
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    )
END
GO

-- Contact Management
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.Contacts') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.Contacts (
        ContactId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL,
        Email NVARCHAR(255) NULL,
        Phone NVARCHAR(50) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    )
END
GO

-- KPI-Contact Mapping
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KpiContacts') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.KpiContacts (
        KpiId INT NOT NULL,
        ContactId INT NOT NULL,
        PRIMARY KEY (KpiId, ContactId),
        FOREIGN KEY (KpiId) REFERENCES monitoring.KPIs(KpiId) ON DELETE CASCADE,
        FOREIGN KEY (ContactId) REFERENCES monitoring.Contacts(ContactId) ON DELETE CASCADE
    )
END
GO

-- Alert History
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.AlertLogs') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.AlertLogs (
        AlertId BIGINT IDENTITY(1,1) PRIMARY KEY,
        KpiId INT NOT NULL,
        TriggerTime DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Message NVARCHAR(500) NOT NULL,
        Details NVARCHAR(MAX) NULL,
        SentVia TINYINT NOT NULL, -- 1=SMS, 2=Email, 3=Both
        SentTo NVARCHAR(MAX) NOT NULL,
        CurrentValue DECIMAL(18,2) NULL,
        HistoricalValue DECIMAL(18,2) NULL,
        DeviationPercent DECIMAL(5,2) NULL,
        IsResolved BIT NOT NULL DEFAULT 0,
        ResolvedTime DATETIME2 NULL,
        ResolvedBy NVARCHAR(100) NULL,
        FOREIGN KEY (KpiId) REFERENCES monitoring.KPIs(KpiId)
    )
END
GO

-- Historical Data Storage for trend analysis
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.HistoricalData') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.HistoricalData (
        HistoricalId BIGINT IDENTITY(1,1) PRIMARY KEY,
        KpiId INT NOT NULL,
        Timestamp DATETIME2 NOT NULL,
        Value DECIMAL(18,2) NOT NULL,
        Period INT NOT NULL, -- Minutes
        MetricKey NVARCHAR(255) NOT NULL,
        FOREIGN KEY (KpiId) REFERENCES monitoring.KPIs(KpiId)
    )
END
GO

-- Configuration Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.Config') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.Config (
        ConfigKey NVARCHAR(50) PRIMARY KEY,
        ConfigValue NVARCHAR(255) NOT NULL,
        Description NVARCHAR(500) NULL,
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    )
END
GO

-- System Status Table for health monitoring
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.SystemStatus') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.SystemStatus (
        StatusId INT IDENTITY(1,1) PRIMARY KEY,
        ServiceName NVARCHAR(100) NOT NULL,
        LastHeartbeat DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Status NVARCHAR(50) NOT NULL, -- Running, Stopped, Error
        ErrorMessage NVARCHAR(MAX) NULL,
        ProcessedKpis INT NOT NULL DEFAULT 0,
        AlertsSent INT NOT NULL DEFAULT 0
    )
END
GO

-- Create indexes for performance
CREATE NONCLUSTERED INDEX IX_KPIs_IsActive_LastRun ON monitoring.KPIs (IsActive, LastRun) INCLUDE (Frequency)
GO

CREATE NONCLUSTERED INDEX IX_AlertLogs_KpiId_TriggerTime ON monitoring.AlertLogs (KpiId, TriggerTime DESC)
GO

CREATE NONCLUSTERED INDEX IX_HistoricalData_KpiId_Timestamp ON monitoring.HistoricalData (KpiId, Timestamp DESC)
GO

CREATE NONCLUSTERED INDEX IX_Contacts_IsActive ON monitoring.Contacts (IsActive) INCLUDE (Email, Phone)
GO

PRINT 'Monitoring schema created successfully!'
