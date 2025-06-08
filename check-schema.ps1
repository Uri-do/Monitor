# PowerShell script to check the database schema
$connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true"

try {
    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "Connected to database successfully"
    Write-Host "Database: $($connection.Database)"
    Write-Host "Server: $($connection.DataSource)"
    Write-Host ""
    
    # Check if the columns exist
    $sql = @"
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'monitoring' 
    AND TABLE_NAME = 'KPIs'
    AND COLUMN_NAME IN (
        'ComparisonOperator',
        'ExecutionContext', 
        'ExecutionStartTime',
        'IsCurrentlyRunning',
        'KpiType',
        'LastMinutes',
        'ScheduleConfiguration',
        'ThresholdValue'
    )
ORDER BY COLUMN_NAME
"@
    
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    $reader = $command.ExecuteReader()
    
    Write-Host "Checking for missing columns in monitoring.KPIs table:"
    Write-Host "=================================================="
    
    $foundColumns = @()
    while ($reader.Read()) {
        $columnName = $reader["COLUMN_NAME"]
        $dataType = $reader["DATA_TYPE"]
        $isNullable = $reader["IS_NULLABLE"]
        $foundColumns += $columnName
        Write-Host "✅ $columnName ($dataType, Nullable: $isNullable)"
    }
    $reader.Close()
    
    # Check for missing columns
    $expectedColumns = @('ComparisonOperator', 'ExecutionContext', 'ExecutionStartTime', 'IsCurrentlyRunning', 'KpiType', 'LastMinutes', 'ScheduleConfiguration', 'ThresholdValue')
    $missingColumns = $expectedColumns | Where-Object { $_ -notin $foundColumns }
    
    if ($missingColumns.Count -gt 0) {
        Write-Host ""
        Write-Host "❌ Missing columns:"
        foreach ($col in $missingColumns) {
            Write-Host "   - $col"
        }
    } else {
        Write-Host ""
        Write-Host "✅ All expected columns are present!"
    }
}
catch {
    Write-Error "Error checking schema: $($_.Exception.Message)"
}
finally {
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
        Write-Host ""
        Write-Host "Database connection closed"
    }
}
