-- Monitoring Grid Stored Procedures
USE [PopAI]
GO

-- Template procedure for Indicator calculations
-- This procedure standardizes the output format for all Indicator monitoring procedures
CREATE OR ALTER PROCEDURE monitoring.usp_MonitorDeposits
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
    
    SET @Key = 'TotalDeposits'
    
    -- Calculate current deposits (cross-database query)
    SELECT @CurrentValue = COUNT(*)
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    WHERE at.updated_dt >= @StartTime
        AND at.transaction_type_id = 263 -- Deposit transaction type
        AND at.is_done = 1

    -- Calculate historical average (same period 4 weeks ago)
    SELECT @HistoricalValue = COUNT(*)
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    WHERE at.updated_dt >= @HistoricalStartTime
        AND at.updated_dt < @HistoricalEndTime
        AND at.transaction_type_id = 263
        AND at.is_done = 1
    
    -- Store historical data for trend analysis
    INSERT INTO monitoring.HistoricalData (KpiId, Timestamp, Value, Period, MetricKey)
    SELECT 
        (SELECT KpiId FROM monitoring.KPIs WHERE SpName = 'monitoring.usp_MonitorDeposits'),
        @CurrentTime,
        @CurrentValue,
        @ForLastMinutes,
        @Key
END
GO

-- Monitor transaction volume and success rates
CREATE OR ALTER PROCEDURE monitoring.usp_MonitorTransactions
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
    
    SET @Key = 'TransactionSuccessRate'
    
    -- Calculate current success rate (cross-database query)
    SELECT @CurrentValue =
        CASE
            WHEN COUNT(*) = 0 THEN 0
            ELSE (CAST(SUM(CASE WHEN at.is_done = 1 THEN 1 ELSE 0 END) AS DECIMAL(18,2)) / COUNT(*)) * 100
        END
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    WHERE at.updated_dt >= @StartTime

    -- Calculate historical success rate
    SELECT @HistoricalValue =
        CASE
            WHEN COUNT(*) = 0 THEN 0
            ELSE (CAST(SUM(CASE WHEN at.is_done = 1 THEN 1 ELSE 0 END) AS DECIMAL(18,2)) / COUNT(*)) * 100
        END
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    WHERE at.updated_dt >= @HistoricalStartTime
        AND at.updated_dt < @HistoricalEndTime
    
    -- Store historical data
    INSERT INTO monitoring.HistoricalData (KpiId, Timestamp, Value, Period, MetricKey)
    SELECT 
        (SELECT KpiId FROM monitoring.KPIs WHERE SpName = 'monitoring.usp_MonitorTransactions'),
        @CurrentTime,
        @CurrentValue,
        @ForLastMinutes,
        @Key
END
GO

-- Monitor settlement companies performance
CREATE OR ALTER PROCEDURE monitoring.usp_MonitorSettlementCompanies
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
    
    SET @Key = 'SettlementSuccessRate'
    
    -- Calculate current settlement success rate (cross-database query)
    SELECT @CurrentValue =
        CASE
            WHEN COUNT(*) = 0 THEN 0
            ELSE (CAST(SUM(CASE WHEN at.is_done = 1 THEN 1 ELSE 0 END) AS DECIMAL(18,2)) / COUNT(*)) * 100
        END
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    INNER JOIN [ProgressPlayDBTest].accounts.tbl_Account_payment_methods apm (NOLOCK) ON apm.account_payment_method_id = at.account_payment_method_id
    INNER JOIN [ProgressPlayDBTest].accounts.tbl_Settlement_companies sc (NOLOCK) ON sc.settlement_company_id = apm.settlement_company_id
    WHERE at.updated_dt >= @StartTime
        AND at.transaction_type_id = 263

    -- Calculate historical settlement success rate
    SELECT @HistoricalValue =
        CASE
            WHEN COUNT(*) = 0 THEN 0
            ELSE (CAST(SUM(CASE WHEN at.is_done = 1 THEN 1 ELSE 0 END) AS DECIMAL(18,2)) / COUNT(*)) * 100
        END
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    INNER JOIN [ProgressPlayDBTest].accounts.tbl_Account_payment_methods apm (NOLOCK) ON apm.account_payment_method_id = at.account_payment_method_id
    INNER JOIN [ProgressPlayDBTest].accounts.tbl_Settlement_companies sc (NOLOCK) ON sc.settlement_company_id = apm.settlement_company_id
    WHERE at.updated_dt >= @HistoricalStartTime
        AND at.updated_dt < @HistoricalEndTime
        AND at.transaction_type_id = 263
    
    -- Store historical data
    INSERT INTO monitoring.HistoricalData (KpiId, Timestamp, Value, Period, MetricKey)
    SELECT 
        (SELECT KpiId FROM monitoring.KPIs WHERE SpName = 'monitoring.usp_MonitorSettlementCompanies'),
        @CurrentTime,
        @CurrentValue,
        @ForLastMinutes,
        @Key
END
GO

-- Monitor country-based deposits
CREATE OR ALTER PROCEDURE monitoring.usp_MonitorCountryDeposits
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
    
    SET @Key = 'CountryDeposits'
    
    -- Calculate current country deposits (cross-database query)
    SELECT @CurrentValue = COUNT(*)
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    INNER JOIN [ProgressPlayDBTest].accounts.tbl_Account_payment_methods apm (NOLOCK) ON apm.account_payment_method_id = at.account_payment_method_id
    INNER JOIN [ProgressPlayDBTest].common.tbl_Players p (NOLOCK) ON p.player_id = at.player_id
    INNER JOIN [ProgressPlayDBTest].common.tbl_Countries c (NOLOCK) ON c.country_id = p.country_id
    WHERE at.updated_dt >= @StartTime
        AND at.transaction_type_id = 263
        AND at.is_done = 1

    -- Calculate historical country deposits
    SELECT @HistoricalValue = COUNT(*)
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    INNER JOIN [ProgressPlayDBTest].accounts.tbl_Account_payment_methods apm (NOLOCK) ON apm.account_payment_method_id = at.account_payment_method_id
    INNER JOIN [ProgressPlayDBTest].common.tbl_Players p (NOLOCK) ON p.player_id = at.player_id
    INNER JOIN [ProgressPlayDBTest].common.tbl_Countries c (NOLOCK) ON c.country_id = p.country_id
    WHERE at.updated_dt >= @HistoricalStartTime
        AND at.updated_dt < @HistoricalEndTime
        AND at.transaction_type_id = 263
        AND at.is_done = 1
    
    -- Store historical data
    INSERT INTO monitoring.HistoricalData (KpiId, Timestamp, Value, Period, MetricKey)
    SELECT 
        (SELECT KpiId FROM monitoring.KPIs WHERE SpName = 'monitoring.usp_MonitorCountryDeposits'),
        @CurrentTime,
        @CurrentValue,
        @ForLastMinutes,
        @Key
END
GO

-- Monitor white label performance
CREATE OR ALTER PROCEDURE monitoring.usp_MonitorWhiteLabelPerformance
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
    
    SET @Key = 'WhiteLabelDeposits'
    
    -- Calculate current white label deposits (cross-database query)
    SELECT @CurrentValue = COUNT(*)
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    INNER JOIN [ProgressPlayDBTest].common.tbl_Players p (NOLOCK) ON p.player_id = at.player_id
    INNER JOIN [ProgressPlayDBTest].common.tbl_White_labels wl (NOLOCK) ON wl.label_id = p.white_label_id
    WHERE at.updated_dt >= @StartTime
        AND at.transaction_type_id = 263
        AND at.is_done = 1

    -- Calculate historical white label deposits
    SELECT @HistoricalValue = COUNT(*)
    FROM [ProgressPlayDBTest].accounts.tbl_Account_transactions at (NOLOCK)
    INNER JOIN [ProgressPlayDBTest].common.tbl_Players p (NOLOCK) ON p.player_id = at.player_id
    INNER JOIN [ProgressPlayDBTest].common.tbl_White_labels wl (NOLOCK) ON wl.label_id = p.white_label_id
    WHERE at.updated_dt >= @HistoricalStartTime
        AND at.updated_dt < @HistoricalEndTime
        AND at.transaction_type_id = 263
        AND at.is_done = 1
    
    -- Store historical data
    INSERT INTO monitoring.HistoricalData (KpiId, Timestamp, Value, Period, MetricKey)
    SELECT 
        (SELECT KpiId FROM monitoring.KPIs WHERE SpName = 'monitoring.usp_MonitorWhiteLabelPerformance'),
        @CurrentTime,
        @CurrentValue,
        @ForLastMinutes,
        @Key
END
GO

PRINT 'Monitoring stored procedures created successfully!'
