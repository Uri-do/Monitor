# Monitoring Grid Frontend Startup Script
Write-Host "=== Monitoring Grid Frontend Startup ===" -ForegroundColor Cyan
Write-Host ""

# Set Node.js path
$nodePath = "C:\Program Files\nodejs"
$env:PATH = "$nodePath;$env:PATH"

# Verify Node.js installation
Write-Host "Checking Node.js installation..." -ForegroundColor Yellow
try {
    $nodeVersion = & node --version
    Write-Host "✓ Node.js version: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Node.js not found! Please install Node.js first." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Verify npm installation
try {
    $npmVersion = & npm --version
    Write-Host "✓ npm version: $npmVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ npm not found!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Starting Vite development server..." -ForegroundColor Yellow
Write-Host "The application will be available at: http://localhost:5173" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Gray
Write-Host ""

# Start the development server
try {
    & npm run dev
} catch {
    Write-Host "✗ Failed to start development server!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
