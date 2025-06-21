-- Check current user roles and system state
-- Run this to see what users and roles exist

PRINT '=== USERS ==='
SELECT 
    UserId,
    Username,
    Email,
    DisplayName,
    IsActive,
    EmailConfirmed,
    CreatedDate
FROM auth.Users
ORDER BY CreatedDate DESC

PRINT ''
PRINT '=== ROLES ==='
SELECT 
    RoleId,
    Name,
    Description,
    IsSystemRole,
    IsActive,
    CreatedDate
FROM auth.Roles
ORDER BY Name

PRINT ''
PRINT '=== USER ROLES ==='
SELECT 
    u.Username,
    u.Email,
    r.Name as RoleName,
    ur.AssignedDate
FROM auth.Users u
INNER JOIN auth.UserRoles ur ON u.UserId = ur.UserId
INNER JOIN auth.Roles r ON ur.RoleId = r.RoleId
ORDER BY u.Username, r.Name

PRINT ''
PRINT '=== ADMIN USER CHECK ==='
SELECT 
    u.Username,
    u.Email,
    CASE WHEN ur.RoleId IS NOT NULL THEN 'YES' ELSE 'NO' END as HasAdminRole
FROM auth.Users u
LEFT JOIN auth.UserRoles ur ON u.UserId = ur.UserId
LEFT JOIN auth.Roles r ON ur.RoleId = r.RoleId AND r.Name = 'Admin'
WHERE u.Email = 'admin@monitoringgrid.com'

-- If no Admin role exists, create it
IF NOT EXISTS (SELECT 1 FROM auth.Roles WHERE Name = 'Admin')
BEGIN
    PRINT ''
    PRINT '=== CREATING ADMIN ROLE ==='
    INSERT INTO auth.Roles (RoleId, Name, Description, IsSystemRole, IsActive, CreatedDate, ModifiedDate, CreatedBy)
    VALUES (NEWID(), 'Admin', 'System Administrator with full access', 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 'System')
    PRINT 'Admin role created'
END

-- If admin user exists but doesn't have Admin role, assign it
IF EXISTS (SELECT 1 FROM auth.Users WHERE Email = 'admin@monitoringgrid.com')
   AND NOT EXISTS (
       SELECT 1 FROM auth.Users u
       INNER JOIN auth.UserRoles ur ON u.UserId = ur.UserId
       INNER JOIN auth.Roles r ON ur.RoleId = r.RoleId
       WHERE u.Email = 'admin@monitoringgrid.com' AND r.Name = 'Admin'
   )
BEGIN
    PRINT ''
    PRINT '=== ASSIGNING ADMIN ROLE ==='
    INSERT INTO auth.UserRoles (UserId, RoleId, AssignedDate, AssignedBy)
    SELECT u.UserId, r.RoleId, SYSUTCDATETIME(), 'System'
    FROM auth.Users u, auth.Roles r
    WHERE u.Email = 'admin@monitoringgrid.com' AND r.Name = 'Admin'
    PRINT 'Admin role assigned to admin@monitoringgrid.com'
END

PRINT ''
PRINT '=== FINAL STATE ==='
SELECT 
    u.Username,
    u.Email,
    r.Name as RoleName,
    ur.AssignedDate
FROM auth.Users u
INNER JOIN auth.UserRoles ur ON u.UserId = ur.UserId
INNER JOIN auth.Roles r ON ur.RoleId = r.RoleId
WHERE u.Email = 'admin@monitoringgrid.com'
ORDER BY r.Name
