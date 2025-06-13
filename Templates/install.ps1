# Enterprise Application Template Installer
# This script installs the template and provides usage examples

Write-Host "🚀 Enterprise Application Template Installer" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

# Check if dotnet CLI is available
if (!(Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Error ".NET CLI is not installed or not in PATH"
    Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Get the current directory (where the template files are)
$templatePath = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "📦 Installing Enterprise Application Template..." -ForegroundColor Yellow
Write-Host "Template path: $templatePath" -ForegroundColor Gray

# Install the template
dotnet new install $templatePath

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to install template"
    exit 1
}

Write-Host "✅ Template installed successfully!" -ForegroundColor Green

# Verify installation
Write-Host "🔍 Verifying installation..." -ForegroundColor Yellow
$templateList = dotnet new list | Select-String "enterprise-app"

if ($templateList) {
    Write-Host "✅ Template verification successful" -ForegroundColor Green
    Write-Host $templateList -ForegroundColor Gray
} else {
    Write-Warning "Template verification failed - template may not be properly installed"
}

Write-Host ""
Write-Host "🎉 Installation Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Usage Examples:" -ForegroundColor Yellow
Write-Host ""

Write-Host "1. Basic CRM Application:" -ForegroundColor Cyan
Write-Host "   dotnet new enterprise-app --name `"Contoso.CRM`" --domain `"Customer`" --output `"./ContosoCRM`"" -ForegroundColor White
Write-Host ""

Write-Host "2. E-commerce Platform:" -ForegroundColor Cyan
Write-Host "   dotnet new enterprise-app --name `"ShopApp.Platform`" --domain `"Product`" --database `"ShopAppDB`" --port 8080 --output `"./ShopApp`"" -ForegroundColor White
Write-Host ""

Write-Host "3. Project Management System:" -ForegroundColor Cyan
Write-Host "   dotnet new enterprise-app --name `"ProjectHub.Core`" --domain `"Project`" --enable-worker true --output `"./ProjectHub`"" -ForegroundColor White
Write-Host ""

Write-Host "4. Minimal API (no auth, no worker):" -ForegroundColor Cyan
Write-Host "   dotnet new enterprise-app --name `"SimpleAPI`" --domain `"Task`" --enable-auth false --enable-worker false --output `"./SimpleAPI`"" -ForegroundColor White
Write-Host ""

Write-Host "🛠️  Available Parameters:" -ForegroundColor Yellow
Write-Host "  --name              Application name and namespace (required)" -ForegroundColor White
Write-Host "  --domain            Primary domain entity (default: Item)" -ForegroundColor White
Write-Host "  --database          Database name (default: {name}DB)" -ForegroundColor White
Write-Host "  --port              API port (default: 5000)" -ForegroundColor White
Write-Host "  --frontend-port     Frontend port (default: 3000)" -ForegroundColor White
Write-Host "  --enable-auth       Include authentication (default: true)" -ForegroundColor White
Write-Host "  --enable-worker     Include worker service (default: true)" -ForegroundColor White
Write-Host "  --enable-docker     Include Docker setup (default: true)" -ForegroundColor White
Write-Host "  --enable-testing    Include test projects (default: true)" -ForegroundColor White
Write-Host "  --skip-restore      Skip package restore (default: false)" -ForegroundColor White
Write-Host ""

Write-Host "📚 Documentation:" -ForegroundColor Yellow
Write-Host "  Template Guide: $templatePath\docs\TEMPLATE_GUIDE.md" -ForegroundColor White
Write-Host "  Setup Script:   $templatePath\setup.ps1" -ForegroundColor White
Write-Host ""

Write-Host "🚀 Quick Start:" -ForegroundColor Yellow
Write-Host "  1. Create your app: dotnet new enterprise-app --name `"MyApp`" --domain `"Customer`"" -ForegroundColor White
Write-Host "  2. Navigate:        cd MyApp" -ForegroundColor White
Write-Host "  3. Setup database:  Update connection strings in appsettings.json" -ForegroundColor White
Write-Host "  4. Run migrations:  dotnet ef database update" -ForegroundColor White
Write-Host "  5. Start API:       dotnet run --project src/MyApp.Api" -ForegroundColor White
Write-Host "  6. Start frontend:  cd src/MyApp.Frontend && npm start" -ForegroundColor White
Write-Host ""

Write-Host "🎯 Features Included:" -ForegroundColor Yellow
Write-Host "  ✅ Clean Architecture with DDD" -ForegroundColor Green
Write-Host "  ✅ CQRS with MediatR" -ForegroundColor Green
Write-Host "  ✅ Result<T> pattern for error handling" -ForegroundColor Green
Write-Host "  ✅ JWT Authentication & RBAC" -ForegroundColor Green
Write-Host "  ✅ Entity Framework Core" -ForegroundColor Green
Write-Host "  ✅ React + TypeScript frontend" -ForegroundColor Green
Write-Host "  ✅ Material-UI components" -ForegroundColor Green
Write-Host "  ✅ Docker containerization" -ForegroundColor Green
Write-Host "  ✅ Comprehensive testing setup" -ForegroundColor Green
Write-Host "  ✅ API documentation with Swagger" -ForegroundColor Green
Write-Host "  ✅ Structured logging" -ForegroundColor Green
Write-Host "  ✅ Health checks & monitoring" -ForegroundColor Green
Write-Host ""

Write-Host "Happy coding! 🎉" -ForegroundColor Cyan

# Pause to let user read the information
Write-Host ""
Write-Host "Press any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
