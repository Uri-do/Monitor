# Test Database Connection to PopAI
Write-Host "🔍 Testing Database Connection to PopAI Database" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green

$connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true"

try {
    # Load SQL Client
    Add-Type -AssemblyName "System.Data.SqlClient"
    
    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "✅ Connected to database: $($connection.Database)" -ForegroundColor Green
    Write-Host "   Server: $($connection.DataSource)" -ForegroundColor Yellow
    
    # Test if monitoring schema exists
    $schemaCmd = $connection.CreateCommand()
    $schemaCmd.CommandText = "SELECT COUNT(*) FROM sys.schemas WHERE name = 'monitoring'"
    $schemaExists = $schemaCmd.ExecuteScalar()
    
    Write-Host "📁 Monitoring schema exists: $($schemaExists -gt 0)" -ForegroundColor $(if($schemaExists -gt 0) { "Green" } else { "Red" })
    
    if ($schemaExists -gt 0) {
        # Test if KPIs table exists
        $tableCmd = $connection.CreateCommand()
        $tableCmd.CommandText = "SELECT COUNT(*) FROM sys.tables t INNER JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'monitoring' AND t.name = 'KPIs'"
        $tableExists = $tableCmd.ExecuteScalar()
        
        Write-Host "📊 KPIs table exists: $($tableExists -gt 0)" -ForegroundColor $(if($tableExists -gt 0) { "Green" } else { "Red" })
        
        if ($tableExists -gt 0) {
            # Count KPIs
            $countCmd = $connection.CreateCommand()
            $countCmd.CommandText = "SELECT COUNT(*) FROM monitoring.KPIs"
            $kpiCount = $countCmd.ExecuteScalar()
            
            Write-Host "🔢 Total KPIs in table: $kpiCount" -ForegroundColor Green
            
            if ($kpiCount -gt 0) {
                # Get sample data
                $sampleCmd = $connection.CreateCommand()
                $sampleCmd.CommandText = @"
                    SELECT TOP 3 
                        KpiId, 
                        Indicator, 
                        Owner, 
                        Priority, 
                        Frequency, 
                        IsActive,
                        LastRun
                    FROM monitoring.KPIs 
                    ORDER BY KpiId
"@
                
                $reader = $sampleCmd.ExecuteReader()
                
                Write-Host "`n📋 Sample KPI Data:" -ForegroundColor Cyan
                Write-Host "===================" -ForegroundColor Cyan
                
                while ($reader.Read()) {
                    Write-Host "   ID: $($reader['KpiId']) | Indicator: $($reader['Indicator']) | Owner: $($reader['Owner']) | Active: $($reader['IsActive'])" -ForegroundColor White
                }
                $reader.Close()
            }
        }
    }

    $connection.Close()
    Write-Host "`n✅ Database connection test completed successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Full Error: $($_.Exception.ToString())" -ForegroundColor Red
}

Write-Host "`nPress any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
