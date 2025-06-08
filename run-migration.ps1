# PowerShell script to run the database migration
$connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true"

# Read the SQL script
$sqlScript = Get-Content "Database/08_AddKpiExecutionTracking.sql" -Raw

try {
    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "Connected to database successfully"
    
    # Execute the script
    $command = New-Object System.Data.SqlClient.SqlCommand($sqlScript, $connection)
    $command.CommandTimeout = 300  # 5 minutes timeout
    
    Write-Host "Executing migration script..."
    $result = $command.ExecuteNonQuery()
    
    Write-Host "Migration completed successfully. Rows affected: $result"
}
catch {
    Write-Error "Error executing migration: $($_.Exception.Message)"
}
finally {
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
        Write-Host "Database connection closed"
    }
}
