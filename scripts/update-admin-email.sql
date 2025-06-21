-- Update the existing admin user's email to match what's expected

PRINT '=== Current admin users ==='
SELECT UserId, Username, Email, DisplayName, IsActive
FROM auth.Users 
WHERE Username LIKE '%admin%' OR Email LIKE '%admin%'

PRINT ''
PRINT '=== Updating admin user email ==='

-- Update the admin user's email to match what the frontend expects
UPDATE auth.Users 
SET 
    Email = 'admin@monitoringgrid.com',
    ModifiedDate = SYSUTCDATETIME(),
    ModifiedBy = 'EmailUpdate'
WHERE Username = 'admin' AND Email = 'admin@example.com'

IF @@ROWCOUNT > 0
    PRINT 'Admin user email updated to admin@monitoringgrid.com'
ELSE
    PRINT 'No admin user found to update'

PRINT ''
PRINT '=== Verification ==='
SELECT 
    u.UserId,
    u.Username,
    u.Email,
    u.DisplayName,
    u.IsActive,
    r.Name as RoleName,
    ur.AssignedDate
FROM auth.Users u
LEFT JOIN auth.UserRoles ur ON u.UserId = ur.UserId
LEFT JOIN auth.Roles r ON ur.RoleId = r.RoleId
WHERE u.Email = 'admin@monitoringgrid.com'
ORDER BY r.Name

PRINT ''
PRINT '=== All admin users ==='
SELECT UserId, Username, Email, DisplayName, IsActive
FROM auth.Users 
WHERE Username LIKE '%admin%' OR Email LIKE '%admin%'
ORDER BY Email
