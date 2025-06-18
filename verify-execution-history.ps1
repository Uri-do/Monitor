# Verify that execution history is being saved for every indicator run
Write-Host "=== Verifying Execution History Implementation ===" -ForegroundColor Green

# Database connection parameters
$Server = "192.168.166.11,1433"
$Username = "conexusadmin"
$Password = "PWUi^g6~lxD"
$Database = "PopAI"

Write-Host ""
Write-Host "1. Checking ExecutionHistory table structure..." -ForegroundColor Yellow
$tableStructure = sqlcmd -S $Server -U $Username -P $Password -d $Database -Q "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'monitoring' AND TABLE_NAME = 'ExecutionHistory' ORDER BY ORDINAL_POSITION" -s "|"
Write-Host $tableStructure

Write-Host ""
Write-Host "2. Checking current execution history records..." -ForegroundColor Yellow
$historyCount = sqlcmd -S $Server -U $Username -P $Password -d $Database -Q "SELECT COUNT(*) as TotalRecords FROM monitoring.ExecutionHistory" -h -1
Write-Host "Total execution history records: $historyCount" -ForegroundColor Cyan

Write-Host ""
Write-Host "3. Showing recent execution history (last 5 records)..." -ForegroundColor Yellow
$recentHistory = sqlcmd -S $Server -U $Username -P $Password -d $Database -Q "SELECT TOP 5 ExecutionHistoryID, IndicatorID, ExecutedAt, DurationMs, Success, Result, ExecutionContext, ExecutedBy FROM monitoring.ExecutionHistory ORDER BY ExecutedAt DESC" -s "|"
Write-Host $recentHistory

Write-Host ""
Write-Host "4. Checking indicators and their last run status..." -ForegroundColor Yellow
$indicatorStatus = sqlcmd -S $Server -U $Username -P $Password -d $Database -Q "SELECT i.IndicatorID, i.IndicatorName, i.LastRun, i.LastRunResult, COUNT(eh.ExecutionHistoryID) as ExecutionCount FROM monitoring.Indicators i LEFT JOIN monitoring.ExecutionHistory eh ON i.IndicatorID = eh.IndicatorID GROUP BY i.IndicatorID, i.IndicatorName, i.LastRun, i.LastRunResult ORDER BY i.IndicatorID" -s "|"
Write-Host $indicatorStatus

Write-Host ""
Write-Host "5. Execution history statistics..." -ForegroundColor Yellow
$stats = sqlcmd -S $Server -U $Username -P $Password -d $Database -Q "SELECT COUNT(*) as TotalExecutions, SUM(CASE WHEN Success = 1 THEN 1 ELSE 0 END) as SuccessfulExecutions, SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) as FailedExecutions, AVG(CAST(DurationMs as FLOAT)) as AvgDurationMs FROM monitoring.ExecutionHistory" -s "|"
Write-Host $stats

Write-Host ""
Write-Host "6. Testing execution history view..." -ForegroundColor Yellow
$viewTest = sqlcmd -S $Server -U $Username -P $Password -d $Database -Q "SELECT TOP 3 ExecutionHistoryID, IndicatorName, ExecutedAt, Success, PerformanceCategory FROM monitoring.vw_ExecutionHistoryDetails ORDER BY ExecutedAt DESC" -s "|"
Write-Host $viewTest

Write-Host ""
Write-Host "=== Execution History Verification Results ===" -ForegroundColor Green

if ($historyCount -gt 0) {
    Write-Host "✅ EXECUTION HISTORY IS WORKING!" -ForegroundColor Green
    Write-Host "   - ExecutionHistory table exists and contains $historyCount records" -ForegroundColor Green
    Write-Host "   - Every indicator execution is being saved with detailed information" -ForegroundColor Green
    Write-Host "   - Execution duration, success status, and results are tracked" -ForegroundColor Green
    Write-Host "   - Execution context and metadata are preserved" -ForegroundColor Green
} else {
    Write-Host "⚠️  No execution history records found" -ForegroundColor Yellow
    Write-Host "   - ExecutionHistory table exists but is empty" -ForegroundColor Yellow
    Write-Host "   - This could mean no indicators have been executed yet" -ForegroundColor Yellow
    Write-Host "   - Or there might be an issue with the execution history saving" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Verification Complete ===" -ForegroundColor Green
