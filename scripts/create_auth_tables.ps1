# PowerShell script to create authentication tables
# Uses the same connection string as the API

$connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true"

# SQL to create auth schema and tables
$sql = @"
-- Create auth schema if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'auth')
BEGIN
    EXEC('CREATE SCHEMA [auth]')
    PRINT 'Schema [auth] created'
END
ELSE
BEGIN
    PRINT 'Schema [auth] already exists'
END

-- Users Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.Users') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.Users (
        UserId NVARCHAR(50) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        DisplayName NVARCHAR(255) NOT NULL,
        FirstName NVARCHAR(100) NULL,
        LastName NVARCHAR(100) NULL,
        Department NVARCHAR(100) NULL,
        Title NVARCHAR(100) NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        PasswordSalt NVARCHAR(255) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        EmailConfirmed BIT NOT NULL DEFAULT 0,
        TwoFactorEnabled BIT NOT NULL DEFAULT 0,
        FailedLoginAttempts INT NOT NULL DEFAULT 0,
        LockoutEnd DATETIME2 NULL,
        LastLogin DATETIME2 NULL,
        LastPasswordChange DATETIME2 NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NULL,
        ModifiedBy NVARCHAR(100) NULL
    )
    PRINT 'Table auth.Users created'
END

-- Roles Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.Roles') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.Roles (
        RoleId NVARCHAR(50) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL UNIQUE,
        Description NVARCHAR(500) NOT NULL DEFAULT '',
        IsSystemRole BIT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NULL,
        ModifiedBy NVARCHAR(100) NULL
    )
    PRINT 'Table auth.Roles created'
END

-- Permissions Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.Permissions') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.Permissions (
        PermissionId NVARCHAR(50) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL UNIQUE,
        Description NVARCHAR(500) NOT NULL DEFAULT '',
        Resource NVARCHAR(100) NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        IsSystemPermission BIT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    )
    PRINT 'Table auth.Permissions created'
END

-- UserRoles Junction Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.UserRoles') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.UserRoles (
        UserId NVARCHAR(50) NOT NULL,
        RoleId NVARCHAR(50) NOT NULL,
        AssignedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        AssignedBy NVARCHAR(100) NULL,
        PRIMARY KEY (UserId, RoleId),
        FOREIGN KEY (UserId) REFERENCES auth.Users(UserId) ON DELETE CASCADE,
        FOREIGN KEY (RoleId) REFERENCES auth.Roles(RoleId) ON DELETE CASCADE
    )
    PRINT 'Table auth.UserRoles created'
END

-- RolePermissions Junction Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.RolePermissions') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.RolePermissions (
        RoleId NVARCHAR(50) NOT NULL,
        PermissionId NVARCHAR(50) NOT NULL,
        AssignedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        AssignedBy NVARCHAR(100) NULL,
        PRIMARY KEY (RoleId, PermissionId),
        FOREIGN KEY (RoleId) REFERENCES auth.Roles(RoleId) ON DELETE CASCADE,
        FOREIGN KEY (PermissionId) REFERENCES auth.Permissions(PermissionId) ON DELETE CASCADE
    )
    PRINT 'Table auth.RolePermissions created'
END

-- RefreshTokens Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.RefreshTokens') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.RefreshTokens (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId NVARCHAR(50) NOT NULL,
        Token NVARCHAR(255) NOT NULL UNIQUE,
        ExpiresAt DATETIME2 NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        IsActive BIT NOT NULL DEFAULT 1,
        RevokedAt DATETIME2 NULL,
        RevokedBy NVARCHAR(100) NULL,
        RevokedReason NVARCHAR(500) NULL,
        IpAddress NVARCHAR(45) NULL,
        UserAgent NVARCHAR(500) NULL,
        FOREIGN KEY (UserId) REFERENCES auth.Users(UserId) ON DELETE CASCADE
    )
    PRINT 'Table auth.RefreshTokens created'
END

-- UserPasswords Table (for password history)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.UserPasswords') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.UserPasswords (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        PasswordSalt NVARCHAR(255) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy NVARCHAR(100) NULL,
        FOREIGN KEY (UserId) REFERENCES auth.Users(UserId) ON DELETE CASCADE
    )
    PRINT 'Table auth.UserPasswords created'
END

-- Insert default roles
MERGE auth.Roles AS target
USING (VALUES 
    ('role-admin', 'Admin', 'System Administrator with full access', 1, 1),
    ('role-manager', 'Manager', 'Manager with read/write access to most features', 1, 1),
    ('role-viewer', 'Viewer', 'Read-only access to dashboards and reports', 1, 1)
) AS source (RoleId, Name, Description, IsSystemRole, IsActive)
ON target.RoleId = source.RoleId
WHEN NOT MATCHED THEN
    INSERT (RoleId, Name, Description, IsSystemRole, IsActive, CreatedBy)
    VALUES (source.RoleId, source.Name, source.Description, source.IsSystemRole, source.IsActive, 'SYSTEM');

PRINT 'Authentication schema setup completed successfully!'
"@

try {
    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "Connected to database successfully"
    
    # Create command
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    $command.CommandTimeout = 120
    
    # Execute
    $result = $command.ExecuteNonQuery()
    Write-Host "Schema creation completed. Rows affected: $result"
    
    $connection.Close()
    Write-Host "Database setup completed successfully!"
}
catch {
    Write-Error "Error: $($_.Exception.Message)"
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
