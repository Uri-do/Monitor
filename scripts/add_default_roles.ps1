# PowerShell script to add default roles and permissions
$connectionString = "Data Source=192.168.166.11,1433;Initial Catalog=PopAI;User ID=conexusadmin;Password=PWUi^g6~lxD;MultipleActiveResultSets=true;TrustServerCertificate=true"

$sql = @"
-- Insert default permissions
MERGE auth.Permissions AS target
USING (VALUES 
    ('perm-system-admin', 'System:Admin', 'Full system administration access', 'System', 'Admin', 1),
    ('perm-user-read', 'User:Read', 'Read user information', 'User', 'Read', 1),
    ('perm-user-write', 'User:Write', 'Create and update users', 'User', 'Write', 1),
    ('perm-user-delete', 'User:Delete', 'Delete users', 'User', 'Delete', 1),
    ('perm-role-read', 'Role:Read', 'Read role information', 'Role', 'Read', 1),
    ('perm-role-write', 'Role:Write', 'Create and update roles', 'Role', 'Write', 1),
    ('perm-kpi-read', 'KPI:Read', 'Read KPI information', 'KPI', 'Read', 1),
    ('perm-kpi-write', 'KPI:Write', 'Create and update KPIs', 'KPI', 'Write', 1),
    ('perm-contact-read', 'Contact:Read', 'Read contact information', 'Contact', 'Read', 1),
    ('perm-contact-write', 'Contact:Write', 'Create and update contacts', 'Contact', 'Write', 1),
    ('perm-alert-read', 'Alert:Read', 'Read alert information', 'Alert', 'Read', 1),
    ('perm-alert-write', 'Alert:Write', 'Create and update alerts', 'Alert', 'Write', 1),
    ('perm-alert-resolve', 'Alert:Resolve', 'Resolve alerts', 'Alert', 'Resolve', 1),
    ('perm-dashboard-read', 'Dashboard:Read', 'Read dashboard information', 'Dashboard', 'Read', 1),
    ('perm-config-read', 'Config:Read', 'Read configuration', 'Config', 'Read', 1)
) AS source (PermissionId, Name, Description, Resource, Action, IsSystemPermission)
ON target.PermissionId = source.PermissionId
WHEN NOT MATCHED THEN
    INSERT (PermissionId, Name, Description, Resource, Action, IsSystemPermission)
    VALUES (source.PermissionId, source.Name, source.Description, source.Resource, source.Action, source.IsSystemPermission);

-- Assign all permissions to admin role
INSERT INTO auth.RolePermissions (RoleId, PermissionId, AssignedBy)
SELECT 'role-admin', PermissionId, 'SYSTEM'
FROM auth.Permissions
WHERE NOT EXISTS (
    SELECT 1 FROM auth.RolePermissions 
    WHERE RoleId = 'role-admin' AND PermissionId = auth.Permissions.PermissionId
);

-- Assign permissions to Manager role
MERGE auth.RolePermissions AS target
USING (VALUES 
    ('role-manager', 'perm-kpi-read', 'SYSTEM'),
    ('role-manager', 'perm-kpi-write', 'SYSTEM'),
    ('role-manager', 'perm-contact-read', 'SYSTEM'),
    ('role-manager', 'perm-contact-write', 'SYSTEM'),
    ('role-manager', 'perm-alert-read', 'SYSTEM'),
    ('role-manager', 'perm-alert-write', 'SYSTEM'),
    ('role-manager', 'perm-alert-resolve', 'SYSTEM'),
    ('role-manager', 'perm-dashboard-read', 'SYSTEM'),
    ('role-manager', 'perm-config-read', 'SYSTEM')
) AS source (RoleId, PermissionId, AssignedBy)
ON target.RoleId = source.RoleId AND target.PermissionId = source.PermissionId
WHEN NOT MATCHED THEN
    INSERT (RoleId, PermissionId, AssignedBy)
    VALUES (source.RoleId, source.PermissionId, source.AssignedBy);

-- Assign permissions to Viewer role
MERGE auth.RolePermissions AS target
USING (VALUES 
    ('role-viewer', 'perm-kpi-read', 'SYSTEM'),
    ('role-viewer', 'perm-contact-read', 'SYSTEM'),
    ('role-viewer', 'perm-alert-read', 'SYSTEM'),
    ('role-viewer', 'perm-dashboard-read', 'SYSTEM')
) AS source (RoleId, PermissionId, AssignedBy)
ON target.RoleId = source.RoleId AND target.PermissionId = source.PermissionId
WHEN NOT MATCHED THEN
    INSERT (RoleId, PermissionId, AssignedBy)
    VALUES (source.RoleId, source.PermissionId, source.AssignedBy);

PRINT 'Default roles and permissions setup completed successfully!'
"@

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "Connected to database successfully"
    
    $command = New-Object System.Data.SqlClient.SqlCommand($sql, $connection)
    $command.CommandTimeout = 120
    
    $result = $command.ExecuteNonQuery()
    Write-Host "Roles and permissions setup completed. Rows affected: $result"
    
    $connection.Close()
    Write-Host "Default roles and permissions setup completed successfully!"
}
catch {
    Write-Error "Error: $($_.Exception.Message)"
    if ($connection.State -eq 'Open') {
        $connection.Close()
    }
}
