-- Create Schedulers Table for Enhanced Scheduling
-- This script creates a dedicated Schedulers table for reusable schedule configurations
USE [PopAI]
GO

PRINT 'Creating Schedulers table for enhanced scheduling...'

-- Create Schedulers table for reusable schedule configurations
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.Schedulers') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.Schedulers (
        SchedulerID INT IDENTITY(1,1) PRIMARY KEY,
        SchedulerName NVARCHAR(100) NOT NULL,
        SchedulerDescription NVARCHAR(500) NULL,
        ScheduleType NVARCHAR(20) NOT NULL CHECK (ScheduleType IN ('interval', 'cron', 'onetime')),
        
        -- Interval-based scheduling
        IntervalMinutes INT NULL,
        
        -- Cron-based scheduling
        CronExpression NVARCHAR(255) NULL,
        
        -- One-time scheduling
        ExecutionDateTime DATETIME2 NULL,
        
        -- Common scheduling properties
        StartDate DATETIME2 NULL,
        EndDate DATETIME2 NULL,
        Timezone NVARCHAR(50) NOT NULL DEFAULT 'UTC',
        IsEnabled BIT NOT NULL DEFAULT 1,
        
        -- Metadata
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NOT NULL DEFAULT 'system',
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedBy NVARCHAR(100) NOT NULL DEFAULT 'system',
        
        -- Constraints
        CONSTRAINT CK_Schedulers_IntervalMinutes CHECK (
            (ScheduleType = 'interval' AND IntervalMinutes IS NOT NULL AND IntervalMinutes > 0) OR
            (ScheduleType != 'interval')
        ),
        CONSTRAINT CK_Schedulers_CronExpression CHECK (
            (ScheduleType = 'cron' AND CronExpression IS NOT NULL) OR
            (ScheduleType != 'cron')
        ),
        CONSTRAINT CK_Schedulers_ExecutionDateTime CHECK (
            (ScheduleType = 'onetime' AND ExecutionDateTime IS NOT NULL) OR
            (ScheduleType != 'onetime')
        )
    );
    
    PRINT 'Created Schedulers table'
END
GO

-- Add SchedulerID column to Indicators table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.Indicators') AND name = 'SchedulerID')
BEGIN
    ALTER TABLE monitoring.Indicators ADD SchedulerID INT NULL;
    PRINT 'Added SchedulerID column to Indicators table'
END
GO

-- Create foreign key relationship
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID('monitoring.FK_Indicators_Schedulers'))
BEGIN
    ALTER TABLE monitoring.Indicators ADD CONSTRAINT FK_Indicators_Schedulers
        FOREIGN KEY (SchedulerID) REFERENCES monitoring.Schedulers(SchedulerID);
    PRINT 'Added foreign key constraint for Schedulers'
END
GO

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.Schedulers') AND name = 'IX_Schedulers_ScheduleType_IsEnabled')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Schedulers_ScheduleType_IsEnabled 
    ON monitoring.Schedulers (ScheduleType, IsEnabled) 
    INCLUDE (SchedulerName, IntervalMinutes, CronExpression);
    PRINT 'Created index on Schedulers'
END
GO

-- Insert default scheduler configurations
MERGE monitoring.Schedulers AS target
USING (VALUES 
    ('Every 5 Minutes', 'Execute every 5 minutes', 'interval', 5, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Every 15 Minutes', 'Execute every 15 minutes', 'interval', 15, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Every 30 Minutes', 'Execute every 30 minutes', 'interval', 30, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Every Hour', 'Execute every hour', 'interval', 60, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Every 2 Hours', 'Execute every 2 hours', 'interval', 120, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Every 6 Hours', 'Execute every 6 hours', 'interval', 360, NULL, NULL, NULL, NULL, 'UTC', 1),
    ('Daily at Midnight', 'Execute daily at midnight', 'cron', NULL, '0 0 * * *', NULL, NULL, NULL, 'UTC', 1),
    ('Daily at 9 AM', 'Execute daily at 9 AM', 'cron', NULL, '0 9 * * *', NULL, NULL, NULL, 'UTC', 1),
    ('Hourly', 'Execute at the top of every hour', 'cron', NULL, '0 * * * *', NULL, NULL, NULL, 'UTC', 1),
    ('Every 15 Minutes (Cron)', 'Execute every 15 minutes using cron', 'cron', NULL, '*/15 * * * *', NULL, NULL, NULL, 'UTC', 1),
    ('Weekly Monday 9 AM', 'Execute every Monday at 9 AM', 'cron', NULL, '0 9 * * 1', NULL, NULL, NULL, 'UTC', 1),
    ('Monthly 1st at Midnight', 'Execute on the 1st of every month at midnight', 'cron', NULL, '0 0 1 * *', NULL, NULL, NULL, 'UTC', 1)
) AS source (SchedulerName, SchedulerDescription, ScheduleType, IntervalMinutes, CronExpression, ExecutionDateTime, StartDate, EndDate, Timezone, IsEnabled)
ON target.SchedulerName = source.SchedulerName
WHEN NOT MATCHED THEN
    INSERT (SchedulerName, SchedulerDescription, ScheduleType, IntervalMinutes, CronExpression, ExecutionDateTime, StartDate, EndDate, Timezone, IsEnabled)
    VALUES (source.SchedulerName, source.SchedulerDescription, source.ScheduleType, source.IntervalMinutes, source.CronExpression, source.ExecutionDateTime, source.StartDate, source.EndDate, source.Timezone, source.IsEnabled);

PRINT 'Inserted default scheduler configurations'
GO

-- Create view for enhanced Indicators with Scheduler information
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID('monitoring.vw_IndicatorsWithSchedulers'))
    DROP VIEW monitoring.vw_IndicatorsWithSchedulers;
GO

CREATE VIEW monitoring.vw_IndicatorsWithSchedulers AS
SELECT
    i.IndicatorID,
    i.IndicatorName,
    i.IndicatorCode,
    i.IndicatorDesc,
    i.CollectorID,
    i.CollectorItemName,
    i.Priority,
    i.LastMinutes,
    i.ThresholdType,
    i.ThresholdField,
    i.ThresholdComparison,
    i.ThresholdValue,
    i.OwnerContactID,
    i.AverageLastDays,
    i.IsActive,
    i.CreatedDate,
    i.ModifiedDate,
    i.SchedulerID,

    -- Scheduler information
    s.SchedulerName,
    s.SchedulerDescription,
    s.ScheduleType,
    s.IntervalMinutes,
    s.CronExpression,
    s.ExecutionDateTime,
    s.StartDate,
    s.EndDate,
    s.Timezone,
    s.IsEnabled AS SchedulerEnabled,

    -- Computed schedule status
    CASE
        WHEN i.IsActive = 1 AND s.IsEnabled = 1 THEN 'Active'
        WHEN i.IsActive = 1 AND s.IsEnabled = 0 THEN 'Indicator Active, Scheduler Disabled'
        WHEN i.IsActive = 0 AND s.IsEnabled = 1 THEN 'Indicator Disabled, Scheduler Active'
        WHEN i.IsActive = 0 AND s.IsEnabled = 0 THEN 'Both Disabled'
        WHEN s.SchedulerID IS NULL THEN 'No Scheduler Assigned'
        ELSE 'Unknown'
    END AS ScheduleStatus
FROM monitoring.Indicators i
    LEFT JOIN monitoring.Schedulers s ON i.SchedulerID = s.SchedulerID;
GO

PRINT 'Created enhanced Indicators view with Scheduler information'

-- Create stored procedure to get available schedulers
IF EXISTS (SELECT * FROM sys.procedures WHERE object_id = OBJECT_ID('monitoring.usp_GetSchedulers'))
    DROP PROCEDURE monitoring.usp_GetSchedulers;
GO

CREATE PROCEDURE monitoring.usp_GetSchedulers
    @IncludeDisabled BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        SchedulerID,
        SchedulerName,
        SchedulerDescription,
        ScheduleType,
        IntervalMinutes,
        CronExpression,
        ExecutionDateTime,
        StartDate,
        EndDate,
        Timezone,
        IsEnabled,
        CreatedDate,
        ModifiedDate
    FROM monitoring.Schedulers
    WHERE (@IncludeDisabled = 1 OR IsEnabled = 1)
    ORDER BY
        CASE ScheduleType
            WHEN 'interval' THEN 1
            WHEN 'cron' THEN 2
            WHEN 'onetime' THEN 3
        END,
        CASE ScheduleType
            WHEN 'interval' THEN IntervalMinutes
            ELSE 999999
        END,
        SchedulerName;
END
GO

PRINT 'Created usp_GetSchedulers stored procedure'

-- Create stored procedure to create/update schedulers
IF EXISTS (SELECT * FROM sys.procedures WHERE object_id = OBJECT_ID('monitoring.usp_UpsertScheduler'))
    DROP PROCEDURE monitoring.usp_UpsertScheduler;
GO

CREATE PROCEDURE monitoring.usp_UpsertScheduler
    @SchedulerID INT = NULL,
    @SchedulerName NVARCHAR(100),
    @SchedulerDescription NVARCHAR(500) = NULL,
    @ScheduleType NVARCHAR(20),
    @IntervalMinutes INT = NULL,
    @CronExpression NVARCHAR(255) = NULL,
    @ExecutionDateTime DATETIME2 = NULL,
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @Timezone NVARCHAR(50) = 'UTC',
    @IsEnabled BIT = 1,
    @ModifiedBy NVARCHAR(100) = 'system'
AS
BEGIN
    SET NOCOUNT ON;

    IF @SchedulerID IS NULL
    BEGIN
        -- Insert new scheduler
        INSERT INTO monitoring.Schedulers (
            SchedulerName, SchedulerDescription, ScheduleType, IntervalMinutes,
            CronExpression, ExecutionDateTime, StartDate, EndDate, Timezone,
            IsEnabled, CreatedBy, ModifiedBy
        )
        VALUES (
            @SchedulerName, @SchedulerDescription, @ScheduleType, @IntervalMinutes,
            @CronExpression, @ExecutionDateTime, @StartDate, @EndDate, @Timezone,
            @IsEnabled, @ModifiedBy, @ModifiedBy
        );

        SELECT SCOPE_IDENTITY() AS SchedulerID;
    END
    ELSE
    BEGIN
        -- Update existing scheduler
        UPDATE monitoring.Schedulers
        SET
            SchedulerName = @SchedulerName,
            SchedulerDescription = @SchedulerDescription,
            ScheduleType = @ScheduleType,
            IntervalMinutes = @IntervalMinutes,
            CronExpression = @CronExpression,
            ExecutionDateTime = @ExecutionDateTime,
            StartDate = @StartDate,
            EndDate = @EndDate,
            Timezone = @Timezone,
            IsEnabled = @IsEnabled,
            ModifiedDate = SYSUTCDATETIME(),
            ModifiedBy = @ModifiedBy
        WHERE SchedulerID = @SchedulerID;

        SELECT @SchedulerID AS SchedulerID;
    END
END
GO

PRINT 'Created usp_UpsertScheduler stored procedure'

PRINT 'Schedulers table creation completed successfully!'
