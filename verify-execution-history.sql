-- Verify execution history data and database schema
USE [PopAI]
GO

-- Check if audit columns exist
PRINT 'Checking HistoricalData table schema...'
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'monitoring' 
    AND TABLE_NAME = 'HistoricalData'
ORDER BY ORDINAL_POSITION
GO

-- Check recent execution history records
PRINT 'Checking recent execution history records...'
SELECT TOP 10
    HistoricalId,
    KpiId,
    Timestamp,
    Value,
    MetricKey,
    ExecutedBy,
    ExecutionMethod,
    IsSuccessful,
    ExecutionTimeMs,
    ErrorMessage
FROM monitoring.HistoricalData 
ORDER BY Timestamp DESC
GO

-- Check if the view exists and works
PRINT 'Testing execution audit view...'
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_KpiExecutionAudit' AND schema_id = SCHEMA_ID('monitoring'))
BEGIN
    SELECT TOP 5 * FROM monitoring.vw_KpiExecutionAudit ORDER BY Timestamp DESC
END
ELSE
BEGIN
    PRINT 'View monitoring.vw_KpiExecutionAudit does not exist'
END
GO

-- Check if the stored procedure exists
PRINT 'Testing stored procedure...'
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'usp_GetKpiExecutionHistory' AND schema_id = SCHEMA_ID('monitoring'))
BEGIN
    EXEC monitoring.usp_GetKpiExecutionHistory @PageSize = 5, @PageNumber = 1
END
ELSE
BEGIN
    PRINT 'Stored procedure monitoring.usp_GetKpiExecutionHistory does not exist'
END
GO

-- Check KPI execution counts
PRINT 'KPI execution summary...'
SELECT 
    k.KpiId,
    k.Indicator,
    k.Owner,
    COUNT(hd.HistoricalId) as ExecutionCount,
    MAX(hd.Timestamp) as LastExecution
FROM monitoring.KPIs k
LEFT JOIN monitoring.HistoricalData hd ON k.KpiId = hd.KpiId
GROUP BY k.KpiId, k.Indicator, k.Owner
ORDER BY ExecutionCount DESC
GO
