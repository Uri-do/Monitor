-- Update Admin User with Correct PBKDF2 Hash
USE [PopAI]
GO

PRINT '=== Updating Admin User Password Hash ==='

UPDATE auth.Users 
SET 
    PasswordHash = 'A0Ysoilo3DZrUMs018sk9KK0n3hnJPFGeaQO3Zpe4x0=:5OgE/8fT34r5BrAflmeSNwHDi0xGnY0iRDBzB5Es0rw=',
    PasswordSalt = 'A0Ysoilo3DZrUMs018sk9KK0n3hnJPFGeaQO3Zpe4x0=',
    ModifiedDate = SYSUTCDATETIME(),
    ModifiedBy = 'SYSTEM_PBKDF2_FIX'
WHERE Username = 'admin'

PRINT '✅ Admin user password hash updated with PBKDF2 format'
PRINT 'Username: admin'
PRINT 'Password: Admin123!'

-- Verify the update
SELECT 
    Username,
    Email,
    DisplayName,
    IsActive,
    LEFT(PasswordHash, 50) + '...' AS PasswordHashPreview,
    ModifiedDate,
    ModifiedBy
FROM auth.Users 
WHERE Username = 'admin'

PRINT '=== Update Complete ==='
