Write-Host "Testing Database Connection..." -ForegroundColor Green

$connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true"

try {
    Add-Type -AssemblyName "System.Data.SqlClient"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "Connected to database: $($connection.Database)" -ForegroundColor Green
    
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) FROM monitoring.KPIs"
    $count = $cmd.ExecuteScalar()
    
    Write-Host "KPI Count: $count" -ForegroundColor Yellow
    
    $connection.Close()
    Write-Host "Test completed successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
