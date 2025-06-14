-- Enhance Historical Data with comprehensive audit information
-- This script adds comprehensive audit fields to track KPI execution details

USE [PopAI]
GO

-- Add new columns to HistoricalData table for comprehensive audit
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('monitoring.HistoricalData') AND name = 'ExecutedBy')
BEGIN
    ALTER TABLE monitoring.HistoricalData ADD 
        ExecutedBy NVARCHAR(100) NULL,
        ExecutionMethod NVARCHAR(50) NULL, -- 'Manual', 'Scheduled', 'API'
        SqlCommand NVARCHAR(MAX) NULL,
        SqlParameters NVARCHAR(MAX) NULL,
        RawResponse NVARCHAR(MAX) NULL,
        ExecutionTimeMs INT NULL,
        ConnectionString NVARCHAR(500) NULL,
        DatabaseName NVARCHAR(100) NULL,
        ServerName NVARCHAR(100) NULL,
        IsSuccessful BIT NOT NULL DEFAULT 1,
        ErrorMessage NVARCHAR(MAX) NULL,
        DeviationPercent DECIMAL(18,4) NULL,
        HistoricalValue DECIMAL(18,4) NULL,
        ShouldAlert BIT NOT NULL DEFAULT 0,
        AlertSent BIT NOT NULL DEFAULT 0,
        SessionId NVARCHAR(100) NULL,
        UserAgent NVARCHAR(500) NULL,
        IpAddress NVARCHAR(50) NULL,
        ExecutionContext NVARCHAR(MAX) NULL -- JSON with additional context
    
    PRINT 'Added comprehensive audit columns to HistoricalData table'
END
ELSE
BEGIN
    PRINT 'Audit columns already exist in HistoricalData table'
END
GO

-- Create index for performance on new audit fields
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.HistoricalData') AND name = 'IX_HistoricalData_ExecutedBy_Timestamp')
BEGIN
    CREATE NONCLUSTERED INDEX IX_HistoricalData_ExecutedBy_Timestamp 
    ON monitoring.HistoricalData (ExecutedBy, Timestamp DESC)
    INCLUDE (IsSuccessful, ExecutionMethod)
    
    PRINT 'Created index IX_HistoricalData_ExecutedBy_Timestamp'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.HistoricalData') AND name = 'IX_HistoricalData_ExecutionMethod_Timestamp')
BEGIN
    CREATE NONCLUSTERED INDEX IX_HistoricalData_ExecutionMethod_Timestamp 
    ON monitoring.HistoricalData (ExecutionMethod, Timestamp DESC)
    INCLUDE (IsSuccessful, ExecutedBy)
    
    PRINT 'Created index IX_HistoricalData_ExecutionMethod_Timestamp'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('monitoring.HistoricalData') AND name = 'IX_HistoricalData_IsSuccessful_Timestamp')
BEGIN
    CREATE NONCLUSTERED INDEX IX_HistoricalData_IsSuccessful_Timestamp 
    ON monitoring.HistoricalData (IsSuccessful, Timestamp DESC)
    INCLUDE (ExecutedBy, ExecutionMethod, ErrorMessage)
    
    PRINT 'Created index IX_HistoricalData_IsSuccessful_Timestamp'
END
GO

-- Create a view for easy querying of execution audit data
CREATE OR ALTER VIEW monitoring.vw_KpiExecutionAudit AS
SELECT 
    hd.HistoricalId,
    hd.KpiId,
    k.Indicator,
    k.Owner as KpiOwner,
    k.SpName,
    hd.Timestamp,
    hd.ExecutedBy,
    hd.ExecutionMethod,
    hd.Value as CurrentValue,
    hd.HistoricalValue,
    hd.DeviationPercent,
    hd.Period,
    hd.MetricKey,
    hd.IsSuccessful,
    hd.ErrorMessage,
    hd.ExecutionTimeMs,
    hd.DatabaseName,
    hd.ServerName,
    hd.ShouldAlert,
    hd.AlertSent,
    hd.SessionId,
    hd.IpAddress,
    hd.SqlCommand,
    hd.RawResponse,
    hd.ExecutionContext,
    -- Calculated fields
    CASE 
        WHEN hd.ExecutionTimeMs < 1000 THEN 'Fast'
        WHEN hd.ExecutionTimeMs < 5000 THEN 'Normal'
        WHEN hd.ExecutionTimeMs < 10000 THEN 'Slow'
        ELSE 'Very Slow'
    END as PerformanceCategory,
    CASE 
        WHEN hd.DeviationPercent IS NULL THEN 'N/A'
        WHEN hd.DeviationPercent < 5 THEN 'Low'
        WHEN hd.DeviationPercent < 15 THEN 'Medium'
        WHEN hd.DeviationPercent < 30 THEN 'High'
        ELSE 'Critical'
    END as DeviationCategory
FROM monitoring.HistoricalData hd
INNER JOIN monitoring.KPIs k ON hd.KpiId = k.KpiId
GO

-- Create a stored procedure to get comprehensive execution history
CREATE OR ALTER PROCEDURE monitoring.usp_GetKpiExecutionHistory
    @KpiId INT = NULL,
    @ExecutedBy NVARCHAR(100) = NULL,
    @ExecutionMethod NVARCHAR(50) = NULL,
    @IsSuccessful BIT = NULL,
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @PageSize INT = 50,
    @PageNumber INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize
    
    -- Get total count
    DECLARE @TotalCount INT
    SELECT @TotalCount = COUNT(*)
    FROM monitoring.vw_KpiExecutionAudit
    WHERE (@KpiId IS NULL OR KpiId = @KpiId)
        AND (@ExecutedBy IS NULL OR ExecutedBy LIKE '%' + @ExecutedBy + '%')
        AND (@ExecutionMethod IS NULL OR ExecutionMethod = @ExecutionMethod)
        AND (@IsSuccessful IS NULL OR IsSuccessful = @IsSuccessful)
        AND (@StartDate IS NULL OR Timestamp >= @StartDate)
        AND (@EndDate IS NULL OR Timestamp <= @EndDate)
    
    -- Get paginated results
    SELECT 
        *,
        @TotalCount as TotalCount,
        @PageNumber as CurrentPage,
        CEILING(CAST(@TotalCount AS FLOAT) / @PageSize) as TotalPages
    FROM monitoring.vw_KpiExecutionAudit
    WHERE (@KpiId IS NULL OR KpiId = @KpiId)
        AND (@ExecutedBy IS NULL OR ExecutedBy LIKE '%' + @ExecutedBy + '%')
        AND (@ExecutionMethod IS NULL OR ExecutionMethod = @ExecutionMethod)
        AND (@IsSuccessful IS NULL OR IsSuccessful = @IsSuccessful)
        AND (@StartDate IS NULL OR Timestamp >= @StartDate)
        AND (@EndDate IS NULL OR Timestamp <= @EndDate)
    ORDER BY Timestamp DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

-- Create a stored procedure to get execution statistics
CREATE OR ALTER PROCEDURE monitoring.usp_GetKpiExecutionStats
    @KpiId INT = NULL,
    @Days INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartDate DATETIME2 = DATEADD(DAY, -@Days, GETUTCDATE())
    
    SELECT 
        k.KpiId,
        k.Indicator,
        k.Owner,
        COUNT(*) as TotalExecutions,
        SUM(CASE WHEN hd.IsSuccessful = 1 THEN 1 ELSE 0 END) as SuccessfulExecutions,
        SUM(CASE WHEN hd.IsSuccessful = 0 THEN 1 ELSE 0 END) as FailedExecutions,
        CAST(SUM(CASE WHEN hd.IsSuccessful = 1 THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) * 100 as SuccessRate,
        AVG(CAST(hd.ExecutionTimeMs AS FLOAT)) as AvgExecutionTimeMs,
        MIN(hd.ExecutionTimeMs) as MinExecutionTimeMs,
        MAX(hd.ExecutionTimeMs) as MaxExecutionTimeMs,
        SUM(CASE WHEN hd.ShouldAlert = 1 THEN 1 ELSE 0 END) as AlertsTriggered,
        SUM(CASE WHEN hd.AlertSent = 1 THEN 1 ELSE 0 END) as AlertsSent,
        MAX(hd.Timestamp) as LastExecution,
        COUNT(DISTINCT hd.ExecutedBy) as UniqueExecutors,
        COUNT(DISTINCT hd.ExecutionMethod) as ExecutionMethods
    FROM monitoring.KPIs k
    LEFT JOIN monitoring.HistoricalData hd ON k.KpiId = hd.KpiId 
        AND hd.Timestamp >= @StartDate
    WHERE (@KpiId IS NULL OR k.KpiId = @KpiId)
    GROUP BY k.KpiId, k.Indicator, k.Owner
    ORDER BY k.Indicator
END
GO

PRINT 'Enhanced HistoricalData table with comprehensive audit capabilities'
PRINT 'Created views and stored procedures for execution analysis'
PRINT ''
PRINT 'New audit fields include:'
PRINT '- ExecutedBy: Who executed the KPI'
PRINT '- ExecutionMethod: How it was executed (Manual/Scheduled/API)'
PRINT '- SqlCommand: Exact SQL command executed'
PRINT '- SqlParameters: Parameters passed to the command'
PRINT '- RawResponse: Raw response from the database'
PRINT '- ExecutionTimeMs: Execution time in milliseconds'
PRINT '- ConnectionString: Which database was used'
PRINT '- DatabaseName/ServerName: Database connection details'
PRINT '- IsSuccessful: Whether execution was successful'
PRINT '- ErrorMessage: Any error that occurred'
PRINT '- DeviationPercent: Calculated deviation'
PRINT '- HistoricalValue: Historical comparison value'
PRINT '- ShouldAlert/AlertSent: Alert status'
PRINT '- SessionId/UserAgent/IpAddress: User context'
PRINT '- ExecutionContext: Additional JSON context'
