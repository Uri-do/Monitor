# PowerShell script to add the Transaction Monitoring KPI to the database

$ServerInstance = "192.168.166.11,1433"
$Database = "PopAI"
$Username = "conexusadmin"
$Password = "PWUi^g6~lxD"

Write-Host "Adding Transaction Monitoring KPI to the monitoring database..." -ForegroundColor Green
Write-Host ""

try {
    # Create connection string
    $ConnectionString = "Data Source=$ServerInstance;Initial Catalog=$Database;User ID=$Username;Password=$Password;TrustServerCertificate=true"
    
    # Create SQL connection
    $Connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $Connection.Open()
    
    Write-Host "Connected to PopAI database successfully." -ForegroundColor Green
    
    # Read the SQL script
    $SqlScript = Get-Content -Path "..\Database\04_AddTransactionMonitoringKPI.sql" -Raw
    
    # Split the script into individual commands (split by GO)
    $Commands = $SqlScript -split '\bGO\b'
    
    foreach ($CommandText in $Commands) {
        $CommandText = $CommandText.Trim()
        if (-not [string]::IsNullOrWhiteSpace($CommandText)) {
            Write-Host "Executing SQL command..." -ForegroundColor Yellow
            
            $Command = New-Object System.Data.SqlClient.SqlCommand($CommandText, $Connection)
            $Command.CommandTimeout = 60
            
            try {
                $Result = $Command.ExecuteNonQuery()
                Write-Host "Command executed successfully. Rows affected: $Result" -ForegroundColor Green
            }
            catch {
                # Try to execute as scalar for SELECT statements
                try {
                    $Command = New-Object System.Data.SqlClient.SqlCommand($CommandText, $Connection)
                    $Reader = $Command.ExecuteReader()
                    
                    if ($Reader.HasRows) {
                        Write-Host "Query results:" -ForegroundColor Cyan
                        
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
                        while ($Reader.Read()) {
                            $RowLine = ""
                            for ($i = 0; $i -lt $ColumnCount; $i++) {
                                $Value = if ($Reader.IsDBNull($i)) { "NULL" } else { $Reader.GetValue($i).ToString() }
                                $RowLine += $Value.PadRight(20) + " "
                            }
                            Write-Host $RowLine
                        }
                    }
                    
                    $Reader.Close()
                }
                catch {
                    Write-Host "Error executing command: $($_.Exception.Message)" -ForegroundColor Red
                }
            }
            
            Write-Host ""
        }
    }
    
    Write-Host "=== KPI Setup Completed ===" -ForegroundColor Green
    Write-Host "Transaction Monitoring KPI has been added to the monitoring system." -ForegroundColor Green
    Write-Host "- KPI Name: Transaction Success Rate" -ForegroundColor Cyan
    Write-Host "- Stored Procedure: [stats].[stp_MonitorTransactions]" -ForegroundColor Cyan
    Write-Host "- Frequency: 30 minutes" -ForegroundColor Cyan
    Write-Host "- Alert Threshold: 5% deviation" -ForegroundColor Cyan
    Write-Host "- Minimum Success Rate: 90%" -ForegroundColor Cyan
    
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
