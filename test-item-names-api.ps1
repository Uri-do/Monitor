# Test script to check collector item names API
$monitorStatsApiUrl = "https://localhost:57652/api/monitorstatistics"
$indicatorsApiUrl = "https://localhost:57652/api/indicators"

# Test collector ID (TransactionsByType has ID 1)
$testCollectorId = 1

Write-Host "Testing Collector Item Names API" -ForegroundColor Yellow
Write-Host "Monitor Stats API URL: $monitorStatsApiUrl" -ForegroundColor Gray
Write-Host "Indicators API URL: $indicatorsApiUrl" -ForegroundColor Gray
Write-Host "Test Collector ID: $testCollectorId" -ForegroundColor Gray

# Skip certificate validation for localhost testing
add-type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint srvPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy

# Test 1: Monitor Statistics API
Write-Host "`n1. Testing Monitor Statistics API: GET /collectors/$testCollectorId/items" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$monitorStatsApiUrl/collectors/$testCollectorId/items" -Method GET -ContentType "application/json"
    Write-Host "✅ Success: Retrieved item names from Monitor Statistics API" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor White

    if ($response.isSuccess -and $response.value) {
        Write-Host "`nItem Names Found:" -ForegroundColor Green
        $response.value | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
    } else {
        Write-Host "No item names found or API returned failure" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

# Test 2: Indicators API
Write-Host "`n2. Testing Indicators API: GET /collectors/$testCollectorId/items" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$indicatorsApiUrl/collectors/$testCollectorId/items" -Method GET -ContentType "application/json"
    Write-Host "✅ Success: Retrieved item names from Indicators API" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 3)" -ForegroundColor White

    if ($response -is [array]) {
        Write-Host "`nItem Names Found:" -ForegroundColor Green
        $response | ForEach-Object { Write-Host "  - $_" -ForegroundColor White }
    } else {
        Write-Host "No item names found" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

# Test 3: Test Dashboard endpoint (should work with AllowAnonymous)
Write-Host "`n3. Testing Indicators Dashboard API: GET /dashboard" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$indicatorsApiUrl/dashboard" -Method GET -ContentType "application/json"
    Write-Host "✅ Success: Retrieved dashboard from Indicators API" -ForegroundColor Green
    Write-Host "Response: $($response | ConvertTo-Json -Depth 2)" -ForegroundColor White
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}

Write-Host "`nTest completed." -ForegroundColor Yellow
