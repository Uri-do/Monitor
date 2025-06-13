# Enterprise Application Template Setup Script
# This script helps you create a new enterprise application from the template

param(
    [Parameter(Mandatory=$true)]
    [string]$Name,
    
    [Parameter(Mandatory=$false)]
    [string]$Domain = "Item",
    
    [Parameter(Mandatory=$false)]
    [string]$Database = "",
    
    [Parameter(Mandatory=$false)]
    [int]$Port = 5000,
    
    [Parameter(Mandatory=$false)]
    [int]$FrontendPort = 3000,
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".",
    
    [Parameter(Mandatory=$false)]
    [bool]$EnableAuth = $true,
    
    [Parameter(Mandatory=$false)]
    [bool]$EnableWorker = $true,
    
    [Parameter(Mandatory=$false)]
    [bool]$EnableDocker = $true,
    
    [Parameter(Mandatory=$false)]
    [bool]$EnableTesting = $true,
    
    [Parameter(Mandatory=$false)]
    [bool]$SkipRestore = $false
)

Write-Host "🚀 Enterprise Application Template Generator" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Validate parameters
if ([string]::IsNullOrWhiteSpace($Name)) {
    Write-Error "Application name is required"
    exit 1
}

if ([string]::IsNullOrWhiteSpace($Database)) {
    $Database = "${Name}DB"
}

$DomainPlural = if ($Domain.EndsWith("s")) { $Domain } else { "${Domain}s" }
$DomainLower = $Domain.ToLower()
$DomainPluralLower = $DomainPlural.ToLower()

Write-Host "📋 Configuration:" -ForegroundColor Yellow
Write-Host "  Application Name: $Name" -ForegroundColor White
Write-Host "  Domain Entity: $Domain" -ForegroundColor White
Write-Host "  Database Name: $Database" -ForegroundColor White
Write-Host "  API Port: $Port" -ForegroundColor White
Write-Host "  Frontend Port: $FrontendPort" -ForegroundColor White
Write-Host "  Output Path: $OutputPath" -ForegroundColor White
Write-Host "  Enable Auth: $EnableAuth" -ForegroundColor White
Write-Host "  Enable Worker: $EnableWorker" -ForegroundColor White
Write-Host "  Enable Docker: $EnableDocker" -ForegroundColor White
Write-Host "  Enable Testing: $EnableTesting" -ForegroundColor White

# Check if dotnet CLI is available
if (!(Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Error ".NET CLI is not installed or not in PATH"
    exit 1
}

# Check if template is installed
$templateList = dotnet new list | Select-String "enterprise-app"
if (!$templateList) {
    Write-Host "📦 Installing Enterprise Application Template..." -ForegroundColor Yellow
    
    # Install template from current directory
    $templatePath = Split-Path -Parent $MyInvocation.MyCommand.Path
    dotnet new install $templatePath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install template"
        exit 1
    }
    
    Write-Host "✅ Template installed successfully" -ForegroundColor Green
}

# Create output directory if it doesn't exist
$fullOutputPath = Join-Path $OutputPath $Name
if (!(Test-Path $fullOutputPath)) {
    New-Item -ItemType Directory -Path $fullOutputPath -Force | Out-Null
}

Write-Host "🏗️  Creating application from template..." -ForegroundColor Yellow

# Build dotnet new command
$dotnetArgs = @(
    "new", "enterprise-app",
    "--name", $Name,
    "--domain", $Domain,
    "--database", $Database,
    "--port", $Port,
    "--frontend-port", $FrontendPort,
    "--output", $fullOutputPath,
    "--force"
)

if (!$EnableAuth) { $dotnetArgs += "--enable-auth", "false" }
if (!$EnableWorker) { $dotnetArgs += "--enable-worker", "false" }
if (!$EnableDocker) { $dotnetArgs += "--enable-docker", "false" }
if (!$EnableTesting) { $dotnetArgs += "--enable-testing", "false" }
if ($SkipRestore) { $dotnetArgs += "--skip-restore", "true" }

# Execute dotnet new command
& dotnet @dotnetArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create application from template"
    exit 1
}

Write-Host "✅ Application created successfully" -ForegroundColor Green

# Navigate to the created application
Push-Location $fullOutputPath

try {
    if (!$SkipRestore) {
        Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
        dotnet restore
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ NuGet packages restored" -ForegroundColor Green
        } else {
            Write-Warning "Failed to restore NuGet packages"
        }
        
        # Install npm packages for frontend
        $frontendPath = "src/$Name.Frontend"
        if (Test-Path $frontendPath) {
            Write-Host "📦 Installing npm packages..." -ForegroundColor Yellow
            Push-Location $frontendPath
            try {
                npm install
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✅ npm packages installed" -ForegroundColor Green
                } else {
                    Write-Warning "Failed to install npm packages"
                }
            } finally {
                Pop-Location
            }
        }
    }
    
    # Build the solution to verify everything is working
    Write-Host "🔨 Building solution..." -ForegroundColor Yellow
    dotnet build --no-restore
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Solution built successfully" -ForegroundColor Green
    } else {
        Write-Warning "Build failed - please check the output above"
    }
    
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "🎉 Application '$Name' created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📁 Location: $fullOutputPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "🚀 Next steps:" -ForegroundColor Yellow
Write-Host "  1. cd $Name" -ForegroundColor White
Write-Host "  2. Update connection strings in appsettings.json" -ForegroundColor White
Write-Host "  3. Run database migrations: dotnet ef database update" -ForegroundColor White
Write-Host "  4. Start the API: dotnet run --project src/$Name.Api" -ForegroundColor White
Write-Host "  5. Start the frontend: cd src/$Name.Frontend && npm start" -ForegroundColor White
Write-Host ""
Write-Host "📚 Documentation:" -ForegroundColor Yellow
Write-Host "  - Architecture Guide: docs/ARCHITECTURE.md" -ForegroundColor White
Write-Host "  - Development Setup: docs/DEVELOPMENT.md" -ForegroundColor White
Write-Host "  - API Documentation: https://localhost:$Port/swagger" -ForegroundColor White
Write-Host ""
Write-Host "Happy coding! 🎯" -ForegroundColor Cyan
