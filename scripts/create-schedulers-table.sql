-- Create the missing monitoring.Schedulers table

-- First check if the monitoring schema exists
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'monitoring')
BEGIN
    PRINT 'Creating monitoring schema...'
    EXEC('CREATE SCHEMA monitoring')
END

-- Create the Schedulers table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Schedulers' AND schema_id = SCHEMA_ID('monitoring'))
BEGIN
    PRINT 'Creating monitoring.Schedulers table...'
    
    CREATE TABLE monitoring.Schedulers (
        SchedulerID int IDENTITY(1,1) NOT NULL,
        SchedulerName nvarchar(100) NOT NULL,
        SchedulerDescription nvarchar(500) NULL,
        ScheduleType nvarchar(20) NOT NULL DEFAULT 'interval',
        IntervalMinutes int NULL,
        CronExpression nvarchar(255) NULL,
        ExecutionDateTime datetime2(7) NULL,
        StartDate datetime2(7) NULL,
        EndDate datetime2(7) NULL,
        Timezone nvarchar(50) NOT NULL DEFAULT 'UTC',
        IsEnabled bit NOT NULL DEFAULT 1,
        CreatedDate datetime2(7) NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy nvarchar(100) NOT NULL DEFAULT 'system',
        ModifiedDate datetime2(7) NULL DEFAULT SYSUTCDATETIME(),
        ModifiedBy nvarchar(100) NOT NULL DEFAULT 'system',
        
        CONSTRAINT PK_Schedulers PRIMARY KEY (SchedulerID),
        CONSTRAINT CK_Schedulers_ScheduleType CHECK (ScheduleType IN ('interval', 'cron', 'onetime')),
        CONSTRAINT CK_Schedulers_IntervalMinutes CHECK (IntervalMinutes IS NULL OR IntervalMinutes > 0)
    )
    
    PRINT 'monitoring.Schedulers table created successfully'
END
ELSE
BEGIN
    PRINT 'monitoring.Schedulers table already exists'
END

-- Insert some default schedulers
IF NOT EXISTS (SELECT 1 FROM monitoring.Schedulers)
BEGIN
    PRINT 'Inserting default schedulers...'
    
    INSERT INTO monitoring.Schedulers (SchedulerName, SchedulerDescription, ScheduleType, IntervalMinutes, IsEnabled, CreatedBy, ModifiedBy)
    VALUES 
        ('Every 5 Minutes', 'Execute every 5 minutes', 'interval', 5, 1, 'System', 'System'),
        ('Every 15 Minutes', 'Execute every 15 minutes', 'interval', 15, 1, 'System', 'System'),
        ('Every 30 Minutes', 'Execute every 30 minutes', 'interval', 30, 1, 'System', 'System'),
        ('Hourly', 'Execute every hour', 'interval', 60, 1, 'System', 'System'),
        ('Daily at 9 AM', 'Execute daily at 9 AM', 'cron', NULL, 1, 'System', 'System'),
        ('Daily at Midnight', 'Execute daily at midnight', 'cron', NULL, 1, 'System', 'System')
    
    -- Update cron expressions for cron-based schedulers
    UPDATE monitoring.Schedulers 
    SET CronExpression = '0 9 * * *' 
    WHERE SchedulerName = 'Daily at 9 AM'
    
    UPDATE monitoring.Schedulers 
    SET CronExpression = '0 0 * * *' 
    WHERE SchedulerName = 'Daily at Midnight'
    
    PRINT 'Default schedulers inserted'
END

-- Verify the table creation
PRINT ''
PRINT '=== Verification ==='
SELECT 
    SchedulerID,
    SchedulerName,
    SchedulerDescription,
    ScheduleType,
    IntervalMinutes,
    CronExpression,
    IsEnabled,
    CreatedDate
FROM monitoring.Schedulers
ORDER BY SchedulerID

PRINT ''
PRINT '=== Table Structure ==='
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'monitoring' AND TABLE_NAME = 'Schedulers'
ORDER BY ORDINAL_POSITION

PRINT ''
PRINT 'SUCCESS: monitoring.Schedulers table is ready!'
