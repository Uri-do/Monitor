# Authentication Flow Test Script
# This script tests the complete authentication implementation

Write-Host "🔐 MonitoringGrid Authentication Flow Test" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Configuration
$ApiBaseUrl = "https://localhost:57652"
$FrontendUrl = "http://localhost:5173"

# Test credentials
$TestUser = @{
    username = "testuser"
    password = "Test123!"
}

Write-Host ""
Write-Host "📋 Test Configuration:" -ForegroundColor Yellow
Write-Host "API URL: $ApiBaseUrl"
Write-Host "Frontend URL: $FrontendUrl"
Write-Host "Test User: $($TestUser.username)"
Write-Host ""

# Function to test API endpoint
function Test-ApiEndpoint {
    param(
        [string]$Url,
        [string]$Description,
        [hashtable]$Headers = @{},
        [string]$Method = "GET",
        [object]$Body = $null
    )
    
    Write-Host "Testing: $Description" -ForegroundColor White
    Write-Host "URL: $Url" -ForegroundColor Gray
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            SkipCertificateCheck = $true
        }
        
        if ($Body) {
            $params.Body = $Body | ConvertTo-Json
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @params
        Write-Host "✅ SUCCESS" -ForegroundColor Green
        return $response
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "❌ FAILED - Status: $statusCode" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
    finally {
        Write-Host ""
    }
}

# Test 1: Public Endpoints
Write-Host "🌐 Testing Public Endpoints" -ForegroundColor Cyan
Write-Host "----------------------------"

$healthResponse = Test-ApiEndpoint -Url "$ApiBaseUrl/health" -Description "Health Check (Public)"
$infoResponse = Test-ApiEndpoint -Url "$ApiBaseUrl/api/info" -Description "API Info (Public)"

# Test 2: Protected Endpoint (Should Fail)
Write-Host "🔒 Testing Protected Endpoints (Should Fail)" -ForegroundColor Cyan
Write-Host "----------------------------------------------"

$dashboardResponse = Test-ApiEndpoint -Url "$ApiBaseUrl/api/kpi/dashboard" -Description "Dashboard (Protected - Should Fail)"

# Test 3: Authentication
Write-Host "🔐 Testing Authentication" -ForegroundColor Cyan
Write-Host "--------------------------"

$loginResponse = Test-ApiEndpoint -Url "$ApiBaseUrl/api/security/auth/login" -Description "User Login" -Method "POST" -Body $TestUser

if ($loginResponse -and $loginResponse.isSuccess -and $loginResponse.token) {
    Write-Host "✅ Login successful! Token received." -ForegroundColor Green
    $token = $loginResponse.token.accessToken
    
    # Test 4: Protected Endpoint with Token (Should Succeed)
    Write-Host ""
    Write-Host "🔓 Testing Protected Endpoints with Token" -ForegroundColor Cyan
    Write-Host "------------------------------------------"
    
    $authHeaders = @{
        "Authorization" = "Bearer $token"
    }
    
    $authenticatedDashboard = Test-ApiEndpoint -Url "$ApiBaseUrl/api/kpi/dashboard" -Description "Dashboard (With Token)" -Headers $authHeaders
    
    if ($authenticatedDashboard) {
        Write-Host "✅ Dashboard data retrieved successfully!" -ForegroundColor Green
        Write-Host "KPI Count: $($authenticatedDashboard.totalKpis)" -ForegroundColor Green
        Write-Host "Active KPIs: $($authenticatedDashboard.activeKpis)" -ForegroundColor Green
    }
}
else {
    Write-Host "❌ Login failed! Cannot test authenticated endpoints." -ForegroundColor Red
}

# Test 5: Frontend URLs
Write-Host ""
Write-Host "🌐 Frontend URL Tests" -ForegroundColor Cyan
Write-Host "----------------------"

Write-Host "Frontend URLs to test manually:"
Write-Host "• Login Page: $FrontendUrl/login" -ForegroundColor Yellow
Write-Host "• Auth Test Page: $FrontendUrl/auth-test" -ForegroundColor Yellow
Write-Host "• Dashboard: $FrontendUrl/dashboard (should redirect to login)" -ForegroundColor Yellow

# Summary
Write-Host ""
Write-Host "📊 Test Summary" -ForegroundColor Cyan
Write-Host "=================="

if ($healthResponse) {
    Write-Host "✅ Public endpoints working" -ForegroundColor Green
} else {
    Write-Host "❌ Public endpoints failing" -ForegroundColor Red
}

if ($dashboardResponse -eq $null) {
    Write-Host "✅ Protected endpoints properly secured" -ForegroundColor Green
} else {
    Write-Host "❌ Protected endpoints not secured!" -ForegroundColor Red
}

if ($loginResponse -and $loginResponse.isSuccess) {
    Write-Host "✅ Authentication working" -ForegroundColor Green
} else {
    Write-Host "❌ Authentication failing" -ForegroundColor Red
}

if ($authenticatedDashboard) {
    Write-Host "✅ Authenticated requests working" -ForegroundColor Green
} else {
    Write-Host "❌ Authenticated requests failing" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎯 Next Steps:" -ForegroundColor Yellow
Write-Host "1. If tests pass, open $FrontendUrl/auth-test in browser"
Write-Host "2. Test the complete login flow in the frontend"
Write-Host "3. Verify dashboard loads with real data after login"
Write-Host ""
Write-Host "Test completed!" -ForegroundColor Cyan
