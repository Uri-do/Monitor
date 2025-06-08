-- Create a test user for MonitoringGrid authentication
USE PopAI;

-- First, let's check if the user tables exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('monitoring'))
BEGIN
    PRINT 'Users table does not exist. Please run the database schema creation scripts first.'
    RETURN;
END

-- Create test user
DECLARE @UserId NVARCHAR(50) = NEWID();
DECLARE @RoleId NVARCHAR(50) = NEWID();
DECLARE @PermissionId NVARCHAR(50) = NEWID();

-- Insert Admin role if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM monitoring.Roles WHERE Name = 'Admin')
BEGIN
    INSERT INTO monitoring.Roles (RoleId, Name, Description, IsSystemRole, IsActive, CreatedDate, ModifiedDate)
    VALUES (@RoleId, 'Admin', 'System Administrator with full access', 1, 1, GETUTCDATE(), GETUTCDATE());
    
    PRINT 'Admin role created with ID: ' + @RoleId;
END
ELSE
BEGIN
    SELECT @RoleId = RoleId FROM monitoring.Roles WHERE Name = 'Admin';
    PRINT 'Using existing Admin role with ID: ' + @RoleId;
END

-- Insert System:Admin permission if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM monitoring.Permissions WHERE Name = 'System:Admin')
BEGIN
    INSERT INTO monitoring.Permissions (PermissionId, Name, Description, Resource, Action, IsSystemPermission)
    VALUES (@PermissionId, 'System:Admin', 'Full system administration access', 'System', 'Admin', 1);
    
    PRINT 'System:Admin permission created with ID: ' + @PermissionId;
END
ELSE
BEGIN
    SELECT @PermissionId = PermissionId FROM monitoring.Permissions WHERE Name = 'System:Admin';
    PRINT 'Using existing System:Admin permission with ID: ' + @PermissionId;
END

-- Link role to permission if not already linked
IF NOT EXISTS (SELECT 1 FROM monitoring.RolePermissions WHERE RoleId = @RoleId AND PermissionId = @PermissionId)
BEGIN
    INSERT INTO monitoring.RolePermissions (RoleId, PermissionId)
    VALUES (@RoleId, @PermissionId);
    
    PRINT 'Admin role linked to System:Admin permission';
END

-- Create test user if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM monitoring.Users WHERE Username = 'admin' OR Email = 'admin@monitoringgrid.com')
BEGIN
    INSERT INTO monitoring.Users (
        UserId, 
        Username, 
        Email, 
        DisplayName, 
        FirstName, 
        LastName, 
        Department, 
        Title, 
        IsActive, 
        EmailConfirmed, 
        CreatedDate, 
        ModifiedDate
    )
    VALUES (
        @UserId,
        'admin',
        'admin@monitoringgrid.com',
        'System Administrator',
        'Admin',
        'User',
        'IT',
        'System Administrator',
        1,
        1,
        GETUTCDATE(),
        GETUTCDATE()
    );
    
    PRINT 'Test user created with ID: ' + @UserId;
    
    -- Create password for the user (password: "Admin123!")
    -- This is a hashed version of "Admin123!" - you'll need to update this with proper hashing
    INSERT INTO monitoring.UserPasswords (
        UserId,
        PasswordHash,
        IsActive,
        CreatedAt,
        ExpiresAt
    )
    VALUES (
        @UserId,
        '$2a$11$rGKqDNO.d1s8GzM5cPjOUeYlYzqF5cPjOUeYlYzqF5cPjOUeYlYzqF', -- This is a placeholder - needs proper bcrypt hash
        1,
        GETUTCDATE(),
        DATEADD(DAY, 90, GETUTCDATE())
    );
    
    -- Assign Admin role to user
    INSERT INTO monitoring.UserRoles (UserId, RoleId)
    VALUES (@UserId, @RoleId);
    
    PRINT 'User assigned to Admin role';
    PRINT '';
    PRINT 'Test user created successfully!';
    PRINT 'Username: admin';
    PRINT 'Email: admin@monitoringgrid.com';
    PRINT 'Password: Admin123!';
    PRINT '';
    PRINT 'Note: You may need to update the password hash with proper bcrypt encryption.';
END
ELSE
BEGIN
    PRINT 'Test user already exists';
END

-- Display current users
PRINT '';
PRINT 'Current users in the system:';
SELECT 
    u.Username,
    u.Email,
    u.DisplayName,
    u.IsActive,
    u.EmailConfirmed,
    STRING_AGG(r.Name, ', ') as Roles
FROM monitoring.Users u
LEFT JOIN monitoring.UserRoles ur ON u.UserId = ur.UserId
LEFT JOIN monitoring.Roles r ON ur.RoleId = r.RoleId
GROUP BY u.Username, u.Email, u.DisplayName, u.IsActive, u.EmailConfirmed;
