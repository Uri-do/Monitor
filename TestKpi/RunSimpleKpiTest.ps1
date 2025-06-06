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
    
    # Create SQL command - just call the procedure with the input parameter
    $Command = New-Object System.Data.SqlClient.SqlCommand("[stats].[stp_MonitorTransactions]", $Connection)
    $Command.CommandType = [System.Data.CommandType]::StoredProcedure
    $Command.CommandTimeout = 60
    
    # Add input parameter - using 1000 minutes as requested
    $Command.Parameters.AddWithValue("@ForLastMinutes", 1000) | Out-Null
    
    Write-Host "Executing stored procedure [stats].[stp_MonitorTransactions] with @ForLastMinutes = 1000..." -ForegroundColor Yellow
    
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
    do {
        $ResultSetCount++
        Write-Host "Result Set ${ResultSetCount}:" -ForegroundColor Yellow
        Write-Host "----------------------------------------"
        
        if ($Reader.HasRows) {
            # Get column names
            $ColumnCount = $Reader.FieldCount
            $ColumnNames = @()
            for ($i = 0; $i -lt $ColumnCount; $i++) {
                $ColumnNames += $Reader.GetName($i)
            }
            
            # Display column headers
            $HeaderLine = ""
            foreach ($ColName in $ColumnNames) {
                $HeaderLine += $ColName.PadRight(20) + " "
            }
            Write-Host $HeaderLine -ForegroundColor Cyan
            Write-Host ("-" * $HeaderLine.Length) -ForegroundColor Gray
            
            # Display rows
            $RowCount = 0
            while ($Reader.Read()) {
                $RowCount++
                $RowLine = ""
                for ($i = 0; $i -lt $ColumnCount; $i++) {
                    $Value = if ($Reader.IsDBNull($i)) { "NULL" } else { $Reader.GetValue($i).ToString() }
                    $RowLine += $Value.PadRight(20) + " "
                }
                Write-Host $RowLine
            }
            
            Write-Host ""
            Write-Host "Rows returned: ${RowCount}" -ForegroundColor Green
        } else {
            Write-Host "No rows returned." -ForegroundColor Yellow
        }
        
        Write-Host ""
        
    } while ($Reader.NextResult())
    
    $Reader.Close()
    
    if ($ResultSetCount -eq 1) {
        Write-Host "Total result sets: ${ResultSetCount}" -ForegroundColor Green
    } else {
        Write-Host "Total result sets: ${ResultSetCount}" -ForegroundColor Green
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
