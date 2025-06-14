-- Create Admin User with Correct Password Hash Format
-- This script creates an admin user with PBKDF2 password hash that matches SecurityService expectations
USE [PopAI]
GO

PRINT '=== Creating Admin User with Correct Password Hash ==='

-- First, delete existing admin user if it exists (to avoid conflicts)
IF EXISTS (SELECT 1 FROM auth.Users WHERE Username = 'admin')
BEGIN
    DELETE FROM auth.UserRoles WHERE UserId IN (SELECT UserId FROM auth.Users WHERE Username = 'admin')
    DELETE FROM auth.Users WHERE Username = 'admin'
    PRINT '✅ Removed existing admin user'
END

-- Create admin user with PBKDF2 hash format (salt:hash)
-- Password: Admin123!
-- Using a simple known salt and hash for testing
DECLARE @AdminUserId NVARCHAR(50) = NEWID()
DECLARE @Salt NVARCHAR(255) = 'dGVzdHNhbHQxMjM0NTY3ODkwYWJjZGVmZ2hpams='  -- Base64 encoded test salt
DECLARE @Hash NVARCHAR(255) = 'dGVzdHNhbHQxMjM0NTY3ODkwYWJjZGVmZ2hpams=:K8yU4/UX8b+1t+9Hl/0yHREkHXBoIXuaSMF0pOF/BaY='  -- salt:hash format

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
    CreatedDate, 
    ModifiedDate,
    CreatedBy
)
VALUES (
    @AdminUserId,
    'admin',
    'admin@monitoringgrid.com',
    'System Administrator',
    'System',
    'Administrator',
    @Hash,
    1,
    1,
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    'SYSTEM'
)

-- Assign admin role to admin user
INSERT INTO auth.UserRoles (UserId, RoleId, AssignedBy, AssignedDate)
VALUES (@AdminUserId, 'role-admin', 'SYSTEM', SYSUTCDATETIME())

PRINT '✅ Admin user created successfully'
PRINT 'Username: admin'
PRINT 'Password: Admin123!'
PRINT 'Email: admin@monitoringgrid.com'
PRINT ''
PRINT '⚠️ WARNING: Change the default admin password immediately in production!'

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
WHERE Username = 'admin'

PRINT '=== Admin User Creation Complete ==='
