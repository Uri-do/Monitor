#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Assigns the Admin role to a user in the MonitoringGrid system
.DESCRIPTION
    This script connects to the PopAI database and assigns the Admin role to a specified user.
    If the Admin role doesn't exist, it creates it first.
.PARAMETER UserEmail
    The email address of the user to assign the Admin role to
.PARAMETER ConnectionString
    The database connection string (optional, uses default if not provided)
.EXAMPLE
    .\assign-admin-role.ps1 -UserEmail "admin@monitoringgrid.com"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$UserEmail,
    
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = "Server=192.168.166.11,1433;Database=PopAI;User Id=conexusadmin;Password=Conexus2024!;TrustServerCertificate=true;Connection Timeout=30;"
)

Write-Host "=== MonitoringGrid Admin Role Assignment ===" -ForegroundColor Green
Write-Host "User Email: $UserEmail" -ForegroundColor Yellow
Write-Host "Connection: 192.168.166.11,1433 -> PopAI" -ForegroundColor Yellow
Write-Host ""

try {
    # Import SQL Server module
    if (-not (Get-Module -ListAvailable -Name SqlServer)) {
        Write-Host "Installing SqlServer module..." -ForegroundColor Yellow
        Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser
    }
    Import-Module SqlServer -Force

    Write-Host "Connecting to database..." -ForegroundColor Yellow

    # Check if user exists
    $userQuery = @"
SELECT UserId, Username, Email, IsActive 
FROM auth.Users 
WHERE Email = '$UserEmail'
"@

    $user = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $userQuery
    
    if (-not $user) {
        Write-Host "ERROR: User with email '$UserEmail' not found!" -ForegroundColor Red
        Write-Host "Available users:" -ForegroundColor Yellow
        $allUsers = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query "SELECT Username, Email FROM auth.Users"
        $allUsers | Format-Table -AutoSize
        exit 1
    }

    $userId = $user.UserId
    $username = $user.Username
    Write-Host "Found user: $username ($UserEmail) - ID: $userId" -ForegroundColor Green

    # Check if Admin role exists
    $roleQuery = @"
SELECT RoleId, Name, Description 
FROM auth.Roles 
WHERE Name = 'Admin'
"@

    $adminRole = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $roleQuery
    
    if (-not $adminRole) {
        Write-Host "Admin role not found. Creating it..." -ForegroundColor Yellow
        
        $createRoleQuery = @"
INSERT INTO auth.Roles (RoleId, Name, Description, IsSystemRole, IsActive, CreatedDate, ModifiedDate, CreatedBy)
VALUES (NEWID(), 'Admin', 'System Administrator with full access', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 'System')
"@
        
        Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $createRoleQuery
        $adminRole = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $roleQuery
        Write-Host "Created Admin role: $($adminRole.RoleId)" -ForegroundColor Green
    }

    $roleId = $adminRole.RoleId
    Write-Host "Admin role ID: $roleId" -ForegroundColor Green

    # Check if user already has Admin role
    $userRoleQuery = @"
SELECT ur.UserId, ur.RoleId, r.Name as RoleName
FROM auth.UserRoles ur
INNER JOIN auth.Roles r ON ur.RoleId = r.RoleId
WHERE ur.UserId = '$userId' AND r.Name = 'Admin'
"@

    $existingUserRole = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $userRoleQuery
    
    if ($existingUserRole) {
        Write-Host "User already has Admin role assigned!" -ForegroundColor Yellow
    } else {
        Write-Host "Assigning Admin role to user..." -ForegroundColor Yellow
        
        $assignRoleQuery = @"
INSERT INTO auth.UserRoles (UserId, RoleId, AssignedDate, AssignedBy)
VALUES ('$userId', '$roleId', SYSUTCDATETIME(), 'System')
"@
        
        Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $assignRoleQuery
        Write-Host "Successfully assigned Admin role to user!" -ForegroundColor Green
    }

    # Verify the assignment
    Write-Host ""
    Write-Host "=== Verification ===" -ForegroundColor Green
    $verifyQuery = @"
SELECT 
    u.Username,
    u.Email,
    r.Name as RoleName,
    ur.AssignedDate
FROM auth.Users u
INNER JOIN auth.UserRoles ur ON u.UserId = ur.UserId
INNER JOIN auth.Roles r ON ur.RoleId = r.RoleId
WHERE u.Email = '$UserEmail'
"@

    $userRoles = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $verifyQuery
    
    if ($userRoles) {
        Write-Host "User roles:" -ForegroundColor Yellow
        $userRoles | Format-Table -AutoSize
    } else {
        Write-Host "No roles found for user!" -ForegroundColor Red
    }

    Write-Host ""
    Write-Host "=== SUCCESS ===" -ForegroundColor Green
    Write-Host "Admin role has been assigned to $UserEmail" -ForegroundColor Green
    Write-Host "The user should now have access to admin pages." -ForegroundColor Green
    Write-Host ""
    Write-Host "Note: The user may need to log out and log back in for the changes to take effect." -ForegroundColor Yellow

} catch {
    Write-Host ""
    Write-Host "=== ERROR ===" -ForegroundColor Red
    Write-Host "Failed to assign admin role: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack trace:" -ForegroundColor Yellow
    Write-Host $_.Exception.StackTrace -ForegroundColor Yellow
    exit 1
}
