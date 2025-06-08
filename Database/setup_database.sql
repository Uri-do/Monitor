-- Complete Database Setup Script for MonitoringGrid
-- Run this script to set up the entire database schema and initial data

-- Step 1: Create Database (if needed)
USE master
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'PopAI')
BEGIN
    CREATE DATABASE [PopAI]
    PRINT 'PopAI database created successfully'
END
ELSE
BEGIN
    PRINT 'PopAI database already exists'
END
GO

USE [PopAI]
GO

-- Step 2: Create auth schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'auth')
BEGIN
    EXEC('CREATE SCHEMA [auth]')
    PRINT 'Schema [auth] created'
END
ELSE
BEGIN
    PRINT 'Schema [auth] already exists'
END
GO

-- Step 3: Create monitoring schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'monitoring')
BEGIN
    EXEC('CREATE SCHEMA monitoring')
    PRINT 'Schema [monitoring] created'
END
ELSE
BEGIN
    PRINT 'Schema [monitoring] already exists'
END
GO

-- Step 4: Create auth tables
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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

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
GO

-- Step 5: Insert initial roles and permissions
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
GO

-- Insert default permissions
MERGE auth.Permissions AS target
USING (VALUES 
    ('perm-system-admin', 'System:Admin', 'Full system administration access', 'System', 'Admin', 1),
    ('perm-user-read', 'User:Read', 'Read user information', 'User', 'Read', 1),
    ('perm-user-write', 'User:Write', 'Create and update users', 'User', 'Write', 1),
    ('perm-user-delete', 'User:Delete', 'Delete users', 'User', 'Delete', 1),
    ('perm-role-read', 'Role:Read', 'Read role information', 'Role', 'Read', 1),
    ('perm-role-write', 'Role:Write', 'Create and update roles', 'Role', 'Write', 1)
) AS source (PermissionId, Name, Description, Resource, Action, IsSystemPermission)
ON target.PermissionId = source.PermissionId
WHEN NOT MATCHED THEN
    INSERT (PermissionId, Name, Description, Resource, Action, IsSystemPermission)
    VALUES (source.PermissionId, source.Name, source.Description, source.Resource, source.Action, source.IsSystemPermission);
GO

-- Assign all permissions to admin role
INSERT INTO auth.RolePermissions (RoleId, PermissionId, AssignedBy)
SELECT 'role-admin', PermissionId, 'SYSTEM'
FROM auth.Permissions
WHERE NOT EXISTS (
    SELECT 1 FROM auth.RolePermissions 
    WHERE RoleId = 'role-admin' AND PermissionId = auth.Permissions.PermissionId
);
GO

-- Step 6: Create default admin user
IF NOT EXISTS (SELECT 1 FROM auth.Users WHERE Username = 'admin')
BEGIN
    DECLARE @AdminUserId NVARCHAR(50) = NEWID()
    DECLARE @PasswordHash NVARCHAR(255) = '$2a$11$8K1p/a0dqbQiAXckiXiLOeNd4XxDJf0uK0/3MpLbpamWBjDfHBXjm' -- Admin123!
    
    INSERT INTO auth.Users (UserId, Username, Email, DisplayName, FirstName, LastName, PasswordHash, IsActive, EmailConfirmed, CreatedBy)
    VALUES (@AdminUserId, 'admin', 'admin@monitoringgrid.com', 'System Administrator', 'System', 'Administrator', @PasswordHash, 1, 1, 'SYSTEM')
    
    -- Assign admin role to admin user
    INSERT INTO auth.UserRoles (UserId, RoleId, AssignedBy)
    VALUES (@AdminUserId, 'role-admin', 'SYSTEM')
    
    PRINT 'Default admin user created with username: admin, password: Admin123!'
    PRINT 'WARNING: Change the default admin password immediately in production!'
END
ELSE
BEGIN
    PRINT 'Default admin user already exists'
END
GO

PRINT 'Database setup completed successfully!'
PRINT 'You can now:'
PRINT '1. Register new users through the API'
PRINT '2. Login with admin/Admin123!'
PRINT '3. Test the authentication system'
