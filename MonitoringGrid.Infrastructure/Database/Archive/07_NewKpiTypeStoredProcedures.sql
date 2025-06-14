-- New KPI Type Stored Procedures
-- This script creates stored procedures for the new KPI types
USE [PopAI]
GO

PRINT 'Creating stored procedures for new KPI types...'

-- Transaction Volume Monitoring Stored Procedure
CREATE OR ALTER PROCEDURE monitoring.usp_MonitorTransactionVolume
    @ForLastMinutes INT,
    @Key NVARCHAR(255) OUTPUT,
    @CurrentValue DECIMAL(18,2) OUTPUT,
    @HistoricalValue DECIMAL(18,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentTime DATETIME2 = GETUTCDATE()
    DECLARE @StartTime DATETIME2 = DATEADD(MINUTE, -@ForLastMinutes, @CurrentTime)
    DECLARE @HistoricalStartTime DATETIME2 = DATEADD(WEEK, -4, @StartTime)
    DECLARE @HistoricalEndTime DATETIME2 = DATEADD(WEEK, -4, @CurrentTime)
    
    SET @Key = 'TransactionVolume'
    
    -- Calculate current transaction volume
    SELECT @CurrentValue = COUNT(*)
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    WHERE at.updated_dt >= @StartTime
        AND at.updated_dt < @CurrentTime
        AND at.is_done = 1
    
    -- Calculate historical average transaction volume
    SELECT @HistoricalValue = COUNT(*) / 4.0 -- Average over 4 weeks
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    WHERE at.updated_dt >= @HistoricalStartTime
        AND at.updated_dt < @HistoricalEndTime
        AND at.is_done = 1
    
    -- Store historical data
    INSERT INTO monitoring.HistoricalData (KpiId, Timestamp, Value, Period, MetricKey)
    SELECT 
        (SELECT KpiId FROM monitoring.KPIs WHERE SpName = 'monitoring.usp_MonitorTransactionVolume'),
        @CurrentTime,
        @CurrentValue,
        @ForLastMinutes,
        @Key
END
GO

-- Threshold Monitoring Stored Procedure
CREATE OR ALTER PROCEDURE monitoring.usp_MonitorThreshold
    @ForLastMinutes INT,
    @ThresholdValue DECIMAL(18,2),
    @ComparisonOperator NVARCHAR(10),
    @Key NVARCHAR(255) OUTPUT,
    @CurrentValue DECIMAL(18,2) OUTPUT,
    @HistoricalValue DECIMAL(18,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentTime DATETIME2 = GETUTCDATE()
    DECLARE @StartTime DATETIME2 = DATEADD(MINUTE, -@ForLastMinutes, @CurrentTime)
    
    SET @Key = 'ThresholdCheck'
    SET @HistoricalValue = @ThresholdValue -- For threshold checks, historical value is the threshold
    
    -- Example: Monitor error count (customize based on your needs)
    -- This is a template - replace with actual metric calculation
    SELECT @CurrentValue = COUNT(*)
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    WHERE at.updated_dt >= @StartTime
        AND at.updated_dt < @CurrentTime
        AND at.is_done = 0 -- Failed transactions as example
    
    -- Alternative examples for different threshold types:
    
    -- CPU Usage example (if you have system metrics table):
    -- SELECT @CurrentValue = AVG(CAST(cpu_usage AS DECIMAL(18,2)))
    -- FROM system_metrics 
    -- WHERE timestamp >= @StartTime AND timestamp < @CurrentTime
    
    -- Queue length example:
    -- SELECT @CurrentValue = COUNT(*)
    -- FROM processing_queue 
    -- WHERE status = 'pending' AND created_date >= @StartTime
    
    -- Response time example:
    -- SELECT @CurrentValue = AVG(CAST(response_time_ms AS DECIMAL(18,2)))
    -- FROM api_logs 
    -- WHERE timestamp >= @StartTime AND timestamp < @CurrentTime
    
    -- Store historical data
    INSERT INTO monitoring.HistoricalData (KpiId, Timestamp, Value, Period, MetricKey)
    SELECT 
        (SELECT KpiId FROM monitoring.KPIs WHERE SpName = 'monitoring.usp_MonitorThreshold'),
        @CurrentTime,
        @CurrentValue,
        @ForLastMinutes,
        @Key
END
GO

-- Trend Analysis Stored Procedure
CREATE OR ALTER PROCEDURE monitoring.usp_MonitorTrends
    @ForLastMinutes INT,
    @Key NVARCHAR(255) OUTPUT,
    @CurrentValue DECIMAL(18,2) OUTPUT,
    @HistoricalValue DECIMAL(18,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentTime DATETIME2 = GETUTCDATE()
    DECLARE @StartTime DATETIME2 = DATEADD(MINUTE, -@ForLastMinutes, @CurrentTime)
    DECLARE @TrendPeriodStart DATETIME2 = DATEADD(MINUTE, -(@ForLastMinutes * 2), @CurrentTime)
    DECLARE @TrendPeriodMid DATETIME2 = DATEADD(MINUTE, -@ForLastMinutes, @CurrentTime)
    
    SET @Key = 'TrendAnalysis'
    
    -- Calculate current period average
    SELECT @CurrentValue = AVG(CAST(transaction_amount AS DECIMAL(18,2)))
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    WHERE at.updated_dt >= @StartTime
        AND at.updated_dt < @CurrentTime
        AND at.is_done = 1
        AND at.transaction_amount > 0
    
    -- Calculate previous period average for trend comparison
    SELECT @HistoricalValue = AVG(CAST(transaction_amount AS DECIMAL(18,2)))
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    WHERE at.updated_dt >= @TrendPeriodStart
        AND at.updated_dt < @TrendPeriodMid
        AND at.is_done = 1
        AND at.transaction_amount > 0
    
    -- Handle null values
    SET @CurrentValue = ISNULL(@CurrentValue, 0)
    SET @HistoricalValue = ISNULL(@HistoricalValue, 0)
    
    -- Store historical data
    INSERT INTO monitoring.HistoricalData (KpiId, Timestamp, Value, Period, MetricKey)
    SELECT 
        (SELECT KpiId FROM monitoring.KPIs WHERE SpName = 'monitoring.usp_MonitorTrends'),
        @CurrentTime,
        @CurrentValue,
        @ForLastMinutes,
        @Key
END
GO

-- Enhanced Transaction Success Rate Monitoring (updated version)
CREATE OR ALTER PROCEDURE monitoring.usp_MonitorTransactionSuccess
    @ForLastMinutes INT,
    @Key NVARCHAR(255) OUTPUT,
    @CurrentValue DECIMAL(18,2) OUTPUT,
    @HistoricalValue DECIMAL(18,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CurrentTime DATETIME2 = GETUTCDATE()
    DECLARE @StartTime DATETIME2 = DATEADD(MINUTE, -@ForLastMinutes, @CurrentTime)
    DECLARE @HistoricalStartTime DATETIME2 = DATEADD(WEEK, -4, @StartTime)
    DECLARE @HistoricalEndTime DATETIME2 = DATEADD(WEEK, -4, @CurrentTime)
    
    DECLARE @CurrentTotal INT, @CurrentSuccess INT
    DECLARE @HistoricalTotal INT, @HistoricalSuccess INT
    
    SET @Key = 'TransactionSuccessRate'
    
    -- Calculate current period success rate
    SELECT 
        @CurrentTotal = COUNT(*),
        @CurrentSuccess = SUM(CASE WHEN is_done = 1 THEN 1 ELSE 0 END)
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    WHERE at.updated_dt >= @StartTime
        AND at.updated_dt < @CurrentTime
    
    -- Calculate historical success rate
    SELECT 
        @HistoricalTotal = COUNT(*),
        @HistoricalSuccess = SUM(CASE WHEN is_done = 1 THEN 1 ELSE 0 END)
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    WHERE at.updated_dt >= @HistoricalStartTime
        AND at.updated_dt < @HistoricalEndTime
    
    -- Calculate percentages
    SET @CurrentValue = CASE 
        WHEN @CurrentTotal > 0 THEN (@CurrentSuccess * 100.0) / @CurrentTotal 
        ELSE 0 
    END
    
    SET @HistoricalValue = CASE 
        WHEN @HistoricalTotal > 0 THEN (@HistoricalSuccess * 100.0) / @HistoricalTotal 
        ELSE 0 
    END
    
    -- Store historical data
    INSERT INTO monitoring.HistoricalData (KpiId, Timestamp, Value, Period, MetricKey)
    SELECT 
        (SELECT KpiId FROM monitoring.KPIs WHERE SpName = 'monitoring.usp_MonitorTransactionSuccess'),
        @CurrentTime,
        @CurrentValue,
        @ForLastMinutes,
        @Key
END
GO

-- Generic KPI execution procedure that can handle different types
CREATE OR ALTER PROCEDURE monitoring.usp_ExecuteKpiByType
    @KpiId INT,
    @KpiType NVARCHAR(50),
    @ForLastMinutes INT,
    @ThresholdValue DECIMAL(18,2) = NULL,
    @ComparisonOperator NVARCHAR(10) = NULL,
    @Key NVARCHAR(255) OUTPUT,
    @CurrentValue DECIMAL(18,2) OUTPUT,
    @HistoricalValue DECIMAL(18,2) OUTPUT,
    @ShouldAlert BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SpName NVARCHAR(255)
    
    -- Get the stored procedure name for this KPI
    SELECT @SpName = SpName FROM monitoring.KPIs WHERE KpiId = @KpiId
    
    -- Execute based on KPI type
    IF @KpiType = 'success_rate'
    BEGIN
        EXEC @SpName @ForLastMinutes, @Key OUTPUT, @CurrentValue OUTPUT, @HistoricalValue OUTPUT
    END
    ELSE IF @KpiType = 'transaction_volume'
    BEGIN
        EXEC monitoring.usp_MonitorTransactionVolume @ForLastMinutes, @Key OUTPUT, @CurrentValue OUTPUT, @HistoricalValue OUTPUT
    END
    ELSE IF @KpiType = 'threshold'
    BEGIN
        EXEC monitoring.usp_MonitorThreshold @ForLastMinutes, @ThresholdValue, @ComparisonOperator, @Key OUTPUT, @CurrentValue OUTPUT, @HistoricalValue OUTPUT
        
        -- Evaluate threshold condition
        SET @ShouldAlert = CASE 
            WHEN @ComparisonOperator = 'gt' AND @CurrentValue > @ThresholdValue THEN 1
            WHEN @ComparisonOperator = 'gte' AND @CurrentValue >= @ThresholdValue THEN 1
            WHEN @ComparisonOperator = 'lt' AND @CurrentValue < @ThresholdValue THEN 1
            WHEN @ComparisonOperator = 'lte' AND @CurrentValue <= @ThresholdValue THEN 1
            WHEN @ComparisonOperator = 'eq' AND @CurrentValue = @ThresholdValue THEN 1
            ELSE 0
        END
    END
    ELSE IF @KpiType = 'trend_analysis'
    BEGIN
        EXEC monitoring.usp_MonitorTrends @ForLastMinutes, @Key OUTPUT, @CurrentValue OUTPUT, @HistoricalValue OUTPUT
    END
    ELSE
    BEGIN
        -- Default to original stored procedure
        EXEC @SpName @ForLastMinutes, @Key OUTPUT, @CurrentValue OUTPUT, @HistoricalValue OUTPUT
    END
    
    -- For non-threshold types, use deviation-based alerting (handled by calling service)
    IF @KpiType != 'threshold'
        SET @ShouldAlert = 0 -- Let the service calculate deviation
END
GO

-- Procedure to get KPI execution statistics
CREATE OR ALTER PROCEDURE monitoring.usp_GetKpiExecutionStats
    @KpiId INT = NULL,
    @KpiType NVARCHAR(50) = NULL,
    @DaysBack INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartDate DATETIME2 = DATEADD(DAY, -@DaysBack, GETUTCDATE())
    
    SELECT 
        k.KpiId,
        k.Indicator,
        k.KpiType,
        kt.Name AS KpiTypeName,
        COUNT(hd.HistoricalId) AS ExecutionCount,
        AVG(hd.Value) AS AverageValue,
        MIN(hd.Value) AS MinValue,
        MAX(hd.Value) AS MaxValue,
        STDEV(hd.Value) AS StandardDeviation,
        MAX(hd.Timestamp) AS LastExecution,
        COUNT(al.AlertId) AS AlertCount
    FROM monitoring.KPIs k
        LEFT JOIN monitoring.KpiTypes kt ON k.KpiType = kt.KpiTypeId
        LEFT JOIN monitoring.HistoricalData hd ON k.KpiId = hd.KpiId AND hd.Timestamp >= @StartDate
        LEFT JOIN monitoring.AlertLogs al ON k.KpiId = al.KpiId AND al.TriggerTime >= @StartDate
    WHERE (@KpiId IS NULL OR k.KpiId = @KpiId)
        AND (@KpiType IS NULL OR k.KpiType = @KpiType)
        AND k.IsActive = 1
    GROUP BY k.KpiId, k.Indicator, k.KpiType, kt.Name
    ORDER BY k.Indicator
END
GO

PRINT 'New KPI type stored procedures created successfully!'
