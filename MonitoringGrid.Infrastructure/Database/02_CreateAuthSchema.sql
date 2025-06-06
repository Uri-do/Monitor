-- Authentication and Authorization Schema
-- This script creates the authentication tables for the MonitoringGrid system

USE [PopAI]
GO

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
GO

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
ELSE
BEGIN
    PRINT 'Table auth.Users already exists'
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
ELSE
BEGIN
    PRINT 'Table auth.Roles already exists'
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
ELSE
BEGIN
    PRINT 'Table auth.Permissions already exists'
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
ELSE
BEGIN
    PRINT 'Table auth.UserRoles already exists'
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
ELSE
BEGIN
    PRINT 'Table auth.RolePermissions already exists'
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
ELSE
BEGIN
    PRINT 'Table auth.RefreshTokens already exists'
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
ELSE
BEGIN
    PRINT 'Table auth.UserPasswords already exists'
END
GO
