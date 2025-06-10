-- Check Authentication Tables and Data
USE [PopAI]
GO

PRINT '=== Checking Authentication Tables and Data ==='

-- Check if auth schema exists
IF EXISTS (SELECT * FROM sys.schemas WHERE name = 'auth')
BEGIN
    PRINT '✅ Auth schema exists'
END
ELSE
BEGIN
    PRINT '❌ Auth schema does not exist'
END

-- Check if Users table exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.Users') AND type in (N'U'))
BEGIN
    PRINT '✅ auth.Users table exists'
    
    -- Show table structure
    PRINT ''
    PRINT '=== Users Table Structure ==='
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        CHARACTER_MAXIMUM_LENGTH,
        IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'auth' AND TABLE_NAME = 'Users'
    ORDER BY ORDINAL_POSITION
    
    -- Show existing users
    PRINT ''
    PRINT '=== Existing Users ==='
    SELECT 
        UserId,
        Username,
        Email,
        DisplayName,
        IsActive,
        EmailConfirmed,
        PasswordHash,
        CreatedDate
    FROM auth.Users
    
    PRINT ''
    PRINT '=== User Count ==='
    SELECT COUNT(*) as UserCount FROM auth.Users
    
END
ELSE
BEGIN
    PRINT '❌ auth.Users table does not exist'
END

-- Check if Roles table exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.Roles') AND type in (N'U'))
BEGIN
    PRINT '✅ auth.Roles table exists'
    
    -- Show existing roles
    PRINT ''
    PRINT '=== Existing Roles ==='
    SELECT 
        RoleId,
        Name,
        Description,
        IsSystemRole,
        IsActive
    FROM auth.Roles
    
END
ELSE
BEGIN
    PRINT '❌ auth.Roles table does not exist'
END

-- Check if UserRoles table exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.UserRoles') AND type in (N'U'))
BEGIN
    PRINT '✅ auth.UserRoles table exists'
    
    -- Show user role assignments
    PRINT ''
    PRINT '=== User Role Assignments ==='
    SELECT 
        ur.UserId,
        u.Username,
        ur.RoleId,
        r.Name as RoleName
    FROM auth.UserRoles ur
    LEFT JOIN auth.Users u ON ur.UserId = u.UserId
    LEFT JOIN auth.Roles r ON ur.RoleId = r.RoleId
    
END
ELSE
BEGIN
    PRINT '❌ auth.UserRoles table does not exist'
END

PRINT ''
PRINT '=== Authentication Tables Check Complete ==='
