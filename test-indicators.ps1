# Simple PowerShell script to test database connection and indicator retrieval
Write-Host "=== Testing Database Connection and Indicators ===" -ForegroundColor Green

# Test database connection
Write-Host "Testing database connection..." -ForegroundColor Yellow
$connectionTest = sqlcmd -S "192.168.166.11,1433" -U "conexusadmin" -P "PWUi^g6~lxD" -d "PopAI" -Q "SELECT 1 as TestConnection" -h -1
if ($LASTEXITCODE -eq 0) {
    Write-Host "Database connection successful" -ForegroundColor Green
} else {
    Write-Host "Database connection failed" -ForegroundColor Red
    exit 1
}

# Get indicator count
Write-Host ""
Write-Host "Getting indicator count..." -ForegroundColor Yellow
$indicatorCount = sqlcmd -S "192.168.166.11,1433" -U "conexusadmin" -P "PWUi^g6~lxD" -d "PopAI" -Q "SELECT COUNT(*) FROM monitoring.Indicators" -h -1
Write-Host "Total indicators: $indicatorCount" -ForegroundColor Cyan

# Get indicator details
Write-Host ""
Write-Host "Getting indicator details..." -ForegroundColor Yellow
$indicatorDetails = sqlcmd -S "192.168.166.11,1433" -U "conexusadmin" -P "PWUi^g6~lxD" -d "PopAI" -Q "SELECT IndicatorID, IndicatorName, IsActive, LastRun, SchedulerID FROM monitoring.Indicators" -s "|"
Write-Host $indicatorDetails

# Get scheduler details
Write-Host ""
Write-Host "Getting scheduler details..." -ForegroundColor Yellow
$schedulerDetails = sqlcmd -S "192.168.166.11,1433" -U "conexusadmin" -P "PWUi^g6~lxD" -d "PopAI" -Q "SELECT SchedulerID, SchedulerName, ScheduleType, IntervalMinutes, IsEnabled FROM monitoring.Schedulers WHERE SchedulerID IN (1, 13)" -s "|"
Write-Host $schedulerDetails

# Check if indicators should be due (never run before)
Write-Host ""
Write-Host "Analyzing due indicators..." -ForegroundColor Yellow
Write-Host "Indicators with schedulers that have never run should be due immediately:" -ForegroundColor Cyan

$query = "SELECT i.IndicatorID, i.IndicatorName, i.IsActive, i.LastRun, s.SchedulerID, s.SchedulerName, s.ScheduleType, s.IntervalMinutes, s.IsEnabled, CASE WHEN i.IsActive = 1 AND s.IsEnabled = 1 AND i.LastRun IS NULL THEN 'SHOULD BE DUE' WHEN i.IsActive = 0 THEN 'INACTIVE' WHEN s.IsEnabled = 0 THEN 'SCHEDULER DISABLED' WHEN s.SchedulerID IS NULL THEN 'NO SCHEDULER' ELSE 'NOT DUE' END as DueStatus FROM monitoring.Indicators i LEFT JOIN monitoring.Schedulers s ON i.SchedulerID = s.SchedulerID ORDER BY i.IndicatorID"

$dueAnalysis = sqlcmd -S "192.168.166.11,1433" -U "conexusadmin" -P "PWUi^g6~lxD" -d "PopAI" -Q $query -s "|"
Write-Host $dueAnalysis

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Green
