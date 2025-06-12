-- Create Simple Test User for Authentication Testing
USE [PopAI]
GO

PRINT '=== Creating Simple Test User ==='

-- Check if auth schema and tables exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'auth')
BEGIN
    PRINT '❌ Auth schema does not exist. Please run the authentication setup scripts first.'
    RETURN
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'auth.Users') AND type in (N'U'))
BEGIN
    PRINT '❌ auth.Users table does not exist. Please run the authentication setup scripts first.'
    RETURN
END

-- Delete existing test user if it exists
IF EXISTS (SELECT 1 FROM auth.Users WHERE Username = 'testuser')
BEGIN
    DELETE FROM auth.UserRoles WHERE UserId IN (SELECT UserId FROM auth.Users WHERE Username = 'testuser')
    DELETE FROM auth.Users WHERE Username = 'testuser'
    PRINT '✅ Removed existing test user'
END

-- Create test user with simple credentials
DECLARE @TestUserId NVARCHAR(50) = NEWID()

-- Password: "Test123!" 
-- Using a simple hash format that the SecurityService can handle
-- This is a placeholder - in production, use proper PBKDF2 hashing
DECLARE @PasswordHash NVARCHAR(255) = 'Test123!_SimpleHash_ForTesting'

INSERT INTO auth.Users (
    UserId, 
    Username, 
    Email, 
    DisplayName, 
    FirstName, 
    LastName, 
    PasswordHash, 
    IsActive, 
    EmailConfirmed, 
    TwoFactorEnabled,
    CreatedDate, 
    ModifiedDate,
    CreatedBy
)
VALUES (
    @TestUserId,
    'testuser',
    'test@monitoringgrid.com',
    'Test User',
    'Test',
    'User',
    @PasswordHash,
    1,
    1,
    0,
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    'SYSTEM'
)

-- Ensure Admin role exists
IF NOT EXISTS (SELECT 1 FROM auth.Roles WHERE RoleId = 'role-admin')
BEGIN
    INSERT INTO auth.Roles (RoleId, Name, Description, IsSystemRole, IsActive, CreatedDate, ModifiedDate)
    VALUES ('role-admin', 'Admin', 'System Administrator', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME())
    PRINT '✅ Admin role created'
END

-- Assign admin role to test user
INSERT INTO auth.UserRoles (UserId, RoleId, AssignedBy, AssignedDate)
VALUES (@TestUserId, 'role-admin', 'SYSTEM', SYSUTCDATETIME())

PRINT '✅ Test user created successfully'
PRINT 'Username: testuser'
PRINT 'Password: Test123!'
PRINT 'Email: test@monitoringgrid.com'
PRINT ''

-- Verify the user was created
SELECT 
    UserId,
    Username,
    Email,
    DisplayName,
    IsActive,
    EmailConfirmed,
    CreatedDate
FROM auth.Users 
WHERE Username = 'testuser'

-- Show user roles
SELECT 
    u.Username,
    r.Name as RoleName,
    ur.AssignedDate
FROM auth.Users u
JOIN auth.UserRoles ur ON u.UserId = ur.UserId
JOIN auth.Roles r ON ur.RoleId = r.RoleId
WHERE u.Username = 'testuser'

PRINT '=== Test User Creation Complete ==='
