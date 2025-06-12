# PowerShell script to test database connection and verify schema
$connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true"

try {
    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "✅ Connected to database successfully"
    Write-Host "Database: $($connection.Database)"
    Write-Host "Server: $($connection.DataSource)"
    Write-Host ""
    
    # Check if the KPIs table exists
    $sql = @"
SELECT 
    TABLE_SCHEMA,
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'KPIs'
ORDER BY TABLE_SCHEMA, TABLE_NAME
"@
    
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    $reader = $command.ExecuteReader()
    
    Write-Host "KPIs table information:"
    Write-Host "======================"
    
    $tableFound = $false
    while ($reader.Read()) {
        $tableFound = $true
        $schema = $reader["TABLE_SCHEMA"]
        $tableName = $reader["TABLE_NAME"]
        $tableType = $reader["TABLE_TYPE"]
        Write-Host "✅ Found table: [$schema].[$tableName] (Type: $tableType)"
    }
    $reader.Close()
    
    if (-not $tableFound) {
        Write-Host "❌ No KPIs table found!"
        return
    }
    
    Write-Host ""
    
    # Get all columns from the KPIs table
    $sql = @"
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    CHARACTER_MAXIMUM_LENGTH,
    NUMERIC_PRECISION,
    NUMERIC_SCALE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = 'monitoring' 
    AND TABLE_NAME = 'KPIs'
ORDER BY ORDINAL_POSITION
"@
    
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    $reader = $command.ExecuteReader()
    
    Write-Host "All columns in monitoring.KPIs table:"
    Write-Host "====================================="
    
    $columnCount = 0
    while ($reader.Read()) {
        $columnCount++
        $columnName = $reader["COLUMN_NAME"]
        $dataType = $reader["DATA_TYPE"]
        $isNullable = $reader["IS_NULLABLE"]
        $columnDefault = $reader["COLUMN_DEFAULT"]
        $maxLength = $reader["CHARACTER_MAXIMUM_LENGTH"]
        
        $typeInfo = $dataType
        if ($maxLength -and $maxLength -ne [DBNull]::Value) {
            $typeInfo += "($maxLength)"
        }
        
        Write-Host "$columnCount. $columnName - $typeInfo (Nullable: $isNullable)"
        if ($columnDefault -and $columnDefault -ne [DBNull]::Value) {
            Write-Host "   Default: $columnDefault"
        }
    }
    $reader.Close()
    
    Write-Host ""
    Write-Host "Total columns found: $columnCount"
    
    # Test a simple SELECT query
    Write-Host ""
    Write-Host "Testing simple SELECT query:"
    Write-Host "============================"
    
    $sql = "SELECT COUNT(*) as RecordCount FROM monitoring.KPIs"
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    $recordCount = $command.ExecuteScalar()
    
    Write-Host "✅ Query successful - Record count: $recordCount"
}
catch {
    Write-Error "Error: $($_.Exception.Message)"
}
finally {
    if ($connection -and $connection.State -eq 'Open') {
        $connection.Close()
        Write-Host ""
        Write-Host "Database connection closed"
    }
}
