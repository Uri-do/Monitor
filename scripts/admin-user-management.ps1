# Admin User Management Script
# Consolidates functionality from CreateAdminUser.cs, FixAdminUser.cs, GenerateAdminHash.cs

param(
    [Parameter(Mandatory=$false)]
    [string]$Action = "create",
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "admin",
    
    [Parameter(Mandatory=$false)]
    [string]$Password = "Admin123!",
    
    [Parameter(Mandatory=$false)]
    [string]$Email = "admin@monitoringgrid.com",
    
    [Parameter(Mandatory=$false)]
    [string]$ConnectionString = "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true"
)

Write-Host "=== MonitoringGrid Admin User Management ===" -ForegroundColor Cyan
Write-Host ""

# Function to generate password hash using .NET
function Generate-PasswordHash {
    param([string]$Password, [string]$Salt = $null)
    
    Add-Type -AssemblyName System.Security
    
    if (-not $Salt) {
        $saltBytes = New-Object byte[] 32
        $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
        $rng.GetBytes($saltBytes)
        $Salt = [Convert]::ToBase64String($saltBytes)
        $rng.Dispose()
    }
    
    $saltBytes = [Convert]::FromBase64String($Salt)
    $pbkdf2 = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($Password, $saltBytes, 600000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
    $hash = $pbkdf2.GetBytes(32)
    $pbkdf2.Dispose()
    
    return "$Salt" + ":" + [Convert]::ToBase64String($hash)
}

# Function to execute SQL command
function Execute-SqlCommand {
    param([string]$Query, [string]$ConnectionString)
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $connection.Open()
        
        $command = New-Object System.Data.SqlClient.SqlCommand($Query, $connection)
        $result = $command.ExecuteNonQuery()
        
        $connection.Close()
        return $result
    }
    catch {
        Write-Host "❌ SQL Error: $($_.Exception.Message)" -ForegroundColor Red
        return -1
    }
}

# Function to create admin user
function Create-AdminUser {
    Write-Host "Creating admin user..." -ForegroundColor Yellow
    
    $userId = [System.Guid]::NewGuid().ToString()
    $passwordHash = Generate-PasswordHash -Password $Password
    
    $sql = @"
-- Remove existing admin user if exists
IF EXISTS (SELECT 1 FROM auth.Users WHERE Username = '$Username')
BEGIN
    DELETE FROM auth.UserRoles WHERE UserId IN (SELECT UserId FROM auth.Users WHERE Username = '$Username')
    DELETE FROM auth.Users WHERE Username = '$Username'
    PRINT '✅ Removed existing admin user'
END

-- Create new admin user
INSERT INTO auth.Users (
    UserId, Username, Email, DisplayName, FirstName, LastName, 
    PasswordHash, IsActive, EmailConfirmed, CreatedDate, ModifiedDate, CreatedBy
)
VALUES (
    '$userId', '$Username', '$Email', 'System Administrator', 
    'System', 'Administrator', '$passwordHash', 1, 1, 
    SYSUTCDATETIME(), SYSUTCDATETIME(), 'SYSTEM'
)

-- Assign admin role (if exists)
IF EXISTS (SELECT 1 FROM auth.Roles WHERE RoleName = 'Administrator')
BEGIN
    INSERT INTO auth.UserRoles (UserId, RoleId)
    SELECT '$userId', RoleId FROM auth.Roles WHERE RoleName = 'Administrator'
    PRINT '✅ Assigned Administrator role'
END

PRINT '✅ Admin user created successfully'
"@

    $result = Execute-SqlCommand -Query $sql -ConnectionString $ConnectionString
    
    if ($result -ge 0) {
        Write-Host "✅ Admin user created successfully!" -ForegroundColor Green
        Write-Host "   Username: $Username" -ForegroundColor White
        Write-Host "   Password: $Password" -ForegroundColor White
        Write-Host "   Email: $Email" -ForegroundColor White
    }
    else {
        Write-Host "❌ Failed to create admin user" -ForegroundColor Red
    }
}

# Function to update admin password
function Update-AdminPassword {
    Write-Host "Updating admin password..." -ForegroundColor Yellow
    
    $passwordHash = Generate-PasswordHash -Password $Password
    
    $sql = @"
UPDATE auth.Users 
SET 
    PasswordHash = '$passwordHash',
    ModifiedDate = SYSUTCDATETIME(),
    ModifiedBy = 'ADMIN_PASSWORD_UPDATE'
WHERE Username = '$Username'

IF @@ROWCOUNT > 0
    PRINT '✅ Admin password updated successfully'
ELSE
    PRINT '❌ Admin user not found'
"@

    $result = Execute-SqlCommand -Query $sql -ConnectionString $ConnectionString
    
    if ($result -ge 0) {
        Write-Host "✅ Admin password updated successfully!" -ForegroundColor Green
        Write-Host "   Username: $Username" -ForegroundColor White
        Write-Host "   New Password: $Password" -ForegroundColor White
    }
    else {
        Write-Host "❌ Failed to update admin password" -ForegroundColor Red
    }
}

# Function to verify admin user
function Verify-AdminUser {
    Write-Host "Verifying admin user..." -ForegroundColor Yellow
    
    $sql = @"
SELECT 
    UserId,
    Username,
    Email,
    DisplayName,
    IsActive,
    EmailConfirmed,
    LEFT(PasswordHash, 50) + '...' AS PasswordHashPreview,
    CreatedDate,
    ModifiedDate
FROM auth.Users 
WHERE Username = '$Username'
"@

    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $connection.Open()
        
        $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
        $reader = $command.ExecuteReader()
        
        if ($reader.Read()) {
            Write-Host "✅ Admin user found:" -ForegroundColor Green
            Write-Host "   UserId: $($reader['UserId'])" -ForegroundColor White
            Write-Host "   Username: $($reader['Username'])" -ForegroundColor White
            Write-Host "   Email: $($reader['Email'])" -ForegroundColor White
            Write-Host "   DisplayName: $($reader['DisplayName'])" -ForegroundColor White
            Write-Host "   IsActive: $($reader['IsActive'])" -ForegroundColor White
            Write-Host "   EmailConfirmed: $($reader['EmailConfirmed'])" -ForegroundColor White
            Write-Host "   PasswordHash: $($reader['PasswordHashPreview'])" -ForegroundColor White
            Write-Host "   CreatedDate: $($reader['CreatedDate'])" -ForegroundColor White
            Write-Host "   ModifiedDate: $($reader['ModifiedDate'])" -ForegroundColor White
        }
        else {
            Write-Host "❌ Admin user not found" -ForegroundColor Red
        }
        
        $connection.Close()
    }
    catch {
        Write-Host "❌ Error verifying admin user: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Function to generate hash only
function Generate-HashOnly {
    Write-Host "Generating password hash..." -ForegroundColor Yellow
    
    $hash = Generate-PasswordHash -Password $Password
    
    Write-Host "✅ Password hash generated:" -ForegroundColor Green
    Write-Host "   Password: $Password" -ForegroundColor White
    Write-Host "   Hash: $hash" -ForegroundColor White
    Write-Host ""
    Write-Host "SQL Update Statement:" -ForegroundColor Cyan
    Write-Host "UPDATE auth.Users SET PasswordHash = '$hash', ModifiedDate = SYSUTCDATETIME() WHERE Username = '$Username'" -ForegroundColor White
}

# Main execution
switch ($Action.ToLower()) {
    "create" { Create-AdminUser }
    "update" { Update-AdminPassword }
    "verify" { Verify-AdminUser }
    "hash" { Generate-HashOnly }
    default {
        Write-Host "❌ Invalid action. Use: create, update, verify, or hash" -ForegroundColor Red
        Write-Host ""
        Write-Host "Examples:" -ForegroundColor Cyan
        Write-Host "  .\admin-user-management.ps1 -Action create" -ForegroundColor White
        Write-Host "  .\admin-user-management.ps1 -Action update -Password 'NewPassword123!'" -ForegroundColor White
        Write-Host "  .\admin-user-management.ps1 -Action verify -Username admin" -ForegroundColor White
        Write-Host "  .\admin-user-management.ps1 -Action hash -Password 'MyPassword'" -ForegroundColor White
        exit 1
    }
}

Write-Host ""
Write-Host "=== Admin User Management Complete ===" -ForegroundColor Cyan
