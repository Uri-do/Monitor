-- Enhanced KPI Scheduling and Types Migration
-- This script adds support for advanced scheduling and multiple KPI types
USE [PopAI]
GO

PRINT 'Starting Enhanced KPI Scheduling and Types Migration...'

-- Add new columns to KPIs table if they don't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'LastMinutes')
BEGIN
    ALTER TABLE monitoring.KPIs ADD LastMinutes INT NOT NULL DEFAULT 1440; -- 24 hours default
    PRINT 'Added LastMinutes column to KPIs table'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'KpiType')
BEGIN
    ALTER TABLE monitoring.KPIs ADD KpiType NVARCHAR(50) NOT NULL DEFAULT 'success_rate';
    PRINT 'Added KpiType column to KPIs table'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'ScheduleConfiguration')
BEGIN
    ALTER TABLE monitoring.KPIs ADD ScheduleConfiguration NVARCHAR(MAX) NULL;
    PRINT 'Added ScheduleConfiguration column to KPIs table'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'ThresholdValue')
BEGIN
    ALTER TABLE monitoring.KPIs ADD ThresholdValue DECIMAL(18,2) NULL;
    PRINT 'Added ThresholdValue column to KPIs table'
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'ComparisonOperator')
BEGIN
    ALTER TABLE monitoring.KPIs ADD ComparisonOperator NVARCHAR(10) NULL;
    PRINT 'Added ComparisonOperator column to KPIs table'
END

-- Create KPI Types lookup table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KpiTypes') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.KpiTypes (
        KpiTypeId NVARCHAR(50) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        RequiredFields NVARCHAR(MAX) NOT NULL, -- JSON array of required field names
        DefaultStoredProcedure NVARCHAR(255) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Created KpiTypes table'
END
GO

-- Create Quartz.NET job storage tables
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.ScheduledJobs') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.ScheduledJobs (
        JobId NVARCHAR(100) PRIMARY KEY,
        KpiId INT NOT NULL,
        JobName NVARCHAR(255) NOT NULL,
        JobGroup NVARCHAR(255) NOT NULL DEFAULT 'KPI_JOBS',
        TriggerName NVARCHAR(255) NOT NULL,
        TriggerGroup NVARCHAR(255) NOT NULL DEFAULT 'KPI_TRIGGERS',
        CronExpression NVARCHAR(255) NULL,
        IntervalMinutes INT NULL,
        StartTime DATETIME2 NULL,
        EndTime DATETIME2 NULL,
        NextFireTime DATETIME2 NULL,
        PreviousFireTime DATETIME2 NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        FOREIGN KEY (KpiId) REFERENCES monitoring.KPIs(KpiId) ON DELETE CASCADE
    );
    PRINT 'Created ScheduledJobs table'
END
GO

-- Insert KPI type definitions
MERGE monitoring.KpiTypes AS target
USING (VALUES 
    ('success_rate', 'Success Rate Monitoring', 
     'Monitors success percentages and compares them against historical averages. Ideal for tracking transaction success rates, API response rates, login success rates, and other percentage-based metrics.',
     '["deviation", "lastMinutes"]', 'monitoring.usp_MonitorTransactions'),
    ('transaction_volume', 'Transaction Volume Monitoring',
     'Tracks transaction counts and compares them to historical patterns. Perfect for detecting unusual spikes or drops in activity, monitoring daily transactions, API calls, user registrations, and other count-based metrics.',
     '["deviation", "minimumThreshold", "lastMinutes"]', 'monitoring.usp_MonitorTransactionVolume'),
    ('threshold', 'Threshold Monitoring',
     'Simple threshold-based monitoring that triggers alerts when values cross specified limits. Useful for monitoring system resources, queue lengths, error counts, response times, and other absolute value metrics.',
     '["thresholdValue", "comparisonOperator"]', 'monitoring.usp_MonitorThreshold'),
    ('trend_analysis', 'Trend Analysis',
     'Analyzes trends over time to detect gradual changes or patterns. Excellent for capacity planning, performance degradation detection, user behavior analysis, and early warning systems for emerging issues.',
     '["deviation", "lastMinutes"]', 'monitoring.usp_MonitorTrends')
) AS source (KpiTypeId, Name, Description, RequiredFields, DefaultStoredProcedure)
ON target.KpiTypeId = source.KpiTypeId
WHEN MATCHED THEN
    UPDATE SET 
        Name = source.Name,
        Description = source.Description,
        RequiredFields = source.RequiredFields,
        DefaultStoredProcedure = source.DefaultStoredProcedure,
        ModifiedDate = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (KpiTypeId, Name, Description, RequiredFields, DefaultStoredProcedure)
    VALUES (source.KpiTypeId, source.Name, source.Description, source.RequiredFields, source.DefaultStoredProcedure);

PRINT 'Inserted/Updated KPI type definitions'
GO

-- Add indexes for new columns
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.KPIs') AND name = 'IX_KPIs_KpiType')
BEGIN
    CREATE NONCLUSTERED INDEX IX_KPIs_KpiType ON monitoring.KPIs (KpiType) INCLUDE (IsActive);
    PRINT 'Created index on KpiType'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.ScheduledJobs') AND name = 'IX_ScheduledJobs_KpiId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ScheduledJobs_KpiId ON monitoring.ScheduledJobs (KpiId) INCLUDE (IsActive);
    PRINT 'Created index on ScheduledJobs.KpiId'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.ScheduledJobs') AND name = 'IX_ScheduledJobs_NextFireTime')
BEGIN
    CREATE NONCLUSTERED INDEX IX_ScheduledJobs_NextFireTime ON monitoring.ScheduledJobs (NextFireTime) WHERE IsActive = 1;
    PRINT 'Created index on ScheduledJobs.NextFireTime'
END

-- Add check constraints for data integrity
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID('monitoring.CK_KPIs_KpiType'))
BEGIN
    ALTER TABLE monitoring.KPIs ADD CONSTRAINT CK_KPIs_KpiType 
        CHECK (KpiType IN ('success_rate', 'transaction_volume', 'threshold', 'trend_analysis'));
    PRINT 'Added check constraint for KpiType'
END

IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID('monitoring.CK_KPIs_ComparisonOperator'))
BEGIN
    ALTER TABLE monitoring.KPIs ADD CONSTRAINT CK_KPIs_ComparisonOperator 
        CHECK (ComparisonOperator IS NULL OR ComparisonOperator IN ('gt', 'gte', 'lt', 'lte', 'eq'));
    PRINT 'Added check constraint for ComparisonOperator'
END

-- Update existing KPIs to have default schedule configuration
UPDATE monitoring.KPIs 
SET ScheduleConfiguration = JSON_OBJECT(
    'scheduleType', 'interval',
    'intervalMinutes', Frequency,
    'isEnabled', CASE WHEN IsActive = 1 THEN 'true' ELSE 'false' END,
    'timezone', 'UTC'
)
WHERE ScheduleConfiguration IS NULL;

PRINT 'Updated existing KPIs with default schedule configuration'

-- Add foreign key constraint for KPI types
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID('monitoring.FK_KPIs_KpiTypes'))
BEGIN
    ALTER TABLE monitoring.KPIs ADD CONSTRAINT FK_KPIs_KpiTypes
        FOREIGN KEY (KpiType) REFERENCES monitoring.KpiTypes(KpiTypeId);
    PRINT 'Added foreign key constraint for KPI types'
END

-- Create view for enhanced KPI information
IF EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID('monitoring.vw_EnhancedKPIs'))
    DROP VIEW monitoring.vw_EnhancedKPIs;
GO

CREATE VIEW monitoring.vw_EnhancedKPIs AS
SELECT 
    k.KpiId,
    k.Indicator,
    k.Owner,
    k.Priority,
    k.Frequency,
    k.LastMinutes,
    k.Deviation,
    k.SpName,
    k.SubjectTemplate,
    k.DescriptionTemplate,
    k.IsActive,
    k.LastRun,
    k.CooldownMinutes,
    k.MinimumThreshold,
    k.CreatedDate,
    k.ModifiedDate,
    k.KpiType,
    k.ScheduleConfiguration,
    k.ThresholdValue,
    k.ComparisonOperator,
    kt.Name AS KpiTypeName,
    kt.Description AS KpiTypeDescription,
    kt.RequiredFields AS KpiTypeRequiredFields,
    kt.DefaultStoredProcedure AS KpiTypeDefaultStoredProcedure,
    sj.JobId AS ScheduledJobId,
    sj.NextFireTime,
    sj.PreviousFireTime,
    CASE 
        WHEN k.IsActive = 1 AND sj.IsActive = 1 THEN 'Scheduled'
        WHEN k.IsActive = 1 AND sj.IsActive = 0 THEN 'Active (Not Scheduled)'
        WHEN k.IsActive = 0 THEN 'Inactive'
        ELSE 'Unknown'
    END AS ScheduleStatus
FROM monitoring.KPIs k
    LEFT JOIN monitoring.KpiTypes kt ON k.KpiType = kt.KpiTypeId
    LEFT JOIN monitoring.ScheduledJobs sj ON k.KpiId = sj.KpiId AND sj.IsActive = 1;
GO

PRINT 'Created enhanced KPIs view'

-- Create stored procedure to get KPI type information
IF EXISTS (SELECT * FROM sys.procedures WHERE object_id = OBJECT_ID('monitoring.usp_GetKpiTypes'))
    DROP PROCEDURE monitoring.usp_GetKpiTypes;
GO

CREATE PROCEDURE monitoring.usp_GetKpiTypes
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        KpiTypeId,
        Name,
        Description,
        RequiredFields,
        DefaultStoredProcedure,
        IsActive,
        CreatedDate,
        ModifiedDate
    FROM monitoring.KpiTypes
    WHERE IsActive = 1
    ORDER BY Name;
END
GO

PRINT 'Created usp_GetKpiTypes stored procedure'

-- Create stored procedure to validate KPI configuration
IF EXISTS (SELECT * FROM sys.procedures WHERE object_id = OBJECT_ID('monitoring.usp_ValidateKpiConfiguration'))
    DROP PROCEDURE monitoring.usp_ValidateKpiConfiguration;
GO

CREATE PROCEDURE monitoring.usp_ValidateKpiConfiguration
    @KpiType NVARCHAR(50),
    @ThresholdValue DECIMAL(18,2) = NULL,
    @ComparisonOperator NVARCHAR(10) = NULL,
    @Deviation DECIMAL(5,2) = NULL,
    @MinimumThreshold DECIMAL(18,2) = NULL,
    @LastMinutes INT = NULL,
    @IsValid BIT OUTPUT,
    @ValidationErrors NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RequiredFields NVARCHAR(MAX);
    DECLARE @Errors TABLE (ErrorMessage NVARCHAR(255));
    
    -- Get required fields for the KPI type
    SELECT @RequiredFields = RequiredFields 
    FROM monitoring.KpiTypes 
    WHERE KpiTypeId = @KpiType AND IsActive = 1;
    
    IF @RequiredFields IS NULL
    BEGIN
        INSERT INTO @Errors VALUES ('Invalid KPI type specified');
    END
    ELSE
    BEGIN
        -- Check required fields based on KPI type
        IF JSON_VALUE(@RequiredFields, '$[0]') = 'deviation' OR JSON_VALUE(@RequiredFields, '$[1]') = 'deviation'
        BEGIN
            IF @Deviation IS NULL OR @Deviation < 0 OR @Deviation > 100
                INSERT INTO @Errors VALUES ('Deviation must be between 0 and 100 percent');
        END
        
        IF JSON_VALUE(@RequiredFields, '$[0]') = 'thresholdValue' OR JSON_VALUE(@RequiredFields, '$[1]') = 'thresholdValue'
        BEGIN
            IF @ThresholdValue IS NULL
                INSERT INTO @Errors VALUES ('Threshold value is required for this KPI type');
        END
        
        IF JSON_VALUE(@RequiredFields, '$[0]') = 'comparisonOperator' OR JSON_VALUE(@RequiredFields, '$[1]') = 'comparisonOperator'
        BEGIN
            IF @ComparisonOperator IS NULL OR @ComparisonOperator NOT IN ('gt', 'gte', 'lt', 'lte', 'eq')
                INSERT INTO @Errors VALUES ('Valid comparison operator is required for this KPI type');
        END
        
        IF JSON_VALUE(@RequiredFields, '$[0]') = 'minimumThreshold' OR JSON_VALUE(@RequiredFields, '$[1]') = 'minimumThreshold' OR JSON_VALUE(@RequiredFields, '$[2]') = 'minimumThreshold'
        BEGIN
            IF @MinimumThreshold IS NULL OR @MinimumThreshold < 0
                INSERT INTO @Errors VALUES ('Minimum threshold must be a positive number');
        END
        
        IF JSON_VALUE(@RequiredFields, '$[0]') = 'lastMinutes' OR JSON_VALUE(@RequiredFields, '$[1]') = 'lastMinutes' OR JSON_VALUE(@RequiredFields, '$[2]') = 'lastMinutes'
        BEGIN
            IF @LastMinutes IS NULL OR @LastMinutes < 1
                INSERT INTO @Errors VALUES ('Data window must be at least 1 minute');
        END
    END
    
    -- Set output parameters
    SELECT @IsValid = CASE WHEN COUNT(*) = 0 THEN 1 ELSE 0 END FROM @Errors;
    
    SELECT @ValidationErrors = STRING_AGG(ErrorMessage, '; ') FROM @Errors;
    IF @ValidationErrors IS NULL SET @ValidationErrors = '';
END
GO

PRINT 'Created usp_ValidateKpiConfiguration stored procedure'

PRINT 'Enhanced KPI Scheduling and Types Migration completed successfully!'
