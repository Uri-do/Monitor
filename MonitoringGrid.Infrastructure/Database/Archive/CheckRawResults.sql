-- Check if raw results are being stored in HistoricalData
USE [PopAI]
GO

PRINT '=== CHECKING RAW RESULTS STORAGE ==='
PRINT ''

-- Check if the enhanced audit columns exist
PRINT '1. Checking if enhanced audit columns exist...'
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'monitoring' 
    AND TABLE_NAME = 'HistoricalData'
    AND COLUMN_NAME IN (
        'ExecutedBy', 'ExecutionMethod', 'SqlCommand', 'SqlParameters', 
        'RawResponse', 'ExecutionTimeMs', 'ConnectionString', 'DatabaseName', 
        'ServerName', 'IsSuccessful', 'ErrorMessage', 'DeviationPercent', 
        'HistoricalValue', 'ShouldAlert', 'AlertSent', 'SessionId', 
        'UserAgent', 'IpAddress', 'ExecutionContext'
    )
ORDER BY COLUMN_NAME
GO

PRINT ''
PRINT '2. Checking recent historical data entries...'
SELECT TOP 10
    h.HistoricalId,
    h.Timestamp,
    k.Indicator,
    h.Value,
    h.ExecutedBy,
    h.ExecutionMethod,
    h.IsSuccessful,
    h.ExecutionTimeMs,
    h.DatabaseName,
    CASE 
        WHEN LEN(h.RawResponse) > 100 THEN LEFT(h.RawResponse, 100) + '...'
        ELSE h.RawResponse
    END as RawResponse_Preview,
    CASE 
        WHEN LEN(h.ExecutionContext) > 100 THEN LEFT(h.ExecutionContext, 100) + '...'
        ELSE h.ExecutionContext
    END as ExecutionContext_Preview
FROM monitoring.HistoricalData h
INNER JOIN monitoring.KPIs k ON h.KpiId = k.KpiId
ORDER BY h.Timestamp DESC
GO

PRINT ''
PRINT '3. Checking for entries with raw response data...'
SELECT 
    COUNT(*) as TotalEntries,
    SUM(CASE WHEN RawResponse IS NOT NULL AND LEN(RawResponse) > 0 THEN 1 ELSE 0 END) as EntriesWithRawResponse,
    SUM(CASE WHEN ExecutionContext IS NOT NULL AND LEN(ExecutionContext) > 0 THEN 1 ELSE 0 END) as EntriesWithExecutionContext,
    SUM(CASE WHEN ExecutionTimeMs IS NOT NULL THEN 1 ELSE 0 END) as EntriesWithExecutionTime
FROM monitoring.HistoricalData
WHERE Timestamp >= DATEADD(HOUR, -24, GETUTCDATE())
GO

PRINT ''
PRINT '4. Sample raw response data (if available)...'
SELECT TOP 3
    h.HistoricalId,
    h.Timestamp,
    k.Indicator,
    h.RawResponse,
    h.ExecutionContext
FROM monitoring.HistoricalData h
INNER JOIN monitoring.KPIs k ON h.KpiId = k.KpiId
WHERE h.RawResponse IS NOT NULL 
    AND LEN(h.RawResponse) > 0
    AND h.Timestamp >= DATEADD(HOUR, -24, GETUTCDATE())
ORDER BY h.Timestamp DESC
GO

PRINT ''
PRINT '5. Checking execution statistics...'
SELECT 
    k.Indicator,
    COUNT(*) as ExecutionCount,
    AVG(CAST(h.ExecutionTimeMs AS FLOAT)) as AvgExecutionTimeMs,
    SUM(CASE WHEN h.IsSuccessful = 1 THEN 1 ELSE 0 END) as SuccessfulExecutions,
    SUM(CASE WHEN h.IsSuccessful = 0 THEN 1 ELSE 0 END) as FailedExecutions,
    MAX(h.Timestamp) as LastExecution
FROM monitoring.HistoricalData h
INNER JOIN monitoring.KPIs k ON h.KpiId = k.KpiId
WHERE h.Timestamp >= DATEADD(HOUR, -24, GETUTCDATE())
GROUP BY k.Indicator, k.KpiId
ORDER BY LastExecution DESC
GO

PRINT ''
PRINT '=== END RAW RESULTS CHECK ==='
