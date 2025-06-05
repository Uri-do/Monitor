# Monitoring Grid Deployment Script
# This script automates the deployment of the Monitoring Grid system

param(
    [Parameter(Mandatory=$true)]
    [string]$MonitoringConnectionString,

    [Parameter(Mandatory=$false)]
    [string]$MainConnectionString,

    [Parameter(Mandatory=$false)]
    [string]$ServicePath = "C:\Services\MonitoringGrid",
    
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = "MonitoringGrid",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipDatabase,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipService,
    
    [Parameter(Mandatory=$false)]
    [switch]$UpdateOnly
)

Write-Host "Monitoring Grid Deployment Script" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator for service installation"
    exit 1
}

try {
    # Step 1: Database Setup
    if (-not $SkipDatabase) {
        Write-Host "`n1. Setting up database..." -ForegroundColor Yellow
        
        $sqlFiles = @(
            "Database/01_CreateSchema.sql",
            "Database/02_InitialData.sql", 
            "Database/03_StoredProcedures.sql"
        )
        
        foreach ($sqlFile in $sqlFiles) {
            if (Test-Path $sqlFile) {
                Write-Host "   Executing $sqlFile..." -ForegroundColor Cyan
                
                # Parse monitoring connection string to extract components
                $connParts = @{}
                $MonitoringConnectionString.Split(';') | ForEach-Object {
                    if ($_ -match '(.+?)=(.+)') {
                        $connParts[$matches[1].Trim()] = $matches[2].Trim()
                    }
                }

                $server = $connParts['data source'] -or $connParts['server']
                $database = $connParts['initial catalog'] -or $connParts['database']
                $username = $connParts['user id'] -or $connParts['uid']
                $password = $connParts['password'] -or $connParts['pwd']
                
                if ($username -and $password) {
                    sqlcmd -S $server -d $database -U $username -P $password -i $sqlFile
                } else {
                    sqlcmd -S $server -d $database -E -i $sqlFile
                }
                
                if ($LASTEXITCODE -ne 0) {
                    throw "Failed to execute $sqlFile"
                }
                
                Write-Host "   ✓ $sqlFile executed successfully" -ForegroundColor Green
            } else {
                Write-Warning "   SQL file not found: $sqlFile"
            }
        }
    }
    
    # Step 2: Build Application
    Write-Host "`n2. Building application..." -ForegroundColor Yellow
    
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }
    
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
    
    Write-Host "   ✓ Application built successfully" -ForegroundColor Green
    
    # Step 3: Publish Application
    Write-Host "`n3. Publishing application..." -ForegroundColor Yellow
    
    if (Test-Path $ServicePath) {
        Write-Host "   Removing existing files..." -ForegroundColor Cyan
        Remove-Item -Path $ServicePath -Recurse -Force
    }
    
    New-Item -ItemType Directory -Path $ServicePath -Force | Out-Null
    
    dotnet publish -c Release -o $ServicePath --self-contained false
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
    
    Write-Host "   ✓ Application published to $ServicePath" -ForegroundColor Green
    
    # Step 4: Update Configuration
    Write-Host "`n4. Updating configuration..." -ForegroundColor Yellow
    
    $configPath = Join-Path $ServicePath "appsettings.json"
    if (Test-Path $configPath) {
        $config = Get-Content $configPath | ConvertFrom-Json
        $config.ConnectionStrings.MonitoringGrid = $MonitoringConnectionString
        if ($MainConnectionString) {
            $config.ConnectionStrings.MainDatabase = $MainConnectionString
        }
        $config | ConvertTo-Json -Depth 10 | Set-Content $configPath
        Write-Host "   ✓ Configuration updated" -ForegroundColor Green
    } else {
        Write-Warning "   Configuration file not found at $configPath"
    }
    
    # Step 5: Service Installation
    if (-not $SkipService) {
        Write-Host "`n5. Installing Windows Service..." -ForegroundColor Yellow
        
        # Stop existing service if running
        $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if ($existingService) {
            if ($existingService.Status -eq 'Running') {
                Write-Host "   Stopping existing service..." -ForegroundColor Cyan
                Stop-Service -Name $ServiceName -Force
                Start-Sleep -Seconds 5
            }
            
            if (-not $UpdateOnly) {
                Write-Host "   Removing existing service..." -ForegroundColor Cyan
                sc.exe delete $ServiceName
                Start-Sleep -Seconds 2
            }
        }
        
        if (-not $UpdateOnly -or -not $existingService) {
            # Create new service
            $exePath = Join-Path $ServicePath "MonitoringGrid.exe"
            Write-Host "   Creating service with executable: $exePath" -ForegroundColor Cyan
            
            sc.exe create $ServiceName binPath= $exePath start= auto
            if ($LASTEXITCODE -ne 0) { throw "Failed to create service" }
            
            # Set service description
            sc.exe description $ServiceName "Monitoring Grid - KPI monitoring and alerting system"
            
            Write-Host "   ✓ Service created successfully" -ForegroundColor Green
        }
        
        # Start service
        Write-Host "   Starting service..." -ForegroundColor Cyan
        Start-Service -Name $ServiceName
        Start-Sleep -Seconds 3
        
        $service = Get-Service -Name $ServiceName
        if ($service.Status -eq 'Running') {
            Write-Host "   ✓ Service started successfully" -ForegroundColor Green
        } else {
            Write-Warning "   Service failed to start. Check Event Log for details."
        }
    }
    
    # Step 6: Verification
    Write-Host "`n6. Verification..." -ForegroundColor Yellow
    
    # Test database connection
    Write-Host "   Testing database connection..." -ForegroundColor Cyan
    $testScript = Join-Path $ServicePath "Scripts\TestConnection.exe"
    if (Test-Path $testScript) {
        & $testScript
    } else {
        Write-Host "   Test script not found, skipping connection test" -ForegroundColor Yellow
    }
    
    Write-Host "`n🎉 Deployment completed successfully!" -ForegroundColor Green
    Write-Host "`nService Details:" -ForegroundColor Cyan
    Write-Host "   Name: $ServiceName" -ForegroundColor White
    Write-Host "   Path: $ServicePath" -ForegroundColor White
    Write-Host "   Status: $($(Get-Service -Name $ServiceName -ErrorAction SilentlyContinue).Status)" -ForegroundColor White
    
    Write-Host "`nNext Steps:" -ForegroundColor Cyan
    Write-Host "   1. Review logs in: $ServicePath\logs\" -ForegroundColor White
    Write-Host "   2. Monitor service status with: Get-Service $ServiceName" -ForegroundColor White
    Write-Host "   3. Check Event Log for any startup issues" -ForegroundColor White
    Write-Host "   4. Verify KPI processing in the database" -ForegroundColor White

} catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    Write-Host "`nTroubleshooting:" -ForegroundColor Yellow
    Write-Host "   1. Ensure you're running as Administrator" -ForegroundColor White
    Write-Host "   2. Check database connectivity" -ForegroundColor White
    Write-Host "   3. Verify .NET 8 Runtime is installed" -ForegroundColor White
    Write-Host "   4. Check Windows Event Log for detailed errors" -ForegroundColor White
    exit 1
}

# Example usage:
# .\Scripts\Deploy.ps1 -MonitoringConnectionString "data source=server;initial catalog=PopAI;user id=user;password=pass" -MainConnectionString "data source=server;initial catalog=ProgressPlayDBTest;user id=user;password=pass"
# .\Scripts\Deploy.ps1 -MonitoringConnectionString "..." -ServicePath "D:\Services\MonitoringGrid" -UpdateOnly
