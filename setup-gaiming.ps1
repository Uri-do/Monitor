# Setup GAIming Solution from Enterprise Template
# This script creates a new GAIming solution based on the enterprise template

param(
    [string]$SourcePath = "..\Monitor\Templates",
    [string]$TargetPath = ".",
    [string]$ProjectName = "GAIming"
)

Write-Host "Setting up $ProjectName solution from enterprise template..." -ForegroundColor Green

# Clean up existing files (except this script)
Write-Host "Cleaning up existing files..." -ForegroundColor Yellow
Get-ChildItem -Path $TargetPath -Exclude "setup-gaiming.ps1" | Remove-Item -Recurse -Force

# Copy template files
Write-Host "Copying template files..." -ForegroundColor Yellow
Copy-Item -Path "$SourcePath\*" -Destination $TargetPath -Recurse -Force

# Rename solution file
Write-Host "Renaming solution file..." -ForegroundColor Yellow
if (Test-Path "$TargetPath\EnterpriseApp.sln") {
    Rename-Item -Path "$TargetPath\EnterpriseApp.sln" -NewName "$ProjectName.sln"
}

# Update solution file content
Write-Host "Updating solution file content..." -ForegroundColor Yellow
$solutionFile = "$TargetPath\$ProjectName.sln"
if (Test-Path $solutionFile) {
    $content = Get-Content $solutionFile -Raw
    $content = $content -replace "EnterpriseApp", $ProjectName
    Set-Content -Path $solutionFile -Value $content
}

# Rename project directories
Write-Host "Renaming project directories..." -ForegroundColor Yellow
$srcPath = "$TargetPath\src"
if (Test-Path $srcPath) {
    Get-ChildItem -Path $srcPath -Directory | ForEach-Object {
        $oldName = $_.Name
        $newName = $oldName -replace "EnterpriseApp", $ProjectName
        if ($oldName -ne $newName) {
            Rename-Item -Path $_.FullName -NewName $newName
            Write-Host "  Renamed $oldName to $newName" -ForegroundColor Cyan
        }
    }
}

# Rename project files
Write-Host "Renaming project files..." -ForegroundColor Yellow
Get-ChildItem -Path "$TargetPath\src" -Recurse -Filter "*.csproj" | ForEach-Object {
    $oldName = $_.Name
    $newName = $oldName -replace "EnterpriseApp", $ProjectName
    if ($oldName -ne $newName) {
        Rename-Item -Path $_.FullName -NewName $newName
        Write-Host "  Renamed $oldName to $newName" -ForegroundColor Cyan
    }
}

# Update project file contents
Write-Host "Updating project file contents..." -ForegroundColor Yellow
Get-ChildItem -Path "$TargetPath\src" -Recurse -Filter "*.csproj" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $newContent = $content -replace "EnterpriseApp", $ProjectName
    if ($content -ne $newContent) {
        Set-Content -Path $_.FullName -Value $newContent
        Write-Host "  Updated $($_.Name)" -ForegroundColor Cyan
    }
}

# Update namespace in C# files
Write-Host "Updating namespaces in C# files..." -ForegroundColor Yellow
Get-ChildItem -Path "$TargetPath\src" -Recurse -Filter "*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -match "namespace EnterpriseApp" -or $content -match "using EnterpriseApp") {
        $newContent = $content -replace "EnterpriseApp", $ProjectName
        Set-Content -Path $_.FullName -Value $newContent
        Write-Host "  Updated namespaces in $($_.Name)" -ForegroundColor Cyan
    }
}

# Update package.json for frontend
Write-Host "Updating frontend package.json..." -ForegroundColor Yellow
$packageJsonPath = "$TargetPath\src\$ProjectName.Frontend\package.json"
if (Test-Path $packageJsonPath) {
    $content = Get-Content $packageJsonPath -Raw | ConvertFrom-Json
    $content.name = $ProjectName.ToLower() + "-frontend"
    $content.description = "$ProjectName Frontend Application"
    $content | ConvertTo-Json -Depth 10 | Set-Content -Path $packageJsonPath
    Write-Host "  Updated package.json" -ForegroundColor Cyan
}

# Update README files
Write-Host "Updating README files..." -ForegroundColor Yellow
Get-ChildItem -Path $TargetPath -Recurse -Filter "README.md" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $newContent = $content -replace "EnterpriseApp", $ProjectName
    if ($content -ne $newContent) {
        Set-Content -Path $_.FullName -Value $newContent
        Write-Host "  Updated $($_.FullName)" -ForegroundColor Cyan
    }
}

# Update appsettings files
Write-Host "Updating appsettings files..." -ForegroundColor Yellow
Get-ChildItem -Path "$TargetPath\src" -Recurse -Filter "appsettings*.json" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $newContent = $content -replace "EnterpriseApp", $ProjectName
    if ($content -ne $newContent) {
        Set-Content -Path $_.FullName -Value $newContent
        Write-Host "  Updated $($_.Name)" -ForegroundColor Cyan
    }
}

# Create .gitignore if it doesn't exist
Write-Host "Creating .gitignore..." -ForegroundColor Yellow
$gitignorePath = "$TargetPath\.gitignore"
if (-not (Test-Path $gitignorePath)) {
    @"
# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/

# Visual Studio
.vs/
*.user
*.userosscache
*.sln.docstates

# User-specific files (MonoDevelop/Xamarin Studio)
*.userprefs

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Aa][Rr][Mm]/
[Aa][Rr][Mm]64/
bld/
[Bb]in/
[Oo]bj/
[Ll]og/

# .NET Core
project.lock.json
project.fragment.lock.json
artifacts/

# StyleCop
StyleCopReport.xml

# Files built by Visual Studio
*_i.c
*_p.c
*_h.h
*.ilk
*.meta
*.obj
*.iobj
*.pch
*.pdb
*.ipdb
*.pgc
*.pgd
*.rsp
*.sbr
*.tlb
*.tli
*.tlh
*.tmp
*.tmp_proj
*_wpftmp.csproj
*.log
*.vspscc
*.vssscc
.builds
*.pidb
*.svclog
*.scc

# Node.js
node_modules/
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# Frontend build
dist/
build/

# Environment variables
.env
.env.local
.env.development.local
.env.test.local
.env.production.local

# IDE
.vscode/
.idea/
*.swp
*.swo
*~

# OS
.DS_Store
Thumbs.db
"@ | Set-Content -Path $gitignorePath
}

Write-Host ""
Write-Host "✅ $ProjectName solution setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update connection strings in appsettings.json files" -ForegroundColor White
Write-Host "2. Run 'dotnet restore' to restore NuGet packages" -ForegroundColor White
Write-Host "3. Run 'dotnet build' to build the solution" -ForegroundColor White
Write-Host "4. Update database configurations as needed" -ForegroundColor White
Write-Host "5. Install frontend dependencies with 'npm install' in the Frontend project" -ForegroundColor White
Write-Host ""
Write-Host "Solution structure:" -ForegroundColor Yellow
Write-Host "📁 $ProjectName/" -ForegroundColor White
Write-Host "  📁 src/" -ForegroundColor White
Write-Host "    📁 $ProjectName.Core/" -ForegroundColor White
Write-Host "    📁 $ProjectName.Infrastructure/" -ForegroundColor White
Write-Host "    📁 $ProjectName.Api/" -ForegroundColor White
Write-Host "    📁 $ProjectName.Worker/" -ForegroundColor White
Write-Host "    📁 $ProjectName.Frontend/" -ForegroundColor White
Write-Host "  📁 docs/" -ForegroundColor White
Write-Host "  📄 $ProjectName.sln" -ForegroundColor White
Write-Host ""
