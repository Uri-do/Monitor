# Simple Database Connection Test
Write-Host "Testing PopAI Database Connection..." -ForegroundColor Green

$connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true"

try {
    Add-Type -AssemblyName "System.Data.SqlClient"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "✅ Connected to: $($connection.Database)" -ForegroundColor Green
    
    # Count KPIs
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM monitoring.KPIs"
    $count = $cmd.ExecuteScalar()
    
    Write-Host "📊 KPI Count: $count" -ForegroundColor Yellow
    
    # Get sample data
    $cmd.CommandText = "SELECT TOP 3 KpiId, Indicator, Owner, IsActive FROM monitoring.KPIs"
    $reader = $cmd.ExecuteReader()
    
    Write-Host "`nSample KPIs:" -ForegroundColor Cyan
    while ($reader.Read()) {
        Write-Host "  ID: $($reader[0]) | $($reader[1]) | Owner: $($reader[2]) | Active: $($reader[3])" -ForegroundColor White
    }
    
    $connection.Close()
    Write-Host "`n✅ Test completed successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}
