# PowerShell script to populate indicators table with sample data
# Make sure your API is running on https://localhost:7001

$apiBaseUrl = "https://localhost:7001/api"
$indicatorsFile = "sample-indicators.json"

Write-Host "🚀 Starting Indicator Population Script" -ForegroundColor Green
Write-Host "API Base URL: $apiBaseUrl" -ForegroundColor Yellow

# Check if API is running
try {
    Write-Host "🔍 Checking if API is running..." -ForegroundColor Yellow
    $healthCheck = Invoke-RestMethod -Uri "$apiBaseUrl/indicator/dashboard" -Method GET -SkipCertificateCheck
    Write-Host "✅ API is running successfully!" -ForegroundColor Green
} catch {
    Write-Host "❌ API is not running or not accessible at $apiBaseUrl" -ForegroundColor Red
    Write-Host "Please make sure your API is running and try again." -ForegroundColor Red
    exit 1
}

# Check if sample data file exists
if (-not (Test-Path $indicatorsFile)) {
    Write-Host "❌ Sample indicators file not found: $indicatorsFile" -ForegroundColor Red
    exit 1
}

# Read sample indicators
try {
    $indicators = Get-Content $indicatorsFile | ConvertFrom-Json
    Write-Host "📄 Loaded $($indicators.Count) sample indicators from $indicatorsFile" -ForegroundColor Green
} catch {
    Write-Host "❌ Error reading sample indicators file: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Create each indicator
$successCount = 0
$errorCount = 0

foreach ($indicator in $indicators) {
    try {
        Write-Host "📝 Creating indicator: $($indicator.indicatorName)" -ForegroundColor Yellow
        
        $response = Invoke-RestMethod -Uri "$apiBaseUrl/indicator" -Method POST -Body ($indicator | ConvertTo-Json -Depth 10) -ContentType "application/json" -SkipCertificateCheck
        
        Write-Host "✅ Successfully created indicator: $($indicator.indicatorName) (ID: $($response.indicatorId))" -ForegroundColor Green
        $successCount++
    } catch {
        Write-Host "❌ Error creating indicator $($indicator.indicatorName): $($_.Exception.Message)" -ForegroundColor Red
        $errorCount++
        
        # Try to get more details from the response
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseBody = $reader.ReadToEnd()
            Write-Host "Response details: $responseBody" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "🎯 Population Summary:" -ForegroundColor Cyan
Write-Host "✅ Successfully created: $successCount indicators" -ForegroundColor Green
Write-Host "❌ Failed to create: $errorCount indicators" -ForegroundColor Red

if ($successCount -gt 0) {
    Write-Host ""
    Write-Host "🎉 Indicators have been populated! You can now:" -ForegroundColor Green
    Write-Host "   • View them in your frontend at http://localhost:5173/" -ForegroundColor White
    Write-Host "   • Check the API at $apiBaseUrl/indicator" -ForegroundColor White
    Write-Host "   • Test execution with POST $apiBaseUrl/indicator/{id}/execute" -ForegroundColor White
}

Write-Host ""
Write-Host "🏁 Script completed!" -ForegroundColor Green
