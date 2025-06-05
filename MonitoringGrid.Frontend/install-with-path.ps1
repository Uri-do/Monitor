# Set up Node.js environment and install dependencies
Write-Host "Setting up Node.js environment..." -ForegroundColor Green

# Add Node.js to PATH for this session
$nodePath = "C:\Program Files\nodejs"
if (Test-Path $nodePath) {
    $env:PATH = "$nodePath;$env:PATH"
    Write-Host "Added Node.js to PATH" -ForegroundColor Green
} else {
    Write-Host "Node.js not found at $nodePath" -ForegroundColor Red
    exit 1
}

# Verify Node.js is available
try {
    $nodeVersion = & node --version
    Write-Host "Node.js version: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "Node.js still not accessible" -ForegroundColor Red
    exit 1
}

# Verify npm is available
try {
    $npmVersion = & npm --version
    Write-Host "npm version: $npmVersion" -ForegroundColor Green
} catch {
    Write-Host "npm not accessible" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Installing dependencies..." -ForegroundColor Yellow

# Install dependencies
try {
    & npm install
    Write-Host "Dependencies installed successfully!" -ForegroundColor Green
} catch {
    Write-Host "Failed to install dependencies!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Installation completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "To start the development server, run:" -ForegroundColor Cyan
Write-Host "  npm run dev" -ForegroundColor White
