# PowerShell script to create the missing monitoring.Schedulers table
$connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true"

# Read the SQL script
$sqlScript = Get-Content "scripts/create-schedulers-table.sql" -Raw

try {
    Write-Host "=== Creating monitoring.Schedulers Table ===" -ForegroundColor Green
    Write-Host "Connection: 192.168.166.11,1433 -> PopAI" -ForegroundColor Yellow
    
    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "Connected to database successfully" -ForegroundColor Green
    
    # Execute the script
    $command = New-Object System.Data.SqlClient.SqlCommand($sqlScript, $connection)
    $command.CommandTimeout = 300  # 5 minutes timeout
    
    Write-Host "Executing Schedulers table creation script..." -ForegroundColor Yellow
    $result = $command.ExecuteNonQuery()
    
    Write-Host "Schedulers table creation completed successfully!" -ForegroundColor Green
    Write-Host "Rows affected: $result" -ForegroundColor Yellow
    
    # Verify the table was created
    Write-Host ""
    Write-Host "Verifying table creation..." -ForegroundColor Yellow
    $verifyCommand = New-Object System.Data.SqlClient.SqlCommand("SELECT COUNT(*) FROM monitoring.Schedulers", $connection)
    $count = $verifyCommand.ExecuteScalar()
    Write-Host "Schedulers table contains $count records" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "=== SUCCESS ===" -ForegroundColor Green
    Write-Host "The monitoring.Schedulers table has been created and populated with default schedulers." -ForegroundColor Green
    Write-Host "The application should now be able to load indicators without database errors." -ForegroundColor Green
}
catch {
    Write-Host ""
    Write-Host "=== ERROR ===" -ForegroundColor Red
    Write-Host "Error creating Schedulers table: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack trace:" -ForegroundColor Yellow
    Write-Host $_.Exception.StackTrace -ForegroundColor Yellow
    exit 1
}
finally {
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
        Write-Host "Database connection closed" -ForegroundColor Yellow
    }
}
