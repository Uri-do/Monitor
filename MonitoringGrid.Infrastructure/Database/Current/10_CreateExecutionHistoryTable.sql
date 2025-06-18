-- Create ExecutionHistory table for tracking indicator execution results
-- This replaces the obsolete HistoricalData table with a more comprehensive execution tracking system

USE [PopAI]
GO

-- Create ExecutionHistory table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.ExecutionHistory') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.ExecutionHistory (
        ExecutionHistoryID BIGINT IDENTITY(1,1) PRIMARY KEY,
        IndicatorID BIGINT NOT NULL,
        ExecutedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        DurationMs BIGINT NOT NULL,
        Success BIT NOT NULL,
        Result NVARCHAR(4000) NULL,
        ErrorMessage NVARCHAR(4000) NULL,
        RecordCount INT NULL,
        ExecutionContext NVARCHAR(100) NULL,
        ExecutedBy NVARCHAR(100) NULL,
        Metadata NVARCHAR(4000) NULL,
        
        -- Foreign key to Indicators table
        CONSTRAINT FK_ExecutionHistory_Indicators 
            FOREIGN KEY (IndicatorID) REFERENCES monitoring.Indicators(IndicatorID) 
            ON DELETE CASCADE
    )
    
    PRINT '✅ ExecutionHistory table created successfully'
END
ELSE
BEGIN
    PRINT '⚠️ ExecutionHistory table already exists'
END
GO

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExecutionHistory_IndicatorID' AND object_id = OBJECT_ID('monitoring.ExecutionHistory'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExecutionHistory_IndicatorID 
    ON monitoring.ExecutionHistory (IndicatorID)
    PRINT '✅ Index IX_ExecutionHistory_IndicatorID created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExecutionHistory_ExecutedAt' AND object_id = OBJECT_ID('monitoring.ExecutionHistory'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExecutionHistory_ExecutedAt 
    ON monitoring.ExecutionHistory (ExecutedAt DESC)
    PRINT '✅ Index IX_ExecutionHistory_ExecutedAt created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExecutionHistory_IndicatorID_ExecutedAt' AND object_id = OBJECT_ID('monitoring.ExecutionHistory'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExecutionHistory_IndicatorID_ExecutedAt 
    ON monitoring.ExecutionHistory (IndicatorID, ExecutedAt DESC)
    PRINT '✅ Index IX_ExecutionHistory_IndicatorID_ExecutedAt created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExecutionHistory_Success' AND object_id = OBJECT_ID('monitoring.ExecutionHistory'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ExecutionHistory_Success 
    ON monitoring.ExecutionHistory (Success)
    PRINT '✅ Index IX_ExecutionHistory_Success created'
END
GO

-- Create a view for easy querying of execution history with indicator details
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_ExecutionHistoryDetails' AND schema_id = SCHEMA_ID('monitoring'))
BEGIN
    DROP VIEW monitoring.vw_ExecutionHistoryDetails
END
GO

CREATE VIEW monitoring.vw_ExecutionHistoryDetails AS
SELECT 
    eh.ExecutionHistoryID,
    eh.IndicatorID,
    i.IndicatorName,
    i.IndicatorCode,
    i.OwnerContactID,
    c.ContactName as OwnerName,
    c.Email as OwnerEmail,
    eh.ExecutedAt,
    eh.DurationMs,
    eh.Success,
    eh.Result,
    eh.ErrorMessage,
    eh.RecordCount,
    eh.ExecutionContext,
    eh.ExecutedBy,
    eh.Metadata,
    -- Calculated fields
    CASE 
        WHEN eh.DurationMs < 1000 THEN 'Fast'
        WHEN eh.DurationMs < 5000 THEN 'Normal'
        WHEN eh.DurationMs < 15000 THEN 'Slow'
        ELSE 'Very Slow'
    END as PerformanceCategory,
    CASE 
        WHEN eh.Success = 1 THEN 'Success'
        ELSE 'Failed'
    END as StatusText
FROM monitoring.ExecutionHistory eh
INNER JOIN monitoring.Indicators i ON eh.IndicatorID = i.IndicatorID
LEFT JOIN monitoring.Contacts c ON i.OwnerContactID = c.ContactID
GO

PRINT '✅ View vw_ExecutionHistoryDetails created'
GO

-- Create stored procedure for getting execution history with pagination
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'usp_GetExecutionHistory' AND schema_id = SCHEMA_ID('monitoring'))
BEGIN
    DROP PROCEDURE monitoring.usp_GetExecutionHistory
END
GO

CREATE PROCEDURE monitoring.usp_GetExecutionHistory
    @IndicatorID BIGINT = NULL,
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL,
    @SuccessOnly BIT = NULL,
    @PageSize INT = 50,
    @PageNumber INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Set default date range if not provided
    IF @StartDate IS NULL
        SET @StartDate = DATEADD(DAY, -30, GETUTCDATE())
    
    IF @EndDate IS NULL
        SET @EndDate = GETUTCDATE()
    
    -- Calculate offset for pagination
    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize
    
    -- Get total count
    DECLARE @TotalCount INT
    SELECT @TotalCount = COUNT(*)
    FROM monitoring.vw_ExecutionHistoryDetails
    WHERE (@IndicatorID IS NULL OR IndicatorID = @IndicatorID)
        AND ExecutedAt >= @StartDate
        AND ExecutedAt <= @EndDate
        AND (@SuccessOnly IS NULL OR Success = @SuccessOnly)
    
    -- Get paginated results
    SELECT 
        *,
        @TotalCount as TotalCount,
        @PageNumber as CurrentPage,
        @PageSize as PageSize,
        CEILING(CAST(@TotalCount AS FLOAT) / @PageSize) as TotalPages
    FROM monitoring.vw_ExecutionHistoryDetails
    WHERE (@IndicatorID IS NULL OR IndicatorID = @IndicatorID)
        AND ExecutedAt >= @StartDate
        AND ExecutedAt <= @EndDate
        AND (@SuccessOnly IS NULL OR Success = @SuccessOnly)
    ORDER BY ExecutedAt DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY
END
GO

PRINT '✅ Stored procedure usp_GetExecutionHistory created'
GO

-- Test the new table and view
PRINT ''
PRINT '=== Testing ExecutionHistory Implementation ==='

-- Check table structure
PRINT 'ExecutionHistory table columns:'
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'monitoring' 
    AND TABLE_NAME = 'ExecutionHistory'
ORDER BY ORDINAL_POSITION
GO

-- Check if any data exists
DECLARE @RecordCount INT
SELECT @RecordCount = COUNT(*) FROM monitoring.ExecutionHistory
PRINT 'Current ExecutionHistory records: ' + CAST(@RecordCount AS NVARCHAR(10))
GO

PRINT ''
PRINT '✅ ExecutionHistory table setup completed successfully!'
PRINT 'The system will now save detailed execution history for every indicator run.'
GO
