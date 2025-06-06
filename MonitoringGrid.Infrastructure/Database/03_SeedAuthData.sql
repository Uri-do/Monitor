-- Seed Authentication Data
-- This script creates default roles, permissions, and admin user

USE [PopAI]
GO

-- Insert default permissions
IF NOT EXISTS (SELECT 1 FROM auth.Permissions WHERE PermissionId = 'perm-kpi-read')
BEGIN
    INSERT INTO auth.Permissions (PermissionId, Name, Description, Resource, Action, IsSystemPermission)
    VALUES 
        ('perm-kpi-read', 'Read KPIs', 'View KPI configurations and data', 'KPI', 'Read', 1),
        ('perm-kpi-write', 'Write KPIs', 'Create and modify KPI configurations', 'KPI', 'Write', 1),
        ('perm-kpi-delete', 'Delete KPIs', 'Delete KPI configurations', 'KPI', 'Delete', 1),
        ('perm-contact-read', 'Read Contacts', 'View contact information', 'Contact', 'Read', 1),
        ('perm-contact-write', 'Write Contacts', 'Create and modify contacts', 'Contact', 'Write', 1),
        ('perm-contact-delete', 'Delete Contacts', 'Delete contacts', 'Contact', 'Delete', 1),
        ('perm-alert-read', 'Read Alerts', 'View alert logs and history', 'Alert', 'Read', 1),
        ('perm-alert-write', 'Write Alerts', 'Manage alert configurations', 'Alert', 'Write', 1),
        ('perm-alert-resolve', 'Resolve Alerts', 'Mark alerts as resolved', 'Alert', 'Resolve', 1),
        ('perm-user-read', 'Read Users', 'View user accounts', 'User', 'Read', 1),
        ('perm-user-write', 'Write Users', 'Create and modify user accounts', 'User', 'Write', 1),
        ('perm-user-delete', 'Delete Users', 'Delete user accounts', 'User', 'Delete', 1),
        ('perm-role-read', 'Read Roles', 'View roles and permissions', 'Role', 'Read', 1),
        ('perm-role-write', 'Write Roles', 'Create and modify roles', 'Role', 'Write', 1),
        ('perm-role-delete', 'Delete Roles', 'Delete roles', 'Role', 'Delete', 1),
        ('perm-system-admin', 'System Administration', 'Full system administration access', 'System', 'Admin', 1),
        ('perm-dashboard-read', 'Read Dashboard', 'View dashboard and reports', 'Dashboard', 'Read', 1),
        ('perm-config-read', 'Read Configuration', 'View system configuration', 'Config', 'Read', 1),
        ('perm-config-write', 'Write Configuration', 'Modify system configuration', 'Config', 'Write', 1)
    
    PRINT 'Default permissions inserted'
END
ELSE
BEGIN
    PRINT 'Default permissions already exist'
END
GO

-- Insert default roles
IF NOT EXISTS (SELECT 1 FROM auth.Roles WHERE RoleId = 'role-admin')
BEGIN
    INSERT INTO auth.Roles (RoleId, Name, Description, IsSystemRole)
    VALUES 
        ('role-admin', 'Administrator', 'Full system access with all permissions', 1),
        ('role-manager', 'Manager', 'Management access to KPIs, contacts, and alerts', 1),
        ('role-operator', 'Operator', 'Operational access to view and manage alerts', 1),
        ('role-viewer', 'Viewer', 'Read-only access to dashboards and reports', 1)
    
    PRINT 'Default roles inserted'
END
ELSE
BEGIN
    PRINT 'Default roles already exist'
END
GO

-- Assign permissions to Administrator role
IF NOT EXISTS (SELECT 1 FROM auth.RolePermissions WHERE RoleId = 'role-admin')
BEGIN
    INSERT INTO auth.RolePermissions (RoleId, PermissionId, AssignedBy)
    SELECT 'role-admin', PermissionId, 'SYSTEM'
    FROM auth.Permissions
    WHERE IsSystemPermission = 1
    
    PRINT 'Administrator role permissions assigned'
END
ELSE
BEGIN
    PRINT 'Administrator role permissions already assigned'
END
GO

-- Assign permissions to Manager role
IF NOT EXISTS (SELECT 1 FROM auth.RolePermissions WHERE RoleId = 'role-manager')
BEGIN
    INSERT INTO auth.RolePermissions (RoleId, PermissionId, AssignedBy)
    VALUES 
        ('role-manager', 'perm-kpi-read', 'SYSTEM'),
        ('role-manager', 'perm-kpi-write', 'SYSTEM'),
        ('role-manager', 'perm-contact-read', 'SYSTEM'),
        ('role-manager', 'perm-contact-write', 'SYSTEM'),
        ('role-manager', 'perm-alert-read', 'SYSTEM'),
        ('role-manager', 'perm-alert-write', 'SYSTEM'),
        ('role-manager', 'perm-alert-resolve', 'SYSTEM'),
        ('role-manager', 'perm-dashboard-read', 'SYSTEM'),
        ('role-manager', 'perm-config-read', 'SYSTEM')
    
    PRINT 'Manager role permissions assigned'
END
ELSE
BEGIN
    PRINT 'Manager role permissions already assigned'
END
GO

-- Assign permissions to Operator role
IF NOT EXISTS (SELECT 1 FROM auth.RolePermissions WHERE RoleId = 'role-operator')
BEGIN
    INSERT INTO auth.RolePermissions (RoleId, PermissionId, AssignedBy)
    VALUES 
        ('role-operator', 'perm-kpi-read', 'SYSTEM'),
        ('role-operator', 'perm-contact-read', 'SYSTEM'),
        ('role-operator', 'perm-alert-read', 'SYSTEM'),
        ('role-operator', 'perm-alert-resolve', 'SYSTEM'),
        ('role-operator', 'perm-dashboard-read', 'SYSTEM')
    
    PRINT 'Operator role permissions assigned'
END
ELSE
BEGIN
    PRINT 'Operator role permissions already assigned'
END
GO

-- Assign permissions to Viewer role
IF NOT EXISTS (SELECT 1 FROM auth.RolePermissions WHERE RoleId = 'role-viewer')
BEGIN
    INSERT INTO auth.RolePermissions (RoleId, PermissionId, AssignedBy)
    VALUES 
        ('role-viewer', 'perm-kpi-read', 'SYSTEM'),
        ('role-viewer', 'perm-contact-read', 'SYSTEM'),
        ('role-viewer', 'perm-alert-read', 'SYSTEM'),
        ('role-viewer', 'perm-dashboard-read', 'SYSTEM')
    
    PRINT 'Viewer role permissions assigned'
END
ELSE
BEGIN
    PRINT 'Viewer role permissions already assigned'
END
GO

-- Create default admin user (password: Admin123!)
-- Note: In production, this should be changed immediately
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

-- Create indexes for performance
CREATE NONCLUSTERED INDEX IX_Users_Username ON auth.Users (Username) INCLUDE (IsActive, Email)
GO
CREATE NONCLUSTERED INDEX IX_Users_Email ON auth.Users (Email) INCLUDE (IsActive, Username)
GO
CREATE NONCLUSTERED INDEX IX_RefreshTokens_Token ON auth.RefreshTokens (Token) INCLUDE (IsActive, ExpiresAt)
GO
CREATE NONCLUSTERED INDEX IX_RefreshTokens_UserId ON auth.RefreshTokens (UserId) INCLUDE (IsActive, ExpiresAt)
GO

PRINT 'Authentication schema setup completed successfully!'
PRINT 'Default admin credentials: admin / Admin123!'
PRINT 'Please change the default password immediately!'
GO
