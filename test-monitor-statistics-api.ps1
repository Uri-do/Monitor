# Test script for Monitor Statistics API endpoints
# Run this after starting the MonitoringGrid.Api

$baseUrl = "https://localhost:7001"  # Adjust port as needed
$apiUrl = "$baseUrl/api/monitorstatistics"

Write-Host "Testing Monitor Statistics API Endpoints" -ForegroundColor Green
Write-Host "Base URL: $apiUrl" -ForegroundColor Yellow

# Test 1: Get Active Collectors
Write-Host "`n1. Testing GET /collectors (active only)" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$apiUrl/collectors?activeOnly=true" -Method GET -ContentType "application/json"
    Write-Host "✅ Success: Retrieved collectors" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor White
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Get All Collectors
Write-Host "`n2. Testing GET /collectors (all)" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$apiUrl/collectors?activeOnly=false" -Method GET -ContentType "application/json"
    Write-Host "✅ Success: Retrieved all collectors" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor White
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Get Specific Collector (if we have data)
Write-Host "`n3. Testing GET /collectors/{id}" -ForegroundColor Cyan
$testCollectorId = 1  # Adjust based on your data
try {
    $response = Invoke-RestMethod -Uri "$apiUrl/collectors/$testCollectorId" -Method GET -ContentType "application/json"
    Write-Host "✅ Success: Retrieved collector $testCollectorId" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor White
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Get Collector Item Names
Write-Host "`n4. Testing GET /collectors/{id}/items" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$apiUrl/collectors/$testCollectorId/items" -Method GET -ContentType "application/json"
    Write-Host "✅ Success: Retrieved item names for collector $testCollectorId" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor White
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Get Statistics
Write-Host "`n5. Testing GET /collectors/{id}/statistics" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$apiUrl/collectors/$testCollectorId/statistics?hours=24" -Method GET -ContentType "application/json"
    Write-Host "✅ Success: Retrieved statistics for collector $testCollectorId" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor White
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n🎯 API Testing Complete!" -ForegroundColor Green
Write-Host "Note: Some tests may fail if the database tables are empty or the API is not running." -ForegroundColor Yellow
