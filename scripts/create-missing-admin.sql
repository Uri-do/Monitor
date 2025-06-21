-- Create the missing admin@monitoringgrid.com user and assign Admin role

DECLARE @UserId NVARCHAR(50) = NEWID()
DECLARE @RoleId NVARCHAR(50)

-- Get the Admin role ID
SELECT @RoleId = RoleId FROM auth.Roles WHERE Name = 'Admin'

PRINT '=== Creating admin@monitoringgrid.com user ==='

-- Create the user (using a simple password hash for now)
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
    FailedLoginAttempts,
    CreatedDate, 
    ModifiedDate, 
    CreatedBy
)
VALUES (
    @UserId,
    'admin',
    'admin@monitoringgrid.com',
    'System Administrator',
    'System',
    'Administrator',
    'temp_hash_needs_reset', -- This will need to be updated with proper hash
    1, -- IsActive
    1, -- EmailConfirmed
    0, -- TwoFactorEnabled
    0, -- FailedLoginAttempts
    SYSUTCDATETIME(),
    SYSUTCDATETIME(),
    'System'
)

PRINT 'User created with ID: ' + @UserId

-- Assign Admin role
INSERT INTO auth.UserRoles (UserId, RoleId, AssignedDate, AssignedBy)
VALUES (@UserId, @RoleId, SYSUTCDATETIME(), 'System')

PRINT 'Admin role assigned'

-- Verify the creation
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
INNER JOIN auth.UserRoles ur ON u.UserId = ur.UserId
INNER JOIN auth.Roles r ON ur.RoleId = r.RoleId
WHERE u.Email = 'admin@monitoringgrid.com'

PRINT ''
PRINT '=== SUCCESS ==='
PRINT 'User admin@monitoringgrid.com created and assigned Admin role'
PRINT 'Note: Password hash needs to be updated with proper value'
