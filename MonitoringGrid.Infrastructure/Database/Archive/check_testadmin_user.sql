-- Check testadmin user details
USE [PopAI]
GO

PRINT '=== Checking testadmin User Details ==='

-- Check the testadmin user
SELECT 
    UserId,
    Username,
    Email,
    DisplayName,
    PasswordHash,
    IsActive,
    EmailConfirmed,
    CreatedDate
FROM auth.Users 
WHERE Username = 'testadmin'

PRINT ''
PRINT '=== Password Hash Analysis ==='

-- Analyze the password hash format
DECLARE @PasswordHash NVARCHAR(255)
SELECT @PasswordHash = PasswordHash FROM auth.Users WHERE Username = 'testadmin'

PRINT 'Password Hash: ' + ISNULL(@PasswordHash, 'NULL')
PRINT 'Hash Length: ' + CAST(LEN(@PasswordHash) AS NVARCHAR(10))
PRINT 'Contains Colon: ' + CASE WHEN CHARINDEX(':', @PasswordHash) > 0 THEN 'YES' ELSE 'NO' END

IF CHARINDEX(':', @PasswordHash) > 0
BEGIN
    DECLARE @ColonPos INT = CHARINDEX(':', @PasswordHash)
    DECLARE @Salt NVARCHAR(255) = LEFT(@PasswordHash, @ColonPos - 1)
    DECLARE @Hash NVARCHAR(255) = SUBSTRING(@PasswordHash, @ColonPos + 1, LEN(@PasswordHash))
    
    PRINT 'Salt Part: ' + @Salt
    PRINT 'Hash Part: ' + @Hash
    PRINT 'Salt Length: ' + CAST(LEN(@Salt) AS NVARCHAR(10))
    PRINT 'Hash Length: ' + CAST(LEN(@Hash) AS NVARCHAR(10))
END
ELSE
BEGIN
    PRINT 'Hash format does not contain colon - not PBKDF2 format!'
END

PRINT ''
PRINT '=== User Role Assignments ==='

-- Check role assignments
SELECT 
    ur.UserId,
    u.Username,
    ur.RoleId,
    r.Name as RoleName,
    ur.AssignedDate
FROM auth.UserRoles ur
LEFT JOIN auth.Users u ON ur.UserId = u.UserId
LEFT JOIN auth.Roles r ON ur.RoleId = r.RoleId
WHERE u.Username = 'testadmin'

PRINT ''
PRINT '=== Analysis Complete ==='
