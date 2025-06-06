# PowerShell script to test the updated KPI execution with 100,000 minutes
# This script will directly test the stored procedure to see if we get actual transaction data

$ServerInstance = "192.168.166.11,1433"
$Database = "ProgressPlayDBTest"
$Username = "saturn"
$Password = "Vt0zXXc800"

Write-Host "Testing Transaction Monitoring KPI with 100,000 minutes..." -ForegroundColor Green
Write-Host "This should capture approximately 69 days of transaction data" -ForegroundColor Yellow
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
    $Command.CommandTimeout = 120  # 2 minutes timeout for large data
    
    # Add input parameter - using 100,000 minutes as configured in the KPI
    $Command.Parameters.AddWithValue("@ForLastMinutes", 100000) | Out-Null
    
    Write-Host "Executing stored procedure [stats].[stp_MonitorTransactions] with @ForLastMinutes = 100,000..." -ForegroundColor Yellow
    
    $StartTime = Get-Date
    
    # Execute and get any result sets
    $Reader = $Command.ExecuteReader()
    
    $EndTime = Get-Date
    
    Write-Host ""
    Write-Host "=== KPI Execution Results ===" -ForegroundColor Cyan
    Write-Host "Execution Time: $([Math]::Round(($EndTime - $StartTime).TotalSeconds, 2)) seconds"
    Write-Host ""
    
    # Process all result sets
    $ResultSetCount = 0
    $TotalRecords = 0
    
    do {
        $ResultSetCount++
        Write-Host "Result Set ${ResultSetCount}:" -ForegroundColor Yellow
        Write-Host "----------------------------------------"
        
        $RecordCount = 0
        $HasData = $false
        
        # Read column names
        $ColumnNames = @()
        for ($i = 0; $i -lt $Reader.FieldCount; $i++) {
            $ColumnNames += $Reader.GetName($i)
        }
        
        # Display header
        $Header = $ColumnNames -join "`t"
        Write-Host $Header -ForegroundColor White
        Write-Host ("-" * $Header.Length) -ForegroundColor Gray
        
        # Read all rows in this result set
        while ($Reader.Read()) {
            $HasData = $true
            $RecordCount++
            $TotalRecords++
            
            $Values = @()
            for ($i = 0; $i -lt $Reader.FieldCount; $i++) {
                $Value = $Reader.GetValue($i)
                if ($Value -eq [System.DBNull]::Value) {
                    $Values += "NULL"
                } else {
                    $Values += $Value.ToString()
                }
            }
            
            $Row = $Values -join "`t"
            Write-Host $Row -ForegroundColor Cyan
        }
        
        if (-not $HasData) {
            Write-Host "No data returned in this result set." -ForegroundColor Red
        } else {
            Write-Host ""
            Write-Host "Records in this result set: $RecordCount" -ForegroundColor Green
        }
        
        Write-Host ""
        
    } while ($Reader.NextResult())
    
    $Reader.Close()
    
    Write-Host "=== SUMMARY ===" -ForegroundColor Magenta
    Write-Host "Total Result Sets: $ResultSetCount" -ForegroundColor White
    Write-Host "Total Records: $TotalRecords" -ForegroundColor White
    Write-Host "Execution Time: $([Math]::Round(($EndTime - $StartTime).TotalSeconds, 2)) seconds" -ForegroundColor White
    
    if ($TotalRecords -gt 0) {
        Write-Host ""
        Write-Host "✅ SUCCESS: The stored procedure returned actual transaction data!" -ForegroundColor Green
        Write-Host "The KPI should now show real results instead of empty data." -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "⚠️  WARNING: No transaction data found even with 100,000 minutes lookback." -ForegroundColor Yellow
        Write-Host "This might indicate:" -ForegroundColor Yellow
        Write-Host "- No transactions exist in the database" -ForegroundColor Yellow
        Write-Host "- The stored procedure needs to be created in ProgressPlayDBTest" -ForegroundColor Yellow
        Write-Host "- Different table structure or data location" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "Error occurred: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.Exception.StackTrace)" -ForegroundColor Red
} finally {
    if ($Connection.State -eq 'Open') {
        $Connection.Close()
        Write-Host ""
        Write-Host "Database connection closed." -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "Test completed!" -ForegroundColor Green
