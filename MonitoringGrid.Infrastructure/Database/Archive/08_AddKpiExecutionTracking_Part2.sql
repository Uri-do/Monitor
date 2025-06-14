-- Add KPI Execution Tracking Migration - Part 2
-- This script creates new tables and adds constraints
USE [PopAI]
GO

PRINT '=== Starting KPI Execution Tracking Migration - Part 2 ==='
PRINT 'Creating new tables and adding constraints...'
PRINT ''

-- Create KpiTypes table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.KpiTypes') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.KpiTypes (
        KpiTypeId NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NOT NULL,
        RequiredFields NVARCHAR(MAX) NOT NULL,
        DefaultStoredProcedure NVARCHAR(255) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_KpiTypes PRIMARY KEY (KpiTypeId)
    );
    
    -- Create indexes
    CREATE INDEX IX_KpiTypes_IsActive ON monitoring.KpiTypes (IsActive);
    CREATE INDEX IX_KpiTypes_Name ON monitoring.KpiTypes (Name);
    
    PRINT '✅ Created KpiTypes table with indexes'
END
ELSE
BEGIN
    PRINT '⚠️ KpiTypes table already exists'
END

-- Create ScheduledJobs table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'monitoring.ScheduledJobs') AND type in (N'U'))
BEGIN
    CREATE TABLE monitoring.ScheduledJobs (
        JobId NVARCHAR(100) NOT NULL,
        KpiId INT NOT NULL,
        JobName NVARCHAR(255) NOT NULL,
        JobGroup NVARCHAR(255) NOT NULL DEFAULT 'KPI_JOBS',
        TriggerName NVARCHAR(255) NOT NULL,
        TriggerGroup NVARCHAR(255) NOT NULL DEFAULT 'KPI_TRIGGERS',
        CronExpression NVARCHAR(255) NULL,
        IntervalMinutes INT NULL,
        StartTime DATETIME2 NULL,
        EndTime DATETIME2 NULL,
        NextFireTime DATETIME2 NULL,
        PreviousFireTime DATETIME2 NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_ScheduledJobs PRIMARY KEY (JobId),
        CONSTRAINT FK_ScheduledJobs_KPIs_KpiId FOREIGN KEY (KpiId) REFERENCES monitoring.KPIs(KpiId) ON DELETE CASCADE
    );
    
    -- Create indexes
    CREATE UNIQUE INDEX IX_ScheduledJobs_JobName_JobGroup ON monitoring.ScheduledJobs (JobName, JobGroup);
    CREATE INDEX IX_ScheduledJobs_KpiId ON monitoring.ScheduledJobs (KpiId) INCLUDE (IsActive);
    CREATE INDEX IX_ScheduledJobs_NextFireTime ON monitoring.ScheduledJobs (NextFireTime) WHERE IsActive = 1;
    CREATE UNIQUE INDEX IX_ScheduledJobs_TriggerName_TriggerGroup ON monitoring.ScheduledJobs (TriggerName, TriggerGroup);
    
    PRINT '✅ Created ScheduledJobs table with indexes and foreign key'
END
ELSE
BEGIN
    PRINT '⚠️ ScheduledJobs table already exists'
END

-- Create Users table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.Users') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.Users (
        UserId NVARCHAR(50) NOT NULL,
        Username NVARCHAR(100) NOT NULL,
        Email NVARCHAR(255) NOT NULL,
        DisplayName NVARCHAR(255) NOT NULL,
        FirstName NVARCHAR(100) NULL,
        LastName NVARCHAR(100) NULL,
        Department NVARCHAR(100) NULL,
        Title NVARCHAR(100) NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        PasswordSalt NVARCHAR(255) NULL,
        IsActive BIT NOT NULL,
        EmailConfirmed BIT NOT NULL,
        TwoFactorEnabled BIT NOT NULL,
        FailedLoginAttempts INT NOT NULL,
        LockoutEnd DATETIME2 NULL,
        LastLogin DATETIME2 NULL,
        LastPasswordChange DATETIME2 NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NULL,
        ModifiedBy NVARCHAR(100) NULL,
        CONSTRAINT PK_Users PRIMARY KEY (UserId)
    );
    
    -- Create indexes
    CREATE UNIQUE INDEX IX_Users_Email ON auth.Users (Email);
    CREATE INDEX IX_Users_IsActive ON auth.Users (IsActive);
    CREATE UNIQUE INDEX IX_Users_Username ON auth.Users (Username);
    
    PRINT '✅ Created Users table with indexes'
END
ELSE
BEGIN
    PRINT '⚠️ Users table already exists'
END

-- Create Roles table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.Roles') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.Roles (
        RoleId NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NOT NULL DEFAULT '',
        IsSystemRole BIT NOT NULL,
        IsActive BIT NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CreatedBy NVARCHAR(100) NULL,
        ModifiedBy NVARCHAR(100) NULL,
        CONSTRAINT PK_Roles PRIMARY KEY (RoleId)
    );
    
    -- Create indexes
    CREATE INDEX IX_Roles_IsActive ON auth.Roles (IsActive);
    CREATE UNIQUE INDEX IX_Roles_Name ON auth.Roles (Name);
    
    PRINT '✅ Created Roles table with indexes'
END
ELSE
BEGIN
    PRINT '⚠️ Roles table already exists'
END

-- Create Permissions table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.Permissions') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.Permissions (
        PermissionId NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NOT NULL DEFAULT '',
        Resource NVARCHAR(100) NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        IsSystemPermission BIT NOT NULL,
        IsActive BIT NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_Permissions PRIMARY KEY (PermissionId)
    );
    
    -- Create indexes
    CREATE INDEX IX_Permissions_IsActive ON auth.Permissions (IsActive);
    CREATE UNIQUE INDEX IX_Permissions_Name ON auth.Permissions (Name);
    CREATE INDEX IX_Permissions_Resource_Action ON auth.Permissions (Resource, Action);
    
    PRINT '✅ Created Permissions table with indexes'
END
ELSE
BEGIN
    PRINT '⚠️ Permissions table already exists'
END

-- Create UserRoles table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.UserRoles') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.UserRoles (
        UserId NVARCHAR(50) NOT NULL,
        RoleId NVARCHAR(50) NOT NULL,
        AssignedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        AssignedBy NVARCHAR(100) NULL,
        CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_UserRoles_Users_UserId FOREIGN KEY (UserId) REFERENCES auth.Users(UserId) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES auth.Roles(RoleId) ON DELETE CASCADE
    );
    
    -- Create indexes
    CREATE INDEX IX_UserRoles_RoleId ON auth.UserRoles (RoleId);
    CREATE INDEX IX_UserRoles_UserId ON auth.UserRoles (UserId);
    
    PRINT '✅ Created UserRoles table with indexes and foreign keys'
END
ELSE
BEGIN
    PRINT '⚠️ UserRoles table already exists'
END

-- Create RolePermissions table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.RolePermissions') AND type in (N'U'))
BEGIN
    CREATE TABLE auth.RolePermissions (
        RoleId NVARCHAR(50) NOT NULL,
        PermissionId NVARCHAR(50) NOT NULL,
        AssignedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        AssignedBy NVARCHAR(100) NULL,
        CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
        CONSTRAINT FK_RolePermissions_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES auth.Roles(RoleId) ON DELETE CASCADE,
        CONSTRAINT FK_RolePermissions_Permissions_PermissionId FOREIGN KEY (PermissionId) REFERENCES auth.Permissions(PermissionId) ON DELETE CASCADE
    );
    
    -- Create indexes
    CREATE INDEX IX_RolePermissions_PermissionId ON auth.RolePermissions (PermissionId);
    CREATE INDEX IX_RolePermissions_RoleId ON auth.RolePermissions (RoleId);
    
    PRINT '✅ Created RolePermissions table with indexes and foreign keys'
END
ELSE
BEGIN
    PRINT '⚠️ RolePermissions table already exists'
END

PRINT ''
PRINT '=== Phase 2 Complete: Tables created ==='
PRINT 'Run the next script (08_AddKpiExecutionTracking_Part3.sql) to add remaining tables and seed data'
GO
