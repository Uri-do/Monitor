# Create Admin User Script
# Generates the correct password hash and creates admin user in MonitoringGrid database

param(
    [Parameter(Mandatory=$false)]
    [string]$Password = "Admin123!",
    
    [Parameter(Mandatory=$false)]
    [string]$Username = "admin",
    
    [Parameter(Mandatory=$false)]
    [string]$Email = "admin@monitoringgrid.com"
)

Write-Host "=== Creating Admin User for MonitoringGrid ===" -ForegroundColor Cyan
Write-Host ""

# Function to generate password hash using PBKDF2 (same as SecurityService)
function Generate-PasswordHash {
    param([string]$Password)
    
    Add-Type -AssemblyName System.Security
    
    # Generate salt
    $saltBytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($saltBytes)
    $salt = [Convert]::ToBase64String($saltBytes)
    $rng.Dispose()
    
    # Generate hash using PBKDF2 with same parameters as SecurityService
    $saltBytes = [Convert]::FromBase64String($salt)
    $pbkdf2 = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($Password, $saltBytes, 600000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
    $hash = $pbkdf2.GetBytes(32)
    $pbkdf2.Dispose()
    
    return "$salt" + ":" + [Convert]::ToBase64String($hash)
}

# Generate password hash
$passwordHash = Generate-PasswordHash -Password $Password
$userId = [System.Guid]::NewGuid().ToString()

Write-Host "Generated credentials:" -ForegroundColor Green
Write-Host "  Username: $Username" -ForegroundColor White
Write-Host "  Password: $Password" -ForegroundColor White
Write-Host "  Email: $Email" -ForegroundColor White
Write-Host "  UserId: $userId" -ForegroundColor White
Write-Host "  PasswordHash: $passwordHash" -ForegroundColor White
Write-Host ""

# Connection string for MonitoringGrid database
$connectionString = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=MonitoringGrid;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"

# SQL to create admin user
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
    PasswordHash, IsActive, EmailConfirmed, TwoFactorEnabled, FailedLoginAttempts,
    CreatedDate, ModifiedDate, CreatedBy
)
VALUES (
    '$userId', '$Username', '$Email', 'System Administrator',
    'System', 'Administrator', '$passwordHash', 1, 1, 0, 0,
    SYSUTCDATETIME(), SYSUTCDATETIME(), 'SYSTEM'
)

-- Assign admin role if it exists
IF EXISTS (SELECT 1 FROM auth.Roles WHERE RoleId = 'role-admin')
BEGIN
    INSERT INTO auth.UserRoles (UserId, RoleId, AssignedBy)
    VALUES ('$userId', 'role-admin', 'SYSTEM')
    PRINT '✅ Assigned admin role'
END

PRINT '✅ Admin user created successfully'
"@

Write-Host "Executing SQL..." -ForegroundColor Yellow

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    $result = $command.ExecuteNonQuery()
    
    $connection.Close()
    
    Write-Host "✅ Admin user created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now login with:" -ForegroundColor Cyan
    Write-Host "  Username: $Username" -ForegroundColor White
    Write-Host "  Password: $Password" -ForegroundColor White
}
catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "SQL Script (run manually if needed):" -ForegroundColor Yellow
    Write-Host $sql -ForegroundColor White
}

Write-Host ""
Write-Host "=== Admin User Creation Complete ===" -ForegroundColor Cyan
