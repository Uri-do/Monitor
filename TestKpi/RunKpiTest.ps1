# PowerShell script to test KPI execution
# This script will execute the [stats].[stp_MonitorTransactions] stored procedure with 1000 minutes

$ServerInstance = "192.168.166.11,1433"
$Database = "ProgressPlayDBTest"
$Username = "saturn"
$Password = "Vt0zXXc800"

Write-Host "Testing KPI Execution: [stats].[stp_MonitorTransactions]" -ForegroundColor Green
Write-Host "Using 1000 minutes for testing with ProgressPlayDBTest database" -ForegroundColor Yellow
Write-Host ""

try {
    # Create connection string
    $ConnectionString = "Data Source=$ServerInstance;Initial Catalog=$Database;User ID=$Username;Password=$Password;TrustServerCertificate=true"
    
    # Create SQL connection
    $Connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $Connection.Open()
    
    Write-Host "Connected to ProgressPlayDBTest database successfully." -ForegroundColor Green
    
    # Create SQL command
    $Command = New-Object System.Data.SqlClient.SqlCommand("[stats].[stp_MonitorTransactions]", $Connection)
    $Command.CommandType = [System.Data.CommandType]::StoredProcedure
    $Command.CommandTimeout = 60
    
    # Add input parameter - using 1000 minutes as requested
    $Command.Parameters.AddWithValue("@ForLastMinutes", 1000) | Out-Null
    
    # Add output parameters
    $KeyParam = $Command.Parameters.Add("@Key", [System.Data.SqlDbType]::NVarChar, 255)
    $KeyParam.Direction = [System.Data.ParameterDirection]::Output
    
    $CurrentParam = $Command.Parameters.Add("@CurrentValue", [System.Data.SqlDbType]::Decimal)
    $CurrentParam.Direction = [System.Data.ParameterDirection]::Output
    $CurrentParam.Precision = 18
    $CurrentParam.Scale = 2
    
    $HistoricalParam = $Command.Parameters.Add("@HistoricalValue", [System.Data.SqlDbType]::Decimal)
    $HistoricalParam.Direction = [System.Data.ParameterDirection]::Output
    $HistoricalParam.Precision = 18
    $HistoricalParam.Scale = 2
    
    Write-Host "Executing stored procedure [stats].[stp_MonitorTransactions] with @ForLastMinutes = 1000..." -ForegroundColor Yellow
    
    $StartTime = Get-Date
    $Command.ExecuteNonQuery() | Out-Null
    $EndTime = Get-Date
    
    # Get output values
    $Key = if ($KeyParam.Value -eq [System.DBNull]::Value) { "NULL" } else { $KeyParam.Value }
    $CurrentValue = if ($CurrentParam.Value -eq [System.DBNull]::Value) { $null } else { [decimal]$CurrentParam.Value }
    $HistoricalValue = if ($HistoricalParam.Value -eq [System.DBNull]::Value) { $null } else { [decimal]$HistoricalParam.Value }
    
    # Calculate deviation if both values are available
    $Deviation = $null
    if ($CurrentValue -ne $null -and $HistoricalValue -ne $null -and $HistoricalValue -ne 0) {
        $Deviation = [Math]::Abs(($CurrentValue - $HistoricalValue) / $HistoricalValue) * 100
    }
    
    # Display results
    Write-Host ""
    Write-Host "=== KPI Execution Results ===" -ForegroundColor Cyan
    Write-Host "Execution Time: $([Math]::Round(($EndTime - $StartTime).TotalSeconds, 2)) seconds"
    Write-Host "Key: $Key"
    Write-Host "Current Value: $(if ($CurrentValue -ne $null) { $CurrentValue.ToString('N2') } else { 'NULL' })"
    Write-Host "Historical Value: $(if ($HistoricalValue -ne $null) { $HistoricalValue.ToString('N2') } else { 'NULL' })"
    
    if ($Deviation -ne $null) {
        Write-Host "Deviation: $($Deviation.ToString('F2'))%"
        
        # Determine if this would trigger an alert (assuming 15% threshold)
        $AlertThreshold = 15.0
        if ($Deviation -gt $AlertThreshold) {
            Write-Host "⚠️  ALERT: Deviation ($($Deviation.ToString('F2'))%) exceeds threshold ($AlertThreshold%)" -ForegroundColor Red
        } else {
            Write-Host "✅ OK: Deviation ($($Deviation.ToString('F2'))%) is within threshold ($AlertThreshold%)" -ForegroundColor Green
        }
    }
    
    Write-Host "=== End Results ===" -ForegroundColor Cyan
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.Exception.StackTrace)" -ForegroundColor Red
} finally {
    if ($Connection -and $Connection.State -eq [System.Data.ConnectionState]::Open) {
        $Connection.Close()
        Write-Host ""
        Write-Host "Database connection closed." -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
